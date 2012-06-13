using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireBus
{
	/// <summary>
	/// A context describing a received message and the ability to reply to it.
	/// </summary>
	public class WireContext
	{
		private readonly WireBusClient _client;
		private readonly uint? _id;

		/// <summary>
		/// The message byte data
		/// </summary>
		public byte[] Message { get; private set; }

		internal WireContext(WireBusClient client, byte[] message, uint? id)
		{
			_client = client;
			_id = id;
			Message = message;
		}

		internal WireContext(WireBusClient client, byte[] message)
			: this(client, message, null)
		{
			
		}

		/// <summary>
		/// Reply to the message
		/// </summary>
		/// <param name="message">the reply message data</param>
		/// <exception cref="InvalidOperationException">non-replyable message</exception>
		public Task ReplyAsync(byte[] message)
		{
			if(_id == null)
				throw new InvalidOperationException("Sender did not provide reply information");

			return _client.InternalSendAsync(message, _id);
		}

		/// <summary>
		/// Reply to the message
		/// </summary>
		/// <param name="message">the reply message data</param>
		/// <exception cref="InvalidOperationException">non-replyable message</exception>
		public void Reply(byte[] message)
		{
			if (_id == null)
				throw new InvalidOperationException("Sender did not provide reply information");

			var task = _client.InternalSendAsync(message, _id);
			task.WaitOne();
		}
	}
}
