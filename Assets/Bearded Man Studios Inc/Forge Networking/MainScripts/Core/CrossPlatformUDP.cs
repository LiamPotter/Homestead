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


#if !UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Net;
#if NetFX_CORE
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
#else
#if !UNITY_WEBGL
using System.Net.Sockets;
#endif
using System.ComponentModel;
using System.Threading;
#endif

namespace BeardedManStudios.Network
{
	public class CrossPlatformUDP : NetWorker
	{
		/// <summary>
		/// The amount of time in milliseconds that must be waited before sending another ping
		/// </summary>
		public int MINIMUM_PING_WAIT_TIME = 50;

#if !NetFX_CORE
		/// <summary>
		/// This is a percentage (between 0 and 1) to drop packets for testing
		/// </summary>
		public float packetDropSimulationChance = 0.0f;

		/// <summary>
		/// This is a time in milliseconds to delay packet reads on the server by for testing
		/// </summary>
		public int NetworkLatencySimulationTime = 0;

		/// <summary>
		/// This is a list of the packets that are currently being delayed for testing
		/// </summary>
		private List<object> latencySimulationPackets = new List<object>();

		/// <summary>
		/// The thread that is responsible for managing the latency behaviors
		/// </summary>
		private Thread latencyThread = null;
#endif

#if NetFX_CORE
		private DatagramSocket ReadClient = null;
#else
		public CachedUdpClient ReadClient { get; private set; }
#endif

#if NetFX_CORE
		private Task reliableWorker = null;
#elif UNITY_IOS || UNITY_IPHONE
		private Thread readWorker = null;
#else
		private BackgroundWorker readWorker = null;
#endif
		/// <summary>
		/// NetworkUDP response delegate
		/// </summary>
		/// <param name="endpoint">The endpoint for the NetworkUDP</param>
		/// <param name="stream">The stream of data receieved</param>
		public delegate void NetworkUDPMessageEvent(string endpoint, NetworkingStream stream);

		/// <summary>
		/// The event to hook into for when data is sent
		/// </summary>
		new public event NetworkMessageEvent dataSent
		{
			add
			{
				dataSentInvoker += value;
			}
			remove
			{
				dataSentInvoker -= value;
			}
		}
		NetworkMessageEvent dataSentInvoker;    // Because iOS is stupid - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when the UDP data is read
		/// </summary>
		public event NetworkUDPMessageEvent udpDataRead
		{
			add
			{
				udpDataReadInvoker += value;
			}
			remove
			{
				udpDataReadInvoker -= value;
			}
		}
		NetworkUDPMessageEvent udpDataReadInvoker;  // Because iOS is stupid - Multi-cast function pointer.

		public ClientManager ClientManager { get; protected set; }

		private PacketManager packetManager = new PacketManager();

		/// <summary>
		/// A mutex for when players are being removed
		/// </summary>
		private object removalMutex = new object();

		/// <summary>
		/// A list of all the Reliable Packets Cache being stored
		/// </summary>
		public Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<uint, KeyValuePair<DateTime, List<BMSByte>>>>> reliablePacketsCache = new Dictionary<NetworkingPlayer, Dictionary<uint, Dictionary<uint, KeyValuePair<DateTime, List<BMSByte>>>>>();

		private static Dictionary<uint, int> packetGroupIds = new Dictionary<uint, int>();
		private const int PACKET_GROUP_TIMEOUT = 5000;                      // Milliseconds

		private static int plusPingTime = 20;
		private const int PAYLOAD_SIZE = 1024;
		private const int MAX_PACKET_SIZE = PAYLOAD_SIZE + sizeof(uint)     // update id
														 + sizeof(int)      // group id
														 + sizeof(ushort)   // group packet count
														 + sizeof(ushort);  // current group order id

		/// <summary>
		/// Whether or not this CrossPlatformUDP (NetWorker) is the server
		/// </summary>
		new public bool IsServer { get; private set; }

		/// <summary>
		/// The list of unique identifiers that packets use for updates
		/// </summary>
		private static Dictionary<string, uint> updateidentifiers = new Dictionary<string, uint>();

#if !NetFX_CORE
#if UNITY_IOS || UNITY_IPHONE
		private Thread reliableWorker = null;
#else
		private BackgroundWorker reliableWorker = null;
#endif
#endif

		/// <summary>
		/// The cached read stream
		/// </summary>
		private NetworkingStream readStream = new NetworkingStream();

		/// <summary>
		/// A list of players who currently timed out
		/// </summary>
		private List<NetworkingPlayer> timeoutDisconnects = new List<NetworkingPlayer>();


		/// <summary>
		/// Cache disconnected players
		/// </summary>
		private readonly List<NetworkingPlayer> disconnectedPlayers = new List<NetworkingPlayer>();

		/// <summary>
		/// The primary Network write buffer cache
		/// </summary>
		private BMSByte writeBuffer = new BMSByte();

		public override List<NetworkingPlayer> Players { get { return ClientManager.Players; } }

		private NetworkingPlayer sender = null;

		/// <summary>
		/// Constructor for the CrossPlatformUDP (NetWorker)
		/// </summary>
		/// <param name="isServer">If this is the server</param>
		/// <param name="maxConnections">Maximum allowed connections on this CrossPlatformUDP (NetWorker)</param>
		/// <param name="usingUnityEngine">True if running with NetworkingManager</param>
		public CrossPlatformUDP(bool isServer, int maxConnections, bool usingUnityEngine = true)
			: base(maxConnections, usingUnityEngine)
		{
			IsServer = isServer;
			ClientManager = new ClientManager();
			packetManager.packetListComplete += StreamCompleted;
		}
		~CrossPlatformUDP() { Disconnect(); }

		public static ulong resendCount = 0;

		// TODO:  Modify write to support one cached packet
		private List<BMSByte> breakDown = new List<BMSByte>(1);
#if NetFX_CORE
		private async void ResendReliableWorker()
#elif UNITY_IOS || UNITY_IPHONE
		private void ResendReliableWorker()
#else
		private void ResendReliableWorker(object sender, DoWorkEventArgs e)
#endif
		{
#if UNITY_IOS || UNITY_IPHONE
			try
			{
#endif
			while (true)
			{
#if !NetFX_CORE
				if (NetworkLatencySimulationTime > 0)
					Thread.Sleep(NetworkLatencySimulationTime * 3);
#endif

#if NetFX_CORE
				if (!Connected)
					return;
#elif UNITY_IOS || UNITY_IPHONE
				if (reliableWorker == null)
					return;
				else if (!reliableWorker.IsAlive)
					return;
				else if (connector == null)
					return;
				else if (readWorker == null)
					return;
				else if (latencyThread == null)
					return;
#else
				if (reliableWorker.CancellationPending)
					return;
#endif

				if (!IsServer && sendNewPing && Connected && (DateTime.Now - previousPingTime).TotalMilliseconds >= MINIMUM_PING_WAIT_TIME)
				{
					sendNewPing = false;
					previousPingTime = DateTime.Now;
					Ping();
				}

				ClientManager.Iterate((client) =>
				{
					packetManager.IterateSendPending(client, (pending) =>
					{
						if (pending.Count == 0)
							return;

						foreach (KeyValuePair<uint, Dictionary<int, PacketManager.SendPackets>> id in pending)
						{
							foreach (KeyValuePair<int, PacketManager.SendPackets> kv in id.Value)
							{
								if ((DateTime.Now - kv.Value.currentTime).TotalMilliseconds > PreviousServerPing + plusPingTime)
								{
									for (int i = 0; i < kv.Value.data.Count; i++)
									{
										if (kv.Value.data[i] == null)
											continue;
										else
										{
											if (breakDown.Count == 0)
												breakDown.Add(kv.Value.data[i]);
											else
												breakDown[0] = kv.Value.data[i];

											Write(id.Key, client, null, true, breakDown);

											kv.Value.UpdateTime();
										}
									}
								}
							}
						}
					});

					return true;
				});

#if NetFX_CORE
				await Task.Delay(ThreadSpeed);
#else
#if UNITY_IOS || UNITY_IPHONE
				if (reliableWorker == null)
					return;
				else if (!reliableWorker.IsAlive)
					return;
				else if (connector == null)
					return;
				else if (readWorker == null)
					return;
				else if (latencyThread == null)
					return;
#endif
				Thread.Sleep(ThreadSpeed);
#endif
			}
#if UNITY_IOS || UNITY_IPHONE
			}
			catch (System.Threading.ThreadInterruptedException ex)
			{
				//UnityEngine.Debug.LogError("Error: " + ex.Message);
			}
			catch (System.Exception ex)
			{
				UnityEngine.Debug.LogError("Network Error: " + ex.Message);
			}
#endif
		}

#if NetFX_CORE
		private Task timeoutTask = null;
#endif

		private ForgeMasterServerPing masterServerPing;

		private NetworkingPlayer CreatePlayer(ulong playerId, object endpoint, string name = "")
		{
#if !NetFX_CORE
			endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
#endif

			return new NetworkingPlayer(playerId, "127.0.0.1", endpoint, name);
		}

#if NetFX_CORE
		/// <summary>
		/// Connect with the CrossPlatformUDP (NetWorker) to a ip and port
		/// </summary>
		/// <param name="hostAddress">Ip address of the connection</param>
		/// <param name="port">Port of the connection</param>
		public async override void Connect(string hostAddress, ushort port)
		{
#else
		private Thread connector;

		/// <summary>
		/// Connect with the CrossPlatformUDP (NetWorker) to a ip and port
		/// </summary>
		/// <param name="hostAddress">Ip address of the connection</param>
		/// <param name="port">Port of the connection</param>
		public override void Connect(string hostAddress, ushort port)
		{
			Host = hostAddress;

#if !BARE_METAL
			if (!IsServer)
				SocketPolicyServer.CheckWebplayer(hostAddress);
#endif

			string localIp = "127.0.0.1";

			previousPingTime = DateTime.Now;

			connector = new Thread(new ParameterizedThreadStart(ThreadedConnect));
			connector.IsBackground = true;
			connector.Start(new object[] { hostAddress, port, localIp });

			PreviousServerPing = 100;
		}

		private void LatencySimulator()
		{
			try
			{
				while (true)
				{
					if (latencySimulationPackets.Count > 0)
					{
						double time = (DateTime.Now - (DateTime)((object[])latencySimulationPackets[0])[0]).TotalMilliseconds;
						string endpoint = (string)((object[])latencySimulationPackets[0])[1];
						BMSByte bytes = (BMSByte)((object[])latencySimulationPackets[0])[2];

						if (Math.Round(NetworkLatencySimulationTime - time) > 0)
							Thread.Sleep((int)Math.Round(NetworkLatencySimulationTime - time));

						lastReadTime = DateTime.Now;

						PacketReceived(endpoint, bytes);

						latencySimulationPackets.RemoveAt(0);
					}
					else
						Thread.Sleep(10);
				}
			}
			catch (Exception e)
			{
#if !UNITY_IOS
				UnityEngine.Debug.LogException(e);
#endif
			}
		}

#if NetFX_CORE
		private void ThreadedConnect(object hostAndPort)
#else
		private void ThreadedConnect(object hostAndPort)
#endif
		{
#if NetFX_CORE
			await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(50));
#else
			Thread.Sleep(50);
#endif

			string hostAddress = (string)((object[])hostAndPort)[0];
			ushort port = (ushort)((object[])hostAndPort)[1];
#endif
			try
			{
				if (IsServer)
				{
#if NetFX_CORE
					ReadClient = new DatagramSocket();
					await ReadClient.BindServiceNameAsync(port.ToString());
#else
					ReadClient = new CachedUdpClient(port);
					ReadClient.EnableBroadcast = true;
					Port = port;

					if (UseNatHolePunch)
						ForgeMasterServer.RegisterNat(this, port, ForgeMasterServer.MasterServerIp);

					groupEP = new IPEndPoint(IPAddress.Any, 0);

#endif
					Me = CreatePlayer(ServerPlayerCounter, ReadClient, "SERVER");
					ServerPlayerCounter++;

					if (!string.IsNullOrEmpty(ForgeMasterServer.MasterServerIp))
						masterServerPing = new ForgeMasterServerPing(this);
				}
				else
				{
					ushort serverPort = port;

#if NetFX_CORE
					ReadClient = new DatagramSocket();
					ReadClient.MessageReceived += ReadAsync;

					HostName serverHost = new HostName(hostAddress);

					await ReadClient.ConnectAsync(serverHost, serverPort.ToString());

					Port = port;

					if (!IsServer)
					{
						lastReadTime = DateTime.Now;
						timeoutTask = new Task(TimeoutCheck);
						timeoutTask.RunSynchronously();
					}
#else
					bool triedOnce = false;

					for (; ; port++)
					{
						try
						{
							ReadClient = new CachedUdpClient(port);

							if (UseNatHolePunch)
								ForgeMasterServer.RequestNat(this, port, hostAddress, serverPort, ForgeMasterServer.MasterServerIp);

							hostEndpoint = new IPEndPoint(IPAddress.Parse(hostAddress), serverPort);
							break;
						}
						catch
						{
							if (triedOnce && port == serverPort)
								throw new Exception("Running UDP locally, the system looped all the way around and back to port " + serverPort + " and found no open ports to run on.");

							triedOnce = true;
						}
					}

					Port = port;
#endif
				}

				if (IsServer)
					OnConnected();

				if (PreviousServerPing == 0)
				{
#if NetFX_CORE
					// TODO:  Do a correct ping for this platform
					PreviousServerPing = 75;
#endif
				}

#if NetFX_CORE
				reliableWorker = Task.Run((Action)ResendReliableWorker);
#elif UNITY_IOS || UNITY_IPHONE
				readWorker = new Thread(ReadAsync);
				readWorker.Start();

				reliableWorker = new Thread(ResendReliableWorker);
				reliableWorker.Start();
#else
				readWorker = new BackgroundWorker();
				readWorker.WorkerSupportsCancellation = true;
				readWorker.DoWork += ReadAsync;
				readWorker.RunWorkerAsync();

				reliableWorker = new BackgroundWorker();
				reliableWorker.WorkerSupportsCancellation = true;
				reliableWorker.DoWork += ResendReliableWorker;
				reliableWorker.RunWorkerAsync(hostAddress);
#endif

				if (!IsServer)
				{
#if NetFX_CORE
					hostEndpoint = new IPEndPoint(hostAddress, port);
#endif

					server = new NetworkingPlayer(0, hostEndpoint.Address.ToString() + "+" + hostEndpoint.Port, hostEndpoint, "SERVER");

					packetManager.RegisterNewClient(server);

					lock (writersBlockMutex)
					{
						if (updateidentifiers == null)
							updateidentifiers = new Dictionary<string, uint>();

						BMSByte tmp = new BMSByte();
						ObjectMapper.MapBytes(tmp, "connect", port, AuthHash);

						writeStream.SetProtocolType(Networking.ProtocolType.ReliableUDP);
						writeStream.Prepare(this, NetworkingStream.IdentifierType.None, 0, tmp, noBehavior: true);
						Write("BMS_INTERNAL_Udp_Connect", writeStream, true);
					}

					// JM: uncommented out timeout code
					Threading.Task.Run(() =>
					{
#if !NetFX_CORE
						Thread.Sleep(ConnectTimeout);
#endif
						if (!Connected)
							OnConnectTimeout();
					}, ConnectTimeout);
				}

#if !NetFX_CORE
				if (NetworkLatencySimulationTime > 0.0f)
				{
					latencyThread = new Thread(LatencySimulator);
					latencyThread.IsBackground = true;
					latencyThread.Start();
				}
#endif
			}
			catch (Exception e)
			{
				OnError(e);
			}
		}

		/// <summary>
		/// Executes buffered RPC's on the servers
		/// </summary>
		public override void GetNewPlayerUpdates()
		{
			Me = CreatePlayer(Uniqueidentifier, null);

			lock (writersBlockMutex)
			{
				BMSByte tmp = new BMSByte();
				ObjectMapper.MapBytes(tmp, "update");

				writeStream.SetProtocolType(Networking.ProtocolType.ReliableUDP);
				writeStream.Prepare(this, NetworkingStream.IdentifierType.None, 0, tmp, noBehavior: true);
				Write("BMS_INTERNAL_Udp_New_Player", writeStream, true);
			}
		}

#if NetFX_CORE
		private void Disconnect(string id, DatagramSocket player, string reason = null)
#else
		private void Disconnect(string id, IPEndPoint endpoint, string reason = null)
#endif
		{
			if (!string.IsNullOrEmpty(reason))
			{
				lock (writersBlockMutex)
				{
					BMSByte tmp = new BMSByte();
					ObjectMapper.MapBytes(tmp, reason);

					writeStream.SetProtocolType(Networking.ProtocolType.UDP);
					writeStream.Prepare(this, NetworkingStream.IdentifierType.Disconnect, 0, tmp, noBehavior: true);
#if NetFX_CORE
					WriteAndClose(id, player, writeStream);
#else
					WriteAndClose(id, endpoint, writeStream);
#endif
				}
			}
		}

		/// <summary>
		/// Disconnect a player on this CrossPlatformUDP (NetWorker)
		/// </summary>
		/// <param name="player">Player to disconnect</param>
		public override void Disconnect(NetworkingPlayer player, string reason = null)
		{
			if (!string.IsNullOrEmpty(reason))
			{
#if NetFX_CORE
				Disconnect("BMS_INTERNAL_Udp_DC_Player_Reason", (DatagramSocket)player.SocketEndpoint, reason);
#else
				Disconnect("BMS_INTERNAL_Udp_DC_Player_Reason", (IPEndPoint)player.SocketEndpoint, reason);
#endif
			}

			DisconnectCleanup(player);
			base.Disconnect(player, reason);
		}

		public void DisconnectCleanup(NetworkingPlayer player)
		{
			lock (removalMutex)
			{
				ClientManager.RemoveClient(player);
				packetManager.DeregisterClient(player);
				CleanUDPRPCForPlayer(player);
			}
		}

		public void DisconnectCleanup(bool timeout = false)
		{
			//if (IsServer)
			//	NatHolePunch.DeRegisterNat();

#if !NetFX_CORE
			if (connector != null)
			{
#if UNITY_IOS || UNITY_IPHONE
				connector.Interrupt();
				connector = null;
#else
				connector.Abort();
#endif
			}

			if (masterServerPing != null)
				masterServerPing.Disconnect();

			if (readWorker != null)
			{
#if UNITY_IOS || UNITY_IPHONE
				readWorker.Interrupt();
				readWorker = null;
#else
				readWorker.CancelAsync();
#endif
			}

			if (latencyThread != null)
			{
#if UNITY_IOS || UNITY_IPHONE
				latencyThread.Interrupt();
				latencyThread = null;
#else
				latencyThread.Abort();
#endif
			}
#endif

			if (reliableWorker != null)
			{
#if NetFX_CORE
				// TODO:  Make sure this is properly killed
				reliableWorker.Wait();
#elif UNITY_IOS || UNITY_IPHONE
				reliableWorker.Interrupt();
				reliableWorker = null;
#else
				reliableWorker.CancelAsync();
#endif
			}

#if NetFX_CORE
			//if (!IsServer)
			//{
			//	if (writeClient != null)
			//		writeClient.Dispose();
			//}
#endif

			if (ReadClient != null)
			{
#if NetFX_CORE
				ReadClient.Dispose();
#else
				ReadClient.Close();
#endif
			}

			ClientManager.RemoveAllClients();
			packetManager.Reset();

			if (timeout)
				OnTimeoutDisconnected();
			else
				OnDisconnected();
		}

		public override void TimeoutDisconnect()
		{
			lock (writersBlockMutex)
			{
				BMSByte tmp = new BMSByte();
				ObjectMapper.MapBytes(tmp, "disconnect");

				writeStream.SetProtocolType(Networking.ProtocolType.ReliableUDP);
				writeStream.Prepare(this, NetworkingStream.IdentifierType.None, 0, tmp, noBehavior: true);
				Write("BMS_INTERNAL_Udp_Disconnect", writeStream, true);
			}

			DisconnectCleanup(true);
		}

		/// <summary>
		/// Disconnect this CrossPlatformUDP (NetWorker)
		/// </summary>
		public override void Disconnect()
		{
			lock (writersBlockMutex)
			{
				BMSByte tmp = new BMSByte();
				ObjectMapper.MapBytes(tmp, "disconnect");

				writeStream.SetProtocolType(Networking.ProtocolType.ReliableUDP);
				writeStream.Prepare(this, NetworkingStream.IdentifierType.None, 0, tmp, noBehavior: true);
				Write("BMS_INTERNAL_Udp_Disconnect", writeStream, true);
			}

			DisconnectCleanup();
		}

		private static object updateidentifiersMutex = new Object();
		private static void TestUniqueIdentifiers(string updateStringId)
		{
			lock (updateidentifiersMutex)
			{
				if (!updateidentifiers.ContainsKey(updateStringId))
					updateidentifiers.Add(updateStringId, (uint)updateidentifiers.Count);
			}
		}

		private List<BMSByte> PreparePackets(string updateStringId, NetworkingStream stream, bool reliable, NetworkingPlayer player = null)
		{
			TestUniqueIdentifiers(updateStringId);
			return PreparePackets(updateidentifiers[updateStringId], stream.Bytes, reliable);
		}

		private List<BMSByte> PreparePackets(uint updateId, NetworkingStream stream, bool reliable, NetworkingPlayer player = null)
		{
			return PreparePackets(updateId, stream.Bytes, reliable, player);
		}

		private object packetBufferMutex = new object();
		private List<BMSByte> packetBufferList = new List<BMSByte>();
		private List<BMSByte> PreparePackets(uint updateId, BMSByte bytes, bool reliable, NetworkingPlayer player = null, byte defaultByte = 0)
		{
			lock (packetBufferMutex)
			{
				ushort packetCount = (ushort)Math.Ceiling((float)bytes.Size / PAYLOAD_SIZE);

				if (packetBufferList.Count < packetCount)
				{
					int currentSize = packetBufferList.Count;
					for (int i = 0; i < packetCount - currentSize; i++)
						packetBufferList.Add(new BMSByte());
				}

				if (!packetGroupIds.ContainsKey(updateId))
					packetGroupIds.Add(updateId, 0);
				else
					packetGroupIds[updateId]++;

				for (ushort i = 0; i < packetCount; i++)
				{
					packetBufferList[i].Clear();

					packetBufferList[i].Append(new byte[1] { defaultByte });

					//packet.AddRange(ObjectMapper.MapBytes(updateId, packetGroupId, packetCount, i, reliable ? (byte)1 : (byte)0));
					ObjectMapper.MapBytes(packetBufferList[i], updateId);
					ObjectMapper.MapBytes(packetBufferList[i], packetGroupIds[updateId]);
					ObjectMapper.MapBytes(packetBufferList[i], packetCount);
					ObjectMapper.MapBytes(packetBufferList[i], i);
					ObjectMapper.MapBytes(packetBufferList[i], reliable ? (byte)1 : (byte)0);

					if (bytes.Size - (i * PAYLOAD_SIZE) > PAYLOAD_SIZE)
						packetBufferList[i].BlockCopy(bytes.byteArr, bytes.StartIndex() + i * PAYLOAD_SIZE, PAYLOAD_SIZE);
					else
						packetBufferList[i].BlockCopy(bytes.byteArr, bytes.StartIndex() + i * PAYLOAD_SIZE, bytes.Size - (i * PAYLOAD_SIZE));

					if (reliable)
					{
						BMSByte bufferClone = new BMSByte().Clone(packetBufferList[i]);

						if (IsServer)
						{
							if (player != null)
								packetManager.PacketSendPending(player, updateId, packetGroupIds[updateId], i, packetCount, bufferClone);
							else
							{
								ClientManager.Iterate((client) =>
								{
									packetManager.PacketSendPending(client, updateId, packetGroupIds[updateId], i, packetCount, bufferClone);

									return true;
								});
							}
						}
						else
							packetManager.PacketSendPending(server, updateId, packetGroupIds[updateId], i, packetCount, bufferClone);
					}
				}
			}

			return packetBufferList;
		}

		private static Header packetHeader = null;
		private Header GetPacketHeader(NetworkingPlayer sender, ref BMSByte buffer)
		{
			if (packetHeader == null)
			{
				packetHeader = new Header(
					BitConverter.ToUInt32(buffer.byteArr, buffer.StartIndex()),
					BitConverter.ToInt32(buffer.byteArr, buffer.StartIndex(sizeof(uint))),
					BitConverter.ToUInt16(buffer.byteArr, buffer.StartIndex(sizeof(uint) + sizeof(int))),
					BitConverter.ToUInt16(buffer.byteArr, buffer.StartIndex(sizeof(uint) + sizeof(int) + sizeof(ushort))),
					buffer.byteArr[buffer.StartIndex(sizeof(uint) + sizeof(int) + sizeof(ushort) + sizeof(ushort))] == 1
				);
			}
			else
			{
				packetHeader.Clone(
					BitConverter.ToUInt32(buffer.byteArr, buffer.StartIndex()),
					BitConverter.ToInt32(buffer.byteArr, buffer.StartIndex(sizeof(uint))),
					BitConverter.ToUInt16(buffer.byteArr, buffer.StartIndex(sizeof(uint) + sizeof(int))),
					BitConverter.ToUInt16(buffer.byteArr, buffer.StartIndex(sizeof(uint) + sizeof(int) + sizeof(ushort))),
					buffer.byteArr[buffer.StartIndex(sizeof(uint) + sizeof(int) + sizeof(ushort) + sizeof(ushort))] == 1
				);
			}

			// The last addition is the size of the payload as it is not needed in this UDP implementation (if first packet)
			//if (packetHeader.packetOrderId == 0)
			//	buffer.RemoveStart(sizeof(uint) + sizeof(int) + sizeof(ushort) + sizeof(ushort) + sizeof(byte) + sizeof(int));
			//else
			buffer.RemoveStart(sizeof(uint) + sizeof(int) + sizeof(ushort) + sizeof(ushort) + sizeof(byte));

			if (packetHeader.reliable)
			{
				if (IsServer)
				{
					if (sender != null)
						WriteReceived(packetHeader.updateId, packetHeader.packetGroupId, packetHeader.packetOrderId, sender);
				}
				else
					WriteReceived(packetHeader.updateId, packetHeader.packetGroupId, packetHeader.packetOrderId, server);

				if (packetManager.HasReadPacket(sender, packetHeader))
					return packetHeader;
			}

			if (buffer.Size <= 0)
				return null;

			packetHeader.SetPayload(buffer);

			return packetHeader;
		}

		public override void Write(NetworkingPlayer player, NetworkingStream stream)
		{
			// TODO:  Implement
		}

		public override void Send(byte[] data, int length, object endpoint = null)
		{
#if !NetFX_CORE
			// TODO:  Skip if sending to itself
#else
			// Don't have machine write to itself
			if (((IPEndPoint)endpoint).Address.ToString() == "127.0.0.1")
				return;
#endif

			if (TrackBandwidth)
				BandwidthOut += (ulong)length;

			try
			{
#if NetFX_CORE
				Task tWrite = Task.Run(async () =>
				{
					DataWriter writer = null;
					if (IsServer)
						writer = new DataWriter(((DatagramSocket)endpoint).OutputStream);
					else
						writer = new DataWriter(ReadClient.OutputStream);
						
					writer.WriteBuffer(data.AsBuffer(), (uint)0, (uint)length);
					await writer.StoreAsync();
						
					writer.DetachStream();
					writer.Dispose();
				});

				tWrite.Wait();
#else
				ReadClient.Send(data, length, (IPEndPoint)endpoint);
#endif
			}
			catch (Exception e)
			{
				if (!(e is ObjectDisposedException))
					throw e;

				// There is no longer an active connection
			}
		}

#if NetFX_CORE
		private void WriteAndClose(string id, DatagramSocket targetSocket, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null)
#else
		private void WriteAndClose(string id, IPEndPoint endpoint, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null)
#endif
		{
			if (packets == null)
				packets = PreparePackets(id, stream, reliable);

			foreach (BMSByte packet in packets)
			{
				try
				{
#if NetFX_CORE
					Task tWrite = Task.Run(async () =>
					{
						DataWriter writer = new DataWriter(targetSocket.OutputStream);
						//uint length = writer.MeasureString(message);
						writer.WriteBuffer(packet.byteArr.AsBuffer(), (uint)packet.StartIndex(), (uint)packet.Size);
						await writer.StoreAsync();

						writer.DetachStream();
						writer.Dispose();
						targetSocket.Dispose();
					});

					tWrite.Wait();
#else
					Send(packet.Compress().byteArr, packet.Size, endpoint);
#endif
				}
				catch
				{
#if NetFX_CORE
					targetSocket.Dispose();
#endif
				}
			}
		}

		private List<BMSByte> CheckMakeRawReliable(string reliableStringId, NetworkingPlayer player, BMSByte data, bool reliable)
		{
			TestUniqueIdentifiers(reliableStringId);
			return PreparePackets(updateidentifiers[reliableStringId], data, reliable, player, 1);
		}

		public override void WriteRaw(NetworkingPlayer player, BMSByte data, string uniqueId, bool reliable = false)
		{
			//if (data[0] != 1)
			//	data.InsertRange(0, new byte[] { 1 });

			List<BMSByte> packets = CheckMakeRawReliable(uniqueId, player, data, reliable);

			try
			{
				foreach (BMSByte packet in packets)
				{
					if (IsServer)
						Send(packet.Compress().byteArr, packet.Size, player.SocketEndpoint);
					else
						Send(packet.Compress().byteArr, packet.Size, hostEndpoint);
				}
			}
			catch
			{
				Disconnect(player);
			}
		}

		public override void WriteRaw(BMSByte data, string uniqueId, bool relayToServer = true, bool reliable = false)
		{
			//if (data[0] != 1)
			//	data.InsertRange(0, new byte[] { 1 });

			List<BMSByte> packets = CheckMakeRawReliable(uniqueId, server, data, reliable);

			if (IsServer)
			{
				if (relayToServer)
				{
					// Make the server send the raw data to itself
					OnRawDataRead(Me, data);

					// The above function moves the pointer so swap it back
					data.MoveStartIndex(-1);
				}

				foreach (BMSByte packet in packets)
				{
					byte[] sendData = packet.Compress().byteArr;

					ClientManager.Iterate((player) =>
					{
						try
						{
							Send(sendData, packet.Size, player.SocketEndpoint);
						}
						catch
						{
							disconnectedPlayers.Add(player);
						}

						return true;
					});

					foreach (NetworkingPlayer player in disconnectedPlayers)
						Disconnect(player);

					disconnectedPlayers.Clear();
				}
			}
			else
			{
				if (ReadClient != null)
					Send(data.Compress().byteArr, data.Size, hostEndpoint);

				// TODO:  RawDataSent
				//if (stream != null && dataSentInvoker != null)
				//	dataSentInvoker(stream);
			}
		}

		public object writersBlockMutex = new object();
		private byte[] receivedMessage = null;
		private byte[] receivedTemp = new byte[4];
		private void WriteReceived(uint updateidentifier, int groupId, ushort orderId, NetworkingPlayer player)
		{
			if (receivedMessage == null)
			{
				List<byte> tmp = new List<byte>();
				tmp.AddRange(new byte[] { 5, 5 });
				tmp.AddRange(BitConverter.GetBytes(updateidentifier));
				tmp.AddRange(BitConverter.GetBytes(groupId));
				tmp.AddRange(BitConverter.GetBytes(orderId));

				receivedMessage = tmp.ToArray();
			}
			else
			{
				int current = 0;
				receivedTemp = BitConverter.GetBytes(updateidentifier);
				for (int i = 2; i < 2 + sizeof(uint); i++)
					receivedMessage[i] = receivedTemp[current++];

				current = 0;
				receivedTemp = BitConverter.GetBytes(groupId);
				for (int i = 2 + sizeof(uint); i < 2 + sizeof(uint) + sizeof(int); i++)
					receivedMessage[i] = receivedTemp[current++];

				current = 0;
				receivedTemp = BitConverter.GetBytes(orderId);
				for (int i = 2 + sizeof(uint) + sizeof(int); i < 2 + sizeof(uint) + sizeof(int) + sizeof(ushort); i++)
					receivedMessage[i] = receivedTemp[current++];
			}

			if (IsServer)
				Send(receivedMessage, receivedMessage.Length, player.SocketEndpoint);
			else
				Send(receivedMessage, receivedMessage.Length, hostEndpoint);
		}

		/// <summary>
		/// Write the data on a given CrossPlatformUDP(NetWorker) from a id, player, Networking stream and reliability
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to be written to server</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Extra parameters being sent</param>
		public override void Write(uint updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null)
		{
			if (packets == null)
				packets = PreparePackets(updateidentifier, stream, reliable, player);

			try
			{
				foreach (BMSByte packet in packets)
				{
					if (IsServer)
						Send(packet.Compress().byteArr, packet.Size, player.SocketEndpoint);
					else
						Send(packet.Compress().byteArr, packet.Size, hostEndpoint);
				}
			}
			catch
			{
				Disconnect(player);
			}
		}

		private void RemoveReliable(uint updateId, NetworkingPlayer player)
		{
			if (IsServer && player == null)
			{
				ClientManager.Iterate((client) =>
				{
					packetManager.RemoveSendById(client, updateId);
					return true;
				});
			}
			else
				packetManager.RemoveSendById(player, updateId);
		}

		/// <summary>
		/// Write the data on a given CrossPlatformUDP(NetWorker) from a id and Networking stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Extra parameters being sent</param>
		public override void Write(uint updateidentifier, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null)
		{
			if (IsServer)
			{
				if (stream != null)
				{
					if (stream.Sender == null)
						stream.AssignSender(Me, stream.NetworkedBehavior);

					if (stream.identifierType == NetworkingStream.IdentifierType.RPC && (stream.Receivers == NetworkReceivers.AllBuffered || stream.Receivers == NetworkReceivers.OthersBuffered))
						ServerBufferRPC(updateidentifier, stream);

					if (ClientManager.Count == 0 || stream.Receivers == NetworkReceivers.Server)
						return;

					if (packets == null)
						packets = PreparePackets(updateidentifier, stream, reliable);
				}
				else if (packets == null)
					throw new NetworkException("There is no message being sent on this request");

				NetworkingPlayer relaySender = null;

				if (stream.RealSenderId == Me.NetworkId)
					relaySender = Me;
				else
					relaySender = Players.Find(p => p.NetworkId == stream.RealSenderId);

				foreach (BMSByte packet in packets)
				{
					byte[] sendData = packet.Compress().byteArr;

					ClientManager.Iterate((player) =>
					{
						if (stream != null)
						{
							//EnteredProximity
							if (stream.Receivers == NetworkReceivers.OthersProximity || stream.Receivers == NetworkReceivers.AllProximity)
							{
								// If the receiver is out of range, do not update them with the message
								if (UnityEngine.Vector3.Distance(stream.Sender.Position, player.Position) > ProximityMessagingDistance)
								{
									if (!ReferenceEquals(player, null) && !ReferenceEquals(player.PlayerObject, null))
										player.PlayerObject.ProximityOutCheck(stream.Sender.PlayerObject);
									if (reliable) RemoveReliable(updateidentifier, player);
									return true;
								}
								else if (!ReferenceEquals(stream.Sender.PlayerObject, null) && !ReferenceEquals(player.PlayerObject, null))
									player.PlayerObject.ProximityInCheck(stream.Sender.PlayerObject);
							}

							if ((stream.Receivers == NetworkReceivers.Others || stream.Receivers == NetworkReceivers.OthersBuffered || stream.Receivers == NetworkReceivers.OthersProximity) && player.NetworkId == stream.RealSenderId)
							{
								if (reliable) RemoveReliable(updateidentifier, player);
								return true;
							}

							if ((stream.Receivers == NetworkReceivers.Owner || stream.Receivers == NetworkReceivers.ServerAndOwner) && !ReferenceEquals(stream.NetworkedBehavior, null) && player.NetworkId != stream.NetworkedBehavior.OwnerId)
							{
								if (reliable) RemoveReliable(updateidentifier, player);
								return true;
							}

							if (stream.Receivers == NetworkReceivers.MessageGroup && player.MessageGroup != relaySender.MessageGroup)
							{
								if (reliable) RemoveReliable(updateidentifier, player);
								return true;
							}
						}

						try
						{
							Send(sendData, packet.Size, player.SocketEndpoint);
						}
						catch
						{
							disconnectedPlayers.Add(player);
						}

						return true;
					});
				}

				// fenglin: Out of sync error when iterating over the clientSockets
				foreach (var player in disconnectedPlayers)
					Disconnect(player);

				disconnectedPlayers.Clear();
			}
			else
			{
				if (packets == null)
					packets = PreparePackets(updateidentifier, stream, reliable);

				if (ReadClient != null)
				{
					foreach (BMSByte packet in packets)
					{
						Send(packet.Compress().byteArr, packet.Size, hostEndpoint);
#if !NetFX_CORE
						Thread.Sleep(1);
#endif
					}
				}

				if (stream != null && dataSentInvoker != null)
					dataSentInvoker(stream);
			}
		}

		/// <summary>
		/// Write the data on a given CrossPlatformUDP(NetWorker) from a id, player and Networking stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to be written to server</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Extra parameters being sent</param>
		public override void Write(string updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null)
		{
			lock (writersBlockMutex)
			{
				if (!updateidentifiers.ContainsKey(updateidentifier))
					updateidentifiers.Add(updateidentifier, (uint)updateidentifiers.Count);

				Write(updateidentifiers[updateidentifier], player, stream, reliable, packets);
			}
		}

		/// <summary>
		/// Write the data on a given CrossPlatformUDP(NetWorker) from a id and Networking stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this is a reliable send</param>
		public override void Write(string updateidentifier, NetworkingStream stream, bool reliable = false)
		{
			lock (writersBlockMutex)
			{
				if (!updateidentifiers.ContainsKey(updateidentifier))
					updateidentifiers.Add(updateidentifier, (uint)updateidentifiers.Count);

				Write(updateidentifiers[updateidentifier], stream, reliable);
			}
		}

		// Obsolete
		public override void Write(NetworkingStream stream)
		{
			throw new NetworkException(4, "This method requires an updateidentifier, use the other Write method if unsure Write(id, stream)");
		}

		private object rpcMutex = new object();
		private bool ReadStream(string endpoint, NetworkingStream stream)
		{
			if (IsServer)
			{
				if (stream.Receivers == NetworkReceivers.MessageGroup && Me.MessageGroup != stream.Sender.MessageGroup)
					return true;

				ClientManager.RunActionOnPlayerEndpoint(endpoint, (player) => { OnDataRead(player, stream); });
			}
			else
				OnDataRead(null, stream);

			// Don't execute this logic on the server if the server doesn't own the object
			if (!ReferenceEquals(stream.NetworkedBehavior, null) && stream.Receivers == NetworkReceivers.Owner)
				return true;

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC)
			{
				lock (rpcMutex)
				{
					if ((new NetworkingStreamRPC(stream)).FailedExecution)
						return false;
				}
			}

			return true;
		}

		private bool ConsumeStream(bool isReliable, BMSByte data)
		{
			readStream = new NetworkingStream(isReliable ? Networking.ProtocolType.ReliableUDP : Networking.ProtocolType.UDP);

			if (readStream.Consume(this, sender, data) == null)
				return false;

			CurrentStreamOwner = readStream.Sender;

			return true;
		}

		private void StreamCompleted(uint updateId, bool isReliable, BMSByte data)
		{
			ConsumeStream(isReliable, data);

			if (ReadStream(sender.Ip, readStream) && IsServer)
				RelayStream(updateId, readStream);
		}

		private void CacheUpdate(NetworkingPlayer sender)
		{
			if (sender == null)
				return;

			uint id = rawBuffer.GetBasicType<uint>(1);
			int groupId = (rawBuffer.GetBasicType<int>(1 + sizeof(int)));
			ushort orderId = (rawBuffer.GetBasicType<ushort>(1 + sizeof(uint) + sizeof(int)));

			packetManager.PacketSendConfirmed(sender, id, groupId, orderId);
		}

#region Connection Request
		private bool ProcessClientConnection(string endpoint, Header header)
		{
			if (!IsServer)
				return false;

			lock (removalMutex)
			{
#if NetFX_CORE
				DatagramSocket newConnection = new DatagramSocket();
				HostName serverHost = new HostName(endpoint);
							
				Task tConnect = Task.Run(async () =>
				{
					// Try to connect asynchronously
					if (endpoint == "127.0.0.1")
						await newConnection.ConnectAsync(serverHost, (Port + 1).ToString());
					else
						await newConnection.ConnectAsync(serverHost, Port.ToString());
				});
				tConnect.Wait();
#else

				// Remove connect from bytes
				ObjectMapper.Map<string>(readStream);

				// Remove the sender port from the stream
				ObjectMapper.Map<ushort>(readStream);

				// Get the auth hash
				string sentAuthHash = ObjectMapper.Map<string>(readStream);
				if (sentAuthHash != AuthHash)
				{
					Disconnect("BMS_INTERNAL_DC_Invalid_Version", groupEP, "Your game version is out of date, please update in order to connect to this server");
					return false;
				}
#endif

				if (Connections >= MaxConnections)
				{
#if NetFX_CORE
					Disconnect("BMS_INTERNAL_DC_Max_Players", newConnection, "Max Players Reached On Server");
#else
					Disconnect("BMS_INTERNAL_DC_Max_Players", groupEP, "Max Players Reached On Server");
#endif

					return false;
				}
				else if (banList.ContainsKey(endpoint.Split('+')[0]))
				{
#if NetFX_CORE
					Disconnect("BMS_INTERNAL_DC_Banned", newConnection, "You have been banned from the server for " + Math.Ceiling((banList[endpoint.Split('+')[0]] - DateTime.Now).TotalMinutes) + " more minutes");
#else
					Disconnect("BMS_INTERNAL_DC_Banned", groupEP, "You have been banned from the server for " + Math.Ceiling((banList[groupEP.Address.ToString()] - DateTime.Now).TotalMinutes) + " more minutes");
#endif
					return false;
				}

				ClientManager.RunActionOnPlayerEndpoint(endpoint, (player) => { Disconnect(player); });

#if NetFX_CORE
				sender = new NetworkingPlayer(ServerPlayerCounter++, endpoint, newConnection, "");
#else
				sender = new NetworkingPlayer(ServerPlayerCounter++, endpoint, new IPEndPoint(groupEP.Address, groupEP.Port), "");
#endif

				ClientManager.AddClient(endpoint, sender);
				packetManager.RegisterNewClient(sender);

				OnPlayerConnected(sender);

				WriteReceived(header.updateId, header.packetGroupId, header.packetOrderId, sender);

				lock (writersBlockMutex)
				{
					writeBuffer.Clear();
					ObjectMapper.MapBytes(writeBuffer, sender.NetworkId);
					writeStream.SetProtocolType(Networking.ProtocolType.ReliableUDP);
					writeStream.Prepare(this, NetworkingStream.IdentifierType.Player, 0, writeBuffer, noBehavior: true);
					Write("BMS_INTERNAL_Set_Player_Id", sender, writeStream, true);
				}

				return true;
			}
		}
#endregion

#region Timeouts
		private void ProcessTimeouts()
		{
			foreach (NetworkingPlayer player in timeoutDisconnects)
				Disconnect(player, "Player timed out");

			timeoutDisconnects.Clear();
		}
#endregion

		private void PacketReceived(string endpoint, BMSByte bytes)
		{
			sender = null;

			if (IsServer)
				sender = ClientManager.GetClientFromEndpoint(endpoint);
			else
				sender = server;

			bytes.MoveStartIndex(1);
			readStream.Reset();

			Header header = null;
			if (bytes.Size > 13)
				header = GetPacketHeader(sender, ref bytes);

			if (header != null && header.reliable && (!IsServer || ClientManager.HasEndpoint(endpoint)))
			{
				if (packetManager.HasReadPacket(sender, header))
					return;
			}

			if (header == null || (header.packetOrderId == 0 && header.packetCount == 1))
			{
				if (base.ProcessReceivedData(sender, header == null ? bytes : header.payload, bytes[0], endpoint, CacheUpdate))
					return;
			}

			if (header == null)
				return;

			if (header.packetCount == 1)
			{
				if (!ConsumeStream(header.reliable, header.payload))
				{
					packetManager.PacketRead(sender, header, true);

					if (IsServer && readStream.QueuedRPC)
						RelayStream(header.updateId, readStream);

					return;
				}

				if (readStream.identifierType == NetworkingStream.IdentifierType.Player)
				{
					if (!Connected)
						OnConnected();
				}
			} else {
				packetManager.PacketRead(sender, header);
				return;
			}

			// Something went wrong with the read stream
			if (!readStream.Ready)
				return;

			if (!IsServer)
			{
				sender = server;

				if (readStream.identifierType == NetworkingStream.IdentifierType.Disconnect)
				{
					DisconnectCleanup();
					OnDisconnected(ObjectMapper.Map<string>(readStream));
					return;
				}
			}
			else
			{
				try
				{
					// New player
					if (ObjectMapper.Compare<string>(readStream, "connect"))
					{
						// If the player has not already connected then process connection request
						if (!ClientManager.HasEndpoint(endpoint))
							ProcessClientConnection(endpoint, header);

						return;
					}
					else if (ObjectMapper.Compare<string>(readStream, "update"))
					{
						UpdateNewPlayer(sender);
						return;
					}
					else if (ObjectMapper.Compare<string>(readStream, "disconnect"))
					{
						// TODO:  If this eventually sends something to the player they will not exist
						Disconnect(sender);
						return;
					}
				}
				catch
				{
#if UNITY_EDITOR
					UnityEngine.Debug.LogError("Mal-formed defalut communication from " + groupEP.Address.ToString() + ":" + groupEP.Port);
					//throw new NetworkException(12, "Mal-formed defalut communication");
#endif

					return;
				}

				// Non "connected" clients should not continue after this point
				if (!ClientManager.HasEndpoint(endpoint))
					return;

				// Timeout checks
				ClientManager.Iterate((player) =>
				{
					// Ping the current sender to prevent timouts
					if (player == sender)
					{
						sender = player;
						sender.Ping();
					}
					else
					{
						// Check to see if any clients have passed the timout time
						if ((DateTime.Now - player.LastPing).TotalSeconds > player.InactiveTimeoutSeconds)
							timeoutDisconnects.Add(player);
					}

					return true;
				});

				// Since we just processed ping times, it is time to process any timeouts
				ProcessTimeouts();
			}

			// TODO:  Look into if this is still required
			if (!IsServer && Uniqueidentifier == 0)
				return;

			packetManager.PacketRead(sender, header);
		}

#region Initial Data Read In
		private string incomingEndpoint = string.Empty;
		private BMSByte readBuffer = new BMSByte();
#if NetFX_CORE
		byte[] readBytes = new byte[0];
		private void ReadAsync(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
#elif UNITY_IOS || UNITY_IPHONE
		private void ReadAsync()
#else
		private void ReadAsync(object eventSender, DoWorkEventArgs e)
#endif
		{
#if NetFX_CORE
			DataReader reader = args.GetDataReader();

			readBytes = new byte[reader.UnconsumedBufferLength];

			reader.ReadBytes(readBytes);

			readBuffer.Clone(readBytes);

			if (!IsServer)
				lastReadTime = DateTime.Now;

			PacketReceived(sender.Information.LocalAddress.RawName, readBuffer);

			//ReadStream(args.RemoteAddress.DisplayName, Convert.ToUInt16(args.RemotePort), new NetworkingStream(protocolType).Consume(bytes));
#else
			bool ignoreError = false;
			if (!IsServer)
			{
				lastReadTime = DateTime.Now;
				Thread timeout = new Thread(TimeoutCheck);
				timeout.IsBackground = true;
				timeout.Start();
			}

			do
			{
				ignoreError = false;

				try
				{
					while (true)
					{
#if UNITY_IOS || UNITY_IPHONE
						if (readWorker == null || !readWorker.IsAlive)
							return;
#else
						if (readWorker.CancellationPending)
							return;
#endif

						readBuffer = ReadClient.Receive(ref groupEP, ref incomingEndpoint);

						if (readBuffer == null || readBuffer.Size <= 0)
							continue;

						if (packetDropSimulationChance > 0)
						{
							if (new Random().NextDouble() <= packetDropSimulationChance)
								continue;
						}

						if (NetworkLatencySimulationTime > 0)
						{
							BMSByte tmp = new BMSByte().Clone(readBuffer);
							tmp.ResetPointer();

							latencySimulationPackets.Add(new object[] { DateTime.Now, incomingEndpoint, tmp });

							continue;
						}

						if (!IsServer)
							lastReadTime = DateTime.Now;

						if (TrackBandwidth)
							BandwidthIn += (ulong)readBuffer.Size;

						lock (writersBlockMutex)
						{
							PacketReceived(incomingEndpoint, readBuffer);
						}
					}
				}
				catch (SocketException ex)
				{
					if (ex.ErrorCode == 10038) // JM: ignore this exception
					{
						// This is an iOS standard exception for stopping a socket
					}
					else if (ex.ErrorCode != 10004)
					{
						// TODO:  In the master server capture this error and see who it is, then remove them from the hosts list

						if (Networking.IsBareMetal)
							Console.WriteLine(ex.Message + " | " + ex.StackTrace);
						else
						{
							// The host forcefully disconnected
							if (ex.ErrorCode == 10054)
							{
								if (IsServer)
									ignoreError = true;
								else
									OnDisconnected("The connection has been forcefully closed");
							}
#if UNITY_EDITOR
							else
							{

								UnityEngine.Debug.LogException(ex);
								UnityEngine.Debug.LogError("Error Code:" + ex.ErrorCode);
							}
#endif
						}
					}
				}
				catch (ThreadAbortException abortEx)
				{
					Console.WriteLine("Closing down on thread abort" + abortEx.StackTrace);
					//Ignore thread aborts
				}
				catch (Exception ex)
				{
#if BARE_METAL
					Console.WriteLine(ex.Message + " | " + ex.StackTrace);
#else
					UnityEngine.Debug.LogException(ex);
#if UNITY_STANDALONE
					string file = "Forge-" + (IsServer ? "Server" : "Client-" + Me.NetworkId) + "-error.txt";
					string message = ex.Message + "\r\n" + ex.StackTrace;
					if (!System.IO.File.Exists(file))
						System.IO.File.WriteAllText(file, message);
					else
						System.IO.File.AppendAllText(file, message);
#endif
#endif
				}
			} while (ignoreError);
#endif
		}
#endregion
	}
}
#endif