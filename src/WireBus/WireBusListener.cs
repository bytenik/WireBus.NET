using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WireBus
{
	/// <summary>
	/// A listener for receiving incoming WireBus.NET connections.
	/// </summary>
	public class WireBusListener : IDisposable
	{
	    private readonly bool _clientMayReply = true;
	    private readonly ushort? _clientFixedLength = null;
	    private readonly bool _serverMayReply = true;
	    private readonly ushort? _serverFixedLength = null;

	    private readonly TcpListener _listener;

		/// <summary>
        /// Create a new host configured to listen on all IP addresses with the specified port
        /// </summary>
        /// <param name="port">the local port</param>
        /// <param name="clientMayReply">whether or not the client can reply</param>
        /// <param name="clientFixedLength">the client's fixed length, or null to use dynamic length</param>
        /// <param name="serverMayReply">whether or not the server can reply</param>
        /// <param name="serverFixedLength">the server's fixed length, or null to use dynamic length</param>
        public WireBusListener(int port,
                bool clientMayReply = true,
                ushort? clientFixedLength = null,
                bool serverMayReply = true,
                ushort? serverFixedLength = null)
            : this(port)
		{
		    _clientMayReply = clientMayReply;
		    _clientFixedLength = clientFixedLength;
		    _serverMayReply = serverMayReply;
		    _serverFixedLength = serverFixedLength;
		}

        /// <summary>
        /// Creates a new host configured to listen on the specified IP and port
        /// </summary>
        /// <param name="address">local address</param>
        /// <param name="port">local port</param>
        /// <param name="clientMayReply">whether or not the client can reply</param>
        /// <param name="clientFixedLength">the client's fixed length, or null to use dynamic length</param>
        /// <param name="serverMayReply">whether or not the server can reply</param>
        /// <param name="serverFixedLength">the server's fixed length, or null to use dynamic length</param>
        public WireBusListener(IPAddress address, int port,
                bool clientMayReply = true,
                ushort? clientFixedLength = null,
                bool serverMayReply = true,
                ushort? serverFixedLength = null)
            : this(address, port)
        {
            _clientMayReply = clientMayReply;
            _clientFixedLength = clientFixedLength;
            _serverMayReply = serverMayReply;
            _serverFixedLength = serverFixedLength;
        }

        /// <summary>
        /// Creates a new host configured to listen on the specified IP and port
        /// </summary>
        /// <param name="endpoint">the local endpoint</param>
        /// <param name="clientMayReply">whether or not the client can reply</param>
        /// <param name="clientFixedLength">the client's fixed length, or null to use dynamic length</param>
        /// <param name="serverMayReply">whether or not the server can reply</param>
        /// <param name="serverFixedLength">the server's fixed length, or null to use dynamic length</param>
        public WireBusListener(IPEndPoint endpoint,
                bool clientMayReply = true,
                ushort? clientFixedLength = null,
                bool serverMayReply = true,
                ushort? serverFixedLength = null)
            : this(endpoint)
        {
            _clientMayReply = clientMayReply;
            _clientFixedLength = clientFixedLength;
            _serverMayReply = serverMayReply;
            _serverFixedLength = serverFixedLength;
        }

	    /// <summary>
		/// Create a new host configured to listen on all IP addresses with the specified port
		/// </summary>
		/// <param name="port">the local port</param>
		public WireBusListener(int port)
		{
			_listener = new TcpListener(IPAddress.Any, port);
		}

		/// <summary>
		/// Creates a new host configured to listen on the specified IP and port
		/// </summary>
		/// <param name="address">local address</param>
		/// <param name="port">local port</param>
		public WireBusListener(IPAddress address, int port)
		{
			_listener = new TcpListener(address, port);
		}

		/// <summary>
		/// Creates a new host configured to listen on the specified IP and port
		/// </summary>
		/// <param name="endpoint">the local endpoint</param>
		public WireBusListener(IPEndPoint endpoint)
		{
			_listener = new TcpListener(endpoint);
		}

		/// <summary>
		/// Start listening for new connections.
		/// </summary>
		public void Start()
		{
			_listener.Start();
		}

		/// <summary>
		/// Stop listening for connections.
		/// </summary>
		public void Stop()
		{
			_listener.Stop();
		}

        /// <summary>
        /// Accept a new WireBus client.
        /// </summary>
        /// <returns></returns>
        public Task<WireBusClient> AcceptWireBusAsync()
        {
            return AcceptWireBusAsync(CancellationToken.None);
        }

        /// <summary>
        /// Accept a new WireBus client.
        /// </summary>
        /// <returns></returns>
        public Task<WireBusClient> AcceptWireBusAsync(CancellationToken token)
        {
            return AcceptWireBusAsync(TimeSpan.FromMilliseconds(-1), token);
        }

        /// <summary>
        /// Accept a new WireBus client.
        /// </summary>
        /// <returns></returns>
        public Task<WireBusClient> AcceptWireBusAsync(TimeSpan timeout)
        {
            return AcceptWireBusAsync(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Accept a new WireBus client.
        /// </summary>
        /// <returns></returns>
        public Task<WireBusClient> AcceptWireBusAsync(int timeoutMilliseconds)
        {
            return AcceptWireBusAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds), CancellationToken.None);
        }

        /// <summary>
        /// Accept a new WireBus client.
        /// </summary>
        /// <returns></returns>
        public async Task<WireBusClient> AcceptWireBusAsync(TimeSpan timeout, CancellationToken token)
        {
            var acceptTask = _listener.AcceptSocketAsync();
            var timeoutTask = TaskEx.Delay(timeout);
            var cancelTask = token.ToTask();

            var result = await TaskEx.WhenAny(acceptTask, timeoutTask, cancelTask);
            if(result == cancelTask)
                throw new OperationCanceledException(token); // this in theory isn't possible, but...
            else if(result == timeoutTask)
                throw new TimeoutException();

            var socket = acceptTask.Result;

            socket.ReceiveTimeout = 60000;
            socket.Blocking = true;
            return new WireBusClient(socket, _clientMayReply, _clientFixedLength, _serverMayReply, _serverFixedLength);
        }

		/// <summary>
		/// Dispose of the listener
		/// </summary>
		public void Dispose()
		{
			try
			{
				_listener.Stop();
			}
			catch
			{
			}
		}
	}
}
