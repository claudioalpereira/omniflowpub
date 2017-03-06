using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;

namespace OFpub
{
    class Program
    {
        const string BROKER_URL = "test.mosquitto.org";
        const string TOPIC = "clealp/sie/of";

        static void Main(string[] args)
        {
            MqttClient client = new MqttClient("test.mosquitto.org");

            client.Connect(Guid.NewGuid().ToString());

            client.Publish(TOPIC, Encoding.UTF8.GetBytes("ailo"));            
        }
    }


    static class OFAPI
    {
        const string SERVER_IP = "62.28.231.130";
        const int SERVER_PORT = 19000;
        const string USER_IMEI = "357976063980593";
        const string USER_NAME = "Siemens";
        const string USER_PASS = "Omni2016";


        private string GetInstData()
        {
            return QueryServer("+getinstdata");
        }

        private void UpdateInstData()
        {
            return QueryServer("+updateinst");
        }

        public string GetConfData()
        {
            return QueryServer("+getconfdata");
        }

        public void UpdateConfData()
        {
            return QueryServer("+updateconf");
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

        public static string QueryServer(string request)
        {
            byte[] bytes = new byte[1024 * 1024];
            string ret = null;

            try
            {
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT);

                Socket sok = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sok.Connect(ipep);

                    byte[] msg = Encoding.ASCII.GetBytes("+login[" & USER_IMEI & " , " & USER_NAME & " , " & USER_PASS & "]");

                    int bytesSent = sok.Send(msg);

                    int bytesRec = sok.Receive(bytes);

                    bytesSent = sok.Send(Encoding.ASCII.GetBytes(request));

                    bytesRec = sok.Receive(bytes);


                    sok.Shutdown(SocketShutdown.Both);
                    sok.Close();

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

            return ret;
        }
    }

}
}
