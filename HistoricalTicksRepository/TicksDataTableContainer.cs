using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoricalTicksRepository
{
    public class TicksDataTableContainer : ITicksContainer
    {
        private DataTable dtTicks;

        public TicksDataTableContainer()
        {
            dtTicks = new DataTable();
            dtTicks.Columns.Add("SymbolName");
            dtTicks.Columns.Add("Value", typeof(int));
            dtTicks.Columns.Add("DecimalValue", typeof(decimal));
            dtTicks.Columns.Add("TickTimeStamp", typeof(DateTime));
            dtTicks.Columns.Add("TickType", typeof(byte));
            dtTicks.Columns.Add("TickProvider");
        }


        public void AddEntry(string SymbolName, int Value, decimal DecimalValue, DateTime TickTimeStamp, byte TickType, string TickProvider)
        {
            dtTicks.Rows.Add(new object[] { SymbolName, Value, DecimalValue, TickTimeStamp, TickType, TickProvider });
        }


        public DataTable ToDataTable()
        {
            return this.dtTicks;
        }

        /// <summary>
        /// get entries count
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return dtTicks.Rows.Count;
        }
    }
}
