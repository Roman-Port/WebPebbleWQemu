using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebbleWQemu.Service.Services
{
    public class StartQemuService
    {
        public static void OnRequest(WebSocketService session, Dictionary<string, string> payload, int id)
        {
            //Start the QEMU session.
            session.LogToClient("Finding a slot for QEMU...", id, 0);
            //Find an open ID.
            int sessionId = -1;
            lock(Program.openSessions)
            {
                for (int i = 0; i < Program.openSessions.Length; i++)
                {
                    if (Program.openSessions[i] == true)
                    {
                        //Open.
                        Program.openSessions[i] = false;
                        sessionId = i;
                        break;
                    }
                }
            }
            //If the session ID is -1, one was not found.
            if(sessionId == -1)
            {
                session.ErrorToClient("Failed to find a QEMU slot; All are full. Please wait a few minutes and try again.", id, -1, true);
                return;
            }

            session.LogToClient("Slot found. Creating QEMU processes...", id, 0);

            //Create a new session.
            session.qemu = new Containers.QemuController(payload["hardware"], sessionId);

            //Go through the setup process.
            string log = session.qemu.SpawnProcesses(out string args);

            session.LogToClient("Started QEMU process with arguments "+args, id, 1);
            session.LogToClient(log, id, 2);

            session.LogToClient("Opened QEMU. Waiting for the process to begin...", id, 3);
            try
            {
                session.qemu.WaitForQemu();
            } catch (Exception ex)
            {
                //Gracefully fail.
                session.ErrorToClient(ex.Message, id, -1, true);
                return;
            }

            //Tell the client that it's aye-okay to connect over VNC.
            session.SendStandardMessage(new Dictionary<string, string>
            {
                {
                    "ok", "true"
                },
                {
                    "url", $"wss://{Program.config.public_host}/?proxy_token={session.vnc_token}"
                }
            }, id);
        }
    }
}
