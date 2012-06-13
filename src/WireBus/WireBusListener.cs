using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WireBus
{
	/// <summary>
	/// A listener for receiving incoming WireBus.NET connections.
	/// </summary>
	public class WireBusListener : IDisposable
	{
		private readonly TcpListener _listener;

		/// <summary>
		/// Create a new host using an existing listener.
		/// </summary>
		/// <param name="listener">a preexisting TcpListener</param>
		public WireBusListener(TcpListener listener)
		{
			_listener = listener;
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
		public async Task<WireBusClient> AcceptWireBusAsync()
		{
			var client = await _listener.AcceptSocketAsync();
			client.ReceiveTimeout = 60000;
			client.Blocking = true;
			var bus = new WireBusClient(client);
			return bus;
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
