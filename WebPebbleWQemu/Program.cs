using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net.WebSockets;

namespace WebPebbleWQemu
{
    class Program
    {
        public static WebPebbleWQemu.RpwsConfig config;

        public static bool[] openSessions = new bool[32];

        /// <summary>
        /// Provides control for QEMU and a proxy bridge for VNC, all via WebSockets.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Starting WebPebble WQemu...");
            //Read in the configuration.
            config = JsonConvert.DeserializeObject<RpwsConfig>(File.ReadAllText("/home/roman/webpebble/qemu_v2/git/bin/config.json"));
            //Populate sessions storage
            for (int i = 0; i < openSessions.Length; i++)
                openSessions[i] = true;
            //Init Kestrel
            MainAsync().GetAwaiter().GetResult();
        }

        public static Task MainAsync()
        {
            //Set Kestrel up to get replies.
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, 80);
                    options.Listen(addr, 443, listenOptions =>
                    {
                        listenOptions.UseHttps(config.ssl_cert_path, "");
                    });

                })
                .UseStartup<Program>()
                .Build();

            return host.RunAsync();
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            app.Run(OnHttpRequest);
        }

        private void OnShutdown()
        {
            
        }

        public static void Log(string msg, LogType type = LogType.Status)
        {
            Console.WriteLine($"[{type.ToString()}: {msg}");
        }

        public enum LogType
        {
            Status,
            Error
        }

        /// <summary>
        /// Write content to the stream.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="content"></param>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task QuickWriteToDoc(Microsoft.AspNetCore.Http.HttpContext context, string content, string type = "text/html", int code = 200)
        {
            try
            {
                var response = context.Response;
                response.StatusCode = code;
                response.ContentType = type;

                //Load the template.
                string html = content;
                var data = Encoding.UTF8.GetBytes(html);
                response.ContentLength = data.Length;
                await response.Body.WriteAsync(data, 0, data.Length);
                //Console.WriteLine(html);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Called on an HTTP request. Usually upgraded to a WebSocket.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            e.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            //If this is a websocket, switch to that.
            if (e.WebSockets.IsWebSocketRequest)
            {
                //Upgrade to a websocket.
                WebSocket ws = await e.WebSockets.AcceptWebSocketAsync();
                Service.WebSocketService session = new Service.WebSocketService();
                await session.StartSession(e, ws);
                return;
            }

            //Not valid.
            await QuickWriteToDoc(e, "Not a websocket request; dropped.", "text/plain", 404);
            Log("Dropping non-websocket request.");
        }
    }
}
