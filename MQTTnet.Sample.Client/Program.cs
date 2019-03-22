using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Sample.Client
{
    class Program
    {
        //Note : change IP if needed
        protected static string ServerAddress => "localhost";

        static async Task Main(string[] args)
        {
            //Sending properties
            var quality = MqttQualityOfServiceLevel.AtLeastOnce;

            //Run a MQTT publish client
            var publishClient = new MqttFactory().CreateMqttClient();
            await publishClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());

            while(true)
            { 
                Console.WriteLine("Type any message to send message from publisher to subscripter...");
                string line = Console.ReadLine();

                //Note : send binary file
                if (line == "-b")
                {
                    var file = File.ReadAllBytes("Resource/sample_image_001.jpg");
                    Console.WriteLine("Binart file sent : ");
                    await publishClient.PublishAsync(new MqttApplicationMessage()
                    {
                        Topic = "andy840119/iot_binary",
                        QualityOfServiceLevel = quality,
                        Payload = file,
                    });
                }
                else
                {
                    Console.WriteLine("Message sent : " + line);
                    await publishClient.PublishAsync(new MqttApplicationMessage()
                    {
                        Topic = "andy840119/iot",
                        QualityOfServiceLevel = quality,
                        Payload = Encoding.UTF8.GetBytes(line),
                    });
                }
            }
        }
    }
}
