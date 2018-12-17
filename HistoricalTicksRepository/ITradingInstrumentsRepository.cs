using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markets.DataTypes;

namespace HistoricalTicksRepository
{
    public interface ITradingInstrumentsRepository
    {
        List<string> GetSupportedForexSymbols();
    }
}
