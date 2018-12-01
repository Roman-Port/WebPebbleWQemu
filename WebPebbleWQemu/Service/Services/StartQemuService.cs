﻿using System;
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
                throw new NotImplementedException();
            }

            session.LogToClient("Slot found. Creating QEMU processes...", id, 0);

            //Create a new session.
            session.qemu = new Containers.QemuController(payload["hardware"], sessionId);

            //Go through the setup process.
            session.qemu.SpawnProcesses();
            session.LogToClient("Opened QEMU. Waiting for the process to begin...", id, 0);
            session.qemu.WaitForQemu();

            //Tell the client that it's aye-okay to connect over VNC.
            session.SendStandardMessage(new Dictionary<string, string>
            {
                {
                    "ok", "true"
                }
            }, id);
        }
    }
}