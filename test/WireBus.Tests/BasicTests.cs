using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace WireBus.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void Connect()
        {
            const int port = 5555;
            var host = new WireBusListener(IPAddress.Loopback, port);
            host.Start();
            var serverTask = host.AcceptWireBusAsync();
            var client = WireBusClient.Connect(IPAddress.Loopback, port);
            serverTask.Wait();
            host.Stop();
        }

        [Test]
        public void ConnectAndDisconnect()
        {
            const int port = 4444;
            var host = new WireBusListener(IPAddress.Loopback, port);
            host.Start();
            var serverTask = host.AcceptWireBusAsync();
            var client = WireBusClient.Connect(IPAddress.Loopback, port);
            var server = serverTask.Result;
            var messageTask = client.ReceiveAsync();
            server.Disconnect();
            host.Stop();
            try
            {
                try
                {
                    messageTask.Wait();
                }
                catch(AggregateException ae)
                {
                    throw ae.Flatten().InnerException;
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
            Assert.Fail();
        }

        [Test]
        public void VerifyData()
        {
			const int port = 2222;
        	byte[] data = new byte[] {51, 18, 83, 133, 0, 4, 86, 99, 255};

			var host = new WireBusListener(IPAddress.Loopback, port);
			host.Start();
			var serverTask = host.AcceptWireBusAsync();
			var client = WireBusClient.Connect(IPAddress.Loopback, port);
        	var server = serverTask.Result;

        	var clientRcv = client.ReceiveAsync();
        	server.Send(data);
        	Assert.IsTrue(clientRcv.Result.Data.SequenceEqual(data), "Server sent data does not match client received");

        	var serverRcv = server.ReceiveAsync();
			client.Send(data);
			Assert.IsTrue(serverRcv.Result.Data.SequenceEqual(data), "Server sent data does not match client received");

			host.Stop();
        }

		[Test]
		public void TooMuchData()
		{
			const int port = 3333;
			var r = new Random();
			var data = new byte[ushort.MaxValue+1];
			r.NextBytes(data);

			var host = new WireBusListener(IPAddress.Loopback, port);
			host.Start();
			var serverTask = host.AcceptWireBusAsync();
			var client = WireBusClient.Connect(IPAddress.Loopback, port);
			var server = serverTask.Result;

			var clientRcv = client.ReceiveAsync();
			try
			{
				server.Send(data);
			}
			catch(ArgumentOutOfRangeException)
			{
				Assert.Pass();
			}

			Assert.Fail("Did not get overflow when sending too much data");
			host.Stop();
		}

		[Test]
		public void VerifyLotsOfData()
		{
			const int port = 6666;
			var r = new Random();
			var data = new byte[ushort.MaxValue];
			r.NextBytes(data);

			var host = new WireBusListener(IPAddress.Loopback, port);
			host.Start();
			var serverTask = host.AcceptWireBusAsync();
			var client = WireBusClient.Connect(IPAddress.Loopback, port);
			var server = serverTask.Result;

			for (int i = 0; i < 100; i++)
			{
				var clientRcv = client.ReceiveAsync();
				server.Send(data);
				Assert.IsTrue(clientRcv.Result.Data.SequenceEqual(data), "Server sent data does not match client received");
			}

			for (int i = 0; i < 100; i++)
			{
				var serverRcv = server.ReceiveAsync();
				client.Send(data);
				Assert.IsTrue(serverRcv.Result.Data.SequenceEqual(data), "Client sent data does not match server received");
			}

			host.Stop();
		}
    }
}
