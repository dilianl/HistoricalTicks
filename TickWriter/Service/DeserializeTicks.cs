using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Platform.Tick.Common.DataTypes;
using Platform.TCPFramework.Clients;
using Platform.TCPFramework.Serialization.Common;

namespace TickWriterService.Service
{
    public class DeserializeTicks
    {
        int m_totalObjectDeserialized = 0;
        public int TotalObejectDeserialized
        {
            get { return m_totalObjectDeserialized; }
        }
        string m_name = String.Empty;
        public string Name
        {
            get {return m_name;}
        }
        Thread thread = null;
        public bool isWorking = false;
        public static int TotalDesObjects = 0;

        public static int totalThreadCount = 0;
        /// <summary>
        /// Get the data from listInput, deserialize it and put the result in listOutput
        /// </summary>
        /// <param name="listInput">List of bytes array</param>
        /// <param name="listOutput">List with MessageTick objects</param>
        /// <param name="t"></param>
        private static void Execute(Object listInput, Object listOutput, Object t)
        {
            if ((listInput is BlockingCollection<byte[]>)&&(listOutput is BlockingCollection<MessageTick>))
            {
                var deserialize = DefaultMessageTickPackSerializationFactory.Instance.Create();
                BlockingCollection<byte[]> listSource = (BlockingCollection<byte[]>)listInput;
                BlockingCollection<MessageTick> listDest = (BlockingCollection<MessageTick>)listOutput;
                
                DeserializeTicks thread = (DeserializeTicks)t;
                thread.isWorking = true;
                while (thread.isWorking)
                {
                    byte[] tmp = null;
                    if (listSource.TryTake(out tmp, TimeSpan.FromMilliseconds(10)))
                    {
                        //deserialize and put in listDest
                        thread.m_totalObjectDeserialized++;

                        MessageTickPack messageTickPack = deserialize.Deserialize(tmp);

                        foreach (MessageTick mt in messageTickPack.MessageTicks)
                        {                            
                            listDest.Add(mt);
                            DeserializeTicks.TotalDesObjects++;
                        }
                        
                        if (listSource.Count == 0)
                        {
                            Thread.Sleep(50);
                        }                        
                    }                    
                    else
                    {
                        Thread.Sleep(50);
                    }                    
                }
            }
        }

        private void ExecuteMethod(BlockingCollection<byte[]> listInput, BlockingCollection<MessageTick> listOutput, DeserializeTicks thread)
        {
            DeserializeTicks.Execute(listInput, listOutput, thread);
        }

        public DeserializeTicks(BlockingCollection<byte[]> listInput, BlockingCollection<MessageTick> listOutput)
        {
            thread = new Thread(() => ExecuteMethod(listInput, listOutput, this));
            m_name = "D" + (totalThreadCount+1).ToString();
            thread.Start();
            totalThreadCount++;
        }


        public void Stop()
        {
            isWorking = false; 
            thread.Abort();
        }        
    }
}
