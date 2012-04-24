using System;
using System.Threading;
using System.Web;
using WellDunne.Asynchrony;

namespace WellDunne.Web
{
    public abstract class AsyncContextHandler : IHttpAsyncHandler
    {
        private sealed class AsyncContextResult : IAsyncResult
        {
            public object AsyncState { get; private set; }
            public WaitHandle AsyncWaitHandle { get; private set; }
            public bool CompletedSynchronously { get { return false; } }
            public bool IsCompleted { get; private set; }

            public AsyncContextResult(object asyncState, WaitHandle asyncWaitHandle)
            {
                AsyncState = asyncState;
                AsyncWaitHandle = asyncWaitHandle;
                IsCompleted = false;
            }

            public void SetCompleted()
            {
                IsCompleted = true;
            }
        }

        public abstract void HandleRequestAsync(HttpContext http, AsyncContext work);

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            var asyncContext = new AsyncContext(0);
            var ar = new AsyncContextResult(extraData, asyncContext.WaitHandle);

            // Set the completion handler for when all asynchronous work completes:
            if (cb != null)
                asyncContext.SetCompleted(() => { ar.SetCompleted(); cb(ar); });
            else
                asyncContext.SetCompleted(() => { });

            // Start the asynchronous work:
            HandleRequestAsync(context, asyncContext);
            
            return ar;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            // TODO(jsd): we could throw exceptions here if we had any.
            return;
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}