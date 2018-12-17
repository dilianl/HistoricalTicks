using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markets.DataTypes;

namespace HistoricalTicksRepository
{
    public interface IHistoricalTicksRepository
    {
        Dictionary<string, LatestSymbolTick> GetLatestSymbolTicks(List<string> supportedSymbols);
        void TicksInsert(ITicksContainer latestTicks, ITicksContainer ticks);
    }
}
