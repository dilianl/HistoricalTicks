using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Markets.DataContracts;
using Markets.Enums;
using Markets.ServiceContracts;

namespace ConvRatesWinService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, IncludeExceptionDetailInFaults = true)]
    public class ConvRatesService : IConvRate
    {
        private readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public double GetConvRate(string curr1, string curr2)
        {
            log.Info(String.Format("GetConvRate for currencies {0} and {1} starts.", curr1, curr2));
            double rate = 0;

            try
            {
                rate = ConvRatesProvider.GetInstance().GetConvRate(curr1, curr2);
            }
            catch (Exception e)
            {
                log.Error(String.Format("GetConvRate error for currencies {0} and {1}", curr1, curr2), e);
                throw;
            }

            log.Info(String.Format("GetConvRate for currencies {0} and {1} ends.", curr1, curr2));

            return rate;
        }


        public double GetConvRate(string curr1, string curr2, DateTime date, EnMtPeriod period, EnMtRatePriceType priceType)
        {
            throw new NotImplementedException();
        }

        
        public double GetConvRateHistorical(string curr1, string curr2, DateTime date)
        {
            throw new NotImplementedException();
        }

        public double GetConvRateHistoricalHourly(string currency1, string currency2, DateTime date)
        {
            throw new NotImplementedException();
        }

        public DcBar GetBarInfo(string symbol, DateTime date)
        {
            throw new NotImplementedException();
        }

        public DcSymbolPrice[] GetAllSymbols()
        {
            throw new NotImplementedException();
        }


        public DcBar[] GetBarInfos(System.Collections.Generic.IEnumerable<string> symbol, string pathRegex, DateTime date)
        {
            throw new NotImplementedException();
        }


        public DcBar[] GetBarInfosWoCheck(System.Collections.Generic.IEnumerable<string> symbols, DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}
