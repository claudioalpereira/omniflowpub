using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OFpub
{
    class OFSock
    {
        private string SERVER_IP;
        private int SERVER_PORT;
        private string USER_IMEI;
        private string USER_NAME;
        private string USER_PASS;
        private Socket sock;

        public OFSock()
        {
            // TODO: read from conf file
            SERVER_IP = "62.28.231.130";
            SERVER_PORT = 19000;
            USER_IMEI = "357976063980593";
            USER_NAME = "Siemens";
            USER_PASS = "Omni2016";

            Console.WriteLine("Instatiating socket...");
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ReconnectSocket();
        }

        ~OFSock()
        {
            sock.Dispose();
        }

        private void ReconnectSocket()
        {
            byte[] received = new byte[1024 * 1024];

            if (!sock.Connected)
            {
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
    }
}
