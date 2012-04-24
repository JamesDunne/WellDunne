using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace WellDunne.Concurrency
{
    /// <summary>
    /// Implements a time-controlled request submitter where no more than X actions per second may be performed.
    /// </summary>
    /// <remarks>
    /// This class does not guarantee evenly-time-distributed actions; all actions are submitted as soon as possible
    /// upon the next clock tick of the second hand.
    /// </remarks>
    public sealed class RateLimitedRequestSubmitter : IDisposable
    {
        private readonly int _maxReqPerSec;
        private readonly ManualResetEvent _quotaNotReached;
        private readonly System.Threading.Timer _timer;

        private int _inflation;
        private int _activeRequests;
        private int _activeRequestsThisSecond;

        public int MaxRequestsPerSecond { get { return _maxReqPerSec; } }

        /// <summary>
        /// Create a rate limited request submitter with the requested rate limit and request submission delegate.
        /// </summary>
        /// <param name="maxRequestsPerSecond">Maximum number of requests to submit per second, or 0 for no limit.</param>
        public RateLimitedRequestSubmitter(int maxRequestsPerSecond)
        {
            if (maxRequestsPerSecond < 0) maxRequestsPerSecond = 0;

            _maxReqPerSec = maxRequestsPerSecond;

            _activeRequests = 0;
            _activeRequestsThisSecond = 0;
            _inflation = 0;

            // Initially, the quota has not been reached:
            _quotaNotReached = new ManualResetEvent(true);

            if (maxRequestsPerSecond > 0)
            {
                // Start a timer to reset _activeRequestsThisSecond counter and set _quotaNotReached signal every second:
                _timer = new Timer((state) =>
                {
                    // Reset the request-per-second counter:
                    int lastRequestsPerSecond = Interlocked.Exchange(ref _activeRequestsThisSecond, 0);
                    int inflation;
                    if ((inflation = Thread.VolatileRead(ref _inflation)) > 0) Interlocked.Decrement(ref _inflation);

                    //Console.Error.WriteLine("{0} + {1}", lastRequestsPerSecond, inflation);

                    // Allow the next batch of enqueued requests to be submitted:
                    _quotaNotReached.Set();
                }, null, 1000, 1000);
            }
            else
            {
                _timer = null;
            }
        }

        private void requestComplete()
        {
            // Only allow a new request if we haven't gone over the per-second limit:
            Interlocked.Decrement(ref _activeRequests);
        }

        public void Dispose()
        {
            // Free up the timer and stop it:
            if (_timer != null) _timer.Dispose();
        }

        /// <summary>
        /// Artificially inflate the count of active requests to quell the submission rate.
        /// </summary>
        public void Inflate()
        {
            Interlocked.Add(ref _inflation, 1);
        }

        /// <summary>
        /// Enqueues a request to be submitted when the rate limit is not reached.
        /// </summary>
        /// <param name="submitRequest">Delegate to be called on a ThreadPool thread to submit the request when its time comes.</param>
        public void Enqueue(Action<Action> submitRequest)
        {
            // No rate limiting? Just submit it then.
            if (_maxReqPerSec == 0)
            {
                submitRequest(() => { });
                return;
            }

            // Create a callback that will be reused to enqueue the request for later if the quota becomes full:
            WaitOrTimerCallback cb = null;
            cb = (state, timedOut) =>
            {
                // We should never get a timeout:
                Debug.Assert(!timedOut);

                // Have we too many active requests out?
                if (Thread.VolatileRead(ref _activeRequestsThisSecond) + Thread.VolatileRead(ref _inflation) >= _maxReqPerSec)
                {
                    // No request slots free, try again later:
                    _quotaNotReached.Reset();
                    // TODO(jsd): Does the RegisteredWaitHandle get cleaned up automatically when the delegate is called?
                    ThreadPool.RegisterWaitForSingleObject(_quotaNotReached, cb, state, -1, true);
                    return;
                }

                Interlocked.Increment(ref _activeRequests);
                Interlocked.Increment(ref _activeRequestsThisSecond);
                _quotaNotReached.Reset();

                // Submit the request:
                var submit_ = (Action<Action>)state;
                submit_(requestComplete);
            };

            // Wait for the quota to not be reached and then attempt to submit this request:
            // TODO(jsd): Does the RegisteredWaitHandle get cleaned up automatically when the delegate is called?
            ThreadPool.RegisterWaitForSingleObject(_quotaNotReached, cb, (object)submitRequest, -1, true);
        }
    }
}
