using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Sample.Tests
{
    [TestClass]
    public class SampleTest
    {
        protected string ServerAddress => "localhost";

        [TestMethod]
        public async Task Sample()
        {
            //Create a topic and connect quality
            var topic = "andy840119/iot";
            var quality = MqttQualityOfServiceLevel.AtLeastOnce;
            var sendByte = Encoding.UTF8.GetBytes("Hello MQTT!");
            var shouldNotSendByte = Encoding.UTF8.GetBytes("Don't receive MQTT!");
            
            //Receive Message(check)
            var receiveMessage = new byte[0];
            var sendTries = 0;

            //Start and run a server
            var server = new MqttFactory().CreateMqttServer();
            await server.StartAsync(new MqttServerOptions());

            //Create a publish client
            var publishClient = new MqttFactory().CreateMqttClient();
            await publishClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());

            //Create a receive client
            var receniveClient = new MqttFactory().CreateMqttClient();
            await receniveClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());
            receniveClient.ApplicationMessageReceived += (object o, MqttApplicationMessageReceivedEventArgs e) =>
            { 
                receiveMessage = e.ApplicationMessage.Payload;
                sendTries ++;
            };

            //Receive client subscribe the topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(quality).Build());

            //Publish Client send a message
            await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = topic,
                    QualityOfServiceLevel = quality,
                    Payload = sendByte,
                });

            await Task.Delay(500);

            //send message and receive message should be equal
            Assert.IsTrue(Equality(sendByte,receiveMessage));

            //UnSubscribe topic
            await receniveClient.UnsubscribeAsync(topic);

            await Task.Delay(500);

            //Publish Client send a message (this message should not be received.)
            await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = topic,
                    QualityOfServiceLevel = quality,
                    Payload = shouldNotSendByte,
                });

            await Task.Delay(500);

            //Check not received
            Assert.IsFalse(Equality(shouldNotSendByte,receiveMessage));
        }

        /// <summary>
        /// Check Bytes array are equal.
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="b1"></param>
        /// <returns></returns>
        public bool Equality(byte[] a1, byte[] b1)
        {
           int i;
           if (a1.Length == b1.Length)
           {
              i = 0;
              while (i < a1.Length && (a1[i]==b1[i])) //Earlier it was a1[i]!=b1[i]
              {
                  i++;
              }
              if (i == a1.Length)
              {
                  return true;
              }
           }

           return false;
        }
    }
}
