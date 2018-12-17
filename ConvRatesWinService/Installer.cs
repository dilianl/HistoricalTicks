using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ConvRatesWinService
{
    [DesignerCategory("Code")]
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();

            var processInstaller = new ServiceProcessInstaller() { Account = ServiceAccount.LocalSystem };
            var serviceInstaller = new ServiceInstaller()
            {
                ServiceName = ConfigurationManager.AppSettings["ServiceName"],
                Description = "Provides convertion rates for currencies",
                StartType = ServiceStartMode.Automatic
            };

            this.Installers.Add(serviceInstaller);
            this.Installers.Add(processInstaller);
        }
    }
}
