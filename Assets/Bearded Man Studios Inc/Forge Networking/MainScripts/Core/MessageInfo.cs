namespace BeardedManStudios.Network
{
	public struct MessageInfo
	{
		public ulong SenderId;
		public byte Frame;

		public MessageInfo(ulong senderId, byte frame)
		{
			SenderId = senderId;
			Frame = frame;
		}
	}
}