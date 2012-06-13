WireBus.NET is a loose wrapper around the .NET network I/O libraries. It allows arbitrary byte blobs to be sent down the wire
rather than relying on byte-oriented connections. It also supports reply semantics.

Using WireBus is extremely simple. To wait for connections, create a WireBusListener and accept connections, much like you would
do with a TcpListener:

    var host = new WireBusListener(1234); // listen on port 1234
    host.Start();
    var serverTask = host.AcceptWireBusAsync();
    var wirebus = serverTask.Result;

To connect to a WireBus.NET host, use the static Connect method on WireBusClient:

    var wirebus = WireBusClient.Connect("localhost", 1234); // connect to localhost:1234

Send messages to the other peer using the Send or SendAsync methods:

    var message = new byte[] { ... };
    wirebus.Send(message);

Received messages are wrapped into a context object that allows you to get at the underlying message data as well as
reply to request messages.

Send a request message and wait for a reply with the SendRequest or SendRequestAsync methods:

    var message = new byte[] { ... };
	var responseCtx = wirebus.SendRequest(message);

Receive a message from the other peer with the Receive or ReceiveAsync methods:

	var ctx = wirebus.Receive();
	// ctx.Data is the message content

Reply to a message with the context's Reply or ReplyAsync method:

    var message = new byte[] { ... };
	ctx.Reply(message);

We use this library at FivePM Technology, Inc. to efficiently send messages between our on-vehicle computers and our cloud-based platform.
However, we think that this library will be useful for many other purposes and hope you find it useful for your project.