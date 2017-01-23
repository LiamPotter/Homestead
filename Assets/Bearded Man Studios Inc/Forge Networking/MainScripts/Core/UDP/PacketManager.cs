using System;
using System.Collections.Generic;

namespace BeardedManStudios.Network
{
	public struct Packet
	{
		public DateTime receiveTime;
		public Header data;

		public Packet(DateTime time, Header data) {
			receiveTime = time;
			Header head = new Header(data.updateId, data.packetGroupId, data.packetCount, data.packetOrderId, data.reliable);
			head.Clone(data);
			this.data = head;
		}
	}

	public class PacketList
	{
		private int size = 1;
		public List<Packet?> packets;

		public PacketList(int size)
		{
			packets = new List<Packet?>(size);

			for (int i = 0; i < size; i++)
				packets.Add(null);

			this.size = size;
		}

		public bool ReceivePacket(Header data)
		{
			// If already have data for this packet, don't overwrite
			if (packets[data.packetOrderId] == null)
				packets[data.packetOrderId] = new Packet(DateTime.Now, data);

			foreach (Packet? p in packets)
			{
				if (p == null)
					return false;
			}

			return packets.Count == size;
		}

		public BMSByte FlushAllData()
		{
			BMSByte messageBuffer = new BMSByte();

			for (int i = 0; i < packets.Count; i++)
			{
				messageBuffer.BlockCopy(packets[i].Value.data.payload.byteArr, packets[i].Value.data.payload.StartIndex(), packets[i].Value.data.payload.Size);
			}

			return messageBuffer;
		}

		public bool HasPacket(int index)
		{
			return packets[index] != null;
		}
	}
	
	public class PacketManager
	{
		public delegate void SendPacketsPending(Dictionary<uint, Dictionary<int, SendPackets>> pending);
		public delegate void PacketListComplete(uint updateId, bool isReliable, BMSByte allData);
		public event PacketListComplete packetListComplete = null;

		public struct SendPackets
		{
			public DateTime currentTime;
			public List<BMSByte> data;

			public SendPackets(DateTime time, List<BMSByte> packets, int count)
			{
				currentTime = time;
				data = packets;

				for (int i = 0; i < count; i++)
					data.Add(null);
			}

			public void UpdateTime()
			{
				currentTime = DateTime.Now;
			}
		}

		// Update ID, Group ID
		private Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, PacketList>>> unreliable = new Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, PacketList>>>();

		private Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, PacketList>>> reliable = new Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, PacketList>>>();

		private Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, SendPackets>>> sent = new Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<int, SendPackets>>>();

		/// <summary>
		/// The mutex for locking the reliable cache list
		/// </summary>
		private static object reliableSendMutex = new object();

		private object packetMutex = new object();

		public void RegisterNewClient(NetworkingPlayer client)
		{
			lock (packetMutex)
			{
				unreliable.Add(client, new Dictionary<uint, Dictionary<int, PacketList>>());
				reliable.Add(client, new Dictionary<uint, Dictionary<int, PacketList>>());
			}

			lock (reliableSendMutex)
			{
				sent.Add(client, new Dictionary<uint, Dictionary<int, SendPackets>>());
			}
		}

		public void DeregisterClient(NetworkingPlayer client)
		{
			lock (packetMutex)
			{
				if (unreliable.ContainsKey(client))
					unreliable.Remove(client);

				if (reliable.ContainsKey(client))
					reliable.Remove(client);

				if (sent.ContainsKey(client))
					sent.Remove(client);
			}
		}

		public void Reset()
		{
			lock (packetMutex)
			{
				unreliable.Clear();
				reliable.Clear();
			}

			lock (reliableSendMutex)
			{
				sent.Clear();
			}
		}

		public PacketList GetPacketList(NetworkingPlayer sender, Header payload)
		{
			lock (packetMutex)
			{
				if (payload.reliable)
				{
					if (!reliable.ContainsKey(sender))
						return null;

					// If we have never processed this packet id add it
					if (!reliable[sender].ContainsKey(payload.updateId))
						reliable[sender].Add(payload.updateId, new Dictionary<int, PacketList>());

					// If we have never processed this group for the packet id then add it
					if (!reliable[sender][payload.updateId].ContainsKey(payload.packetGroupId))
						reliable[sender][payload.updateId].Add(payload.packetGroupId, new PacketList(payload.packetCount));

					return reliable[sender][payload.updateId][payload.packetGroupId];
				}

				if (!unreliable.ContainsKey(sender))
					return null;

				// If we have never processed this packet id add it
				if (!unreliable[sender].ContainsKey(payload.updateId))
					unreliable[sender].Add(payload.updateId, new Dictionary<int, PacketList>());

				// If we have never processed this group for the packet id then add it
				if (!unreliable[sender][payload.updateId].ContainsKey(payload.packetGroupId))
					unreliable[sender][payload.updateId].Add(payload.packetGroupId, new PacketList(payload.packetCount));

				return unreliable[sender][payload.updateId][payload.packetGroupId];
			}
		}

		public void PacketRead(NetworkingPlayer sender, Header payload, bool skipCompleteEvent = false)
		{
			lock (packetMutex)
			{
				if (sender == null)
					return;

				PacketList targetPacketList = GetPacketList(sender, payload);

				if (targetPacketList == null)
					return;

				if (!skipCompleteEvent && targetPacketList.ReceivePacket(payload))
				{
					// TODO:  Clear out older buffers
					if (packetListComplete != null)
					{
						packetListComplete(payload.updateId, payload.reliable, targetPacketList.FlushAllData());
					}
				}
			}
		}

		public bool HasReadPacket(NetworkingPlayer sender, Header payload)
		{
			lock (packetMutex)
			{
				if (sender == null)
					return false;

				PacketList targetPacketList = GetPacketList(sender, payload);

				if (targetPacketList == null)
					return false;

				return targetPacketList.HasPacket(payload.packetOrderId);
			}
		}

		public void PacketSendPending(NetworkingPlayer target, uint updateId, int groupId, ushort orderId, int packetCount, BMSByte data)
		{
			lock (reliableSendMutex)
			{
				if (!sent.ContainsKey(target))
					return;

				if (!sent[target].ContainsKey(updateId))
					sent[target].Add(updateId, new Dictionary<int, SendPackets>());

				if (!sent[target][updateId].ContainsKey(groupId))
					sent[target][updateId].Add(groupId, new SendPackets(DateTime.Now, new List<BMSByte>(packetCount), packetCount));

				sent[target][updateId][groupId].data[orderId] = data;
			}
		}

		public void PacketSendConfirmed(NetworkingPlayer target, uint updateId, int groupId, ushort orderId)
		{
			lock (reliableSendMutex)
			{
				if (!sent.ContainsKey(target))
					return;

				if (!sent[target].ContainsKey(updateId))
					return;

				if (!sent[target][updateId].ContainsKey(groupId))
					return;

				sent[target][updateId][groupId].data[orderId] = null;
			}
		}

		public void IterateSendPending(NetworkingPlayer target, SendPacketsPending action)
		{
			lock (reliableSendMutex)
			{
				if (sent[target].Count == 0 || action == null)
					return;

				action(sent[target]);
			}
		}

		public void RemoveSendById(NetworkingPlayer target, uint updateId)
		{
			lock (reliableSendMutex)
			{
				if (sent.ContainsKey(target) && sent[target].ContainsKey(updateId))
					sent[target].Remove(updateId);
			}
		}
	}
}