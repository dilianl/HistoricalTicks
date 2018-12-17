using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataObjects;
using Markets.DataTypes;
using Markets.Enums;

namespace HistoricalTicksRepository
{
    public class TradingInstrumentsDBRepository : ITradingInstrumentsRepository
    {
        public string ConnString { get; private set; }

        public TradingInstrumentsDBRepository(string conn)
        {
            ConnString = conn;
        }


        public List<string> GetSupportedForexSymbols()
        {
            List<string> symbols = new List<string>();

            using (var dataContext = GetDataContext())
            {
                symbols = dataContext.vSymbols.Where(s => s.CategoryId == (int)ETradingInstrumentsCategory.Forex && s.Symbol.Length == 6).Select(s => s.Symbol).ToList();
            }

            return symbols;
        }

        private TradingInstrumentsDBDataContext GetDataContext()
        {
            return new TradingInstrumentsDBDataContext(ConnString);
        }
    }
}
