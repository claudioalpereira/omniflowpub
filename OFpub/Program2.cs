using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;

namespace OFpub
{

    class Program2
    {
        // OmniFlow Server
        const string OF_IP = "62.28.231.130";
        const int OF_PORT = 19000;
        const string OF_IMEI = "357976063980593";
        const string OF_USER = "Siemens";
        const string OF_PASS = "Omni2016";
        static int _instdataRefreshRate = 6 * 1000;       // defaults to 5 minutes
        static int _confdataRefreshRate = 1 * 60 * 60 * 1000;  // defaults to 1 hour
        static int _measuresRefreshRate = 24 * 60 * 60 * 1000; // defaults to 1 day
        static OFAPI of;

        // MQTT Server
        static string _brokerUrl = "test.mosquitto.org";
        const string TOPIC_INSTDATA = "clealp/sie/of/instdata";
        const string TOPIC_CONFDATA = "clealp/sie/of/confdata";
        const string TOPIC_MEASURES = "clealp/sie/of/measures";
        const string TOPIC_PUT = "clealp/sie/of/put";
        const string TOPIC = "clealp/sie/of";
        static MqttClient client;

        public void Main()
        {
            var ofData = new OFData();
            of = new OFAPI(OF_IP, OF_PORT, OF_IMEI, OF_USER, OF_PASS);
            var instCtoken = new CancellationTokenSource();

            var getInstTask = Task.Factory.StartNew((dummy) =>
            {
                string inst;
                while (true)
                {
                    instCtoken.Token.ThrowIfCancellationRequested();
                    inst = of.GetInstData();
                    if (!inst.Equals(ofData.InstDAta))
                    {
                        Task.Run(() => PublishInstData(inst));
                        ofData.InstDAta = inst;
                    }
                    Thread.Sleep(5000);
                }
            }, instCtoken, TaskCreationOptions.LongRunning);
        }
        private void PublishInstData(string instdata)
        {

        }

    }

    // This class is only thread-safe because string is non-mutable. 
    // If used with an object, we'd only protect the access to the object reference.
    // Once a thread has a reference to the object, it can change it outside the lock.
    class OFData
    {

        private string _status;
        private object _statusLock = new object();
        public DateTime StatusUpdate { get; private set; }
        public string Status
        {
            get
            {
                lock (_statusLock)
                {
                    return _status;
                }
            }
            set
            {
                lock (_statusLock)
                {
                    _status = value;
                    StatusUpdate = DateTime.Now;
                }
            }
        }

        private string _instData;
        private object _instDataLock = new object();
        public DateTime InstDataUpdate { get; private set; }
        public string InstDAta
        {
            get
            {
                lock (_instDataLock)
                {
                    return _instData;
                }
            }
            set
            {
                lock (_instDataLock)
                {
                    _instData = value;
                    InstDataUpdate = DateTime.Now;
                }
            }
        }

        private string _confData;
        private object _confDataLock = new object();
        public DateTime ConfDataUpdate { get; private set; }
        public string ConfData
        {
            get
            {
                lock (_confDataLock)
                {
                    return _confData;
                }
            }
            set
            {
                lock (_confDataLock)
                {
                    _confData = value;
                    ConfDataUpdate = DateTime.Now;
                }
            }
        }

        private string _measures;
        private object _measuresLock = new object();
        public DateTime MeasuresUpdate { get; private set; }
        public string Measures
        {
            get
            {
                lock (_measuresLock)
                {
                    return _measures;
                }
            }
            set
            {
                lock (_measuresLock)
                {
                    _measures = value;
                    MeasuresUpdate = DateTime.Now;
                }
            }
        }
    }

    class OFAPI
    {
        private string SERVER_IP;
        private int SERVER_PORT;
        private string USER_IMEI;
        private string USER_NAME;
        private string USER_PASS;
        private byte[] LOGIN_CREDENTIALS;
        private IPEndPoint ENDPOINT;
        private Socket sok;

        public OFAPI(string serverIP, int serverPort, string imei, string user, string pass)
        {
            SERVER_IP = serverIP;
            SERVER_PORT = serverPort;
            USER_IMEI = imei;
            USER_NAME = user;
            USER_PASS = pass;
            LOGIN_CREDENTIALS = Encoding.ASCII.GetBytes("+login[" + USER_IMEI + " , " + USER_NAME + " , " + USER_PASS + "]");
            ENDPOINT = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT);
        }

        public string GetInstData()
        {
            Console.WriteLine("getting inst data from omniflow...");
            var r = QueryServer("+getinstdata");
            Console.WriteLine("received inst data from omniflow");
            return r;
        }

        public void UpdateInstData()
        {
            QueryServer("+updateinst");
        }

        public string GetConfData()
        {
            return QueryServer("+getconfdata");
        }

        public void UpdateConfData()
        {
            QueryServer("+updateconf");
        }

        public string GetState()
        {
            return QueryServer("+getstate");
        }

        public string GetMeasures(DateTime? from = null, DateTime? to = null)
        {
            from = from ?? DateTime.Now.AddDays(-10);
            to = to ?? DateTime.Now;

            return QueryServer(string.Format("+getmeasures[{0:yyyy-MM-dd},{1:yyyy-MM-dd}]", from, to)).Trim('"');
        }

        public void ConnectToBroker()
        {
            byte[] bytes = new byte[1024 * 1024];
            if (sok.Connected)
            {
                return;
            }

            Console.WriteLine("Connecting to broker...");

            try
            {
                sok = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sok.Connect(ENDPOINT);
                    
                    int bytesSent = sok.Send(LOGIN_CREDENTIALS);
                    int bytesRec = sok.Receive(bytes);
             
                    Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());

                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());

                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public string QueryServer(string request)
        {
            if (!sok.Connected)
            {
                ConnectToBroker();
            }

            byte[] bytes = new byte[1024 * 1024];
            string ret = null;

            try
            {
                try
                {
                    int bytesSent = sok.Send(Encoding.ASCII.GetBytes(request));

                    int bytesRec = sok.Receive(bytes);

                    return Encoding.ASCII.GetString(bytes, 0, bytesRec);
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());

                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());

                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return ret;
    }

}

}
