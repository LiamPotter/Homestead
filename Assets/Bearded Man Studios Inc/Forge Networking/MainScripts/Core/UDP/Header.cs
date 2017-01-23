namespace BeardedManStudios.Network
{
	/// <summary>
	/// The main packet header
	/// </summary>
	public class Header
	{
		public uint updateId;
		public int packetGroupId;
		public ushort packetCount;
		public ushort packetOrderId;
		public bool reliable;
		public BMSByte payload = new BMSByte();

		public Header(uint u, int g, ushort c, ushort o, bool r)
		{
			updateId = u;
			packetGroupId = g;
			packetCount = c;
			packetOrderId = o;
			reliable = r;
		}

		public Header(Header other)
		{
			Clone(other);
		}

		public void Clone(Header other)
		{
			updateId = other.updateId;
			packetGroupId = other.packetGroupId;
			packetCount = other.packetCount;
			packetOrderId = other.packetOrderId;
			reliable = other.reliable;
			payload.Clone(other.payload);
		}

		public void Clone(uint u, int g, ushort c, ushort o, bool r)
		{
			updateId = u;
			packetGroupId = g;
			packetCount = c;
			packetOrderId = o;
			reliable = r;
		}

		public void SetPayload(BMSByte b)
		{
			payload.Clone(b);
		}
	}
}
