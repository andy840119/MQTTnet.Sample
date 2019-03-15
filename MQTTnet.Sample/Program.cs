using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Sample
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

            //Run a MQTT publish client
            var publishClient = new MqttFactory().CreateMqttClient();
            await publishClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());

            //Run a MQTT receive client
            var receniveClient = new MqttFactory().CreateMqttClient();
            await receniveClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());
            receniveClient.ApplicationMessageReceived += (object o, MqttApplicationMessageReceivedEventArgs e) =>
            { 
                //Record received message.
                var receiveBytes = e.ApplicationMessage.Payload;
                var receiveMessage = Encoding.UTF8.GetString(receiveBytes);
                
                Console.WriteLine(receiveMessage);
            };

            //Receive client subscribe a topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(quality).Build());

            while(true)
            { 
                Console.WriteLine("Type any message to send message from publisher to subscripter...");
                string line = Console.ReadLine();

                Console.WriteLine("Message sent : " + line);
                await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = topic,
                    QualityOfServiceLevel = quality,
                    Payload = Encoding.UTF8.GetBytes(line),
                });

                await Task.Delay(500);
            }
        }
    }
}
