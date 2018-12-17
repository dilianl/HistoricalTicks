using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using DataObjects;
using Markets.DataTypes;
using Markets.Enums;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel;

namespace HistoricalTicksRepository
{
    public class HistoricalTicksDBRepository : IHistoricalTicksRepository
    {
        public string ConnString { get; private set; }
     

        public HistoricalTicksDBRepository(string conn)
        {
            ConnString = conn;
        }


        public Dictionary<string, LatestSymbolTick> GetLatestSymbolTicks(List<string> supportedSymbols)
        {
            Dictionary<string, LatestSymbolTick> ticks = new Dictionary<string, LatestSymbolTick>();

            using (var dataContext = GetDataContext())
            {
                ticks = (from t in dataContext.LatestSymbolTicks
                               where t.SymbolCategory == Convert.ToByte((int)ETradingInstrumentsCategory.Forex) && supportedSymbols.Contains(t.SymbolName) && t.TickType == (byte)EnTickType.BID
                               select new LatestSymbolTick
                               {
                                   BidValue = t.DecimalValue,
                                   Timestamp = t.TickTimeStamp,
                                   Symbol = t.SymbolName
                               }).ToDictionary(t => t.Symbol);
            }

            return ticks;
        }


        public void TicksInsert(ITicksContainer latestTicks, ITicksContainer ticks)
        {
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                SqlCommand cmd = new SqlCommand("spTicksInsert", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter p = cmd.Parameters.AddWithValue("@LatestSymbolTicks", ((TicksDataTableContainer)latestTicks).ToDataTable());
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "LatestSymbolTicksType";

                SqlParameter p2 = cmd.Parameters.AddWithValue("@Ticks", ((TicksDataTableContainer)ticks).ToDataTable());
                p2.SqlDbType = SqlDbType.Structured;
                p2.TypeName = "TicksType";

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        private HistoricalTicksDataContext GetDataContext()
        {
            return new HistoricalTicksDataContext(ConnString);
        }
    }
}
