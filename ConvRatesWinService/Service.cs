using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ConfigurationsManager.ConfigInfrastructure;
using ConfigurationsManager.ConfigInfrastructure.MtBoats;
using log4net;
using Markets.ServiceContracts;


namespace ConvRatesWinService
{
    public partial class Service : ServiceBase
    {
        private readonly ILog log = LogManager.GetLogger(typeof(Service));
        private ServiceHost serviceHost = null;


        public Service()
        {
            this.ServiceName = ConfigurationManager.AppSettings["ServiceName"];
            InitializeComponent();
        }


        public void ConsoleStart(string[] args)
        {
            this.OnStart(args);
        }


        protected override void OnStart(string[] args)
        {
            try
            {
                serviceHost = new ServiceHost(typeof(ConvRatesService));
                serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior());

                NetTcpBinding netTcpBinding = new NetTcpBinding(SecurityMode.None)
                {
                    ReceiveTimeout = TimeSpan.FromSeconds(1)
                };

                string serviceUrl = ConfigurationManager.AppSettings["ConvRatesServiceUrl"];

                serviceHost.AddServiceEndpoint(typeof(IConvRate), netTcpBinding, serviceUrl);
                serviceHost.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexTcpBinding(), serviceUrl + "mex");
                serviceHost.Open();

                ConvRatesProvider.GetInstance().Init();

                log.Info("ConvRatesWinService host is UP.");
            }
            catch (Exception e)
            {
                log.Error("ConvRatesWinService OnStart failed:", e);
            }
        }


        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }
        }
    }
}
