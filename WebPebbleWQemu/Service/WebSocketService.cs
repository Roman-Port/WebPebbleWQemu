﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WebPebbleWQemu.Entities;
using WebPebbleWQemu.Service.Services;
using WebPebbleWQemu.Service.Containers;

namespace WebPebbleWQemu.Service
{
    public delegate void WebSocketServ(WebSocketService session, Dictionary<string, string> payload, int id);
    
    /// <summary>
    /// The base WebSocket service. More services will be added in seperate files.
    /// </summary>
    public class WebSocketService : KestrelWebsocketEmulation
    {
        public static readonly Dictionary<IncomingRpwsRequestType, WebSocketServ> services = new Dictionary<IncomingRpwsRequestType, WebSocketServ>
        {
            {
                IncomingRpwsRequestType.StartQemu, new WebSocketServ(StartQemuService.OnRequest)
            }
        };

        /* Session vars */
        /// <summary>
        /// QEMU session. Only not null if we got a request to start it.
        /// </summary>
        public QemuController qemu;


        /// <summary>
        /// Text frame, likely to be to us. Handle it with a service after deserialization.
        /// </summary>
        /// <param name="content"></param>
        public override void OnMessage(string content)
        {
            //Deserialize the header info first.
            IncomingRpwsRequest<Dictionary<string, string>> request = JsonConvert.DeserializeObject<IncomingRpwsRequest<Dictionary<string, string>>>(content);
            //Check the type against our records.
            services[request.type](this, request.payload, request.id);
        }

        /// <summary>
        /// Send a reply in text format.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        public void SendStandardMessage(Dictionary<string, string> data, int id)
        {
            //Construct message.
            OutgoingRpwsRequest<Dictionary<string, string>> request = new OutgoingRpwsRequest<Dictionary<string, string>>();
            request.id = id;
            request.payload = data;
            request.type = OutgoingRpwsRequestType.Reply;
            //Send
            Send(JsonConvert.SerializeObject(request));
        }

        /// <summary>
        /// Send an event in text format.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        public void SendStandardMessage(Dictionary<string, string> data, OutgoingRpwsRequestType type)
        {
            //Construct message.
            OutgoingRpwsRequest<Dictionary<string, string>> request = new OutgoingRpwsRequest<Dictionary<string, string>>();
            request.id = -1;
            request.payload = data;
            request.type = type;
            //Send
            Send(JsonConvert.SerializeObject(request));
        }

        /// <summary>
        /// Binary frame, likely to be from the VNC connection. Forward to the VNC stream.
        /// </summary>
        /// <param name="content"></param>
        public override void OnBinaryMessage(byte[] content)
        {
            base.OnBinaryMessage(content);
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
