using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OFpub
{
    //http://csharpindepth.com/Articles/General/Singleton.aspx
    /// <summary>
    /// Singleton class that wraps all Omniflow API communication
    /// </summary>
    /// 
    public sealed class OFWrapper
    {
        private static readonly OFWrapper instance = new OFWrapper();
        public static OFWrapper Instance
        {
            get
            {
                return instance;
            }
        }
           
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static OFWrapper()
        {
        }

        private string SERVER_IP;
        private int SERVER_PORT;
        private string USER_IMEI;
        private string USER_NAME;
        private string USER_PASS;
        private Socket sok;

        private OFWrapper()
        {
            // TODO: read from conf file
            SERVER_IP = "62.28.231.130";
            SERVER_PORT = 19000;
            USER_IMEI = "357976063980593";
            USER_NAME = "Siemens";
            USER_PASS = "Omni2016";

            Console.WriteLine("Instatiating socket...");
            sok = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ReconnectSocket();
        }
        ~OFWrapper()
        {
            sok.Dispose();
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

        private void ReconnectSocket()
        {
            byte[] received = new byte[1024 * 1024];

          if(!sok.Connected)
            {
                Console.WriteLine("Connecting socket to {0}:{1}", SERVER_IP, SERVER_PORT);
                sok.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT));
                if (sok.Connected)
                {
                    Console.WriteLine("Socket Connected!");
                }
                else
                {
                    Console.Error.WriteLine("Cannot connect to socket!!!");
                }
           
                Console.WriteLine("Login in at OF server...");
                sok.Send(Encoding.ASCII.GetBytes("+login[" + USER_IMEI + " , " + USER_NAME + " , " + USER_PASS + "]"));

                int bytesReceived = sok.Receive(received);
                Console.WriteLine("Received from server: ", Encoding.ASCII.GetString(received, 0, bytesReceived));
            }
        }

        public string QueryServer(string request)
        {
            byte[] bytes = new byte[1024 * 1024];
            string ret = null;

//            if (!sok.Connected)
//                ReconnectSocket();

            sok.Send(Encoding.ASCII.GetBytes(request));

            int bytesRec = sok.Receive(bytes);
            
            //sok.Shutdown(SocketShutdown.Both);
            //sok.Close();

            ret = Encoding.ASCII.GetString(bytes, 0, bytesRec);
               
            return ret;
        }
    }
}
