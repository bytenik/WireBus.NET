using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
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
    public class WireBusClient
    {
        private Type _envelopeType = typeof(DefaultMessageEnvelope);
        private readonly TcpClient _client;

        internal WireBusClient(TcpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Receive a message from the peer, blocking until one is available.
        /// </summary>
        /// <typeparam name="T">the type of message</typeparam>
        /// <returns>the message retrieved</returns>
        public Task<T> ReceiveAsync<T>()
        {
            return ReceiveWithEnvelopeAsync<T>().ContinueWith(task =>
                                                                  {
                                                                      task.Wait();
                                                                      return task.Result.Item1;
                                                                  });
        }

        /// <summary>
        /// Receive a message and its envelope from the peer, blocking until a message is available.
        /// </summary>
        /// <typeparam name="T">the type of message</typeparam>
        /// <returns>the message and envelope retrieved</returns>
        public Task<Tuple<T, IEnvelope>> ReceiveWithEnvelopeAsync<T>()
        {
            return Task.Factory.StartNew(delegate()
            {
                var stream = _client.GetStream();
                object envelope;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(_client.GetStream(), PrefixStyle.Base128, unused => _envelopeType, out envelope);
                var env = envelope as IEnvelope;
                if (envelope == null)
                    throw new InvalidEnvelopeException("Invalid or null envelope.");
                var type = Type.GetType(env.TypeName);
                object message;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream, PrefixStyle.Base128, unused => type, out message);
                var msg = (T) message;
                return new Tuple<T, IEnvelope>(msg, env);
            });
        }

        /// <summary>
        /// Send a message to the peer.
        /// </summary>
        /// <param name="message">the message to send</param>
        public void Send(object message)
        {
            var envelope = Activator.CreateInstance(_envelopeType) as IEnvelope;
            envelope.TypeName = message.GetType().AssemblyQualifiedName;

            lock (_client)
            {
                var stream = _client.GetStream();
                Serializer.NonGeneric.SerializeWithLengthPrefix(stream, envelope, PrefixStyle.Base128, 1);
                Serializer.NonGeneric.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128, 2);
            }
        }

        /// <summary>
        /// Send a message to the peer.
        /// </summary>
        /// <param name="message">the message to send</param>
        public Task SendAsync(object message)
        {
            return Task.Factory.StartNew(() => Send(message));
        }

        /// <summary>
        /// Disconnect from the peer.
        /// </summary>
        public void Disconnect()
        {
            _client.Close();
        }

        /// <summary>
        /// Connect to a peer, returning a new WireBus.
        /// </summary>
        /// <param name="host">the target host</param>
        /// <param name="port">the target port</param>
        /// <returns>the WireBus wrapping the new connection</returns>
        public static Task<WireBusClient> ConnectAsync(string host, int port)
        {
            var client = new TcpClient();
            return Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, host, port, null)
                .ContinueWith(task => new WireBusClient(client));
        }

        /// <summary>
        /// Connect to a peer, returning a new WireBus.
        /// </summary>
        /// <param name="host">the target host</param>
        /// <param name="port">the target port</param>
        /// <returns>the WireBus wrapping the new connection</returns>
        public static Task<WireBusClient> ConnectAsync(IPAddress host, int port)
        {
            var client = new TcpClient();
            return Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, host, port, null)
                .ContinueWith(task => new WireBusClient(client));
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
            bus.Wait();
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
            bus.Wait();
            return bus.Result;
        }
    }
}