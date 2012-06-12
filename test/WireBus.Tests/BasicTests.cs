﻿using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WireBus.Tests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void Connect()
        {
            const int port = 12345;
            var host = new WireBusListener(IPAddress.Loopback, port);
            host.Start();
            var serverTask = host.AcceptWireBusAsync();
            var client = WireBusClient.Connect(IPAddress.Loopback, port);
            serverTask.Wait();
            host.Stop();
        }

        [Serializable, DataContract]
        public class TestMessage
        {
            [DataMember(Order=1)]
            public int TestMember;
        }

        [TestMethod]
        public void ConnectAndDisconnect()
        {
            const int port = 12345;
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

        [TestMethod]
        public void VerifyData()
        {
            
        }
    }
}
