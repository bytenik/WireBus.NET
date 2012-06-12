﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.ServiceModel;

namespace WireBus
{
	/// <summary>
	/// A WireBus client, responsible for wrapping a TCP/IP connection and serializing the data going over the bus.
	/// </summary>
	public class WireBusClient : IDisposable
	{
		private readonly Socket _socket;
		private readonly object _lock = new object();

		private readonly Dictionary<uint, TaskCompletionSource<WireContext>> _replyReceivers = new Dictionary<uint, TaskCompletionSource<WireContext>>();
		private readonly BlockingCollection<TaskCompletionSource<WireContext>> _receivers = new BlockingCollection<TaskCompletionSource<WireContext>>();

		internal WireBusClient(Socket socket)
		{
			_socket = socket;

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
			try
			{
				var lengthBuf = new byte[sizeof(ushort)];
				await SocketReceiveAsync(_socket, lengthBuf, 0, sizeof (ushort));
				ushort length = BitConverter.ToUInt16(lengthBuf, 0);

				var idBuf = new byte[sizeof(uint)];
				uint? id;
				await SocketReceiveAsync(_socket, idBuf, 0, 1);
				if (idBuf[0] == 0)
					id = null;
				else
				{
					await SocketReceiveAsync(_socket, idBuf, 1, sizeof(uint)-1);
					id = BitConverter.ToUInt32(idBuf, 0);
				}

				var buf = new byte[length];
				for (int bytes = 0; bytes < length; )
					bytes += await SocketReceiveAsync(_socket, buf, bytes, length-bytes);

				if (id == null)
					_receivers.Take().SetResult(new WireContext(this, buf));
				else if (_replyReceivers.ContainsKey(id.Value))
				{
					_replyReceivers[id.Value].SetResult(new WireContext(this, buf));
					_replyReceivers.Remove(id.Value);
				}
				else
					_receivers.Take().SetResult(new WireContext(this, buf, id));				
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

			ReceiveMessages();
		}

		internal async Task InternalSendAsync(byte[] message, uint? id)
		{
			var buffer = new byte[sizeof(ushort) + 1 + message.Length];
			BitConverter.GetBytes((ushort)message.Length).CopyTo(buffer, 0);
			if (id == null)
			{
				buffer[sizeof(ushort)] = 0;
				message.CopyTo(buffer, sizeof(ushort) + 1);
			}
			else
			{
				BitConverter.GetBytes(id.Value).CopyTo(buffer, sizeof (ushort));
				message.CopyTo(buffer, sizeof (ushort) + sizeof (uint));
			}

			Monitor.Enter(_lock);
			try
			{
				var segment = new ArraySegment<byte>(buffer);
				await Task<int>.Factory.FromAsync(_socket.BeginSend, _socket.EndSend, new List<ArraySegment<byte>> { segment }, SocketFlags.None, null);
			}
			finally
			{
				Monitor.Exit(_lock);
			}
		}

		/// <summary>
		/// Send a message to the peer.
		/// </summary>
		/// <param name="message">the message to send</param>
		public Task SendAsync(byte[] message)
		{
			return InternalSendAsync(message, null);
		}

		private uint _maxId = 0;
		private readonly object _idLock = new object();
		private uint GetNextId()
		{
			lock(_idLock)
			unchecked
			{
				// do not ever let the LSB (little-endian -- first byte) be 0 since that means no reply id
				_maxId++;
				if ((_maxId & 0xFFu) == 0)
					_maxId++;
				if ((_maxId & 0xFFu) == 0) // check again due to possible wraparound from 0-ending number to 0
					_maxId++;

				return _maxId;
			}
		}

		/// <summary>
		/// Send a message to the peer.
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

		public Task<WireContext> ReceiveAsync()
		{
			var source = new TaskCompletionSource<WireContext>();
			_receivers.Add(source);
			return source.Task;
		}

		/// <summary>
		/// Disconnect from the peer.
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

		/// <summary>
		/// Connect to a peer, returning a new WireBus.
		/// </summary>
		/// <param name="host">the target host</param>
		/// <param name="port">the target port</param>
		/// <returns>the WireBus wrapping the new connection</returns>
		public static Task<WireBusClient> ConnectAsync(string host, int port)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null)
				.ContinueWith(task => new WireBusClient(socket));
		}

		/// <summary>
		/// Connect to a peer, returning a new WireBus.
		/// </summary>
		/// <param name="host">the target host</param>
		/// <param name="port">the target port</param>
		/// <returns>the WireBus wrapping the new connection</returns>
		public static Task<WireBusClient> ConnectAsync(IPAddress host, int port)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null)
				.ContinueWith(task => new WireBusClient(socket));
		}

		/// <summary>
		/// Connect to a peer, returning a new WireBus.
		/// </summary>
		/// <param name="host">the target host</param>
		/// <param name="port">the target port</param>
		/// <returns>the WireBus wrapping the new connection</returns>
		public static WireBusClient Connect(string host, int port)
		{
			var bus = ConnectAsync(host, port);
			return bus.Result;
		}

		/// <summary>
		/// Connect to a peer, returning a new WireBus.
		/// </summary>
		/// <param name="host">the target host</param>
		/// <param name="port">the target port</param>
		/// <returns>the WireBus wrapping the new connection</returns>
		public static WireBusClient Connect(IPAddress host, int port)
		{
			var bus = ConnectAsync(host, port);
			return bus.Result;
		}

		public void Dispose()
		{
			Disconnect();
		}
	}
}