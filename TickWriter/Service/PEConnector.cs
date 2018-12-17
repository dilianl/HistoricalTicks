namespace PPMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using log4net;

    using Platform.General.IO;
    using Platform.Tick.Common.DataTypes;

    //using PPMonitor.Properties;
    
    using Platform.TCPFramework.TcpClients;
    using Platform.TCPFramework.Common;


    internal enum EnConnectorState
    {
        NonInitialized,
        Connecting,
        Connected,
        Disconnected
    }
    internal class PEConnector
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private HashSet<string> loggingSymbols = new HashSet<string>();
        //private HashSet<string> loggingSymbols = string.IsNullOrWhiteSpace(Settings.Default.LoggingSymbols) ? new HashSet<string>() :
        //    new HashSet<string>(Settings.Default.LoggingSymbols.Split(';'));

        private object sync = new object();
        private List<BinaryTick> tickBuffer = new List<BinaryTick>();
        private List<BinaryQuote> quoteBuffer = new List<BinaryQuote>();

        private PacketTcpClient_zmq ppClient = null;
        

        private long msgCount = 0;
        public long MsgCount { get { return msgCount; } }

        public string FullServerName { get { return ppClient == null ? "" : ppClient.FullServerName; } }

        public event EventHandler OnConnected;

        public EnConnectorState State
        {
            get
            {
                if (ppClient == null) return EnConnectorState.NonInitialized;
                if (ppClient.Connected) return EnConnectorState.Connected;
                if (ppClient.Started) return EnConnectorState.Connecting;
                return EnConnectorState.Disconnected;
            }
        }

        private IPEndPoint ppEndPoint = null;
        public IPEndPoint PPEndPoint { get { return ppEndPoint; } }

        public Exception TryParseEndPoint(string endPointText, out IPEndPoint endPoint)
        {
            endPoint = null;
            try
            {
                string[] ppAddressAndPort = endPointText.Split(':');
                IPAddress address = IPAddress.Parse(ppAddressAndPort[0]);
                int port = int.Parse(ppAddressAndPort[1]);
                endPoint = new IPEndPoint(address, port);
                return null;
            }
            catch (Exception ex) { return ex; }
        }

        public Exception CreatePPClient(string newEndPoint)
        {
            IPEndPoint tmpEndPoint = null;
            Exception result = TryParseEndPoint(newEndPoint, out tmpEndPoint);
            if (result != null) 
                return result;
            return CreatePPClient(tmpEndPoint);
        }
        public Exception CreatePPClient(IPEndPoint newEndPoint)
        {
            if (newEndPoint == null) return new ArgumentException("newEndPoint");
            try
            {
                ppClient = new PacketTcpClient_zmq(new BinaryClientStartParams(DataDirection.Receive, newEndPoint, 60, 30, "TickWriter",
                    Mode.Recieve_Sync, 5, 3));
                ppClient.OnNewMessages += new NewMessages(ppClient_OnNewMessages);
                ppClient.OnTcpClientConnected += ppClient_OnTcpClientConnected;
                ppEndPoint = newEndPoint;
                return null;
            }
            catch (Exception ex)
            {
                ppClient = null;
                return ex;
            }
        }

        public Exception StartPPClient()
        {
            try
            {
                if (ppClient != null && !ppClient.Started)
                {
                    ppClient.Start();
                    msgCount = 0;
                }
                return null;
            }
            catch (Exception ex) { return ex; }
        }

        public Exception StopPPClient()
        {
            try
            {
                if (ppClient != null && ppClient.Started)
                    ppClient.Stop();
                return null;
            }
            catch (Exception ex) { return ex; }
        }

        public Exception RestartPPClient(IPEndPoint newEndPoint)
        {
            Exception error = StopPPClient();
            if (error != null) return error;
            error = CreatePPClient(newEndPoint);
            if (error != null) return error;
            return StartPPClient();
        }

        private void ppClient_OnNewMessages(string sender, List<IMembersSerializable> messages)
        {
            msgCount += messages.Count;
            DateTime now = DateTime.Now;
            string sNow = now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            List<BinaryTick> bTicks = messages.OfType<BinaryTick>().ToList();
            bTicks.ForEach(bt =>
            {
                bt.Description += "ppMon:" + sNow;
            });

            List<BinaryQuote> bQuotes = messages.OfType<BinaryQuote>().ToList();
            lock (sync)
            {
                //TODO: can implement max buffer limit
                tickBuffer.AddRange(bTicks);
                quoteBuffer.AddRange(bQuotes);
            }
        }

        void ppClient_OnTcpClientConnected(CommunicationState_zmq state)
        {
            if (OnConnected != null)
                OnConnected(this, EventArgs.Empty);
        }

        private void ppClient_OnTcpClientConnected(CommunicationState state)
        {
            if (OnConnected != null)
                OnConnected(this, EventArgs.Empty);
        }

        public void GetBuffers(out List<Platform.Tick.Common.DataTypes.BinaryTick> ticks, out List<BinaryQuote> quotes)
        {
            ticks = null;
            quotes = null;
            lock (sync)
            {
                ticks = tickBuffer;
                tickBuffer = new List<BinaryTick>();
                quotes = quoteBuffer;
                quoteBuffer = new List<BinaryQuote>();
            }
        }
    }
}
