using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ProtoBuf.ServiceModel;

namespace WireBus
{
    public class WireBus
    {
        private Type _envelopeType = typeof(DefaultMessageEnvelope);
        private readonly TcpClient _client;

        internal WireBus(TcpClient client)
        {
            _client = client;
        }

        public Task<T> ReceiveAsync<T>()
        {
            return ReceiveWithEnvelopeAsync<T>().ContinueWith(task =>
                                                                  {
                                                                      task.Wait();
                                                                      return task.Result.Item1;
                                                                  });
        }

        public Task<Tuple<T, IEnvelope>> ReceiveWithEnvelopeAsync<T>()
        {
            return Task.Factory.StartNew(delegate()
            {
                var stream = _client.GetStream();
                object envelope;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(_client.GetStream(), PrefixStyle.Base128, unused => _envelopeType, out envelope);
                var env = envelope as IEnvelope;
                var type = Type.GetType(env.TypeName);
                object message;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream, PrefixStyle.Base128, unused => type, out message);
                var msg = (T) message;
                return new Tuple<T, IEnvelope>(msg, env);
            });
        }

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

        public Task SendAsync(object message)
        {
            return Task.Factory.StartNew(() => Send(message));
        }

        public void SendBatch(object[] messages)
        {
            foreach (var message in messages)
                Send(message);
        }
    }
}
