using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoricalTicksRepository
{
    public interface ITicksContainer
    {
        void AddEntry(string SymbolName, int Value, decimal DecimalValue, DateTime TickTimeStamp, byte TickType, string TickProvider);
        int Count();
    }
}
