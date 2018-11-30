using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebbleWQemu.Entities
{
    /// <summary>
    /// Incoming message to us, not the proxy.
    /// </summary>
    public class IncomingRpwsRequest<T>
    {
        /// <summary>
        /// Incoming request type, used for serialiation and redirection.
        /// </summary>
        public IncomingRpwsRequestType type;

        /// <summary>
        /// The request ID echoed back to the client. Echo will be -1 if this is an event.
        /// </summary>
        public int id;

        /// <summary>
        /// The the payload data. Use the type first to determine the type.
        /// </summary>
        public T payload;
    }

    /// <summary>
    /// The type of request. Used for deserialization.
    /// </summary>
    public enum IncomingRpwsRequestType
    {
        Ping
    }
}
