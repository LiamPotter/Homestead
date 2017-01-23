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
using System.Collections.Generic;
using System.Linq;

#if UNITY_5_3
#endif

#if !NetFX_CORE
#if !UNITY_WEBGL
using System.Net;
using System.Threading;
#endif
#else
using Windows.Networking.Sockets;
#endif

namespace BeardedManStudios.Network
{
#if BARE_METAL
	public abstract class NetWorker : MarshalByRefObject
#else
	public abstract class NetWorker
#endif
	{
		public const string CLIENT_READY_DYNAMIC_COMMAND = "ready";

		protected const int READ_THREAD_TIMEOUT = 100;
		public string Host { get; protected set; }
		public ushort Port { get; protected set; }
		public string AuthHash = "ThankYouForUsingForgeNetworking";
		public bool UsingUnityEngine { get; protected set; }

		private int threadSpeed = READ_THREAD_TIMEOUT;
		protected int ThreadSpeed { get { return threadSpeed; } set { threadSpeed = value; } }
		public void SetThreadSpeed(int speed) { ThreadSpeed = speed; }
		public NetworkingPlayer CurrentStreamOwner { get; protected set; }

		public Dictionary<string, DateTime> banList = new Dictionary<string, DateTime>();

		public bool MasterServerFlag { get; set; }

		/// <summary>
		/// The maximum connections allowed on this NetWorker(Socket)
		/// </summary>
		public int MaxConnections { get; set; } // JM: made set public in case server wants to change value after startup

		/// <summary>
		/// Current amount of connections
		/// </summary>
		public int Connections { get; private set; }

		/// <summary>
		/// Players conencted to this NetWorker(Socket)
		/// </summary>
		public virtual List<NetworkingPlayer> Players { get; protected set; }

		/// <summary>
		/// The cached write stream
		/// </summary>
		protected NetworkingStream writeStream = new NetworkingStream();
		protected object writeMutex = new object();

		/// <summary>
		/// Assigns a list of players for a client
		/// </summary>
		public void AssignPlayers(List<NetworkingPlayer> playerList)
		{
			Players = playerList;
		}

		protected object rpcBuffersMutex = new Object();
		protected Dictionary<ulong, List<NetworkingStream>> rpcBuffer = new Dictionary<ulong, List<NetworkingStream>>();
		protected Dictionary<ulong, List<KeyValuePair<uint, NetworkingStream>>> udpRpcBuffer = new Dictionary<ulong, List<KeyValuePair<uint, NetworkingStream>>>();

		protected Dictionary<string, List<Action<NetworkingPlayer>>> dynamicCommands = new Dictionary<string, List<Action<NetworkingPlayer>>>();

		/// <summary>
		/// Basic event response delegate
		/// </summary>
		public delegate void BasicEvent();

		/// <summary>
		/// Basic event response delegate
		/// </summary>
		public delegate void PingReceived(HostInfo host, int time);

#if !UNITY_WEBGL
		// JM: Added for threaded lan discovery
		public delegate void LANEndPointFound(IPEndPoint endpoint);
#endif
		
	  /// <summary>
		/// Network Exception response delegate
		/// </summary>
		/// <param name="exception">Exception thrown</param>
		public delegate void NetworkErrorEvent(Exception exception);

		/// <summary>
		/// Network Message response delegate
		/// </summary>
		/// <param name="stream">Stream responded with</param>
		public delegate void NetworkMessageEvent(NetworkingStream stream);

		/// <summary>
		/// Direct Network Message response delegate
		/// </summary>
		/// <param name="player">Player responded with</param>
		/// <param name="stream">Stream responded with</param>
		public delegate void DirectNetworkMessageEvent(NetworkingPlayer player, NetworkingStream stream);

		/// <summary>
		/// Direct Raw Network Message response delegate
		/// </summary>
		/// <param name="data">Stream responded with</param>
		public delegate void DirectRawNetworkMessageEvent(NetworkingPlayer player, BMSByte data);

		/// <summary>
		/// Player connection response delegate
		/// </summary>
		/// <param name="player">Player who connected</param>
		public delegate void PlayerConnectionEvent(NetworkingPlayer player);

		/// <summary>
		/// Player banned response delegate
		/// </summary>
		/// <param name="ip">The ip address that was banned</param>
		/// <param name="time">The time that the ip address' ban will be lifted</param>
		public delegate void PlayerBannedEvent(string ip, DateTime time);

		/// <summary>
		/// An delegate signature to use for registering events around dynamic commands
		/// </summary>
		/// <param name="player">The player that called the dynamic command</param>
		public delegate void DynamicCommandEvent(NetworkingPlayer player);

		/// <summary>
		/// String response delegate
		/// </summary>
		/// <param name="message">String message responded with</param>
		public delegate void StringResponseEvent(string message);

		/// <summary>
		/// Byte array response delegate
		/// </summary>
		/// <param name="bytes">Byte array responded with</param>
		public delegate void ByteResponseEvent(byte[] bytes);

		/// <summary>
		/// This will make it so that only players who are close to one another will get updates about each other
		/// </summary>
		public bool ProximityBasedMessaging { get; set; }

		/// <summary>
		/// This is the distance in Unity units of the range that players need to be in to get updates about each other
		/// </summary>
		public float ProximityMessagingDistance { get; set; }

		protected List<NetworkingPlayer> alreadyUpdated = new List<NetworkingPlayer>();

		/// <summary>
		/// For determining if nat hole punching should be used
		/// </summary>
		public bool UseNatHolePunch { get; set; }

		/// <summary>
		/// When this is true the bandwidth usage will be tracked
		/// </summary>
		public bool TrackBandwidth { get; set; }

		/// <summary>
		/// This represents all of the bytes that have came in
		/// </summary>
		public static ulong BandwidthIn { get; protected set; }

		/// <summary>
		/// This represents all of the bytes that have went out
		/// </summary>
		public static ulong BandwidthOut { get; protected set; }

#if !UNITY_WEBGL
		protected IPEndPoint hostEndpoint = null;
#endif

#if NetFX_CORE
		protected object groupEP = null;
#elif !UNITY_WEBGL
		protected IPEndPoint groupEP = null;
#endif

		protected NetworkingPlayer server = null;

		/// <summary>
		/// This will turn on proximity based messaging, see ProximityBasedMessaging property of this class
		/// </summary>
		/// <param name="updateDistance">The distance in Unity units of the range that players need to be in to get updates about each other</param>
		public void MakeProximityBased(float updateDistance)
		{
			ProximityBasedMessaging = true;
			ProximityMessagingDistance = updateDistance;
		}

		/// <summary>
		/// This is a referenct to the current players identity on the Network (server and client)
		/// </summary>
		public NetworkingPlayer Me { get; protected set; }

#region Events
		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) connects
		/// </summary>
		public event BasicEvent connected
		{
			add
			{
				connectedInvoker += value;
			}
			remove
			{
				connectedInvoker -= value;
			}
		}
		BasicEvent connectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) disconnects
		/// </summary>
		public event BasicEvent disconnected
		{
			add
			{
				disconnectedInvoker += value;
			}
			remove
			{
				disconnectedInvoker -= value;
			}
		} BasicEvent disconnectedInvoker;   // Because iOS doesn't have a JIT - Multi-cast function pointer.

		public int ConnectTimeout = 5000;
		/// <summary>
		/// An event that will fire if no connection could be established within a given time period
		/// </summary>
		public event BasicEvent connectTimeout
		{
			add
			{
				connectTimeoutInvoker += value;
			}
			remove
			{
				connectTimeoutInvoker -= value;
			}
		}
		BasicEvent connectTimeoutInvoker;    // Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when a server disconnects
		/// </summary>
		public event StringResponseEvent serverDisconnected
		{
			add
			{
				serverDisconnectedInvoker += value;
			}
			remove
			{
				serverDisconnectedInvoker -= value;
			}
		} StringResponseEvent serverDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		public event DynamicCommandEvent clientReady
		{
			add
			{
				clientReadyInvoker += value;
			}
			remove
			{
				clientReadyInvoker -= value;
			}
		} DynamicCommandEvent clientReadyInvoker;

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) receives and error
		/// </summary>
		public event NetworkErrorEvent error
		{
			add
			{
				errorInvoker += value;
			}
			remove
			{
				errorInvoker -= value;
			}
		} NetworkErrorEvent errorInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) sends data
		/// </summary>
		public event NetworkMessageEvent dataSent
		{
			add
			{
				dataSentInvoker += value;
			}
			remove
			{
				dataSentInvoker -= value;
			}
		} NetworkMessageEvent dataSentInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnDataSent(NetworkingStream stream) { if (dataSentInvoker != null) dataSentInvoker(stream); }

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) reads data
		/// </summary>
		public event DirectNetworkMessageEvent dataRead
		{
			add
			{
				dataReadInvoker += value;
			}
			remove
			{
				dataReadInvoker -= value;
			}
		} DirectNetworkMessageEvent dataReadInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) reads raw data
		/// </summary>
		public event DirectRawNetworkMessageEvent rawDataRead
		{
			add
			{
				rawDataReadInvoker += value;
			}
			remove
			{
				rawDataReadInvoker -= value;
			}
		} DirectRawNetworkMessageEvent rawDataReadInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) recieves another player
		/// </summary>
		public event PlayerConnectionEvent playerConnected
		{
			add
			{
				playerConnectedInvoker += value;
			}
			remove
			{
				playerConnectedInvoker -= value;
			}
		} PlayerConnectionEvent playerConnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnPlayerConnected(NetworkingPlayer player)
		{
			player.Connected = true;

			if (playerConnectedInvoker != null)
			{
#if BARE_METAL
				playerConnectedInvoker(player);
#else
				Unity.MainThreadManager.Run(delegate()
				{
					playerConnectedInvoker(player);
				});
#endif
			}

			Connections++;
		}

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) disconnects another player
		/// </summary>
		public event PlayerConnectionEvent playerDisconnected
		{
			add
			{
				playerDisconnectedInvoker += value;
			}
			remove
			{
				playerDisconnectedInvoker -= value;
			}
		} PlayerConnectionEvent playerDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket)/server bans a player
		/// </summary>
		public event PlayerBannedEvent playerBanned
		{
			add
			{
				playerBannedInvoker += value;
			}
			remove
			{
				playerBannedInvoker -= value;
			}
		} PlayerBannedEvent playerBannedInvoker;
#endregion

#region Event Child Callers
#if NetFX_CORE
		protected async void OnConnected()
#else
		protected void OnConnected()
#endif
		{
#if NetFX_CORE
			await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(50));
#elif !UNITY_WEBGL
			Thread.Sleep(50);
#endif

			Connected = true;
			Disconnected = false;

#if BARE_METAL
			if (connectedInvoker != null)
				connectedInvoker();
#else
			if (connectedInvoker != null)
			{
				try
				{
					if (!UsingUnityEngine)
						connectedInvoker();
					else
					{
						// If there is not a MAIN_THREAD_MANAGER then throw the error and disconnect
						Unity.MainThreadManager.Run(delegate ()
						{
							connectedInvoker();
						});
					}
				}
#if UNITY_EDITOR
				catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
#else
				catch
				{
#endif
					Disconnect();
				}
			}
#endif
		}

		protected void OnDisconnected()
		{
			Connected = false;
			Disconnected = true;

			if (disconnectedInvoker != null)
				disconnectedInvoker();
		}

		protected void OnDisconnected(string reason)
		{
			if (IsServer) return;

			Connected = false;
			Disconnected = true;

			if (serverDisconnectedInvoker != null) serverDisconnectedInvoker(reason);
		}

		protected void OnConnectTimeout()
		{
			if (IsServer) return;

			Connected = false;
			Disconnected = true;

			if (connectTimeoutInvoker != null) connectTimeoutInvoker();
		}

		protected void OnError(Exception exception)
		{
			if (errorInvoker != null)
			{
#if BARE_METAL
				errorInvoker(exception);
#else
				Unity.MainThreadManager.Run(delegate() { errorInvoker(exception); });
#endif
			}
		}

		protected void OnDataRead(NetworkingPlayer player, NetworkingStream stream)
		{
			if (dataReadInvoker != null) dataReadInvoker(player, stream);

			if (stream.identifierType == NetworkingStream.IdentifierType.Custom)
				OnCustomDataRead(stream.Customidentifier, player, stream);
		}

		protected void OnRawDataRead(NetworkingPlayer sender, BMSByte data)
		{
			if (rawDataReadInvoker != null)
			{
				data.MoveStartIndex(sizeof(byte));
				rawDataReadInvoker(sender, data);
			}
		}
#endregion

#region Timeout Disconnect
		protected DateTime lastReadTime;
		public int LastRead
		{
			get
			{
				return (int)(DateTime.Now - lastReadTime).TotalMilliseconds;
			}
		}
		public int ReadTimeout = 0;

#if NetFX_CORE
		protected async void TimeoutCheck()
#else
		protected void TimeoutCheck()
#endif
		{
			while (true)
			{
				if (ReadTimeout == 0)
				{
#if NetFX_CORE
					await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(3000));
#else
					System.Threading.Thread.Sleep(3000);
#endif
					continue;
				}

#if NetFX_CORE
				await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(ReadTimeout - LastRead + 1));
#else
				System.Threading.Thread.Sleep(ReadTimeout - LastRead + 1);
#endif

				if (Connected && LastRead >= ReadTimeout)
				{
					TimeoutDisconnect();
				}
			}
		}

		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) disconnects
		/// </summary>
		public event BasicEvent timeoutDisconnected
		{
			add
			{
				timeoutDisconnectedInvoker += value;
			}
			remove
			{
				timeoutDisconnectedInvoker -= value;
			}
		} BasicEvent timeoutDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnTimeoutDisconnected()
		{
			Connected = false;
			Disconnected = true;

			if (timeoutDisconnectedInvoker != null)
				timeoutDisconnectedInvoker();
		}
#endregion

		public void ThrowException(NetworkException exception) { OnError(exception); }

		private Dictionary<uint, Action<NetworkingPlayer, NetworkingStream>> customDataRead = new Dictionary<uint, Action<NetworkingPlayer, NetworkingStream>>();

		/// <summary>
		/// Add a custom event to the NetWorker(Socket) read event
		/// </summary>
		/// <param name="id">Unique identifier to pass with</param>
		/// <param name="action">Action to be added to the events to be called upon</param>
		/// <param name="explicitOverwrite">If set to true then this is a purposeful overwrite of an id</param>
		public void AddCustomDataReadEvent(uint id, Action<NetworkingPlayer, NetworkingStream> action, bool explicitOverwrite = false)
		{
			if (!customDataRead.ContainsKey(id))
				customDataRead.Add(id, action);
			else
			{
				if (!explicitOverwrite)
					UnityEngine.Debug.LogWarning("You are overwriting a custom identifier for id " + id);

				customDataRead[id] = action;
			}
		}

		/// <summary>
		/// Adds a callback function for when a specified command is recieved on the Network
		/// </summary>
		/// <param name="command">The name of the command to listen for</param>
		/// <param name="action">The callback action to add to the stack</param>
		public void AddDynaicCommandEvent(string command, Action<NetworkingPlayer> action)
		{
			if (!dynamicCommands.ContainsKey(command))
				dynamicCommands.Add(command, new List<Action<NetworkingPlayer>>());

			dynamicCommands[command].Add(action);
		}

		/// <summary>
		/// Removes all callbacks from a given command
		/// </summary>
		/// <param name="command">The name of the command to remove all callbacks for</param>
		public void RemoveDynaicCommandEvent(string command)
		{
			if (dynamicCommands.ContainsKey(command))
				dynamicCommands.Remove(command);
		}

		/// <summary>
		/// Removes a specific callback from a given command event stack
		/// </summary>
		/// <param name="command">The name of the command to remove a specified callback for</param>
		/// <param name="action">The specific callback to remove from the command stack</param>
		public void RemoveDynaicCommandEvent(string command, System.Action<NetworkingPlayer> action)
		{
			if (dynamicCommands.ContainsKey(command))
				dynamicCommands[command].Remove(action);
		}

		/// <summary>
		/// Remove a custom event from the NetWorker(Socket) read event
		/// </summary>
		/// <param name="id">Unique identifier to pass with</param>
		public void RemoveCustomDataReadEvent(uint id) { if (customDataRead.ContainsKey(id)) customDataRead.Remove(id); }

		protected void OnCustomDataRead(uint id, NetworkingPlayer player, NetworkingStream stream) { if (customDataRead.ContainsKey(id)) customDataRead[id](player, stream); }

		protected void OnPlayerDisconnected(NetworkingPlayer player)
		{
			Unity.MainThreadManager.Run(() =>
			{
				foreach (SimpleNetworkedMonoBehavior behavior in SimpleNetworkedMonoBehavior.NetworkedBehaviors.Values)
				{
					if (behavior == null)
						continue;

#if NetFX_CORE
					if (behavior is NetworkedMonoBehavior)
#else
					if (behavior.GetType().IsSubclassOf(typeof(NetworkedMonoBehavior)))
#endif
					{
						NetworkedMonoBehavior nBehavior = (NetworkedMonoBehavior)behavior;
						if (nBehavior.OwnerId == player.NetworkId) 
						{
							if (nBehavior.isPlayer || nBehavior.destroyOnDisconnect) // JM: added destroy on disconnect.  code in snmb to destroy would not run in time
							{
								Networking.Destroy(nBehavior);
							} 
							else
							{
								// JM: assign undeleted objects to server on disconnect
								nBehavior.ChangeOwner(0);
							}
						}
					}
				}
			});

			if (Players.Contains(player))
				Players.Remove(player);

			if (playerDisconnectedInvoker != null)
			{
#if BARE_METAL
				playerDisconnectedInvoker(player);
#else
				Unity.MainThreadManager.Run(delegate ()
				{
					playerDisconnectedInvoker(player);
				});
#endif
			}

			Connections--;
		}

		/// <summary>
		/// Returns a value whether or not this NetWorker(Socket) is the server
		/// </summary>
		public bool IsServer
		{
			get
			{
#if !UNITY_WEBGL
				if (this is DefaultServerTCP)
					return true;

				if (this is WinMobileServer)
					return true;

				if (this is CrossPlatformUDP)
					return ((CrossPlatformUDP)this).IsServer;
#endif

				return false;
			}
		}

		/// <summary>
		/// The player count on this NetWorker(Socket)
		/// </summary>
		public ulong ServerPlayerCounter { get; protected set; }

		/// <summary>
		/// Whether or not this NetWorker(Socket) is connected
		/// </summary>
		public bool Connected { get; protected set; }

		/// <summary>
		/// Whether or not this NetWorker(Socket) was once connected and now is disconnected
		/// </summary>
		public bool Disconnected { get; protected set; }

		/// <summary>
		/// The unique identifier for this socket (Often known as the player id)
		/// </summary>
		public ulong Uniqueidentifier { get; private set; }

#region Constructors and Destructor
		/// <summary>
		/// Constructor of the NetWorker(Socket)
		/// </summary>
		public NetWorker()
		{
#if !BARE_METAL
			Unity.NetWorkerKiller.AddNetWorker(this);
#endif
			UsingUnityEngine = true;
		}

		/// <summary>
		/// Constructor of the NetWorker(Socket) with a Maximum allowed connections count
		/// </summary>
		/// <param name="maxConnections">The maximum number of connections allowed on this NetWorker(Socket)</param>
		public NetWorker(int maxConnections)
		{
			MaxConnections = maxConnections;
#if !BARE_METAL
			Unity.NetWorkerKiller.AddNetWorker(this);
#endif
			UsingUnityEngine = true;
		}

		public NetWorker(int maxConnections, bool usingUnityEngine)
		{
			MaxConnections = maxConnections; // JM: fixed typo assigning MaxConnections to self

			if (usingUnityEngine)
				Unity.NetWorkerKiller.AddNetWorker(this);

			UsingUnityEngine = usingUnityEngine;
		}
		~NetWorker() { }
#endregion

		abstract public void Connect(string hostAddress, ushort port);

		abstract public void Disconnect();

		abstract public void TimeoutDisconnect();

		/// <summary>
		/// Disconnect a player on this NetWorker(Socket)
		/// </summary>
		/// <param name="player">Player to disconnect</param>
		public virtual void Disconnect(NetworkingPlayer player, string reason = null)
		{
			if (alreadyUpdated.Contains(player))
				alreadyUpdated.Remove(player);

			OnPlayerDisconnected(player);
		}

#region Write
		abstract public void Write(NetworkingStream stream);

		abstract public void Write(NetworkingPlayer player, NetworkingStream stream);

		abstract public void Send(byte[] data, int length, object endpoint = null);

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		public virtual void Write(string updateidentifier, NetworkingStream stream, bool reliable = false) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(uint updateidentifier, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier, Player, and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to write with</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(string updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier, Player, and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to write with</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(uint updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		public virtual void WriteRaw(NetworkingPlayer player, BMSByte data, string uniqueId, bool reliable = false) { }
		public virtual void WriteRaw(BMSByte data, string uniqueId, bool relayToServer = true, bool reliable = false) { }
#endregion

		/// <summary>
		/// Read the data of a player and data stream
		/// </summary>
		/// <param name="player">Player to read from</param>
		/// <param name="stream">Network stream being read from</param>
		public void DataRead(NetworkingPlayer player, NetworkingStream stream)
		{
			OnDataRead(player, stream);
		}

		/// <summary>
		/// Get the new player updates
		/// </summary>
		public virtual void GetNewPlayerUpdates() { }

		public void ClearBufferRPC()
		{
			if (!IsServer)
				return;

			lock (rpcBuffersMutex)
			{
				udpRpcBuffer.Clear();
				rpcBuffer.Clear();
			}
		}

		protected void ServerBufferRPC(NetworkingStream stream)
		{
			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
				lock (rpcBuffersMutex)
				{
					if (!rpcBuffer.ContainsKey(stream.RealSenderId))
						rpcBuffer.Add(stream.RealSenderId, new List<NetworkingStream>());

					NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType);
					clonedStream.Bytes.BlockCopy(stream.Bytes.byteArr, stream.Bytes.StartIndex(), stream.Bytes.Size);
					clonedStream.AssignSender(Me, stream.NetworkedBehavior);

					rpcBuffer[stream.RealSenderId].Add(clonedStream);
				}
			}
		}

		protected void RelayStream(NetworkingStream stream)
		{
			if (stream.Receivers == NetworkReceivers.Server)
				return;

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
				NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType).PrepareFinal(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, senderId: stream.RealSenderId);
				clonedStream.AssignSender(stream.Sender, stream.NetworkedBehavior);

				lock (rpcBuffersMutex)
				{
					if (!rpcBuffer.ContainsKey(stream.RealSenderId))
						rpcBuffer.Add(stream.RealSenderId, new List<NetworkingStream>());

					rpcBuffer[stream.RealSenderId].Add(clonedStream);
				}
			}

			lock (writeMutex)
			{
				writeStream.SetProtocolType(stream.ProtocolType);
				writeStream.Prepare(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId, noBehavior: ReferenceEquals(stream.NetworkedBehavior, null));

				// Write what was read to all the clients
				Write(writeStream);
			}
		}

		protected void ServerBufferRPC(uint updateidentifier, NetworkingStream stream)
		{
			if (stream.Receivers != NetworkReceivers.AllBuffered && stream.Receivers != NetworkReceivers.OthersBuffered)
				return;

			if (!ReferenceEquals(stream.NetworkedBehavior, null) && !stream.NetworkedBehavior.IsClearedForBuffer)
			{
				stream.NetworkedBehavior.ResetBufferClear();
				return;
			}

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
				lock (rpcBuffersMutex)
				{
					if (!udpRpcBuffer.ContainsKey(stream.RealSenderId))
						udpRpcBuffer.Add(stream.RealSenderId, new List<KeyValuePair<uint, NetworkingStream>>());

					NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType);
					clonedStream.Bytes.BlockCopy(stream.Bytes.byteArr, stream.Bytes.StartIndex(), stream.Bytes.Size);
					clonedStream.AssignSender(Me, stream.NetworkedBehavior);

					udpRpcBuffer[stream.RealSenderId].Add(new KeyValuePair<uint, NetworkingStream>(updateidentifier, clonedStream));
				}
			}
		}

		protected void RelayStream(uint updateidentifier, NetworkingStream stream)
		{
			lock(writeMutex)
			{
				writeStream.SetProtocolType(stream.ProtocolType);
				writeStream.Prepare(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId, noBehavior: ReferenceEquals(stream.NetworkedBehavior, null));

				Write(updateidentifier, writeStream);
			}
		}

		protected void UpdateNewPlayer(NetworkingPlayer player)
		{
			if (alreadyUpdated.Contains(player))
				return;

			alreadyUpdated.Add(player);

			lock (rpcBuffersMutex)
			{
				if (rpcBuffer.Count > 0)
				{
					foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
					{
						foreach (NetworkingStream stream in kv.Value)
						{
							Write(player, stream);
						}
					}
				}

				if (udpRpcBuffer.Count > 0)
				{
					foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
					{
						foreach (KeyValuePair<uint, NetworkingStream> stream in kv.Value)
						{
							Write(stream.Key, player, stream.Value, true);
						}
					}
				}
			}
		}

		protected void CleanRPCForPlayer(NetworkingPlayer player)
		{
			lock (rpcBuffersMutex)
			{
				if (rpcBuffer.ContainsKey(player.NetworkId))
					rpcBuffer.Remove(player.NetworkId);
			}
		}

		protected void CleanUDPRPCForPlayer(NetworkingPlayer player)
		{
			lock (rpcBuffersMutex)
			{
				if (udpRpcBuffer.ContainsKey(player.NetworkId))
					udpRpcBuffer.Remove(player.NetworkId);
			}
		}

		/// <summary>
		/// Executes a custom read on an id, player and a stream
		/// </summary>
		/// <param name="id">The id of this read</param>
		/// <param name="player">The player to call this read</param>
		/// <param name="stream">The stream to pass the read to</param>
		public void ExecuteCustomRead(uint id, NetworkingPlayer player, NetworkingStream stream)
		{
			customDataRead[id](player, stream);
		}

		/// <summary>
		/// Assign a unique id to this NetWorker(Socket)
		/// </summary>
		/// <param name="id">Unique ID to assign with</param>
		public void AssignUniqueId(ulong id)
		{
			Uniqueidentifier = id;
		}

		/// <summary>
		/// Removes a buffered ID from the Networker
		/// </summary>
		/// <param name="id"></param>
		public bool ClearBufferedInstantiateFromID(ulong id)
		{
			bool removedSuccessfull = false;
			if (IsServer)
			{
				ulong key = 0;
				int x = 0;
				byte[] uniqueID = new byte[sizeof(ulong)];
				BMSByte streamBytes = null;
				int unique = -1;
				string methodName = string.Empty;
				ulong NetworkID = 0;

				lock (rpcBuffersMutex)
				{
#if !UNITY_WEBGL
					if (this is CrossPlatformUDP)
					{
						if (udpRpcBuffer.Count > 0)
						{
							foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
							{
								x = 0;
								foreach (KeyValuePair<uint, NetworkingStream> stream in kv.Value)
								{
									if (!ReferenceEquals(stream.Value.NetworkedBehavior, NetworkingManager.Instance))
									{
										x++;
										continue;
									}

									streamBytes = stream.Value.Bytes;

									for (int i = 0; i < sizeof(int); ++i)
										uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.STREAM_UNIQUE_ID + i];

									unique = BitConverter.ToInt32(uniqueID, 0);

									if (NetworkingManager.Instance.GetRPC(unique) != null)
									{
										methodName = NetworkingManager.Instance.GetRPC(unique).Name;

										if (methodName == NetworkingStreamRPC.INSTANTIATE_METHOD_NAME)
										{
											for (int i = 0; i < uniqueID.Length; ++i)
												uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.NetWORKING_UNIQUE_ID + i];

											NetworkID = BitConverter.ToUInt64(uniqueID, 0);

											if (NetworkID == id)
											{
												removedSuccessfull = true;
												key = kv.Key;
												break;
											}
										}
									}

									if (removedSuccessfull)
										break;

									x++;
								}

								if (removedSuccessfull)
									break;
							}

							if (removedSuccessfull)
							{
								//Successfully removed the instantiate from the buffer
#if NetWORKING_DEBUG_BUFFER
								string debugText = "UDP BUFFER\n=================\nBefore:\n";
								foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
								{
									debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
								}
								debugText += "Remove Key[" + key + "] x[" + x + "]\n";
#endif
								udpRpcBuffer[key].RemoveAt(x);

								for (int i = 0; i < udpRpcBuffer[key].Count; ++i)
								{
									// If any part of this buffer is invalid or the id matches then mark it to be removed
									if (ReferenceEquals(udpRpcBuffer[key][i].Value, null) || ReferenceEquals(udpRpcBuffer[key][i].Value.NetworkedBehavior, null) || udpRpcBuffer[key][i].Value.NetworkedBehavior.NetworkedId == id)
									{
										udpRpcBuffer[key].RemoveAt(i--);
									}
								}
#if NetWORKING_DEBUG_BUFFER
								debugText += "=================\nAfter:\n";
								foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
								{
									debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
								}
								UnityEngine.Debug.Log(debugText);
#endif
							}
						}
					}
					else
					{
#endif
						if (rpcBuffer.Count > 0)
						{
							foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
							{
								x = 0;

								foreach (NetworkingStream stream in kv.Value)
								{
									if (!ReferenceEquals(stream.NetworkedBehavior, NetworkingManager.Instance))
										continue;

									streamBytes = stream.Bytes;

									for (int i = 0; i < sizeof(int); ++i)
										uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.STREAM_UNIQUE_ID + i];

									unique = BitConverter.ToInt32(uniqueID, 0);

									if (NetworkingManager.Instance.GetRPC(unique) != null)
									{
										methodName = NetworkingManager.Instance.GetRPC(unique).Name;

										if (methodName == NetworkingStreamRPC.INSTANTIATE_METHOD_NAME)
										{
											for (int i = 0; i < uniqueID.Length; ++i)
												uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.NetWORKING_UNIQUE_ID + i];

											NetworkID = BitConverter.ToUInt64(uniqueID, 0);

											if (NetworkID == id)
											{
												removedSuccessfull = true;
												key = kv.Key;
												break;
											}
										}
									}

									if (removedSuccessfull)
										break;

									x++;
								}

								if (removedSuccessfull)
									break;
							}

							if (removedSuccessfull)
							{
								//Successfully removed the instantiate from the buffer
#if NetWORKING_DEBUG_BUFFER
							string debugText = "RPC BUFFER\n=================\nBefore:\n";
							foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";								
							}
							debugText += "Remove Key[" + key + "] x[" + x + "]\n";
#endif
								rpcBuffer[key].RemoveAt(x);
								for (int i = 0; i < rpcBuffer[key].Count; ++i)
								{
									if (rpcBuffer[key][i].NetworkedBehavior.NetworkedId == id)
										rpcBuffer[key].RemoveAt(i--);
								}
#if NetWORKING_DEBUG_BUFFER
							debugText += "=================\nAfter:\n";
							foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
							}
							UnityEngine.Debug.Log(debugText);
#endif
							}
						}
#if !UNITY_WEBGL
					}
#endif
				}
			}

			return removedSuccessfull;
		}

		public double PreviousServerPing { get; protected set; }						// Milliseconds
		protected DateTime previousPingTime;
		protected bool sendNewPing = true;

		public delegate void PingEvent(string ipAndPort);
		public event PingEvent pingEvent
		{
			add
			{
				pingEventInvoker += value;
			}
			remove
			{
				pingEventInvoker -= value;
			}
		} PingEvent pingEventInvoker;


		public void Ping(object endpoint = null, object overrideHost = null)
		{
#if UNITY_WEBGL
			var overridedHost = overrideHost;
#else
			var overridedHost = overrideHost as IPEndPoint;
#endif
			if (IsServer && endpoint == null)
			{
#if !NetFX_CORE
				if (overrideHost == null)
#endif
					return;
			}

			byte[] ping = new byte[1] { 3 };
			try
			{
			   if (overridedHost != null)
			   {
				  Send(ping, ping.Length, overridedHost);
			   }
			   else
			   {
#if UNITY_WEBGL
					Send(ping, ping.Length);
#else
					if (IsServer)
					 Send(ping, ping.Length, endpoint);
				  else
					 Send(ping, ping.Length, hostEndpoint);
#endif
				}
			}
			catch
			{
				if (!IsServer)
					Disconnect();
			}
		}

		public void ExecutedPing()
		{
			PreviousServerPing = (DateTime.Now - previousPingTime).TotalMilliseconds;
			sendNewPing = true;
		}

		public virtual void ProcessReliableUDPRawMessage(BMSByte rawBuffer) { }

		protected BMSByte rawBuffer = new BMSByte();
		public virtual bool ProcessReceivedData(NetworkingPlayer sender, BMSByte bytes, byte startByte, string endpoint = "", Action<NetworkingPlayer> cacheUpdate = null)
		{
			if (bytes.Size == 0)
			{
				if (bytes.byteArr[0] == 3)
				{
#if !UNITY_WEBGL
					if (IsServer)
					{
						// TODO:  Implement ping for WinRT
						if (pingEventInvoker != null && !string.IsNullOrEmpty(endpoint))
							pingEventInvoker(endpoint);
						
						if (sender == null)
						{
							var splitEndpoint = endpoint.Split('+');

#if NetFX_CORE
							Ping(new IPEndPoint(splitEndpoint[0], int.Parse(splitEndpoint[1]) ));
#else
							Ping(new IPEndPoint(IPAddress.Parse(splitEndpoint[0]), int.Parse(splitEndpoint[1])));
#endif
						}
						else
							Ping(sender.SocketEndpoint);
					}
					else
#endif
						ExecutedPing();

					return true;
				}

#if !UNITY_WEBGL
				if (IsServer)
					Send(new byte[1] { 1 }, 1, groupEP);
#endif

				return true;
			}

			if (startByte != 0)
			{
				if (!IsServer)
					sender = server;
				else if (sender != null)
					sender.Ping();

				rawBuffer.Clone(bytes);

				switch (rawBuffer.byteArr[0])
				{
					case 1:	// User raw write
						OnRawDataRead(sender, rawBuffer);
						break;
					case 2:	// Scene load raw write
						// The server is never told what scene to load
						if (IsServer) return true;
						string sceneName = rawBuffer.GetString(1);
#if UNITY_5_3
						Unity.MainThreadManager.Run(() => { Unity.UnitySceneManager.LoadScene(sceneName); });
#else
						Unity.MainThreadManager.Run(() => { UnityEngine.Application.LoadLevel(sceneName); });
#endif
						break;
					case 3:	// Cache request
						// Retiring old method of dealing with cache in favor of WriteCustom
						//if (IsServer)
						//{
						//	if (sender != null)
						//		Cache.NetworkRead(rawBuffer, sender);
						//}
						//else
						//	Cache.NetworkRead(rawBuffer);
						break;
					case 4: // Nat registration request
#if !UNITY_WEBGL
						if (IsServer)
						{
							if (!ReferenceEquals(ForgeMasterServer.Instance, null))
							{
								if (rawBuffer.byteArr[1] == 1)
								{
#if !NetFX_CORE
									string[] parts = endpoint.Split('+');
									ushort internalPort = BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									ForgeMasterServer.Instance.RegisterNatRequest(parts[0], ushort.Parse(parts[1]), internalPort);
#endif
								}
								else if (rawBuffer.byteArr[1] == 2)
								{
									string[] parts = endpoint.Split('+');
									ushort internalPort = BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									ushort targetPort = BitConverter.ToUInt16(rawBuffer.byteArr, 4);
									string targetHost = Encryptor.Encoding.GetString(rawBuffer.byteArr, 6, rawBuffer.byteArr.Length - 7);

									ForgeMasterServer.Instance.PullNatRequest(parts[0], ushort.Parse(parts[1]), internalPort, targetHost, targetPort);
								}
							}
							else
							{

#if !NetFX_CORE
								if (rawBuffer.byteArr[1] == 3)
								{
									ushort targetPort = BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									string targetHost = Encryptor.Encoding.GetString(rawBuffer.byteArr, 4, rawBuffer.byteArr.Length - 4);

									IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(targetHost), targetPort);

									Send(new byte[] { 4, 4, 0 }, 3, targetEndpoint);
								}
#endif
							}

							return true;
						}
#endif
						break;
					case 5:
						if (cacheUpdate != null)
							cacheUpdate(sender);
						break;
					case 6: // Set the message group for this client
						if (IsServer)
						{
							if (sender != null)
								sender.SetMessageGroup(BitConverter.ToUInt16(rawBuffer.byteArr, 1));
						}
						break;
					case 7: // Dynamic command
						string command = rawBuffer.GetString(1);

						if (command == CLIENT_READY_DYNAMIC_COMMAND && clientReadyInvoker != null)
							clientReadyInvoker(sender);

						if (dynamicCommands.ContainsKey(command))
						{
							foreach (Action<NetworkingPlayer> callback in dynamicCommands[command])
								callback(sender);
						}
						break;
				}

				return true;
			}

			return false;
		}

		public void BanPlayer(ulong playerId, int minutes)
		{
			NetworkingPlayer player = null;

			try { player = Players.First(p => p.NetworkId == playerId); }
			catch { throw new NetworkException("Could not find the player with id " + playerId); }

			BanPlayer(player, minutes);
		}

		public void BanPlayer(NetworkingPlayer player, int minutes)
		{
			string ip = player.Ip.Split('+')[0];

			banList.Add(ip, (DateTime.Now + new TimeSpan(0, minutes, 0)));
			Disconnect(player, "Server has banned you for " + minutes + " minutes");

			if (playerBannedInvoker != null)
				playerBannedInvoker(ip, banList[ip]);
		}
	}
}