using Markets.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TickWriterService.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    class TickWriter : ITickWriter
    {        
        private ConcurrentDictionary<ITickWriterEvents, object> _subscribrers = new ConcurrentDictionary<ITickWriterEvents, object>();        

        public void SubscribeForEvent()
        {
            ITickWriterEvents subscriber = OperationContext.Current.GetCallbackChannel<ITickWriterEvents>();            
            // Add the subscriber
            _subscribrers.TryAdd(subscriber, null);

            // Listen for close connection and channel faults
            ICommunicationObject co = (ICommunicationObject)subscriber;
            co.Faulted += new EventHandler(DisconnectSubscriber);
            co.Closed += new EventHandler(DisconnectSubscriber);            
        }        

        public void ProvideLatestSymbols(Dictionary<string, LatestSymbolTick> symbols)
        {            
            FireEvent(se =>
            {
                se.ProvideSymbols(symbols);
            }, timeout: 10000);
        }

        //
        // Common method to fire async event one way
        // Handles timeouts and channel faults
        // 
        private void FireEvent(Action<ITickWriterEvents> action, int timeout /*ms*/)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            var tasks = new Dictionary<Task<int>, ITickWriterEvents>();

            // Fire the event in parallel to all subscribers
            //
            foreach (var se in _subscribrers.Keys)
            {
                Task<int> t = Task<int>.Factory.StartNew(() =>
                {
                    ICommunicationObject co = (ICommunicationObject)se;

                    try
                    {
                        if (co.State == CommunicationState.Opened)
                        {
                            // Call the actual event
                            action(se);
                            return 0;
                        }
                    }
                    catch
                    {
                        // Something went wrong while sending the message
                        // Fail the task
                    }

                    return -1;
                });

                tasks.Add(t, se);
            }

            // Check taks completion status
            //
            Task.Factory.StartNew(() =>
            {
                // Wait for all tasks to complete
                bool allSucceeded = Task.WaitAll(tasks.Keys.ToArray(), timeout);

                // Drop the faulty clients - they didn't process the event
                if (!allSucceeded)
                {
                    foreach (var t in tasks.Keys)
                    {
                        if (!t.IsCompleted || t.Result != 0)
                        {
                            DisconnectSubscriber(tasks[t], null);
                        }
                    }
                }
            });
        }

        private void DisconnectSubscriber(object sender, EventArgs e)
        {
            // Unsubscribe
            object dummy;
            _subscribrers.TryRemove((ITickWriterEvents)sender, out dummy);

            // Abort the communicaton channel
            ICommunicationObject co = (ICommunicationObject)sender;
            co.Abort();
        }
    }
}
