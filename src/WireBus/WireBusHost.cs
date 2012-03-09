using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WireBus
{
    public class WireBusHost
    {
        private readonly TcpListener _listener;

        public WireBusHost(TcpListener listener)
        {
            _listener = listener;
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
    }
}
