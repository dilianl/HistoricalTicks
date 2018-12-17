using Autofac;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TickWriterService.Service;
using System.Collections.Concurrent;
using System.Threading;
using Markets.DataTypes;
using Platform.Tick.Common.DataTypes;
using Platform.TCPFramework.Clients;
using Platform.TCPFramework.Serialization.Common;
using ConfigurationsManager;
using ConfigurationsManager.ConfigInfrastructure;

namespace TickWriterService
{
    /// <summary>
    /// Historical Server Tick Writer class
    /// </summary>
    internal class HSTickWriter : ServiceBase
    {
        List<DeserializeTicks> listDeserializedThreads = new List<DeserializeTicks>();
        SaveTicks saveTicksThread = null;
        static BlockingCollection<byte[]> listTicksInBytes = new BlockingCollection<byte[]>();
        static BlockingCollection<MessageTick> listTicks = new BlockingCollection<MessageTick>();
        private static readonly ILog _logger = LogManager.GetLogger(typeof(HSTickWriter));

        private ServiceHost tickWriterServiceHost;

        private Configurations config;
        /// <summary>
        /// Historical Service configuration
        /// </summary>
        public static HistoricalTicksConfiguration hsConfig;
 
        public static TickWriter provider;
        public static MessageTickClient client = null;

        public void ConsoleStart(string[] args)
        {
            OnStart(args);
        }

        private static NetTcpBinding CreateTcpBinding()
        {
            var serviceTcpBinding = new NetTcpBinding(SecurityMode.None);
            serviceTcpBinding.MaxReceivedMessageSize = Int32.MaxValue;
            serviceTcpBinding.ReaderQuotas.MaxStringContentLength = 2147483647;
            serviceTcpBinding.ReaderQuotas.MaxDepth = 2147483647;
            serviceTcpBinding.ReaderQuotas.MaxNameTableCharCount = 16384 * 10;

            return serviceTcpBinding;
        }

        private Exception TryParseEndPoint(string endPointText, out IPEndPoint endPoint)
        {
            endPoint = null;
            try
            {
                string[] ppAddressAndPort = endPointText.Split(':');
                IPAddress address = IPAddress.Parse(ppAddressAndPort[0]);
                int port = int.Parse(ppAddressAndPort[1]);
                endPoint = new IPEndPoint(address, port);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private Exception CreatePPClient(IPEndPoint newEndPoint)
        {
            if (newEndPoint == null)
            {
                return new ArgumentException("newEndPoint");
            }

            try
            {
                client = new MessageTickClient(newEndPoint, 1000, DefaultMessageTickPackSerializationFactory.Instance.Create());
                client.OnConnected += this.Client_OnConnected;
                //client.OnMessages += this.Client_OnMessages;
                client.OnData += client_OnData;
                _logger.Info("Starting the client subscribed to PE...");
                Console.WriteLine("Starting the client subscribed to PE...");
                client.Start();
                _logger.Info("Client started!");
                Console.WriteLine("Client started!");
                return null;
            }
            catch (Exception ex)
            {
                client = null;
                return ex;
            }
        }

        private void client_OnData(List<byte[]> obj)
        {
            foreach (byte[] item in obj)
            {
                listTicksInBytes.Add(item);
            }
        }

        private void Client_OnConnected(object sender, EventArgs e)
        {
            client.Subscribe(SubscribeForEverythingSubscription.Instance);
        }

        public long MsgPackCount { get; private set; }
        private List<Tuple<DateTime, List<MessageTickPack>>> tickBuffer = new List<Tuple<DateTime, List<MessageTickPack>>>();

        private void Client_OnMessages(List<MessageTickPack> messages)
        {
            DateTime now = DateTime.UtcNow;
            MsgPackCount += messages.Count;
            //this.Speed.Process(this.MsgPackCount);
            //lock (this.sync)
            {
                this.tickBuffer.Add(new Tuple<DateTime, List<MessageTickPack>>(now, messages));
            }
        }

        private void connectToPЕClient(string newEndPoint, string addressToPE)
        {
            _logger.Info(String.Format("Subscribe to PE: {0}", addressToPE));
            Console.WriteLine(String.Format("Subscribe to PE: {0}", addressToPE));
            IPEndPoint tmpEndPoint;
            Exception error = TryParseEndPoint(newEndPoint, out tmpEndPoint);
            if (error != null)
            {
                // Log the Error
                return;
            }

            error = CreatePPClient(tmpEndPoint);
            if (error != null)
            {
                //Log the error
                return;
            }
        }

        void CreateSaveTicksThread(int saveIntervalInMiliseconds)
        {
            _logger.Info("Saving MessageTick thread/s  are going to start...");
            Console.WriteLine("Saving MessageTick thread/s  are going to start...");
            saveTicksThread = new SaveTicks(listTicks, saveIntervalInMiliseconds, hsConfig.HistoricalTicksConnString);
            _logger.Info("Saving MessageTick thread/s are started");
            Console.WriteLine("Saving MessageTick thread/s are started");
        }

        void CreateDesirializedThreads(int count)
        {
            _logger.Info(String.Format("{0} deserialization thread/s  are going to start...", count));
            Console.WriteLine(String.Format("{0} deserialization thread/s are going to start...", count));
            for (int i = 0; i < count; i++)
            {
                listDeserializedThreads.Add(new DeserializeTicks(listTicksInBytes, listTicks));
            }
            _logger.Info(String.Format("{0} deserialization thread/s are started", count));
            Console.WriteLine(String.Format("{0} deserialization thread/s are started", count));
        }

        ServiceHost CreateServiceHost(Type serviceType, Type endpointType, NetTcpBinding tcpBinding, string address, string name)
        {
            var host = new ServiceHost(new TickWriter(), new Uri(address));

            host.AddServiceEndpoint(endpointType, tcpBinding, address);
            host.Description.Behaviors.Add(new ServiceMetadataBehavior());
            host.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexTcpBinding(), address + "mex");
            host.Open();

            _logger.InfoFormat("Startеd {0} on: {1}", name, address);
            return host;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                config = ConfigurationsManager.ConfigurationsManager.GetConfigurations();
                hsConfig = config.HistoricalTicksConfiguration;
            
                var appSettings = ConfigurationManager.AppSettings;
                var serviceTcpBinding = CreateTcpBinding();
                tickWriterServiceHost = CreateServiceHost(typeof(TickWriter), typeof(ITickWriter), serviceTcpBinding, appSettings[TickWriterEn.AppSettings.Address.ToString()], "TickWriter Service");

                //tickWriterServiceHost = new ServiceHost(new TickWriter(), new Uri(appSettings[TickWriterEn.AppSettings.Address.ToString()]));

                provider = (TickWriter)tickWriterServiceHost.SingletonInstance;

                _logger.Info("Start TickWriter service");
                Console.WriteLine("Start TickWriter service");
                //var builder = new ContainerBuilder();
                //builder.Register(c => new TickWriter()).As<ITickWriter>();

                #region create client for ticks
                connectToPЕClient(appSettings[TickWriterEn.AppSettings.AddressToPE.ToString()], appSettings[TickWriterEn.AppSettings.AddressToPE.ToString()]);
                #endregion

                #region create threads list that will deserialize ticks
                int countDeserializedThread = 0;
                if (!int.TryParse(appSettings[TickWriterEn.AppSettings.CountDeserializeThread.ToString()], out countDeserializedThread))
                { countDeserializedThread = 1; }
                CreateDesirializedThreads(countDeserializedThread);
                #endregion

                #region start Save Tick thread
                int saveIntervalInMiliseconds = 0;
                if (!int.TryParse(appSettings[TickWriterEn.AppSettings.SaveIntervalInMiliseconds.ToString()], out saveIntervalInMiliseconds))
                { saveIntervalInMiliseconds = 1000; }

                CreateSaveTicksThread(saveIntervalInMiliseconds);
                #endregion
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("{0} {1}", ex.ToString(), ex.InnerException == null ? "" : ex.InnerException.ToString());
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                if ((client != null) && (client.Started))
                {
                    client.Unsubscribe(SubscribeForEverythingSubscription.Instance);
                    client.Stop();
                }

                foreach (DeserializeTicks t in listDeserializedThreads)
                {
                    t.Stop();
                }

                if (saveTicksThread != null)
                {                    
                    saveTicksThread.Stop();
                }

                if (tickWriterServiceHost != null)
                    tickWriterServiceHost.Close();
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("{0} {1}", ex.ToString(), ex.InnerException == null ? "" : ex.InnerException.ToString());
            }
        }
    }
}
