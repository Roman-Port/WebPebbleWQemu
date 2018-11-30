using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WebPebbleWQemu.Entities;

namespace WebPebbleWQemu.Service
{
    /// <summary>
    /// The base WebSocket service. More services will be added in seperate files.
    /// </summary>
    public class WebSocketService : KestrelWebsocketEmulation
    {
        /// <summary>
        /// Text frame, likely to be to us. Handle it with a service after deserialization.
        /// </summary>
        /// <param name="content"></param>
        public override void OnMessage(string content)
        {
            //Deserialize the header info first.
            IncomingRpwsRequest<object> request = JsonConvert.DeserializeObject<IncomingRpwsRequest<object>>(content);
            //Check the type against our records.
            Console.WriteLine(request.type.ToString());
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
