using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebPebbleWQemu.VncProxyService
{
    public class VncProxy : KestrelWebsocketEmulation
    {
        public string token;
        public TcpClient client;
        public byte[] buffer = new byte[2048 * 2];

        public VncProxy(string token)
        {
            this.token = token;

            //Get the session information 
            var session = Program.vnc_tokens[token];
            int port = session.qemu.port_vnc;
            session.vncProxy = this;

            //Connect with the TCP client and start the proxy server.
            client = new TcpClient();
            client.Connect(new IPEndPoint(IPAddress.Loopback, port));

            //Start getting data.
            BeginAwaitData();
        }

        private void BeginAwaitData()
        {
            client.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnGotDataFromProxy), null);
        }

        private void OnGotDataFromProxy(IAsyncResult ar)
        {
            //Copy this frame and broadcast it to the end client.
            int len = client.Client.EndReceive(ar);

            //Copy this into a smaller array and send it to the client.
            byte[] buf = new byte[len];
            Array.Copy(buffer, buf, len);

            //Send to the end client
            Send(buf);

            //Begin listening for new content.
            BeginAwaitData();
        }

        public override void OnBinaryMessage(byte[] content)
        {
            //Got new content from our WebSocket client. Proxy it to the VNC connection.
            client.Client.Send(content);
        }

        public override void OnClose()
        {
            //Shut down the connection to VNC.
            client.Close();

            //Clear ourselves from the session.
            Program.vnc_tokens[token].vncProxy = null;
        }
    }
}
