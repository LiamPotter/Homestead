/*-----------------------------+------------------------------\
|                                                             |
|                        !!!NOTICE!!!                         |
|                                                             |
|  These libraries are under heavy development so they are    |
|  subject to make many changes as development continues.     |
|  For this reason, the libraries may not be well commented.  |
|  THANK YOU for supporting forge with all your feedback      |
|  suggestions, bug reports and comments!                     |
|                                                             |
|                               - The Forge Team              |
|                                 Bearded Man Studios, Inc.   |
|                                                             |
|  This source code, project files, and associated files are  |
|  copyrighted by Bearded Man Studios, Inc. (2012-2015) and   |
|  may not be redistributed without written permission.       |
|                                                             |
\------------------------------+-----------------------------*/



#if BMS_DEBUGGING
#define BMS_DEBUGGING_UNITY
#endif

using System;

namespace BeardedManStudios.Network
{
	public class NetworkingStream
	{
		/// <summary>
		/// Identifier of None
		/// </summary>
		public const char identifier_NONE = 'n';

		/// <summary>
		/// Identifier of a RPC
		/// </summary>
		public const char identifier_RPC = 'm';

		/// <summary>
		/// Identifier of a Player
		/// </summary>
		public const char identifier_PLAYER = 'p';

		/// <summary>
		/// Identifier of a NetworkedBehavior
		/// </summary>
		public const char identifier_NetWORKED_BEHAVIOR = 'b';

		/// <summary>
		/// Identifier of a disconnect
		/// </summary>
		public const char identifier_DISCONNECT = 'd';

		/// <summary>
		/// Identifier of a custom
		/// </summary>
		public const char identifier_CUSTOM = 'c';

		/// <summary>
		/// All Identifier types to be used
		/// </summary>
		public enum IdentifierType
		{
			None = 0,
			RPC = 1,
			Player = 2,
			NetworkedBehavior = 3,
			Disconnect = 4,
			Custom = 5
		}

		/// <summary>
		/// Send of the NetworkingStream
		/// </summary>
		public NetworkingPlayer Sender { get; private set; }

		/// <summary>
		/// The type of identifier of the NetworkingStream
		/// </summary>
		public IdentifierType identifierType = IdentifierType.None;

		/// <summary>
		/// Recievers of the NetworkingStream
		/// </summary>
		public NetworkReceivers Receivers { get; set; }

		/// <summary>
		/// The actual sender's id, if relayed from server this would be the client that sent it
		/// </summary>
		public ulong RealSenderId { get; private set; }

		/// <summary>
		/// The NetworkedBehavoir of the NetworkingStream
		/// </summary>
		public SimpleNetworkedMonoBehavior NetworkedBehavior { get; private set; }

		/// <summary>
		/// The id for the SimpleNetworkedMonoBehavior to be used by Bare Metal
		/// </summary>
		public ulong NetworkedBehaviorId { get; private set; }

		/// <summary>
		/// Bytes being sent
		/// </summary>
		public BMSByte Bytes { get { return bytes; } protected set { bytes = value; } }
		private BMSByte bytes = new BMSByte();

		/// <summary>
		/// Byte read index being sent
		/// </summary>
		public int ByteReadIndex { get; protected set; }

		/// <summary>
		/// NetworkID for the NetworkingStream
		/// </summary>
		public int NetworkId { get; protected set; }

		/// <summary>
		/// The ProtocolType of the NetworkingStream
		/// </summary>
		public Networking.ProtocolType ProtocolType { get; protected set; }

		public void SetProtocolType(Networking.ProtocolType protocolType) { ProtocolType = protocolType; }

		/// <summary>
		/// Whether this NetworkingStream is a buffered RPC
		/// </summary>
		public bool BufferedRPC { get; protected set; }

		/// <summary>
		/// To reset the byte read index of the NetworkingStream
		/// </summary>
		public void ResetByteReadIndex() { ByteReadIndex = 0; }

		/// <summary>
		/// Get the custom identifier of the NetworkingStream
		/// </summary>
		public uint Customidentifier { get; private set; }

		public byte FrameIndex { get; private set; }

		private object NetworkedObjectMutex = new Object();

		/// <summary>
		/// Basic Constructor of the NetworkingStream
		/// </summary>
		public NetworkingStream() { Bytes = new BMSByte(); if (Networking.PrimarySocket == null) { ProtocolType = Networking.ProtocolType.TCP; } else { ProtocolType = Networking.PrimaryProtocolType; } }

		/// <summary>
		/// Constructor of the NetworkingStream with a given protocol type
		/// </summary>
		/// <param name="protocolType">The type of protocol being usedo n this Networking stream</param>
		public NetworkingStream(Networking.ProtocolType protocolType) { Bytes = new BMSByte(); this.ProtocolType = protocolType; }

		public bool Ready { get; private set; }
		public bool QueuedRPC { get; private set; }
		public void Reset() { Ready = false; Bytes.Clear(); QueuedRPC = false; }
		public void ManualReady(byte frameIndex = 0) { Ready = true; FrameIndex = frameIndex; }

		/// <summary>
		/// Prepare the NetworkingStream to be used
		/// </summary>
		/// <param name="socket">The NetWorker socket to be used</param>
		/// <param name="identifierType">The type of Identifier it is going to prepare</param>
		/// <param name="NetworkedBehavior">NetworkedBehavior to use</param>
		/// <param name="extra">Extra parameters to prepare</param>
		/// <param name="receivers">Who shall be receiving this NetworkingStream</param>
		/// <param name="bufferedRPC">To know if this is a Buffered RPC</param>
		/// <param name="customidentifier">A custom Identifier to be passed through</param>
		/// <returns></returns>
		public NetworkingStream Prepare(NetWorker socket, IdentifierType identifierType, ulong NetworkBehaviorId, BMSByte extra = null, NetworkReceivers receivers = NetworkReceivers.All, bool bufferedRPC = false, uint customidentifier = 0, ulong senderId = 0, bool noBehavior = false)
		{
			if (noBehavior)
				NetworkedBehavior = null;
			else
			{
				lock (NetworkedObjectMutex)
				{
					NetworkedBehavior = SimpleNetworkedMonoBehavior.Locate(NetworkBehaviorId);
				}
			}

			if (ReferenceEquals(NetworkedBehavior, null) && (extra == null || extra.Size == 0))
				throw new NetworkException(9, "Prepare was called but nothing was sent to write");

			NetworkedBehaviorId = NetworkBehaviorId;
			
			return PrepareFinal(socket, identifierType, NetworkBehaviorId, extra, receivers, bufferedRPC, customidentifier, senderId);
		}

		/// <summary>
		/// The final steps for preparing the NetworkingStream
		/// </summary>
		/// <param name="socket">The NetWorker socket to be used</param>
		/// <param name="identifierType">The type of Identifier it is going to prepare</param>
		/// <param name="behaviorNetworkId">NetworkedBehavior to use</param>
		/// <param name="extra">Extra parameters to prepare</param>
		/// <param name="receivers">Who shall be receiving this NetworkingStream</param>
		/// <param name="bufferedRPC">To know if this is a Buffered RPC</param>
		/// <param name="customidentifier">A custom Identifier to be passed through</param>
		/// <returns></returns>
		public NetworkingStream PrepareFinal(NetWorker socket, IdentifierType identifierType, ulong behaviorNetworkId, BMSByte extra = null, NetworkReceivers receivers = NetworkReceivers.All, bool bufferedRPC = false, uint customidentifier = 0, ulong senderId = 0)
		{
			lock (NetworkedObjectMutex)
			{
				if (senderId == 0)
					senderId = socket.Me != null ? socket.Me.NetworkId : 0;

				NetworkedBehaviorId = behaviorNetworkId;
				RealSenderId = senderId;
				Receivers = receivers;
				Customidentifier = customidentifier;
				BufferedRPC = Receivers == NetworkReceivers.AllBuffered || Receivers == NetworkReceivers.OthersBuffered;

				Bytes.Clear();

				if (ProtocolType == Networking.ProtocolType.TCP)
					ObjectMapper.MapBytes(bytes, (byte)0);

				ObjectMapper.MapBytes(bytes, (int)ProtocolType);

				ObjectMapper.MapBytes(bytes, (int)receivers);
				ObjectMapper.MapBytes(bytes, socket.Uniqueidentifier);

				if (ProtocolType != Networking.ProtocolType.HTTP && ProtocolType != Networking.ProtocolType.QuickUDP && ProtocolType != Networking.ProtocolType.QuickTCP)
				{
					this.identifierType = identifierType;

					if (identifierType == IdentifierType.None)
						Bytes.BlockCopy<byte>(((byte)identifier_NONE), 1);
					else if (identifierType == IdentifierType.RPC)
					{
						Bytes.BlockCopy<byte>(((byte)identifier_RPC), 1);
						Bytes.BlockCopy<byte>(((byte)(bufferedRPC ? 1 : 0)), 1);
					}
					else if (identifierType == IdentifierType.Player)
						Bytes.BlockCopy<byte>(((byte)identifier_PLAYER), 1);
					else if (identifierType == IdentifierType.NetworkedBehavior)
						Bytes.BlockCopy<byte>(((byte)identifier_NetWORKED_BEHAVIOR), 1);
					else if (identifierType == IdentifierType.Disconnect)
						Bytes.BlockCopy<byte>(((byte)identifier_DISCONNECT), 1);
					else if (identifierType == IdentifierType.Custom)
						Bytes.BlockCopy<byte>(((byte)identifier_CUSTOM), 1);

					ObjectMapper.MapBytes(bytes, behaviorNetworkId);
				}

				if (identifierType == IdentifierType.Custom)
					ObjectMapper.MapBytes(bytes, Customidentifier);

				if (extra != null)
					Bytes.BlockCopy(extra.byteArr, extra.StartIndex(), extra.Size);

				if (!ReferenceEquals(NetworkingManager.Instance, null))
					ObjectMapper.MapBytes(Bytes, NetworkingManager.Instance.CurrentFrame);
				else
					ObjectMapper.MapBytes(Bytes, (byte)0);

				Ready = true;
				return this;
			}
		}

		/// <summary>
		/// To consume the data of the NetWorker with a player's data
		/// </summary>
		/// <param name="socket">The NetWorker socket to be used</param>
		/// <param name="sender">The player who is sending the data</param>
		/// <param name="message">Data that is being sent</param>
		/// <returns></returns>
		public NetworkingStream Consume(NetWorker socket, NetworkingPlayer sender, BMSByte message)
		{
			lock (NetworkedObjectMutex)
			{
				Sender = sender;

				NetworkedBehaviorId = 0;
				ByteReadIndex = 0;
				Bytes.Clone(message);
				FrameIndex = message[message.StartIndex() + message.Size - 1];

				ProtocolType = (Networking.ProtocolType)ObjectMapper.Map<int>(this);
				Receivers = (NetworkReceivers)ObjectMapper.Map<int>(this);
				RealSenderId = ObjectMapper.Map<ulong>(this);

				if (ProtocolType == Networking.ProtocolType.HTTP || ProtocolType == Networking.ProtocolType.QuickUDP || ProtocolType == Networking.ProtocolType.QuickTCP)
				{
					Ready = true;
					return this;
				}

				char identifier = (char)ReadByte();// ObjectMapper.Map<char>(this);

				if (identifier == identifier_NONE)
					identifierType = IdentifierType.None;
				else if (identifier == identifier_RPC)
				{
					identifierType = IdentifierType.RPC;
					BufferedRPC = ReadByte() == 1;
				}
				else if (identifier == identifier_PLAYER)
					identifierType = IdentifierType.Player;
				else if (identifier == identifier_NetWORKED_BEHAVIOR)
					identifierType = IdentifierType.NetworkedBehavior;
				else if (identifier == identifier_DISCONNECT)
					identifierType = IdentifierType.Disconnect;
				else if (identifier == identifier_CUSTOM)
					identifierType = IdentifierType.Custom;
				else
					return null;

				NetworkedBehaviorId = ObjectMapper.Map<ulong>(this);

				NetworkedBehavior = SimpleNetworkedMonoBehavior.Locate(NetworkedBehaviorId);

				if (NetworkedBehaviorId > 0 && ReferenceEquals(NetworkedBehavior, null) && identifierType != IdentifierType.RPC)
					return null;

				// Remove the size of ProtocolType, identifier, NetworkId, etc.
				Bytes.RemoveStart(ByteReadIndex);

				ByteReadIndex = 0;

				if (socket.Uniqueidentifier == 0 && !socket.IsServer && identifierType == IdentifierType.Player)
				{
					if (socket != null)
					{
						socket.AssignUniqueId(ObjectMapper.Map<ulong>(this));
						Bytes.RemoveStart(sizeof(ulong));
					}
					else
						Bytes.RemoveStart(sizeof(ulong));

					if (socket != null && !socket.IsServer)
					{
						if (!socket.MasterServerFlag)
						{
							if (socket.UsingUnityEngine && ((ReferenceEquals(NetworkingManager.Instance, null) || !NetworkingManager.Instance.IsSetup || ReferenceEquals(NetworkingManager.Instance.OwningNetWorker, null))))
								NetworkingManager.setupActions.Add(socket.GetNewPlayerUpdates);
							else
								socket.GetNewPlayerUpdates();
						}
					}
				}

				ByteReadIndex = 0;

				if (identifierType == IdentifierType.NetworkedBehavior && !ReferenceEquals(NetworkedBehavior, null))
				{
					if (NetworkedBehavior is NetworkedMonoBehavior)
						((NetworkedMonoBehavior)NetworkedBehavior).PrepareDeserialize(this);
					else
						throw new Exception("Only NetworkedMonoBehaviors can be used for serialization and deserialization across the Network, object with id " + NetworkedBehavior.NetworkedId + " is not a \"NetworkedMonoBehavior\"");
				}

				if (identifierType == IdentifierType.Custom)
				{
					Customidentifier = ObjectMapper.Map<uint>(this);
					Bytes.RemoveStart(sizeof(uint));
				}

				ByteReadIndex = 0;

				Ready = true;

				if (NetworkedBehaviorId > 0 && ReferenceEquals(NetworkedBehavior, null))
				{
					if (identifierType == IdentifierType.RPC)
					{
						SimpleNetworkedMonoBehavior.QueueRPCForInstantiate(NetworkedBehaviorId, this);
						QueuedRPC = true;
						return null;
					}

					return null;
				}

				return this;
			}
		}

		/// <summary>
		/// Assign a sender to the NetworkingStream
		/// </summary>
		/// <param name="sender">The player for this NetworkingStream</param>
		/// <param name="targetBehavior">The owning behavior of this stream</param>
		public void AssignSender(NetworkingPlayer sender, SimpleNetworkedMonoBehavior targetBehavior)
		{
			Sender = sender;
			AssignBehavior(targetBehavior);
		}

		public void AssignBehavior(SimpleNetworkedMonoBehavior targetBehavior)
		{
			lock (NetworkedObjectMutex)
			{
				if (!ReferenceEquals(targetBehavior, null))
				{
					NetworkedBehavior = targetBehavior;
					NetworkedBehaviorId = targetBehavior.NetworkedId;
				}
			}
		}

		/// <summary>
		/// Replace the Bytes with new bytes given a start and count
		/// </summary>
		/// <param name="bytes">Bytes to be replacing with</param>
		/// <param name="start">Start index</param>
		/// <param name="count">The amount of bytes being removed</param>
		public void ReplaceInsertBytes(BMSByte bytes, int start, int count)
		{
			Bytes.RemoveRange(start, count);
			Bytes.InsertRange(start, bytes);
		}

		private int peekIndex = 0;

		/// <summary>
		/// Start the byte peek reading index
		/// </summary>
		public void StartPeek()
		{
			peekIndex = ByteReadIndex;
		}

		/// <summary>
		/// To stop the peek being read
		/// </summary>
		public void StopPeek()
		{
			ByteReadIndex = peekIndex;
		}

		/// <summary>
		/// Read the bytes with a given count
		/// </summary>
		/// <param name="readBytes">The byte array to update</param>
		/// <param name="count">Amount of bytes to read</param>
		/// <returns>The updated byte array</returns>
		//public byte[] Read(ref byte[] readBytes, int count)
		public byte[] Read(int count)
		{
			byte[] readBytes = new byte[count];
			//if (readBytes.Length < count)
			//	readBytes = new byte[count];

			try
			{
				for (int i = 0; i < count; i++)
					readBytes[i] = Bytes.byteArr[Bytes.StartIndex(ByteReadIndex) + i];
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				UnityEngine.Debug.LogException(ex);
#endif
			}

			ByteReadIndex += count;

			return readBytes;
		}

		/// <summary>
		/// Read a bytes out of the Bytes
		/// </summary>
		/// <returns>A read byte</returns>
		public byte ReadByte()
		{
			return Bytes.byteArr[Bytes.StartIndex(ByteReadIndex++)];
		}

		//public override string ToString()
		//{
		//	if (Bytes == null || Bytes.Size == 0)
		//		return string.Empty;

		//	return BitConverter.ToString(Bytes.ToArray());
		//}

		public T ReadObject<T>()
		{
			if (Bytes == null || Bytes.Size == 0)
				throw new NetworkException(10, "There are no more bytes to be read");

			return (T)ObjectMapper.Map<T>(this);
		}
	}
}
