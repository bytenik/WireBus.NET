using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WireBus
{
	/// <summary>
	/// A WireBus client, responsible for wrapping a TCP/IP connection and serializing the data going over the bus.
	/// </summary>
	public class WireBusClient : IDisposable
	{
		private readonly Socket _socket;

		private readonly Dictionary<uint, TaskCompletionSource<WireContext>> _replyReceivers = new Dictionary<uint, TaskCompletionSource<WireContext>>();
		private readonly BlockingCollection<TaskCompletionSource<WireContext>> _receivers = new BlockingCollection<TaskCompletionSource<WireContext>>();

	    private readonly ConnectionSemantics _semantics;

        /// <summary>
        /// The remote endpoint of the client.
        /// </summary>
        public EndPoint RemoteEndPoint { get { return _socket.RemoteEndPoint; }}

		internal WireBusClient(Socket socket, ConnectionSemantics semantics)
		{
			_socket = socket;
		    _semantics = semantics;

		    ReceiveMessages();
		}

        internal WireBusClient(Socket socket)
        {
            _socket = socket;
            _semantics = new ConnectionSemantics();

            ReceiveMessages();
        }

        private static async Task<int> SocketReceiveAsync(Socket s, byte[] buffer, int start, int length)
		{
			return await Task<int>.Factory.FromAsync(s.BeginReceive, s.EndReceive,
				                            new List<ArraySegment<byte>> {new ArraySegment<byte>(buffer, start, length)},
				                            SocketFlags.None, null);
		}

		internal async void ReceiveMessages()
		{
			while(true) try
			{
                var tinyBuf = new byte[sizeof(ushort)];
			 
                ushort length;
                if (_semantics.PeerLength == null)
                {
                    // read the message length (2 bytes)
                    await SocketReceiveAsync(_socket, tinyBuf, 0, sizeof (ushort));
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(tinyBuf);
                    length = BitConverter.ToUInt16(tinyBuf, 0);
                }
                else
                    length = _semantics.PeerLength.Value;

                uint? id;
			    if (_semantics.DisableRequests)
			        id = null;
			    else
			    {
			        // read the id (1 byte if no id, 4 bytes if there is)
			        var idBuf = new byte[sizeof (uint)];
			        await SocketReceiveAsync(_socket, idBuf, 0, 1);
			        if (idBuf[0] == 0)
			            id = null;
			        else
			        {
			            await SocketReceiveAsync(_socket, idBuf, 1, sizeof (uint) - 1);
			            if (!BitConverter.IsLittleEndian)
			                Array.Reverse(tinyBuf);
			            id = BitConverter.ToUInt32(idBuf, 0);
			        }
			    }

			    var buf = new byte[length];
				for (int bytes = 0; bytes < length; )
					bytes += await SocketReceiveAsync(_socket, buf, bytes, length-bytes);

				if (id == null || !_replyReceivers.ContainsKey(id.Value))
				{
				    TaskCompletionSource<WireContext> tcs;
				    do
				    {
				        tcs = _receivers.Take();
				    } while (tcs.Task.IsCanceled || tcs.Task.IsFaulted);
				    tcs.SetResult(new WireContext(this, buf));
				}
				else
				{
					_replyReceivers[id.Value].SetResult(new WireContext(this, buf));
					_replyReceivers.Remove(id.Value);
				}
			}
			catch (SocketException)
			{
				Disconnect();
				return;
			}
			catch (ObjectDisposedException)
			{
				Disconnect();
				return;
			}
		}

		internal async Task InternalSendAsync(byte[] message, uint? id)
		{
            if (_semantics.LocalLength != null && message.Length != _semantics.LocalLength)
                throw new ArgumentOutOfRangeException("message", "Message must be exactly " + _semantics.LocalLength + " bytes; consider turning off fixed-length messages?");
			if(message.LongLength > ushort.MaxValue)
				throw new ArgumentOutOfRangeException("message", "Message cannot be more than " + uint.MaxValue + " bytes");

		    int pos = 0;
            var buffer = new byte[sizeof(ushort) + sizeof(uint) + message.Length];

            if (_semantics.LocalLength == null)
            {
                var lengthBytes = BitConverter.GetBytes((ushort) message.Length);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                lengthBytes.CopyTo(buffer, pos);
                pos += lengthBytes.Length;
            }

            if (!_semantics.DisableRequests)
            {
                if (id == null)
                {
                    buffer[pos++] = 0;
                }
                else
                {
                    var idBytes = BitConverter.GetBytes(id.Value);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(idBytes);
                    idBytes.CopyTo(buffer, pos);
                    pos += idBytes.Length;
                }
            }

		    message.CopyTo(buffer, pos);
            pos += message.Length;

			var segment = new ArraySegment<byte>(buffer, 0, pos);
			await Task<int>.Factory.FromAsync(_socket.BeginSend, _socket.EndSend, new List<ArraySegment<byte>> { segment }, SocketFlags.None, null);
		}

		/// <summary>
		/// Send a message to the peer.
		/// </summary>
		/// <param name="message">the message to send</param>
		public Task SendAsync(byte[] message)
		{
			return InternalSendAsync(message, null);
		}

		/// <summary>
		/// Send a message to the peer.
		/// </summary>
		/// <param name="message">the message to send</param>
		public void Send(byte[] message)
		{
			var task = SendAsync(message);
			task.WaitOne();
		}

#pragma warning disable 420
		private volatile int _maxId = 0;
		private uint GetNextId()
		{
			// do not ever let the LSB (little-endian -- first byte) be 0 since that means no reply id
			int id;
			do
			{
				id = Interlocked.Increment(ref _maxId);
			} while ((id & 0xFFu) == 0);
			
			return (uint) id;
		}
#pragma warning restore 420

		/// <summary>
		/// Send a message to the peer and wait for a reply
		/// </summary>
		/// <param name="message">the message to send</param>
		public async Task<WireContext> SendRequestAsync(byte[] message)
		{
			var source = new TaskCompletionSource<WireContext>();
			var id = GetNextId();
			_replyReceivers.Add(id, source);

			await InternalSendAsync(message, id);

			return await source.Task;
		}

		/// <summary>
		/// Send a message to the peer and wait for a reply
		/// </summary>
		/// <param name="message">the message to send</param>
		public WireContext SendRequest(byte[] message)
		{
			var task = SendRequestAsync(message);
			task.WaitOne();
			return task.Result;
		}

		/// <summary>
		/// Receive a message from the network
		/// </summary>
		/// <returns>a wire context describing the message received</returns>
		public Task<WireContext> ReceiveAsync()
		{
			var source = new TaskCompletionSource<WireContext>();
			_receivers.Add(source);
			return source.Task;
		}

        /// <summary>
        /// Receive a message from the network
        /// </summary>
        /// <returns>a wire context describing the message received</returns>
        public Task<WireContext> ReceiveAsync(TimeSpan timeout)
        {
            return ReceiveAsync(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Receive a message from the network
        /// </summary>
        /// <returns>a wire context describing the message received</returns>
        public Task<WireContext> ReceiveAsync(int timeoutMilliseconds)
        {
            return ReceiveAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds));
        }

        /// <summary>
        /// Receive a message from the network
        /// </summary>
        /// <returns>a wire context describing the message received</returns>
        public Task<WireContext> ReceiveAsync(TimeSpan timeout, CancellationToken token)
        {
            var source = new TaskCompletionSource<WireContext>();
            
            using (var timeoutCTS = new CancellationTokenSource())
            {
                timeoutCTS.CancelAfter(timeout);
                timeoutCTS.Token.Register(() => source.TrySetException(new TimeoutException()));
            }

            token.Register(() => source.TrySetCanceled());

            _receivers.Add(source);

            return source.Task;
        }

        /// <summary>
        /// Receive a message from the network
        /// </summary>
        /// <returns>a wire context describing the message received</returns>
        public Task<WireContext> ReceiveAsync(CancellationToken token)
        {
            var source = new TaskCompletionSource<WireContext>();
            _receivers.Add(source);

            token.Register(() => source.TrySetCanceled());

            return source.Task;
        }

		/// <summary>
		/// Receive a message from the network
		/// </summary>
		/// <returns>a wire context describing the message received</returns>
		public WireContext Receive()
		{
			var task = ReceiveAsync();
			task.WaitOne();
			return task.Result;
		}

		/// <summary>
		/// Disconnect from the peer
		/// </summary>
		public void Disconnect()
		{
			try
			{
				_socket.Close();
			}
			catch
			{
				// no-op
			}

			foreach (var s in _receivers)
				s.SetCanceled();
			foreach (var s in _replyReceivers.Values)
				s.SetCanceled();
			_replyReceivers.Clear();
		}

        #region Connect/ConnectAsync overloads

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
	    /// <param name="semantics">the semantics of the connection</param>
	    /// <returns>the bus wrapping the new connection</returns>
	    public static Task<WireBusClient> ConnectAsync(string host, int port, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null)
                .ContinueWith(task => new WireBusClient(socket, semantics));
        }

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
	    /// <param name="timeout">the timeout after which to give up</param>
	    /// <param name="semantics">the semantics of the connection</param>
	    /// <returns>the bus wrapping the new connection</returns>
	    public static Task<WireBusClient> ConnectAsync(string host, int port, TimeSpan timeout, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            return ConnectAsync(host, port, timeout, CancellationToken.None, semantics);
        }

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
	    /// <param name="millisecondsTimeout">the timeout in milliseconds after which to give up</param>
	    /// <param name="semantics">the semantics of the connection</param>
	    /// <returns>the bus wrapping the new connection</returns>
	    public static Task<WireBusClient> ConnectAsync(string host, int port, int millisecondsTimeout, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            return ConnectAsync(host, port, TimeSpan.FromMilliseconds(millisecondsTimeout), semantics);
        }

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
	    /// <param name="token">token used to cancel connection attempt</param>
	    /// <param name="semantics">the semantics of the connection</param>
	    /// <returns>the bus wrapping the new connection</returns>
	    public static Task<WireBusClient> ConnectAsync(string host, int port, CancellationToken token, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            return ConnectAsync(host, port, TimeSpan.FromMilliseconds(-1), token, semantics);
        }

        /// <summary>
        /// Connect to a peer, returning a new bus.
        /// </summary>
        /// <param name="host">the target host</param>
        /// <param name="port">the target port</param>
        /// <param name="semantics">the semantics of the connection</param>
        /// <param name="timeout">the timeout after which to give up</param>
        /// <param name="token">token used to cancel connection attempt</param>
        /// <returns>the bus wrapping the new connection</returns>
        public static Task<WireBusClient> ConnectAsync(string host, int port, TimeSpan timeout, CancellationToken token, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null)
                .ContinueWith(task => new WireBusClient(socket, semantics))
                .NaiveTimeoutAndCancellation(timeout, token);
        }

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
        /// <param name="semantics">the semantics of the connection</param>
	    /// <returns>the bus wrapping the new connection</returns>
	    public static Task<WireBusClient> ConnectAsync(IPAddress host, int port, ConnectionSemantics semantics = new ConnectionSemantics())
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null)
                .ContinueWith(task => new WireBusClient(socket, semantics));
        }

	    /// <summary>
	    /// Connect to a peer, returning a new bus.
	    /// </summary>
	    /// <param name="host">the target host</param>
	    /// <param name="port">the target port</param>
        /// <param name="semantics">the semantics of the connection</param>
        /// <returns>the bus wrapping the new connection</returns>
	    public static WireBusClient Connect(string host, int port, ConnectionSemantics semantics = new ConnectionSemantics())
		{
			var bus = ConnectAsync(host, port, semantics);
			return bus.Result;
		}

		/// <summary>
		/// Connect to a peer, returning a new bus.
		/// </summary>
		/// <param name="host">the target host</param>
		/// <param name="port">the target port</param>
        /// <param name="semantics">the semantics of the connection</param>
        /// <returns>the bus wrapping the new connection</returns>
		public static WireBusClient Connect(IPAddress host, int port, ConnectionSemantics semantics = new ConnectionSemantics())
		{
			var bus = ConnectAsync(host, port);
			return bus.Result;
		}

        #endregion

        /// <summary>
		/// Dispose of the client
		/// </summary>
		public void Dispose()
		{
			Disconnect();
		}
	}
}