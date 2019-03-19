using Microsoft.AspNetCore.Http;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Sample.Web.Middlewares
{
    /// <summary>
    /// See : 
    /// https://gunnarpeipman.com/aspnet/aspnet-core-websocket-chat/
    /// </summary>
    public class MQTTMiddleware
    {
        protected static string ServerAddress => "localhost";

        private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly RequestDelegate _next;
 
        public MQTTMiddleware(RequestDelegate next)
        {
            _next = next;

            //Initial MQTT server
            var result = Task.Run(
            async () =>
                {
                    await InitialMQTTServer();
                });
            result.Wait();
        }
 
        public async Task Invoke(HttpContext context)
        {
            //Break if is not Websocket
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }
 
            //Get and save Websocket connection into list
            CancellationToken ct = context.RequestAborted;
            WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid().ToString();
            _sockets.TryAdd(socketId, currentSocket);
 
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
 
                //Waiting to receive message from Websocket
                var response = await ReceiveStringAsync(currentSocket, ct);
                if(string.IsNullOrEmpty(response))
                {
                    if(currentSocket.State != WebSocketState.Open)
                    {
                        //Break the loop and close the socket
                        break;
                    }

                    //Do not send the message
                    continue;
                }
 
                //Send message to evety connected client
                await SendMessageToEveryConnectUser(response,ct);
            }
 
            //Remove disconnected client from list
            WebSocket dummy;
            _sockets.TryRemove(socketId, out dummy);
 
            //Close websocket
            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            currentSocket.Dispose();
        }

        #region MQTT

        /// <summary>
        /// Initialize MQTT
        /// </summary>
        /// <returns></returns>
        private async Task InitialMQTTServer()
        { 
            //Sending properties
            var topic = "andy840119/iot";
            var quality = MqttQualityOfServiceLevel.AtLeastOnce;

            //Run a MQTT Server
            var server = new MqttFactory().CreateMqttServer();
            await server.StartAsync(new MqttServerOptions());

            //Run a MQTT receive client
            var receniveClient = new MqttFactory().CreateMqttClient();
            await receniveClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(ServerAddress).Build());
            receniveClient.ApplicationMessageReceived += (object o, MqttApplicationMessageReceivedEventArgs e) =>
            { 
                //Record received message.
                var receiveBytes = e.ApplicationMessage.Payload;
                var receiveMessage = Encoding.UTF8.GetString(receiveBytes);


                Console.WriteLine("Message received :");
                Console.WriteLine(receiveMessage);

                var result = Task.Run(
                async () =>
                    {
                        await SendMessageToEveryConnectUser(receiveMessage,default(CancellationToken));
                    });
                result.Wait();
            };

            //Receive client subscribe a topic
            await receniveClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(quality).Build());
        }

        #endregion

        #region Message

        /// <summary>
        /// Send message to every connected user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendMessageToEveryConnectUser(string message,CancellationToken ct)
        { 
            foreach (var socket in _sockets)
            {
                if(socket.Value.State != WebSocketState.Open)
                {
                    continue;
                }
 
                await SendStringAsync(socket.Value, message, ct);
            }
        }
 
        /// <summary>
        /// Send message to WebSocket client
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }
 
        /// <summary>
        /// Receive message from WebSocket client
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();
 
                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);
 
                ms.Seek(0, SeekOrigin.Begin);
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    return null;
                }
 
                // Encoding UTF8: https://tools.ietf.org/html/rfc6455#section-5.6
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        #endregion
    }
}
