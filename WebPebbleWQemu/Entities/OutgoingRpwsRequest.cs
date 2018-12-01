using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebbleWQemu.Entities
{
    /// <summary>
    /// Message to them, not the proxy.
    /// </summary>
    public class OutgoingRpwsRequest<T>
    {
        /// <summary>
        /// Incoming request type, used for serialiation and redirection.
        /// </summary>
        public OutgoingRpwsRequestType type;

        /// <summary>
        /// The request ID echoed back to the client. Echo will be -1 if this is an event.
        /// </summary>
        public int id;

        /// <summary>
        /// The the payload data. Use the type first to determine the type.
        /// </summary>
        public T payload;
    }

    public enum OutgoingRpwsRequestType
    {
        Reply, //Only used if ID != -1
        LogEvent, //Logging.
    }
}
