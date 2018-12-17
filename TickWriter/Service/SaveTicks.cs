using HistoricalTicksRepository;
using Markets.DataTypes;
using Platform.Tick.Common.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TickWriterService.Service
{
    public class SaveTicks
    {
        /// <summary>
        /// Containst only the latest BID symbols
        /// </summary>
        public static Dictionary<string, MessageTick> symbolsCacheBid = new Dictionary<string, MessageTick>();
        /// <summary>
        /// Contains all latest symbols, except BID
        /// </summary>
        public static Dictionary<string, MessageTick> symbolsCacheAsc = new Dictionary<string, MessageTick>();
        
        Thread thread = null;        
        IHistoricalTicksRepository ticksRepository = null;
        int saveIntervalInMs = 1000;

        bool isWorking = false;
        public bool isFinished = false;
        

        static void SaveAndSendTicks(ITicksContainer containerTicks, IHistoricalTicksRepository repository)
        {
            long start = DateTime.Now.Ticks;
            //ITicksContainer _containerTicks = containerTicks;            

            Dictionary<string, LatestSymbolTick> dictToSend = new Dictionary<string, LatestSymbolTick>();

            ITicksContainer containerLatestTicks = new TicksDataTableContainer();
            foreach (KeyValuePair<string, MessageTick> tick in symbolsCacheBid)
            {
                containerLatestTicks.AddEntry(tick.Value.Symbol, (int)tick.Value.Value, tick.Value.DecimalValue, tick.Value.DateTime, (byte)tick.Value.TickType, tick.Value.Provider);

                LatestSymbolTick latestSymbolTick = new LatestSymbolTick { Symbol = tick.Value.Symbol, BidValue = tick.Value.DecimalValue, Timestamp = tick.Value.DateTime };
                dictToSend.Add(latestSymbolTick.Symbol, latestSymbolTick);
            }

            foreach (KeyValuePair<string, MessageTick> tick in symbolsCacheAsc)
            {
                containerLatestTicks.AddEntry(tick.Value.Symbol, (int)tick.Value.Value, tick.Value.DecimalValue, tick.Value.DateTime, (byte)tick.Value.TickType, tick.Value.Provider);                
            }

            symbolsCacheBid.Clear();
            symbolsCacheAsc.Clear();

            double msPrepare = TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalMilliseconds;
            start = DateTime.Now.Ticks;

            Int32 totalTickCount = containerTicks.Count();
            repository.TicksInsert(containerLatestTicks, containerTicks);
            double msInsert = TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalMilliseconds;

            containerLatestTicks = null;

            if (HSTickWriter.provider != null)
            { HSTickWriter.provider.ProvideLatestSymbols(dictToSend); }

            Console.WriteLine("Saved in " + DateTime.Now.Date.ToShortDateString() + ", " + DateTime.Now.ToLongTimeString() + "; prepare: " + msPrepare.ToString() + "; ïnsert:" + msInsert.ToString() + "; count: " + totalTickCount.ToString());
        }

        static void AddToSymbolCache(MessageTick tick)
        {
            Dictionary<string, MessageTick> dict = null;
            if (tick.TickType == Platform.Tick.Common.Enums.EnTickType.BID)
            {
                dict = symbolsCacheBid;
            }
            else
            {
                dict = symbolsCacheAsc;
            }
            if (dict.ContainsKey(tick.Symbol))
            {
                MessageTick tmp = dict[tick.Symbol];
                if (tmp.DateTime < tick.DateTime)
                {
                    dict[tick.Symbol] = tick;
                }
            }
            else
            {
                dict.Add(tick.Symbol, tick);
                //symbolsCache.Add(tick.Symbol, tick, (key, oldValue) => tick);
            }
        }

        static void Execute(Object listTicks, Object t)
        {
            if (listTicks is BlockingCollection<MessageTick>)
            {
                ITicksContainer containerTicks = new TicksDataTableContainer();

                DateTime lastSave = DateTime.Now;
                BlockingCollection<MessageTick> list = (BlockingCollection<MessageTick>)listTicks;
                SaveTicks thread = (SaveTicks)t;

                thread.isWorking = true;
                while (thread.isWorking)
                {
                    MessageTick messageTick = null;
                    while (list.TryTake(out messageTick, TimeSpan.FromMilliseconds(10)))
                    {
                        containerTicks.AddEntry(messageTick.Symbol, (int)messageTick.Value, messageTick.DecimalValue, messageTick.DateTime, (byte)messageTick.TickType, messageTick.Provider);
                        AddToSymbolCache(messageTick);

                        if (((int)(DateTime.Now - lastSave).TotalMilliseconds) >= thread.saveIntervalInMs)
                        {
                            // SaveTicks                            
                            SaveAndSendTicks(containerTicks, thread.ticksRepository);
                            containerTicks = new TicksDataTableContainer();
                            lastSave = DateTime.Now;
                        }
                    }
                    Thread.Sleep(20);
                }
                if (containerTicks.Count() > 0)
                {
                    SaveAndSendTicks(containerTicks, thread.ticksRepository);
                }
                thread.isFinished = true;
                //Console.WriteLine("FINISHED! Total Save Ticks: " + TotalSavedObjects + "; Total Received Ticks: " + DeserializeTicks.TotalDesObjects);
            }
        }

        void ExecuteMethod(BlockingCollection<MessageTick> listTicks, SaveTicks thread)
        {
            SaveTicks.Execute(listTicks, thread);
        }

        /// <summary>
        /// Create thread that will save Ticks
        /// </summary>
        /// <param name="listTicks">List with source ticks</param>
        /// <param name="saveIntervalMs">Interval in miliseconds to save the ticks</param>
        /// <param name="historicalTicksConnString">connection string to DB</param>
        public SaveTicks(BlockingCollection<MessageTick> listTicks, int saveIntervalMs, string historicalTicksConnString)
        {
            ticksRepository = new HistoricalTicksDBRepository(historicalTicksConnString);

            saveIntervalInMs = saveIntervalMs;
            thread = new Thread(() => ExecuteMethod(listTicks, this));
            thread.Start();            
        }

        public void Stop()
        {
            isWorking = false;
            while (!isFinished)
            {
                Thread.Sleep(20);
            } 
            thread.Abort();
        }
    }
}
