using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebbleWQemu
{
    /// <summary>
    /// Configuration file read by the system at launch.
    /// </summary>
    public class RpwsConfig
    {
        /// <summary>
        /// Path to the SSL certificate used by the server.
        /// </summary>
        public string ssl_cert_path;

        /// <summary>
        /// Binary files for running the emulator.
        /// </summary>
        public Dictionary<string, RpwsConfig_FlashBinary> flash_bins;

        /// <summary>
        /// Holds sessions for QEMU. Path will be appended to this path.
        /// </summary>
        public string qemu_session_dir;

        /// <summary>
        /// The execuatable for qemu
        /// </summary>
        public string qemu_binary;
    }

    /// <summary>
    /// Config class for flash data.
    /// </summary>
    public class RpwsConfig_FlashBinary
    {
        public string micro_flash;
        public string spi_flash;
        public string layouts;
        public string[] args;
    }
}
