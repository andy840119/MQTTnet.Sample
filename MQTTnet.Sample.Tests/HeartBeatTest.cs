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
    public class HeartBeatTest : BaseTest
    {
        [TestMethod]
        public async Task TestHeartBeat()
        { 
            //Run a MQTT Server
            var option = new MqttServerOptionsBuilder()
                .WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(1)).Build();
            var server = new MqttFactory().CreateMqttServer();
            server.ClientDisconnected += (a,b)=>
            { 
                
            };
            await server.StartAsync(option);

            //Run a MQTT publish client
            var publishClient = new MqttFactory().CreateMqttClient();
            await publishClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());

            //Publish Client send a message
            await publishClient.PublishAsync(new MqttApplicationMessage()
                { 
                    Topic = Topic,
                    QualityOfServiceLevel = ConnectQuality,
                    Payload = Encoding.UTF8.GetBytes("MQTT Connect"),
                });

            var connectedClient = server.GetClientSessionsStatus();
            Assert.AreEqual(1,connectedClient.Count);

            //Waiting
            await Task.Delay(5000);

            connectedClient = server.GetClientSessionsStatus();
            //Server should disconnect the connection
            Assert.AreEqual(0,connectedClient);

        }
    }
}
