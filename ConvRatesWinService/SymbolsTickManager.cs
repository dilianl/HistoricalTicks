using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ConfigurationsManager;
using ConfigurationsManager.ConfigInfrastructure;
using HistoricalTicksRepository;
using Markets.DataTypes;
using System.Collections;
using log4net;
using LatestSymbolsClient;
using System.Collections.Concurrent;

namespace ConvRatesWinService
{
    public class SymbolsTickManager
    {
        private static ConcurrentDictionary<string, LatestSymbolTick> dictLatestTicks = new ConcurrentDictionary<string, LatestSymbolTick>();
        private static List<string> supportedSymbols = new List<string>();
        private static object lockObj = new object();
        private IHistoricalTicksRepository ticksRepository;
        private static ITradingInstrumentsRepository instrumentsRepository;
        private readonly Timer instrumentsRefreshTimer = new Timer(OnInstrumentsRefresh_Elapsed);

        private static Configurations mainConfig;
        private static ConvRatesServiceConfiguration config;
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        

        internal SymbolsTickManager()
        {
            mainConfig = ConfigurationsManager.ConfigurationsManager.GetConfigurations();
            config = mainConfig.ConvRatesServiceConfiguration;
            ticksRepository = new HistoricalTicksDBRepository(mainConfig.HistoricalTicksConfiguration.HistoricalTicksConnString);
            instrumentsRepository = new TradingInstrumentsDBRepository(mainConfig.BackOfficeConfiguration.TradingInstrumentsManagementConnectionString);

            long instrumentsRefreshTimerPeriodMsec = Convert.ToInt64(config.SupportedSymbolsRefreshPeriodMin) * 60000;
            instrumentsRefreshTimer.Change(0, instrumentsRefreshTimerPeriodMsec);

            registerLatestSymbolsProviders();
        }


        public void LoadTicks()
        {
            supportedSymbols = instrumentsRepository.GetSupportedForexSymbols();
            lock (lockObj)
            {
                dictLatestTicks = new ConcurrentDictionary<string,LatestSymbolTick>(ticksRepository.GetLatestSymbolTicks(supportedSymbols));
                AddReverseSymbols();
            }
        }


        public void RefreshTicks(Dictionary<string, LatestSymbolTick> ticks)
        {
            Dictionary<string, LatestSymbolTick> dictNewTicksFiltered = ticks.Where(t => supportedSymbols.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);

            foreach (string symbol in dictNewTicksFiltered.Keys)
            {
                dictLatestTicks[symbol] = dictNewTicksFiltered[symbol];
            }

            AddReverseSymbols();
        }


        public decimal GetConvRate(string symbol, DateTime currTime)
        {
            decimal rate = decimal.Zero;

            if (dictLatestTicks.ContainsKey(symbol))
            {
                DateTime lastTimestamp = dictLatestTicks[symbol].Timestamp;
                TimeSpan timeSpan = currTime - lastTimestamp;

                List<string> listVolatileSymbols = new List<string>(config.VolatileSymbolsList.Split(','));
                int cacheExpireSec = listVolatileSymbols.Contains(symbol) ? Convert.ToInt32(config.VolatileSymbolsCacheExpirationSec) : Convert.ToInt32(config.SymbolsCacheExpirationSec);

                if (timeSpan.TotalSeconds > cacheExpireSec)
                {
                    try
                    {
                        LoadTicks();
                    }
                    catch (TimeoutException e)
                    {
                        //still keeping last successfully refreshed rates
                        log.Error("GetConvRate -> LoadTicks timeout error: ", e);
                    }
                }

                //additional check in case the list with symbols has been refreshed with loadticks
                if (dictLatestTicks.ContainsKey(symbol))
                {
                    rate = dictLatestTicks[symbol].BidValue;
                }
            }

            return rate;
        }


        private void AddReverseSymbols()
        {
            List<string> symbols = dictLatestTicks.Keys.ToList();

            foreach (string symbol in symbols)
            {
                string curr1 = symbol.Substring(0, 3);
                string curr2 = symbol.Substring(3, 3);
                string reverseSymbol = curr2 + curr1;

                dictLatestTicks[reverseSymbol] = new LatestSymbolTick
                {
                    Symbol = reverseSymbol,
                    BidValue = 1 / dictLatestTicks[symbol].BidValue,
                    Timestamp = dictLatestTicks[symbol].Timestamp
                };
            }
        }


        private static void OnInstrumentsRefresh_Elapsed(object target)
        {
            lock (lockObj)
            {
                try
                {
                    supportedSymbols = instrumentsRepository.GetSupportedForexSymbols();
                }
                catch (TimeoutException e)
                {
                    // still keeping last successfully refreshed symbols definition
                    log.Error("OnInstrumentsRefresh_Elapsed -> GetSupportedForexSymbols timeout error: ", e);
                }
            }
        }


        private void registerLatestSymbolsProviders()
        {
            List<string> serviceUrls = new List<string>(mainConfig.HistoricalTicksConfiguration.TicksServicesUrl.Split(','));
            foreach (string url in serviceUrls)
            {
                LatestSymbols provider = new LatestSymbols(url);
                provider.OnReceiveSymbols += provider_OnReceiveSymbols;
                provider.Start();
            }
        }

        private void provider_OnReceiveSymbols(Dictionary<string, LatestSymbolTick> ticks)
        {
            //Console.WriteLine("called: number of ticks provided = " + ticks.Count() + " datetime = " + DateTime.Now);
            RefreshTicks(ticks);
        }
    }
}
