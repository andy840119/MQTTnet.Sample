using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
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
            //Sending properties
            var topic = "andy840119/iot";
            var quality = MqttQualityOfServiceLevel.AtLeastOnce;
            var sendByte = Encoding.UTF8.GetBytes("Hello MQTT!");
            var shouldNotSendByte = Encoding.UTF8.GetBytes("Don't receive MQTT!");
            
            //Receive Properties(check)
            var receiveMessage = new byte[0];
            var sendTries = 0;

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
                receiveMessage = e.ApplicationMessage.Payload;
                sendTries ++;
            };

            //Receive client subscribe a topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(quality).Build());

            //Publish Client send a message
            await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = topic,
                    QualityOfServiceLevel = quality,
                    Payload = sendByte,
                });

            //Wait
            await Task.Delay(500);

            //Check receive client get mesage
            Assert.IsTrue(Equality(sendByte,receiveMessage));

            //Receive client unSubscribe topic
            await receniveClient.UnsubscribeAsync(topic);

            //Wait
            await Task.Delay(500);

            //Publish Client send a message. (this message should not be received.)
            await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = topic,
                    QualityOfServiceLevel = quality,
                    Payload = shouldNotSendByte,
                });

            //Wait
            await Task.Delay(500);

            //Check receive client not get message
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
