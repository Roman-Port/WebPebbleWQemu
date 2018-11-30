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
    }
}
