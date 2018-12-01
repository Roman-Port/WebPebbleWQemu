using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebPebbleWQemu.Service.Containers
{
    /// <summary>
    /// Controls QEMU processes.
    /// </summary>
    public class QemuController
    {
        /// <summary>
        /// Hardware name (Aplite, Basalt)
        /// </summary>
        public string hardware;

        /// <summary>
        /// The unique (reusable) session ID, starting at zero. Used to calculate ports.
        /// </summary>
        public int sessionId;

        /// <summary>
        /// The unique, empty directroy where temporary files can be stored for this session.
        /// </summary>
        public string sessionDir;

        /// <summary>
        /// QEMU itself.
        /// </summary>
        public Process qemuProcess;

        /* PORTS */
        public int portStartRange;
        public int port_qemuTcp1;
        public int port_qemuSerial;
        public int port_gdbTcp;
        public int port_vnc;

        /* Sockets */
        public Socket sock_qemu_serial;

        public const int PORT_START_RANGE = 61209; //Where to start the port range
        public const int PORT_COUNT_SESSION = 3; //Ports per session (VNC is NOT included)

        public QemuController(string hardware, int sessionId)
        {
            this.hardware = hardware;
            this.sessionId = sessionId;
            this.sessionDir = $"{Program.config.qemu_session_dir}sid_{sessionId.ToString()}/";

            //Ensure the session dir is new.
            if(Directory.Exists(sessionDir))
                Directory.Delete(sessionDir, true);
            Directory.CreateDirectory(sessionDir);

            //Configure ports
            ConfigurePorts();
        }

        /// <summary>
        /// Called by the constructor to create ports.
        /// </summary>
        private void ConfigurePorts()
        {
            portStartRange = PORT_START_RANGE + (sessionId * PORT_COUNT_SESSION);
            port_qemuTcp1 = portStartRange;
            port_qemuSerial = portStartRange + 1;
            port_gdbTcp = portStartRange + 2;
            port_vnc = 5900 + sessionId;
        }

        /// <summary>
        /// Create the directory for this session and copy the spi and micro into it.
        /// </summary>
        private void CopyImages()
        {
            //First, get the data.
            RpwsConfig_FlashBinary data = Program.config.flash_bins[hardware];
            //Copy the content
            File.Copy(data.spi_flash, sessionDir + "spi_flash.bin");
            File.Copy(data.micro_flash, sessionDir + "micro_flash.bin");
        }

        /// <summary>
        /// Create processes. Should be done near the creation of this object.
        /// </summary>
        public string SpawnProcesses(out string args)
        {
            //First, copy the images so we have a clean spot.
            CopyImages();

            //Create the boot args for QEMU
            args = "-rtc base=localtime ";
            args += "-serial null ";
            args += "-serial tcp::" + port_qemuTcp1 + ",server,nowait ";
            args += "-serial tcp::" + port_qemuSerial + ",server ";
            args += "-drive file=" + sessionDir + "micro_flash.bin" + ",if=pflash,format=raw ";
            args += "-gdb tcp::" + port_gdbTcp + ",server ";
            args += "-vnc :" + sessionId.ToString() + " "; //This pushes the video output to vnc. See more here: https://stackoverflow.com/questions/22967925/running-qemu-remotely-via-ssh

            //Add command line args from the config json for this platform.
            foreach(string s in Program.config.flash_bins[hardware].args)
                args += s.Replace("qemu_spi_flash", sessionDir + "spi_flash.bin") + " ";

            //Now, start the QEMU process.
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = Program.config.qemu_binary, Arguments = args };
            qemuProcess = new Process() { StartInfo = startInfo, };
            qemuProcess.Start();
            return $"QEMU session {sessionId} launched QEMU with PID of {qemuProcess.Id}.";
        }

        public void WaitForQemu()
        {
            //Keep trying to connect to QEMU.
            Program.Log("Waiting for firmware to boot. Using IP " + port_qemuSerial.ToString());
            int i = 0;
            for (i = 0; i < 40; i++)
            {
                try
                {
                    Thread.Sleep(50);
                    sock_qemu_serial = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    sock_qemu_serial.ReceiveTimeout = 20000;
                    IAsyncResult result = sock_qemu_serial.BeginConnect(IPAddress.Loopback, port_qemuSerial, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(200, true);
                    if (sock_qemu_serial.Connected)
                    {
                        sock_qemu_serial.EndConnect(result);
                        break;
                    }
                    throw new Exception("Timed out, trying again.");
                }
                catch (Exception ex)
                {
                    sock_qemu_serial = null;
                }
            }
            //Check if this timed out.
            if (sock_qemu_serial == null)
            {
                throw new Exception("Timed out while waiting for QEMU firmware to boot.");
            }
            //Ignore messages until boot is done. This is a bit gross
            List<byte> buf = new List<byte>();
            i = 0;
            while (true)
            {
                byte[] mini_buf = new byte[1];
                sock_qemu_serial.Receive(mini_buf, 0, 1, SocketFlags.None);
                buf.Add(mini_buf[0]);
                i++;
                //Check if QEMU is telling us we're ready.
                string s = Encoding.ASCII.GetString(buf.ToArray());
                if (s.Contains("<SDK Home>") || s.Contains("<Launcher>") || s.Contains("Ready for communication"))
                    break;
            }
            //We're ready.
        }
    }
}
