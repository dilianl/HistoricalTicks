using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigurationsManager;

namespace ConvRatesWinService
{
    public class ConvRatesProvider
    {
        private static ConvRatesProvider convRatesProvider;
        private static object lockObj = new object();
        private static SymbolsTickManager tickManager;
        private const double RATE_EURBGN = 1.95583;

        private ConvRatesProvider()
        {
            tickManager = new SymbolsTickManager();
            tickManager.LoadTicks();
        }


        public static ConvRatesProvider GetInstance()
        {
            if (convRatesProvider == null)
            {
                lock (lockObj)
                {
                    if (convRatesProvider == null)
                    {
                        convRatesProvider = new ConvRatesProvider();
                    }
                }
            }

            return convRatesProvider;
        }


        public void Init()
        {
            GetInstance();
        }


        public double GetConvRate(string curr1, string curr2)
        {
            decimal rate = 0;

            if (curr1.ToUpper() == curr2.ToUpper())
            {
                return 1;
            }

            if (curr1 == "BGN" || curr2 == "BGN")
            {
                return GetBGNConvRate(curr1, curr2);
            }

            string symbol = curr1 + curr2;

            rate = tickManager.GetConvRate(symbol, DateTime.Now);

            if (rate == 0)
            {
                decimal rate1 = tickManager.GetConvRate(curr1 + "USD", DateTime.Now);
                decimal rate2 = tickManager.GetConvRate(curr2 + "USD", DateTime.Now);

                if (rate1 == 0)
                {
                    throw new ArgumentException("Missing symbol: " + curr1 + "USD");
                }
                if (rate2 == 0)
                {
                    throw new ArgumentException("Missing symbol: " + curr2 + "USD");
                }

                rate = rate1 / rate2;
            }

            return Convert.ToDouble(rate);
        }


        private double GetBGNConvRate(string curr1, string curr2)
        {
            double result = double.NaN;

            if (curr1 == "BGN")
            {
                result = (1 / RATE_EURBGN) * GetConvRate("EUR", curr2);
            }
            else
            {
                result = RATE_EURBGN * GetConvRate(curr1, "EUR");
            }

            return result;
        }
    }
}
