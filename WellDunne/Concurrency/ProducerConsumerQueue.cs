using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace WellDunne.Concurrency
{
    /// <summary>
    /// A multiple-producer single-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of item to enqueue and dequeue</typeparam>
    public class ProducerConsumerQueue<T>
    {
        private Queue<T> _Q1;
        private Queue<T> _Q2;
        private volatile Queue<T> _CurrentWriteQueue;

        private ManualResetEvent _HandlerFinishedEvent;
        private ManualResetEvent _UnblockHandlerEvent;
        private AutoResetEvent _DataAvailableEvent;

        /// <summary>
        /// An event raised when data is to be consumed.
        /// </summary>
        public event ConsumeDelegate Consume;
        public delegate void ConsumeDelegate(T data);

        /// <summary>
        /// If an exception was thrown by the consumer, this event is fired to handle it.
        /// </summary>
        public event HandleConsumerExceptionDelegate HandleConsumerException;
        public delegate void HandleConsumerExceptionDelegate(Exception ex);

        /// <summary>
        /// Construct a multiple-producer, single-consumer queue.
        /// </summary>
        public ProducerConsumerQueue()
        {
            _Q1 = new Queue<T>();
            _Q2 = new Queue<T>();

            _CurrentWriteQueue = _Q1;

            _HandlerFinishedEvent = new ManualResetEvent(true);
            _UnblockHandlerEvent = new ManualResetEvent(true);
            _DataAvailableEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Gets the total count of items still queued.
        /// </summary>
        public int Count
        {
            get
            {
                int q1Count, q2Count;
                lock (_Q1) { q1Count = _Q1.Count; }
                lock (_Q2) { q2Count = _Q2.Count; }
                return q1Count + q2Count;
            }
        }

        /// <summary>
        /// Enqueues data onto the queue to be consumed by the thread running ConsumerFunc.
        /// </summary>
        /// <param name="produceData"></param>
        public void Produce(Func<T> produceData)
        {
            _UnblockHandlerEvent.WaitOne();
            _HandlerFinishedEvent.Reset();

            T data = produceData();

            // Get a local copy of the current writer queue so nobody
            // changes the reference between lock() and Enqueue()
            Queue<T> cwq = _CurrentWriteQueue;

            // Synchronize access to the writer queue from multiple producer threads:
            lock (cwq) { cwq.Enqueue(data); }

            _DataAvailableEvent.Set();
            _HandlerFinishedEvent.Set();
        }

        /// <summary>
        /// Gets a ThreadStart delegate for starting the consumer thread.
        /// </summary>
        public ThreadStart ConsumerThreadStart
        {
            get
            {
                return new ThreadStart(ConsumerFunc);
            }
        }

        /// <summary>
        /// The thread function which waits for data in the queue and then consumes it.
        /// </summary>
        private void ConsumerFunc()
        {
            T data;
            Queue<T> readQueue;

            // Loop forever, waiting for data to consume.
            // Shut down properly with Thread.Abort().
            while (true)
            {
                try
                {
                    // Wait for data to be available:
                    _DataAvailableEvent.WaitOne();

                    // Block the producer:
                    _UnblockHandlerEvent.Reset();
                    // Wait for the producer to finish:
                    _HandlerFinishedEvent.WaitOne();

                    // Swap read/write queues:
                    readQueue = _CurrentWriteQueue;
                    _CurrentWriteQueue = (_CurrentWriteQueue == _Q1) ? _Q2 : _Q1;

                    // Unblock the producer:
                    _UnblockHandlerEvent.Set();

                    if (Consume == null)
                    {
                        // No consumer function so we've got a dummy queue:
                        readQueue.Clear();
                        continue;
                    }

                    if (HandleConsumerException != null)
                    {
                        // Just read all items in the read queue:
                        //lock (readQueue)  // TODO: This lock may be redundant.
                        {
                            while (readQueue.Count > 0)
                            {
                                // Dequeue the data from the read queue:
                                data = readQueue.Dequeue();

                                // Consume the data with the Consume event:
                                try
                                {
                                    Consume(data);
                                }
                                catch (Exception ex)
                                {
                                    HandleConsumerException(ex);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Just read all items in the read queue:
                        //lock (readQueue)  // TODO: This lock may be redundant.
                        {
                            while (readQueue.Count > 0)
                            {
                                // Dequeue the data from the read queue:
                                data = readQueue.Dequeue();

                                // Consume the data with the Consume event:
                                Consume(data);
                            }
                        }
                    }
                }
                catch (ThreadAbortException /*tae*/)
                {
                    // Graceful shutdown of consumer thread.
                    break;
                }
            }
        }
    }
}
