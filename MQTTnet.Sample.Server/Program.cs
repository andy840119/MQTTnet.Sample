using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Sample.Server
{
    class Program
    {
        protected static string ServerAddress => "localhost";

        static async Task Main(string[] args)
        {
            //Sending properties
            var topic = "andy840119/iot";
            var quality = MqttQualityOfServiceLevel.AtLeastOnce;

            //Run a MQTT Server
            var server = new MqttFactory().CreateMqttServer();
            await server.StartAsync(new MqttServerOptions());
            server.ClientConnected += (a,b)=>
            {
                Console.WriteLine($"Client connect ({b.ClientId}) :");
            };
            server.ClientDisconnected += (a,b)=>
            {
                Console.WriteLine($"Client disconnect ({b.ClientId}) :");
            };

            //Run a MQTT receive client
            var receiveCount = 0;
            var receniveClient = new MqttFactory().CreateMqttClient();
            await receniveClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());
            receniveClient.ApplicationMessageReceived += (object o, MqttApplicationMessageReceivedEventArgs e) =>
            { 
                //Record received message.
                var receiveBytes = e.ApplicationMessage.Payload;
                var receiveMessage = Encoding.UTF8.GetString(receiveBytes);

                Console.WriteLine($"Message received ({receiveCount}) :");
                Console.WriteLine(receiveMessage);

                receiveCount ++;
            };

            //Receive client subscribe a topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(quality).Build());

            //Just prevent console close
            while(true)
            { 
                await Task.Delay(10);
            }
        }
    }
}
