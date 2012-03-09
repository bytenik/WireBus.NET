using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WireBus
{
    public class WireBusHost : IDisposable
    {
        private readonly TcpListener _listener;

        public WireBusHost(TcpListener listener)
        {
            _listener = listener;
        }

        public WireBusHost(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public WireBusHost(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start()
        {
            _listener.Start();
        }

        public void Stop()
        {   
            _listener.Stop();
        }

        public Task<WireBus> AcceptWireBusAsync()
        {
            return Task<TcpClient>.Factory.FromAsync(_listener.BeginAcceptTcpClient, _listener.EndAcceptTcpClient, null)
                .ContinueWith(task =>
                                  {
                                      if (task.Exception != null)
                                          return null;

                                      var bus = new WireBus(task.Result);
                                      return bus;
                                  });
        }

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
