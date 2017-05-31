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
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


namespace OFpub
{
    /// <summary>
    /// 
    /// </summary>
    /// <todo>
    /// - usar config file em vez de hardcoded configs
    /// - aceitar configs como args da linha de comandos
    /// </todo>
    class Program
    {
 
        static int _instdataRefreshRate =       5 * 60 * 1000; // defaults to 5 minutes
        static int _confdataRefreshRate =  1 * 60 * 60 * 1000; // defaults to 1 hour
        static int _measuresRefreshRate = 24 * 60 * 60 * 1000; // defaults to 1 day
   
        static OFWrapper of;

        // MQTT Server
        static string _brokerUrl = "52.39.125.106";
        const string TOPIC_INSTDATA = "clealp/sie/of/instdata";
        const string TOPIC_CONFDATA = "clealp/sie/of/confdata";
        const string TOPIC_MEASURES = "clealp/sie/of/measures";
        const string TOPIC_PUT = "clealp/sie/of/put";
        const string CLIENT_ID = "ofwrapper_TEST";
        const string TOPIC_CMD = "clealp/sie/of/cmd/"+CLIENT_ID;
        static MqttClient client;

        static ConfDataClass _confDataObj;

        static void Main(string[] args)
        {
            if (args.Length > 0)
                _brokerUrl = args[0];

            // MQTT bootstrap
            client = new MqttClient(_brokerUrl);
            // DEBUG:
            client.Subscribe(new[] { TOPIC_PUT, TOPIC_CMD, TOPIC_CONFDATA }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            client.MqttMsgPublishReceived += ClientRecievedMessage;

            client.Connect(Guid.NewGuid().ToString());
            client.Publish("clealp/sie/of/test",Encoding.UTF8.GetBytes("olá"));
            //Console.WriteLine("connected:"+ client.IsConnected);


            //// Omniflow bootstrap
            //of = OFWrapper.Instance;
            //// So the first get, gets the 
            //of.UpdateInstData();
            //Thread.Sleep(10*1000); // As stated on the OmniflowAPI doc

            ////INST DATA
            ////
            ////TODO: bloquear a thread em vez de fazer sleep para poupar recursos
            //var t1 = Task.Run(() =>
            //{
            //    do
            //    {
            //        try
            //        {
            //            Console.WriteLine("publishing instdata");

            //            var instdata = of.GetInstData();

            //            client.Publish(TOPIC_INSTDATA, Encoding.UTF8.GetBytes(instdata), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            //            Console.WriteLine("[{0}] published instdata", DateTime.UtcNow);

            //            of.UpdateInstData();
            //            Thread.Sleep(_instdataRefreshRate);
            //        }
            //        catch (Exception ex)
            //        {
            //            // debaixodotapetator pattern
            //            Console.WriteLine("EXCEPTION:\n" + ex.Message);
            //        }
            //    } while (true);

            //});

            //var t2 = Task.Run(() =>
            //{
            //    do
            //    {
            //        try
            //        {
            //            Console.WriteLine("publishing confdata");

            //            of.UpdateConfData();
            //            Thread.Sleep(10*1000); // As stated on the OmniflowAPI doc

            //            var data = of.GetConfData();

            //            client.Publish(TOPIC_CONFDATA, Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            //            Console.WriteLine("[{0}] published confdata", DateTime.UtcNow);


            //            Thread.Sleep(_confdataRefreshRate);
            //        }
            //        catch (Exception ex)
            //        {
            //            // debaixodotapetator pattern
            //            Console.WriteLine("EXCEPTION:\n" + ex.Message);
            //        }
            //    } while (true);

            //});

            //Task.WaitAll(t1, t2);    

            Thread.Sleep(60000);
        }

        private static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            throw new NotImplementedException();
        }

        private class MyMQTT
        {
            private string _server;
            private int _port;

            public MyMQTT(string server, int port)
            {
                _server = server;
                _port = port;
               
            }
        }

        static void ClientRecievedMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = System.Text.Encoding.Default.GetString(e.Message);
            //System.Console.WriteLine("Message received: " + message);

            switch (e.Topic)
            {
                case TOPIC_PUT:

                    var newConfData = CreateConfDataClass(message);
                    UpdateV(newConfData.V1, newConfData.V2, newConfData.V3);

                    Task.Run(() =>
                    {
                        //of.PutValue(key, value);
                        //Console.WriteLine("put " + key + " " + value);
                       // of.UpdateConfData();
                        //Thread.Sleep(20 * 1000); // wait 10s, as stated in API doc
                        client.Publish(TOPIC_CONFDATA, Encoding.UTF8.GetBytes(of.GetConfData()), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                    });
                    break;

                case TOPIC_CONFDATA:
                    lock(_confDataObj)
                    {
                        _confDataObj = CreateConfDataClass(message);
                    }

                    break;
                default:

                    break;
            }
        }

        private static void UpdateV(int? v1, int? v2, int? v3)
        {
            
            if (v1 != null && v1 > _confDataObj.V2)
            {
                of.PutValue("V1", v1.Value);
                // of.UpdateConfData();
                Thread.Sleep(20 * 1000);
                v1 = null;
            }
            if (v2 != null && v2 > _confDataObj.V3 && v2 < _confDataObj.V1)
            {
                of.PutValue("V2", v2.Value);
                // of.UpdateConfData();
                Thread.Sleep(20 * 1000);
                v2 = null;
            }
            if (v3 != null && v3 < _confDataObj.V2)
            {
                of.PutValue("V3", v3.Value);
                // of.UpdateConfData();
                Thread.Sleep(20 * 1000);
                v3 = null;
            }

            if (v1 == null && v2 == null && v3 == null)
                return;
            else
                UpdateV(v1, v2, v3);
        }

        private static Dictionary<string, int> ParseConfData(string cf)
        {
            return cf.Replace("{", string.Empty)
            .Replace("}", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(part => part.Split(':'))
               .ToDictionary(split => split[0].Replace(" ", string.Empty), split => int.Parse(split[1].Replace(" ", string.Empty)));
        }

        private static ConfDataClass CreateConfDataClass(string s)
        {
            var ret = new ConfDataClass();

            var d = ParseConfData(s);
            
            if (d.ContainsKey("T1")) ret.V1 = d["T1"];
            if (d.ContainsKey("T2")) ret.V1 = d["T2"];
            if (d.ContainsKey("T3")) ret.V1 = d["T3"];
            if (d.ContainsKey("V1")) ret.V1 = d["V1"];
            if (d.ContainsKey("V2")) ret.V1 = d["V2"];
            if (d.ContainsKey("V3")) ret.V1 = d["V3"];
            if (d.ContainsKey("DeltaV1")) ret.V1 = d["DeltaV1"];
            if (d.ContainsKey("DeltaV2")) ret.V1 = d["DeltaV2"];
            if (d.ContainsKey("DeltaV3")) ret.V1 = d["DeltaV3"];
            if (d.ContainsKey("POn")) ret.V1 = d["POn"];
            if (d.ContainsKey("PPir")) ret.V1 = d["PPir"];
            if (d.ContainsKey("P1")) ret.V1 = d["P1"];
            if (d.ContainsKey("P2")) ret.V1 = d["P2"];

            return ret;
        }

        private class ConfDataClass
        {
            public int T1 { get; set; } // min:15   max:120
            public int T2 { get; set; } // min:5    max:60
            public int T3 { get; set; } // min:10   max:2000

            public int V1 { get; set; } // min:V2   max:16500
            public int V2 { get; set; } // min:V3   max:V1
            public int V3 { get; set; } // min:900   max:V2

            public int DeltaV1 { get; set; } // min:100   max:1500
            public int DeltaV2 { get; set; } // min:100   max:1500
            public int DeltaV3 { get; set; } // min:100   max:1500

            public int POn { get; set; } // min:PPir   max:100
            public int PPir { get; set; } // min:P1   max:POn
            public int P1 { get; set; } // min:P2   max:PPir
            public int P2 { get; set; } // min:0   max:P1

            public DateTime UpdateConfDateTime { get; set; }


        }
    }
   
}


    */
