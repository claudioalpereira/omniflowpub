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
        // OmniFlow Server
        //const string OF_IP = "62.28.231.130";
        //const string OF_IP = "test.mosquitto.org";
        //const int OF_PORT = 19000;
        //const string OF_IMEI = "357976063980593";
        //const string OF_USER = "Siemens";
        //const string OF_PASS = "Omni2016";
        static int _instdataRefreshRate =       5 * 60 * 1000; // defaults to 5 minutes
        static int _confdataRefreshRate =  1 * 60 * 60 * 1000; // defaults to 1 hour
        static int _measuresRefreshRate = 24 * 60 * 60 * 1000; // defaults to 1 day
        //static OFAPI of;
        static OFWrapper of;

        // MQTT Server
        static string _brokerUrl = "52.39.125.106";
        const string TOPIC_INSTDATA = "clealp/sie/of/instdata";
        const string TOPIC_CONFDATA = "clealp/sie/of/confdata";
        const string TOPIC_MEASURES = "clealp/sie/of/measures";
        const string TOPIC_PUT = "clealp/sie/of/put";
        const string CLIENT_ID = "ofwrapper";
        const string TOPIC_CMD = "clealp/sie/of/cmd/"+CLIENT_ID;
        static MqttClient client;
       // static string _instdata = "";
        //static string _constdata = "";

        static void Main(string[] args)
        {
            if (args.Length > 0)
                _brokerUrl = args[0];

            // MQTT bootstrap
            client = new MqttClient(_brokerUrl);
            // DEBUG:
            client.Subscribe(new[] { TOPIC_PUT, TOPIC_CMD }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            client.MqttMsgPublishReceived += ClientRecievedMessage;

            client.Connect(Guid.NewGuid().ToString());

            Console.WriteLine("connected:"+ client.IsConnected);


            // Omniflow bootstrap
            of = OFWrapper.Instance;
            // So the first get, gets the 
            of.UpdateInstData();
            Thread.Sleep(10*1000); // As stated on the OmniflowAPI doc

            //INST DATA
            //
            //TODO: bloquear a thread em vez de fazer sleep para poupar recursos
            var t1 = Task.Run(() =>
            {
                do
                {
                    try
                    {
                        Console.WriteLine("publishing instdata");

                        var instdata = of.GetInstData();

                        client.Publish(TOPIC_INSTDATA, Encoding.UTF8.GetBytes(instdata), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                        Console.WriteLine("[{0}] published instdata", DateTime.UtcNow);

                        of.UpdateInstData();
                        Thread.Sleep(_instdataRefreshRate);
                    }
                    catch (Exception ex)
                    {
                        // debaixodotapetator pattern
                        Console.WriteLine("EXCEPTION:\n" + ex.Message);
                    }
                } while (true);

            });

            var t2 = Task.Run(() =>
            {
                do
                {
                    try
                    {
                        Console.WriteLine("publishing confdata");

                        of.UpdateConfData();
                        Thread.Sleep(10*1000); // As stated on the OmniflowAPI doc

                        var data = of.GetConfData();

                        client.Publish(TOPIC_CONFDATA, Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                        Console.WriteLine("[{0}] published confdata", DateTime.UtcNow);


                        Thread.Sleep(_confdataRefreshRate);
                    }
                    catch (Exception ex)
                    {
                        // debaixodotapetator pattern
                        Console.WriteLine("EXCEPTION:\n" + ex.Message);
                    }
                } while (true);

            });

            //CONF DATA
            //
            //TODO: bloquear a thread em vez de fazer sleep para poupar recursos
            //var t2 = Task.Run(() =>
            //{
            //    var ofapi = new OFAPI(OF_IP, OF_PORT, OF_IMEI, OF_USER, OF_PASS);
            //    do
            //    {
            //        try
            //        {
            //            client.Publish(TOPIC_CONFDATA, Encoding.UTF8.GetBytes(ofapi.GetConfData()), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            //            ofapi.UpdateConfData();
            //            Console.WriteLine("published confdata");
            //            Thread.Sleep(_confdataRefreshRate);
            //        }
            //        catch (Exception ex)
            //        {
            //            // debaixodotapetator pattern
            //            Console.WriteLine("EXCEPTION:\n" + ex.Message);
            //        }
            //    } while (true);

            //});

            ////MEASURES
            ////
            ////TODO: bloquear a thread em vez de fazer sleep para poupar recursos
            //var t3 = Task.Run(() =>
            //{
            //    var ofapi = new OFAPI(OF_IP, OF_PORT, OF_IMEI, OF_USER, OF_PASS);
            //    do
            //    {
            //        try
            //        {
            //            client.Publish(TOPIC_MEASURES, Encoding.UTF8.GetBytes(ofapi.GetMeasures()), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            //            Console.WriteLine("published measures");
            //            Thread.Sleep(_measuresRefreshRate);
            //        }
            //        catch (Exception ex)
            //        {
            //            // debaixodotapetator pattern
            //            Console.WriteLine("EXCEPTION:\n" + ex.Message);
            //        }
            //    } while (true);

            //});

            Task.WaitAll(t1, t2);    
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



        //private class OFAPI
        //{
        //    private string SERVER_IP;
        //    private int SERVER_PORT;
        //    private string USER_IMEI;
        //    private string USER_NAME;
        //    private string USER_PASS;

        //    public OFAPI(string serverIP, int serverPort, string imei, string user, string pass)
        //    {
        //        SERVER_IP = serverIP;
        //        SERVER_PORT = serverPort;
        //        USER_IMEI = imei;
        //        USER_NAME = user;
        //        USER_PASS = pass;
        //    }

        //    public string GetInstData()
        //    {
        //        Console.WriteLine("getting inst data from omniflow...");
        //        var r =  QueryServer("+getinstdata");
        //        Console.WriteLine("received inst data from omniflow");
        //        return r;
        //    }

        //    public void UpdateInstData()
        //    {
        //        QueryServer("+updateinst");
        //    }

        //    public string GetConfData()
        //    {
        //        return QueryServer("+getconfdata");
        //    }

        //    public void UpdateConfData()
        //    {
        //        QueryServer("+updateconf");
        //    }

        //    public string GetState()
        //    {
        //        return QueryServer("+getstate");
        //    }

        //    public string GetMeasures(DateTime? from = null, DateTime? to = null)
        //    {
        //        from = from ?? DateTime.Now.AddDays(-10);
        //        to = to ?? DateTime.Now;

        //        return QueryServer(string.Format("+getmeasures[{0:yyyy-MM-dd},{1:yyyy-MM-dd}]", from, to)).Trim('"');
        //    }

        //    public string QueryServer(string request)
        //    {
        //        byte[] bytes = new byte[1024 * 1024];
        //        string ret = null;

        //        try
        //        {
        //            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT);

        //            Socket sok = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //            try
        //            {
        //                sok.Connect(ipep);

        //                byte[] msg = Encoding.ASCII.GetBytes("+login[" + USER_IMEI + " , " + USER_NAME + " , " + USER_PASS + "]");

        //                int bytesSent = sok.Send(msg);

        //                int bytesRec = sok.Receive(bytes);

        //                bytesSent = sok.Send(Encoding.ASCII.GetBytes(request));

        //                bytesRec = sok.Receive(bytes);


        //                sok.Shutdown(SocketShutdown.Both);
        //                sok.Close();

        //                ret = Encoding.ASCII.GetString(bytes, 0, bytesRec);
        //            }
        //            catch (ArgumentNullException ane)
        //            {
        //                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());

        //            }
        //            catch (SocketException se)
        //            {
        //                Console.WriteLine("SocketException : {0}", se.ToString());

        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine("Unexpected exception : {0}", e.ToString());
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine(e.ToString());
        //        }

        //        return ret;
        //    }
        //}
        static void ClientRecievedMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = System.Text.Encoding.Default.GetString(e.Message);
            //System.Console.WriteLine("Message received: " + message);

            switch (e.Topic)
            {
                case TOPIC_PUT:
                    var tt = message.Replace("{", string.Empty).Replace("}", string.Empty).Split(':');
                    var key = tt[0];
                    var value = tt[1];
                    Task.Run(() =>
                    {
                        of.PutValue(key, value);
                        Console.WriteLine("put " + key + " " + value);
                       // of.UpdateConfData();
                        Thread.Sleep(10 * 1000); // wait 10s, as stated in API doc
                        client.Publish(TOPIC_CONFDATA, Encoding.UTF8.GetBytes(of.GetConfData()), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                    });
                    break;
                default:

                    break;
            }
        }
    }
   
}

