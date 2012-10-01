using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WireBus
{
    /// <summary>
    /// Describes the semantics of a WireBus connection. "Peer" refers to the other (non-local) connected party.
    /// The semantics of the local WireBus must match the peer WireBus, or messages will not be transmitted properly.
    /// </summary>
    public struct ConnectionSemantics
    {
        /// <summary>
        /// Whether request messages may be sent/received. Request messages are messages to which the other party can reply.
        /// </summary>
        public bool DisableRequests { get; private set; }

        /// <summary>
        /// The local fixed-length size of a message. If null, variable length semantics will be used.
        /// This MUST match the <see cref="PeerLength"/> semantic of the peer.
        /// </summary>
        public ushort? LocalLength { get; private set; }

        /// <summary>
        /// The fixed-length size of a message sent from the peer. If null, variable length semantics will be used.
        /// This MUST match the <see cref="LocalLength"/> semantic of the peer.
        /// </summary>
        public ushort? PeerLength { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="ConnectionSemantics"/> instance with the specified semantics.
        /// </summary>
        /// <param name="disableRequests"> </param>
        /// <param name="localLength"> </param>
        /// <param name="peerLength"> </param>
        public ConnectionSemantics(bool disableRequests = false, ushort? localLength = null, ushort? peerLength = null)
            : this()
        {
            DisableRequests = disableRequests;
            LocalLength = localLength;
            PeerLength = peerLength;
        }
    }
}
