using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TickWriterService.Service
{
    [ServiceContract(CallbackContract = typeof(ITickWriterEvents))]
    public interface ITickWriter
    {
        //[OperationContract]        
        //bool PingMe();

        [OperationContract]
        void SubscribeForEvent();
    }
}
