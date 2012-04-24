//#define Sync
#define IOCPQueue

using System;
using System.Data.SqlClient;
using System.Threading;
using WellDunne.Concurrency;
using System.Data;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Data.Async;

namespace WellDunne.Asynchrony
{
    public sealed class AsyncContext
    {
        private readonly Action<Exception> _defaultExceptionHandler = (ex) => { Console.Error.WriteLine(ex.ToString()); };

        private int workerCount;
        private ManualResetEvent workDone;

        private int _sqlQueriesCompleted, _sqlQueriesErrored;
        private Action<Exception> _err;
        private Action _completed;

        private readonly Semaphore _throttleIO;
        private readonly int _ioCount;

        private static void _defaultCompleted() { }

        public AsyncContext(int ioCount)
        {
            _ioCount = ioCount;
            if (_ioCount > 0)
                _throttleIO = new Semaphore(_ioCount, _ioCount);
            else
                _throttleIO = null;

            _err = _defaultExceptionHandler;

            workDone = new ManualResetEvent(true);
            _completed = _defaultCompleted;
            workerCount = 0;
        }

        public void Reset()
        {
            workerCount = 0;
            _sqlQueriesCompleted = 0;
            _sqlQueriesErrored = 0;
        }

        public void SetExceptionHandler(Action<Exception> err)
        {
            _err = err ?? _defaultExceptionHandler;
        }

        public void SetCompleted(Action completed)
        {
            if (completed == null) _completed = _defaultCompleted;
            else _completed = completed;
        }

        /// <summary>
        /// Resets SqlQueriesCompleted to 0, atomically.
        /// </summary>
        public void ResetQueryCount()
        {
            Interlocked.Exchange(ref _sqlQueriesCompleted, 0);
            Interlocked.Exchange(ref _sqlQueriesErrored, 0);
        }

        public int SqlQueriesCompleted { get { return _sqlQueriesCompleted; } }
        public int SqlQueriesErrored { get { return _sqlQueriesErrored; } }
        public int WorkerCount { get { return workerCount; } }

        public void Done()
        {
            workDone.Set();

            // TODO(jsd): Which thread is ideal to call this on?
            _completed();
        }

        internal void ReleaseIOThrottle()
        {
            if (_ioCount > 0) _throttleIO.Release();
        }

        private void asyncOperationStart()
        {
            Interlocked.Increment(ref workerCount);
            workDone.Reset();
        }

        private void asyncOperationEnd()
        {
            if (Interlocked.Decrement(ref workerCount) == 0)
                Done();
        }

        public WaitHandle WaitHandle { get { return workDone; } }

        /// <summary>
        /// Blocks the current thread until all async operations are complete.
        /// </summary>
        public void Wait()
        {
            // TODO(jsd): Clean this up to lightly spin-wait and check for lack of progress.
            workDone.WaitOne();
        }

        private abstract class WorkerBase
        {
            public AsyncContext Context { get; private set; }
            public WaitHandle WaitHandle { get; private set; }
            private RegisteredWaitHandle _handle;
            private int _completed;

            public WorkerBase(AsyncContext context, WaitHandle waitHandle)
            {
                Context = context;
                WaitHandle = waitHandle;
                _handle = null;
                _completed = 0;
            }

            public WorkerBase(AsyncContext context)
                : this(context, null)
            {
            }

            public void SetHandle(RegisteredWaitHandle handle)
            {
                if (Thread.VolatileRead(ref _completed) == 0)
                {
                    Interlocked.CompareExchange(ref _handle, handle, null);
                }
                else
                {
                    // NOTE(jsd): Unused.
                    handle.Unregister(null);
                    handle = null;
                }
            }

            public void UnregisterHandle()
            {
                if (_handle != null)
                {
                    _handle.Unregister(null);
                    _handle = null;
                }
            }

            public void Complete()
            {
                Interlocked.Exchange(ref _completed, 1);

                var context = Context;

                Clear();
                context.asyncOperationEnd();
            }

            public void Clear()
            {
                _clearState();
                Context = null;
                WaitHandle = null;
                _handle = null;
            }

            protected abstract void _clearState();
        }

        public void Foreach<T>(IEnumerable<T> coll, Action complete, Action<T, Action> iteration)
        {
            Action next = null;

            var en = coll.GetEnumerator();

            next = () =>
            {
                if (en == null) return;

                if (en.MoveNext())
                {
                    QueueWorker(null, iteration, en.Current, next);
                }
                else
                {
                    en.Dispose();
                    en = null;
                    if (complete != null) complete();
                }
            };

            next();
        }

        public void ParallelForeach<T>(IEnumerable<T> coll, int parallelism, Action complete, Action<T, Action> iteration)
        {
            Action next = null;

            int iterators = 0;
            int isCompleted = 0;
            object enLock = new object();
            var en = coll.GetEnumerator();

            next = () =>
            {
                // Must lock to synchronize concurrent iterators:
                int newCount;

                lock (enLock)
                {
                    newCount = Interlocked.Decrement(ref iterators);

                    // NOTE(jsd): Rely on the current thread's cache being aware of the `null` if it was set by another thread.
                    if (en != null)
                    {
                        if (en.MoveNext())
                        {
                            newCount = Interlocked.Increment(ref iterators);
                            QueueWorker(null, iteration, en.Current, next);
                        }
                        else
                        {
                            en.Dispose();
                            Interlocked.Exchange(ref en, null);
                            //Console.WriteLine("complete!");
                            Interlocked.Exchange(ref isCompleted, 1);
                        }
                    }
                }

                if ((newCount == 0) & (isCompleted == 1))
                {
                    if (complete != null) complete();
                }
            };

            // Queue up the first m iterations:
            for (int i = 0; i < parallelism; ++i)
            {
                // Must lock to synchronize concurrent iterators:
                lock (enLock)
                {
                    // NOTE(jsd): Rely on the current thread's cache being aware of the `null` if it was set by another thread.
                    if (en != null)
                    {
                        if (en.MoveNext())
                        {
                            Interlocked.Increment(ref iterators);
                            QueueWorker(null, iteration, en.Current, next);
                        }
                        else
                        {
                            en.Dispose();
                            Interlocked.Exchange(ref en, null);
                            //Console.WriteLine("complete!");
                            Interlocked.Exchange(ref isCompleted, 1);
                            if (iterators == 0)
                            {
                                if (complete != null) complete();
                            }
                            break;
                        }
                    }
                }
            }
        }

        #region Web Requests

        private static void _defaultHandleException(Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

        private static void _defaultDoNothing()
        {
        }

        public void WebRequest(string httpMethod, Uri requestUri, Action<Exception> handleException, Action completedWithError, Action<HttpWebRequest> preBody, Action<HttpWebRequest, Stream> postBody, Action<HttpWebRequest, HttpWebResponse> handleResponse)
        {
            var req = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(requestUri);
            req.Method = httpMethod;

            var _handleException = handleException ?? _defaultHandleException;
            var _completedWithError = completedWithError ?? _defaultDoNothing;

            try
            {
                if (preBody != null) preBody(req);

                var ast = new AsyncWebRequestExecutionState(this, req, _handleException, _completedWithError, postBody, handleResponse);

                asyncOperationStart();
                if (postBody != null)
                {
                    // Send the body:
                    req.BeginGetRequestStream(_completeGetRequestStream, ast);
                }
                else
                {
                    req.BeginGetResponse(_completeGetResponse, ast);
                }
            }
            catch (Exception ex)
            {
                _handleException(ex);
                _completedWithError();
                asyncOperationEnd();
            }
        }
        
        public void WebRequestSync(string httpMethod, Uri requestUri, Action<System.Net.HttpWebRequest> preBody, Action<System.Net.HttpWebRequest, System.IO.Stream> postBody, Action<System.Net.HttpWebRequest, System.IO.Stream> handleResponse)
        {
            var req = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(requestUri);
            req.Method = httpMethod;
            
            if (preBody != null) preBody(req);

            if (postBody != null)
            {
                using (var reqstr = req.GetRequestStream())
                    postBody(req, reqstr);
            }

            var rsp = (System.Net.HttpWebResponse)req.GetResponse();

            using (var rs = rsp.GetResponseStream())
                handleResponse(req, rs);
        }

        private sealed class AsyncWebRequestExecutionState
        {
            public AsyncContext context { get; private set; }
            public HttpWebRequest request { get; private set; }
            public Action<Exception> handleException { get; private set; }
            public Action completedWithError { get; private set; }
            public Action<HttpWebRequest, Stream> postBody { get; private set; }
            public Action<HttpWebRequest, HttpWebResponse> handleResponse { get; private set; }

            public AsyncWebRequestExecutionState(AsyncContext context, HttpWebRequest request, Action<Exception> handleException, Action completedWithError, Action<HttpWebRequest, Stream> postBody, Action<HttpWebRequest, HttpWebResponse> handleResponse)
            {
                this.context = context;
                this.request = request;
                this.handleException = handleException;
                this.completedWithError = completedWithError;
                this.postBody = postBody;
                this.handleResponse = handleResponse;
            }

            public void clear()
            {
                context = null;
                request = null;
                handleException = null;
                completedWithError = null;
                postBody = null;
                handleResponse = null;
            }
        }

        private static void _completeGetResponse(IAsyncResult ar)
        {
            var r = (AsyncWebRequestExecutionState)ar.AsyncState;

            HttpWebResponse rsp;
            try
            {
                rsp = (HttpWebResponse)r.request.EndGetResponse(ar);
            }
            catch (Exception ex)
            {
                r.handleException(ex);
                r.completedWithError();
                r.context.asyncOperationEnd();
                r.clear();
                r = null;
                return;
            }

            try
            {
                r.handleResponse(r.request, rsp);
                r.context.asyncOperationEnd();
            }
            catch (Exception ex)
            {
                r.handleException(ex);
                r.completedWithError();
                r.context.asyncOperationEnd();
            }
            finally
            {
                rsp.Close();
                r.clear();
                r = null;
                rsp = null;
            }
        }

        private static void _completeGetRequestStream(IAsyncResult ar)
        {
            var r = (AsyncWebRequestExecutionState)ar.AsyncState;

            System.IO.Stream reqstr;
            try
            {
                reqstr = r.request.EndGetRequestStream(ar);
            }
            catch (Exception ex)
            {
                r.handleException(ex);
                r.completedWithError();
                r.context.asyncOperationEnd();
                r.clear();
                r = null;
                reqstr = null;
                return;
            }

            try
            {
                using (reqstr)
                    r.postBody(r.request, reqstr);
            }
            catch (Exception ex)
            {
                r.handleException(ex);
                r.completedWithError();
                r.context.asyncOperationEnd();
                r.clear();
                r = null;
                reqstr = null;
                return;
            }

            // Start an async operation to get the response:
            r.request.BeginGetResponse(_completeGetResponse, r);
        }

        #endregion

        public void StartCustomIO()
        {
            asyncOperationStart();
        }

        public void EndCustomIO()
        {
            asyncOperationEnd();
        }

        public SqlAsyncCommand CreateQuery(string queryText, SqlAsyncConnectionString asyncConnectionString)
        {
            var query = SqlAsyncCommand.Query(queryText, asyncConnectionString);
            return query;
        }

        public SqlAsyncCommand CreateQuery(string queryText, SqlAsyncConnectionString asyncConnectionString, [Optional] int? commandTimeout)
        {
            var query = SqlAsyncCommand.Query(queryText, asyncConnectionString, commandTimeout);
            return query;
        }

        private sealed class ExecuteReaderState<T> : WorkerBase
        {
            public SqlAsyncCommand Query { get; private set; }
            public Action<SqlAsyncCommand, Exception> ErrorHandler { get; private set; }
            public ReadResultDelegate<T> ReadResult { get; private set; }
            public Action<T> UseResult { get; private set; }

            public ExecuteReaderState(AsyncContext context, SqlAsyncCommand query, Action<SqlAsyncCommand, Exception> err, ReadResultDelegate<T> readResult, Action<T> useResult)
                : base(context)
            {
                this.Query = query;
                this.ErrorHandler = err;
                this.ReadResult = readResult;
                this.UseResult = useResult;
            }

            protected override void _clearState()
            {
                Query = null;
                ErrorHandler = null;
                ReadResult = null;
                UseResult = null;
            }
        }

        private static void _completeExecuteReader<T>(T result, ExecuteReaderState<T> st)
        {
            // Jump to a worker thread to process the query's result:
            if (st.UseResult != null)
            {
#if IOCPQueue
                st.Context.QueueWorker(null, st.UseResult, result);
#else
                st.UseResult(result);
#endif
            }

            result = default(T);

            // Track some statistics:
            Interlocked.Increment(ref st.Context._sqlQueriesCompleted);

            st.Context.ReleaseIOThrottle();

            st.Complete();
        }

        private static void _completeThrottleWaitAsyncExecuteReader<T>(object state, bool timedOut)
        {
            var ast = (ExecuteReaderState<T>)state;

            ast.Query.ExecuteReader<T, ExecuteReaderState<T>>(ast.ErrorHandler, ast.ReadResult, _completeExecuteReader<T>, ast);

            ast.UnregisterHandle();
        }

        public void ExecuteReaderAsync<T>(SqlAsyncCommand query, Action<SqlAsyncCommand, Exception> err, ReadResultDelegate<T> readResult, Action<T> useResult)
        {
            asyncOperationStart();

            Action<SqlAsyncCommand, Exception> errorHandler = (q, ex) =>
            {
                err(q, ex);
                Interlocked.Increment(ref _sqlQueriesErrored);
                asyncOperationEnd();
            };

            var ast = new ExecuteReaderState<T>(this, query, errorHandler, readResult, useResult);

            if (_ioCount > 0)
                ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(_throttleIO, _completeThrottleWaitAsyncExecuteReader<T>, (object)ast, -1, true));
            else
                query.ExecuteReader<T, ExecuteReaderState<T>>(ast.ErrorHandler, ast.ReadResult, _completeExecuteReader<T>, ast);
        }

        private sealed class ExecuteNonQueryState<T> : WorkerBase
        {
            public SqlAsyncCommand Query { get; private set; }
            public Action<SqlAsyncCommand, Exception> ErrorHandler { get; private set; }
            public Func<SqlParameterCollection, int, T> ProcessResult { get; private set; }
            public Action<T> UseResult { get; private set; }

            public ExecuteNonQueryState(AsyncContext context, SqlAsyncCommand query, Action<SqlAsyncCommand, Exception> err, Func<SqlParameterCollection, int, T> processResult, Action<T> useResult)
                : base(context)
            {
                this.Query = query;
                this.ErrorHandler = err;
                this.ProcessResult = processResult;
                this.UseResult = useResult;
            }

            protected override void _clearState()
            {
                Query = null;
                ErrorHandler = null;
                ProcessResult = null;
                UseResult = null;
            }
        }

        private static void _completeExecuteNonQuery<T>(T result, ExecuteNonQueryState<T> st)
        {
            // Jump to a worker thread to process the query's result:
            if (st.UseResult != null)
            {
#if IOCPQueue
                st.Context.QueueWorker(null, st.UseResult, result);
#else
                st.UseResult(result);
#endif
            }

            // Track some statistics:
            Interlocked.Increment(ref st.Context._sqlQueriesCompleted);

            st.Context.ReleaseIOThrottle();

            st.Complete();
        }

        private static void _completeThrottleWaitAsyncExecuteNonQuery<T>(object state, bool timedOut)
        {
            var ast = (ExecuteNonQueryState<T>)state;

            ast.Query.ExecuteNonQuery<T, ExecuteNonQueryState<T>>(ast.ErrorHandler, ast.ProcessResult, _completeExecuteNonQuery<T>, ast);

            ast.UnregisterHandle();
        }

        public void ExecuteNonQueryAsync<T>(SqlAsyncCommand query, Action<SqlAsyncCommand, Exception> err, Func<SqlParameterCollection, int, T> processResult, Action<T> useResult)
        {
            asyncOperationStart();

            Action<SqlAsyncCommand, Exception> errorHandler = (q, ex) =>
            {
                err(q, ex);
                Interlocked.Increment(ref _sqlQueriesErrored);
                asyncOperationEnd();
            };

            var ast = new ExecuteNonQueryState<T>(this, query, errorHandler, processResult, useResult);

            if (_ioCount > 0)
                ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(_throttleIO, _completeThrottleWaitAsyncExecuteNonQuery<T>, (object)ast, -1, true));
            else
                query.ExecuteNonQuery<T, ExecuteNonQueryState<T>>(ast.ErrorHandler, ast.ProcessResult, _completeExecuteNonQuery<T>, ast);
        }

        private sealed class ExecuteNonQueryState : WorkerBase
        {
            public SqlAsyncCommand Query { get; private set; }
            public Action<SqlAsyncCommand, Exception> ErrorHandler { get; private set; }
            public Action Done { get; private set; }

            public ExecuteNonQueryState(AsyncContext context, Action<SqlAsyncCommand, Exception> err, SqlAsyncCommand query, Action done)
                : base(context)
            {
                this.Query = query;
                this.ErrorHandler = err;
                this.Done = done;
            }

            protected override void _clearState()
            {
                Query = null;
                ErrorHandler = null;
                Done = null;
            }
        }

        private static void _completeExecuteNonQuery(ExecuteNonQueryState st)
        {
            // Jump to a worker thread to process the query's result:
            if (st.Done != null)
            {
#if IOCPQueue
                st.Context.QueueWorker(null, st.Done);
#else
                st.UseResult(result);
#endif
            }

            // Track some statistics:
            Interlocked.Increment(ref st.Context._sqlQueriesCompleted);

            st.Context.ReleaseIOThrottle();

            st.Complete();
        }

        private static void _completeThrottleWaitAsyncExecuteNonQuery(object state, bool timedOut)
        {
            var ast = (ExecuteNonQueryState)state;

            ast.Query.ExecuteNonQuery(ast.ErrorHandler, _completeExecuteNonQuery, ast);

            ast.UnregisterHandle();
        }

        public void ExecuteNonQueryAsync(SqlAsyncCommand query, Action<SqlAsyncCommand, Exception> err, Action done)
        {
            asyncOperationStart();

            Action<SqlAsyncCommand, Exception> errorHandler = (q, ex) =>
            {
                err(q, ex);
                Interlocked.Increment(ref _sqlQueriesErrored);
                asyncOperationEnd();
            };

            var ast = new ExecuteNonQueryState(this, errorHandler, query, done);

            if (_ioCount > 0)
                ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(_throttleIO, _completeThrottleWaitAsyncExecuteNonQuery, (object)ast, -1, true));
            else
                query.ExecuteNonQuery<ExecuteNonQueryState>(ast.ErrorHandler, _completeExecuteNonQuery, ast);
        }

        #region Private classes

        private sealed class WorkerState : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action DoWork { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action doWork)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
            }

            public void Run()
            {
                try { DoWork(); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
            }
        }

        private sealed class WorkerState<T1> : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action<T1> DoWork { get; private set; }
            public T1 N1 { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action<T1> doWork, T1 n1)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
                N1 = n1;
            }

            public void Run()
            {
                try { DoWork(N1); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
                N1 = default(T1);
            }
        }

        private sealed class WorkerState<T1, T2> : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action<T1, T2> DoWork { get; private set; }
            public T1 N1 { get; private set; }
            public T2 N2 { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action<T1, T2> doWork, T1 n1, T2 n2)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
                N1 = n1;
                N2 = n2;
            }

            public void Run()
            {
                try { DoWork(N1, N2); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
                N1 = default(T1);
                N2 = default(T2);
            }
        }

        private sealed class WorkerState<T1, T2, T3> : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action<T1, T2, T3> DoWork { get; private set; }
            public T1 N1 { get; private set; }
            public T2 N2 { get; private set; }
            public T3 N3 { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action<T1, T2, T3> doWork, T1 n1, T2 n2, T3 n3)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
                N1 = n1;
                N2 = n2;
                N3 = n3;
            }

            public void Run()
            {
                try { DoWork(N1, N2, N3); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
                N1 = default(T1);
                N2 = default(T2);
                N3 = default(T3);
            }
        }

        private sealed class WorkerState<T1, T2, T3, T4> : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action<T1, T2, T3, T4> DoWork { get; private set; }
            public T1 N1 { get; private set; }
            public T2 N2 { get; private set; }
            public T3 N3 { get; private set; }
            public T4 N4 { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action<T1, T2, T3, T4> doWork, T1 n1, T2 n2, T3 n3, T4 n4)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
                N1 = n1;
                N2 = n2;
                N3 = n3;
                N4 = n4;
            }

            public void Run()
            {
                try { DoWork(N1, N2, N3, N4); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
                N1 = default(T1);
                N2 = default(T2);
                N3 = default(T3);
                N4 = default(T4);
            }
        }

        private sealed class WorkerState<T1, T2, T3, T4, T5> : WorkerBase
        {
            public Action<Exception> HandleError { get; private set; }
            public Action<T1, T2, T3, T4, T5> DoWork { get; private set; }
            public T1 N1 { get; private set; }
            public T2 N2 { get; private set; }
            public T3 N3 { get; private set; }
            public T4 N4 { get; private set; }
            public T5 N5 { get; private set; }

            public WorkerState(AsyncContext context, WaitHandle waitHandle, Action<Exception> err, Action<T1, T2, T3, T4, T5> doWork, T1 n1, T2 n2, T3 n3, T4 n4, T5 n5)
                : base(context, waitHandle)
            {
                HandleError = err;
                DoWork = doWork;
                N1 = n1;
                N2 = n2;
                N3 = n3;
                N4 = n4;
                N5 = n5;
            }

            public void Run()
            {
                try { DoWork(N1, N2, N3, N4, N5); }
                catch (Exception ex) { HandleError(ex); }
            }

            protected override void _clearState()
            {
                HandleError = null;
                DoWork = null;
                N1 = default(T1);
                N2 = default(T2);
                N3 = default(T3);
                N4 = default(T4);
                N5 = default(T5);
            }
        }

        #endregion

        #region QueueWorker

        private static void _workerReady(object state)
        {
            var st = (WorkerState)state;
            st.Run();
            st.Complete();
        }

        private static void _workerReady<T1>(object state)
        {
            var st = (WorkerState<T1>)state;
            st.Run();
            st.Complete();
        }

        private static void _workerReady<T1, T2>(object state)
        {
            var st = (WorkerState<T1, T2>)state;
            st.Run();
            st.Complete();
        }

        private static void _workerReady<T1, T2, T3>(object state)
        {
            var st = (WorkerState<T1, T2, T3>)state;
            st.Run();
            st.Complete();
        }

        private static void _workerReady<T1, T2, T3, T4>(object state)
        {
            var st = (WorkerState<T1, T2, T3, T4>)state;
            st.Run();
            st.Complete();
        }

        private static void _workerReady<T1, T2, T3, T4, T5>(object state)
        {
            var st = (WorkerState<T1, T2, T3, T4, T5>)state;
            st.Run();
            st.Complete();
        }

        private static void _workerException(Exception ex)
        {
            Trace.WriteLine(ex.ToString(), "QueueWorker");
        }

        public void QueueWorker(Action<Exception> err, Action worker)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady, (object)new WorkerState(this, null, err ?? _workerException, worker));
        }

        public void QueueWorker<T1>(Action<Exception> err, Action<T1> worker, T1 arg1)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady<T1>, (object)new WorkerState<T1>(this, null, err ?? _workerException, worker, arg1));
        }

        public void QueueWorker<T1, T2>(Action<Exception> err, Action<T1, T2> worker, T1 arg1, T2 arg2)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady<T1, T2>, (object)new WorkerState<T1, T2>(this, null, err ?? _workerException, worker, arg1, arg2));
        }

        public void QueueWorker<T1, T2, T3>(Action<Exception> err, Action<T1, T2, T3> worker, T1 arg1, T2 arg2, T3 arg3)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady<T1, T2, T3>, (object)new WorkerState<T1, T2, T3>(this, null, err ?? _workerException, worker, arg1, arg2, arg3));
        }

        public void QueueWorker<T1, T2, T3, T4>(Action<Exception> err, Action<T1, T2, T3, T4> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady<T1, T2, T3, T4>, (object)new WorkerState<T1, T2, T3, T4>(this, null, err ?? _workerException, worker, arg1, arg2, arg3, arg4));
        }

        public void QueueWorker<T1, T2, T3, T4, T5>(Action<Exception> err, Action<T1, T2, T3, T4, T5> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            asyncOperationStart();

            ThreadPool.UnsafeQueueUserWorkItem(_workerReady<T1, T2, T3, T4, T5>, (object)new WorkerState<T1, T2, T3, T4, T5>(this, null, err ?? _workerException, worker, arg1, arg2, arg3, arg4, arg5));
        }

        public delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        #endregion

        #region QueueWaiter

        private static void _waiterReady(object state, bool timedOut)
        {
            var st = (WorkerState)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        private static void _waiterReady<T1>(object state, bool timedOut)
        {
            var st = (WorkerState<T1>)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        private static void _waiterReady<T1, T2>(object state, bool timedOut)
        {
            var st = (WorkerState<T1, T2>)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        private static void _waiterReady<T1, T2, T3>(object state, bool timedOut)
        {
            var st = (WorkerState<T1, T2, T3>)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        private static void _waiterReady<T1, T2, T3, T4>(object state, bool timedOut)
        {
            var st = (WorkerState<T1, T2, T3, T4>)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        private static void _waiterReady<T1, T2, T3, T4, T5>(object state, bool timedOut)
        {
            var st = (WorkerState<T1, T2, T3, T4, T5>)state;
            st.Run();
            st.UnregisterHandle();
            st.Complete();
        }

        public void QueueWaiter(WaitHandle waiter, Action<Exception> err, Action worker)
        {
            asyncOperationStart();

            var ast = new WorkerState(this, waiter, err, worker);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady, (object)ast, -1, true));
        }

        public void QueueWaiter<T1>(WaitHandle waiter, Action<Exception> err, Action<T1> worker, T1 arg1)
        {
            asyncOperationStart();

            var ast = new WorkerState<T1>(this, waiter, err, worker, arg1);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady<T1>, (object)ast, -1, true));
        }

        public void QueueWaiter<T1, T2>(WaitHandle waiter, Action<Exception> err, Action<T1, T2> worker, T1 arg1, T2 arg2)
        {
            asyncOperationStart();

            var ast = new WorkerState<T1, T2>(this, waiter, err, worker, arg1, arg2);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady<T1, T2>, (object)ast, -1, true));
        }

        public void QueueWaiter<T1, T2, T3>(WaitHandle waiter, Action<Exception> err, Action<T1, T2, T3> worker, T1 arg1, T2 arg2, T3 arg3)
        {
            asyncOperationStart();

            var ast = new WorkerState<T1, T2, T3>(this, waiter, err, worker, arg1, arg2, arg3);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady<T1, T2, T3>, (object)ast, -1, true));
        }

        public void QueueWaiter<T1, T2, T3, T4>(WaitHandle waiter, Action<Exception> err, Action<T1, T2, T3, T4> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            asyncOperationStart();

            var ast = new WorkerState<T1, T2, T3, T4>(this, waiter, err, worker, arg1, arg2, arg3, arg4);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady<T1, T2, T3, T4>, (object)ast, -1, true));
        }

        public void QueueWaiter<T1, T2, T3, T4, T5>(WaitHandle waiter, Action<Exception> err, Action<T1, T2, T3, T4, T5> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            asyncOperationStart();

            var ast = new WorkerState<T1, T2, T3, T4, T5>(this, waiter, err, worker, arg1, arg2, arg3, arg4, arg5);
            ast.SetHandle(ThreadPool.UnsafeRegisterWaitForSingleObject(waiter, _waiterReady<T1, T2, T3, T4, T5>, (object)ast, -1, true));
        }

        #endregion
    }
}
