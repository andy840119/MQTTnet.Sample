using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Sample.Tests
{
    [TestClass]
    public class LoadingTest : BaseTest
    {
        [TestMethod]
        public async Task Loading1000PublishClient()
        { 
            //Create 1000 publish client
            var publishServers = 1000;

            //send tries
            var sendTries = 10;

            //Run a MQTT Server
            var server = new MqttFactory().CreateMqttServer();
            await server.StartAsync(new MqttServerOptions());

            //Create publish client(s)
            var publishers = new List<IMqttClient>();
            for(int i=0;i<publishServers;i++)
            { 
                var publishClient = new MqttFactory().CreateMqttClient();
                await publishClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());

                publishers.Add(publishClient);
            }

            var receiveIndexes = new List<int>();
            //Run a MQTT receive client
            var receniveClient = new MqttFactory().CreateMqttClient();
            await receniveClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());
            receniveClient.ApplicationMessageReceived += (object o, MqttApplicationMessageReceivedEventArgs e) =>
            { 
                var receiveIndex = int.Parse(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                receiveIndexes.Add(receiveIndex);
            };

            //Receive client subscribe a topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(Topic).WithQualityOfServiceLevel(ConnectQuality).Build());

            //Sending message
            for(int i = 0;i<sendTries;i++)
            { 
                for(int j=0;j<publishServers;j++)
                { 
                    var index = j + i * publishServers;

                    //Publish Client send a message
                    await publishers[j].PublishAsync(new MqttApplicationMessage()
                        { 
                            Topic = Topic,
                            QualityOfServiceLevel = ConnectQuality,
                            Payload = Encoding.UTF8.GetBytes(index.ToString()),
                        });
                }
            }
            
            //Wait
            await Task.Delay(1000);

            //check receive message
            for(int i=0;i<sendTries * publishServers ;i++)
            { 
                if(!receiveIndexes.Contains(i))
                    Assert.AreEqual(-1,i);
            }

            Console.WriteLine($"Total receive : {receiveIndexes.Count}");

            //Success
            Assert.IsTrue(true);
            
            //stop server
            await receniveClient.DisconnectAsync();
            foreach(var publisher in publishers)
            { 
                await publisher.DisconnectAsync();
            }
            await server.StopAsync();
        }
    }
}
