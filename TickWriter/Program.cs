using Platform.TCPFramework.Serialization.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TickWriterService
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new HSTickWriter();

            if (Environment.UserInteractive)
            {
                DefaultMessageTickPackSerializationFactory.Instance = new Platform.TCPFramework.Serialization.Binary.MessageTickPackSerializationFactory();

                service.ConsoleStart(args);
                System.Console.WriteLine("Press any key to exit...");
                Console.ReadLine();                
                service.Stop();                
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { service });
            }
        }
    }
}
