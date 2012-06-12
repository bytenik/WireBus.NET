using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireBus
{
	public class WireContext
	{
		private readonly WireBusClient _client;
		private readonly uint? _id;
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

		public Task Reply(byte[] message)
		{
			if(_id == null)
				throw new InvalidOperationException("Sender did not provide reply information");

			return _client.InternalSendAsync(message, _id);
		}
	}
}
