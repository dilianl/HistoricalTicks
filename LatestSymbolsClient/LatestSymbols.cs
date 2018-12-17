using LatestSymbolsClient.TickWriterLS;
using log4net;
using Markets.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LatestSymbolsClient
{
    public class LatestSymbols : TickWriterLS.ITickWriterCallback
    {
        public event Action<Dictionary<string, Markets.DataTypes.LatestSymbolTick>> OnReceiveSymbols;

        string urlToEndPoint = String.Empty;

        TickWriterClient client = null;
        List<Int32> listLastAccesedTime = new List<int>();
        Thread thread = null;
        bool isWorking = false;
        DateTime lastReceiveData = DateTime.MinValue;
        int threadSleepInMS = 100;
        readonly ILog _logger = LogManager.GetLogger(typeof(LatestSymbols));
        public LatestSymbols(string urlEndPoint)
        {
            urlToEndPoint = urlEndPoint;
            CreateClient();
        }

        void CreateClient()
        {
            NetTcpBinding ntb = new NetTcpBinding(SecurityMode.None);
            Uri baseAddress = new Uri(urlToEndPoint);
            WSDualHttpBinding wsd = new WSDualHttpBinding();
            EndpointAddress ea = new EndpointAddress(baseAddress);
            client = new TickWriterLS.TickWriterClient(new InstanceContext(this), ntb, ea);
        }

        int AverageLaseAccessTime
        {
            get { return listLastAccesedTime.Count > 0 ? (int)(listLastAccesedTime.Sum() / listLastAccesedTime.Count) : 0; }
        }

        int TotalMilisecondsFromLastAccess
        {
            get
            {
                long ticks = DateTime.Now.Ticks - lastReceiveData.Ticks;
                return TimeSpan.FromTicks(ticks).Seconds * 1000 + TimeSpan.FromTicks(ticks).Milliseconds;
            }
        }


        void _sprovideSymbols(Dictionary<string, Markets.DataTypes.LatestSymbolTick> symbols)
        {
            if (OnReceiveSymbols != null)
                this.OnReceiveSymbols(symbols);
        }

        public void ProvideSymbols(Dictionary<string, Markets.DataTypes.LatestSymbolTick> symbols)
        {
            if (lastReceiveData != DateTime.MinValue)
            {
                if (TotalMilisecondsFromLastAccess > 500)
                {
                    listLastAccesedTime.Add(TotalMilisecondsFromLastAccess);
                }

                if (listLastAccesedTime.Count > 100)
                { listLastAccesedTime.RemoveAt(0); }
            }

            lastReceiveData = DateTime.Now;
            _sprovideSymbols(symbols);
        }

        public void Start()
        {
            client.SubscribeForEvent();

            if (thread == null)
            {
                thread = new Thread(() => ExecuteMethod());
                thread.Start();
            }
        }

        object ExecuteMethod()
        {
            isWorking = true;
            while (isWorking)
            {
                int avg = AverageLaseAccessTime;
                if (avg > 0)
                {
                    int t = TotalMilisecondsFromLastAccess;
                    if (AverageLaseAccessTime * 1.50 < t) // if don't have event more than 50% of average time
                    {
                        _logger.Info(" " + t.ToString());
                        _logger.Info(String.Format("Don't have events more than {0}, trying to reconnect ...", t));
                        try
                        {
                            client = null;
                            Console.WriteLine("Try to reconnect...");
                            CreateClient();
                            Start();
                            client.SubscribeForEvent();
                            _logger.Info("Connected!");
                            listLastAccesedTime.Clear();
                            lastReceiveData = DateTime.MinValue;
                            Console.WriteLine("Success reconect");
                            threadSleepInMS = 100;
                        }
                        catch
                        {
                            _logger.Info("Unsuccesfull reconnect!");
                            client = null;
                            Console.WriteLine("Error");
                            if (threadSleepInMS < 5000)
                            {
                                threadSleepInMS += threadSleepInMS;
                            }
                        }
                    }
                }
                Thread.Sleep(threadSleepInMS);
            }
            return null;
        }
    }
}
