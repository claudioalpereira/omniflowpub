using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        private string SERVER_IP;
        private int SERVER_PORT;
        private string USER_IMEI;
        private string USER_NAME;
        private string USER_PASS;
        private Socket stateSock;
        //private Socket instSock;
        //private Socket confSock;
        //private Socket measuresSock;
        //private Socket putSock;
        
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static OFWrapper()
        {
        }

        private OFWrapper()
        {
            SERVER_IP = "62.28.231.130";
            SERVER_PORT = 19000;
            USER_IMEI = "357976063980593";
            USER_NAME = "Siemens";
            USER_PASS = "Omni2016";

            Console.WriteLine("Putting on the socks...");
            stateSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //instSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //confSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //measuresSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //putSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConnectSocket(stateSock);
            //ConnectSocket(instSock);
            //ConnectSocket(confSock);
            //ConnectSocket(measuresSock);
            //ConnectSocket(putSock);
        }

        ~OFWrapper()
        {

        }

        public static OFWrapper Instance
        {
            get { return instance; }
        }

        public string GetState()
        {
            return SocketQuery(stateSock, "+getstate");
            
        }
        public string GetInstData()
        {
            return SocketQuery(stateSock, "+getinstdata");

        }
        public void UpdateInstData()
        {
            SocketQuery(stateSock, "+updateinst");

        }
        public string GetConfData()
        {
            return SocketQuery(stateSock, "+getconfdata");

        }
        public void UpdateConfData()
        {
            SocketQuery(stateSock, "+updateconf");

        }
        public string GetMeasures(DateTime? from = null, DateTime? to = null)
        {
            from = from ?? DateTime.Now.AddDays(-10);
            to = to ?? DateTime.Now;

            return SocketQuery(stateSock, string.Format("+getmeasures[{0:yyyy-MM-dd},{1:yyyy-MM-dd}]", from, to)).Trim('"');
        }

        public void PutValue(string key, string value)
        {
            SocketQuery(stateSock, string.Format("+put[{0},{1}]", key, value));
            UpdateInstData();
            UpdateConfData();
        }


        private void ConnectSocket(Socket sock)
        {
            if (!sock.Connected)
            {
                byte[] received = new byte[1024 * 1024];

                Console.WriteLine("Connecting socket to {0}:{1}", SERVER_IP, SERVER_PORT);
                sock.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT));
                if (sock.Connected)
                {
                    Console.WriteLine("Socket Connected!");
                }
                else
                {
                    Console.Error.WriteLine("Cannot connect to socket!!!");
                    return;
                }

                Console.WriteLine("Login in at OF server...");
                sock.Send(Encoding.ASCII.GetBytes("+login[" + USER_IMEI + " , " + USER_NAME + " , " + USER_PASS + "]"));

                int bytesReceived = sock.Receive(received);
                Console.WriteLine("Logged on server: ", Encoding.ASCII.GetString(received, 0, bytesReceived));
            }
        }

        /// <summary>
        /// Sends a message to the server and wait for aresponse.
        /// Used for retrieving data e.g. State, InstData, etc...
        /// </summary>
        /// <param name="sock">Socket</param>
        /// <param name="request">request command (e.g. +getconfdata)</param>
        /// <returns></returns>
        public string SocketQuery(Socket sock, string request)
        {
            byte[] bytes = new byte[1024 * 1024];
            string ret = null;

            lock (sock)
            {
                try
                {
                    try
                    {
                        int bytesSent = sock.Send(Encoding.ASCII.GetBytes(request));

                        int bytesRec = sock.Receive(bytes);

                        ret = Encoding.ASCII.GetString(bytes, 0, bytesRec);
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
            

            return ret;
        }

        /// <summary>
        /// Sends a message to the server and doesn't wait for a response.
        /// Used for PUT and UPDATE commands.
        /// </summary>
        /// <param name="sock">Socket</param>
        /// <param name="request">Request command (e.g. +updateconf)</param>
        public void SocketSend(Socket sock, string request)
        {
            byte[] bytes = new byte[1024 * 1024];

            try
            {
                try
                {
                    int bytesSent = sock.Send(Encoding.ASCII.GetBytes(request));
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
    }
}
