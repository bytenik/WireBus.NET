﻿using System;
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
    public class RequestReplyTests
    {
        [Test]
        public void SimpleRequestReply()
        {
            const int port = 12345;
        	const int bytes = 5;

            var host = new WireBusListener(IPAddress.Loopback, port);
            host.Start();
            var serverTask = host.AcceptWireBusAsync();
            var client = WireBusClient.Connect(IPAddress.Loopback, port);
            var server = serverTask.Result;

            var messageTask = client.ReceiveAsync();
        	var replyTask = server.SendRequestAsync(new byte[bytes]);

        	Assert.AreEqual(messageTask.Result.Data.Length, bytes, "Received byte count does not match sent");

        	messageTask.Result.ReplyAsync(new byte[bytes]);

			Assert.AreEqual(replyTask.Result.Data.Length, bytes, "Received byte count of reply does not match sent");
        }
    }
}
