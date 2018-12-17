using Markets.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TickWriterService
{
    public interface ITickWriterEvents
    {
        [OperationContract(IsOneWay = true)]
        void ProvideSymbols(Dictionary<string, LatestSymbolTick> symbols);                       
    }
}
