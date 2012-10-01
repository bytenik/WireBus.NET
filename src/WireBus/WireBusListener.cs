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
	    private readonly TcpListener _listener;
	    private readonly ConnectionSemantics _semantics;

	    /// <summary>
	    /// Creates a new host configured to listen on the specified IP and port
	    /// </summary>
	    /// <param name="endpoint">the local endpoint</param>
	    /// <param name="semantics">the connection semantics</param>
	    public WireBusListener(IPEndPoint endpoint, ConnectionSemantics semantics = new ConnectionSemantics())
	    {
            _semantics = semantics;
            _listener = new TcpListener(endpoint);
        }

	    /// <summary>
		/// Create a new host configured to listen on all IP addresses with the specified port
		/// </summary>
		/// <param name="port">the local port</param>
        /// <param name="semantics">the connection semantics</param>
        public WireBusListener(int port, ConnectionSemantics semantics = new ConnectionSemantics())
	    {
	        _semantics = semantics;
	        _listener = new TcpListener(IPAddress.Any, port);
	    }

	    /// <summary>
		/// Creates a new host configured to listen on the specified IP and port
		/// </summary>
		/// <param name="address">local address</param>
		/// <param name="port">local port</param>
        /// <param name="semantics">the connection semantics</param>
        public WireBusListener(IPAddress address, int port, ConnectionSemantics semantics = new ConnectionSemantics())
	    {
	        _semantics = semantics;
	        _listener = new TcpListener(address, port);
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
            var socket = await _listener.AcceptSocketAsync().NaiveTimeoutAndCancellation(timeout, token);

            socket.ReceiveTimeout = 60000;
            socket.Blocking = true;

            return new WireBusClient(socket, _semantics);
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
