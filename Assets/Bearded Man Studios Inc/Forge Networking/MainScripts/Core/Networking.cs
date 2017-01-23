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
using BeardedManStudios.Threading;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

#if !NetFX_CORE
#if !UNITY_WEBGL
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
#endif
#if !UNITY_WEBPLAYER
#endif
#endif

namespace BeardedManStudios.Network
{

#if BARE_METAL
	public class Networking : MarshalByRefObject
#else
	public class Networking
#endif
	{
#if BARE_METAL
		public void StaticSetPrimarySocket(NetWorker NetWorker) { Networking.SetPrimarySocket(NetWorker); }
		public void StaticDisconnect(NetWorker socket) { Networking.Disconnect(socket); }
		public NetWorker StaticHost(ushort port, TransportationProtocolType comType, int maxConnections, bool winRT = false, string overrideIP = null, bool allowWebplayerConnection = false, bool relayToAll = true, bool useNat = false) { return Networking.Host(port, comType, maxConnections, winRT, overrideIP, allowWebplayerConnection, relayToAll, useNat); }
		public Dictionary<ulong, SimpleNetworkedMonoBehavior> NetworkedBehaviors { get { return SimpleNetworkedMonoBehavior.NetworkedBehaviors; } }
		public void AssignMap(SimpleJSON.JSONNode map) { BareMetal.ClassMap.AssignMap(map); }
		public ulong BandwidthIn { get { return NetWorker.BandwidthIn; } }
		public ulong BandwidthOut { get { return NetWorker.BandwidthOut; } }
		public void UpdateBareMetalTime() { BareMetal.BareMetalTime.UpdateTime(); }
		public void UpdateSNMBehaviors() { NetworkingManager.Instance.ExecuteRPCStack(); lock (SimpleNetworkedMonoBehavior.NetworkedBehaviorsMutex) { foreach (SimpleNetworkedMonoBehavior behavior in SimpleNetworkedMonoBehavior.NetworkedBehaviors.Values) behavior.BareMetalUpdate(); } }
#endif

		/// <summary>
		/// The various types of protocols that are supported in the system - Mainly used internally
		/// </summary>
		public enum ProtocolType
		{
			QuickTCP = 0,
			TCP = 1,
			QuickUDP = 2,
			UDP = 3,
			ReliableUDP = 4,
			HTTP = 5
		}

		/// <summary>
		/// The different transporation protocols that are available in the system
		/// TCP:  http://en.wikipedia.org/wiki/Transmission_Control_Protocol
		/// UDP:  http://en.wikipedia.org/wiki/User_Datagram_Protocol
		/// </summary>
		public enum TransportationProtocolType
		{
			TCP = 1,
			UDP = 3
		}

		/// <summary>
		/// If true then the server will not automatically relay raw messages to clients
		/// </summary>
		public static bool ControlledRaw { get; set; }

		/// <summary>
		/// A dictionary of all the NetWorkers(Sockets) being used throughout the current process
		/// </summary>
		public static Dictionary<ushort, NetWorker> Sockets { get; private set; }

		/// <summary>
		/// Determine if a NetWorker(Socket) is connected on a given port
		/// </summary>
		/// <param name="port">The port number that is to be checked for a connection create by this system</param>
		/// <returns>True if there thi system has an established connection on the given port and false if there isn't a connection on that port</returns>
		public static bool IsConnected(ushort port) { if (Sockets == null || !Sockets.ContainsKey(port)) return false; return Sockets[port].Connected; }

		/// <summary>
		/// Determine if a NetWorker(Socket) reference is connected, (Dumbly returns socket.Connected)
		/// </summary>
		/// <param name="socket">NetWorker(Socket) to be checked</param>
		/// <returns>True if the referenced NetWorker has established a connection</returns>
		public static bool IsConnected(NetWorker socket) { return socket.Connected; }

		public delegate void ConnectionEvent(NetWorker socket);

		/// <summary>
		/// Fires whenever a connection has been made by any of the Sockets that are managed by this class (Though "Host" and "Connect")
		/// </summary>
		public static event ConnectionEvent connected
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
		static ConnectionEvent connectedInvoker;    // Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// Fires whenever a ping has been recieved by the ping request <see cref="Networking.Ping" />
		/// </summary>
		public static event NetWorker.PingReceived pingReceived
		{
			add
			{
				pingReceivedInvoker += value;
			}
			remove
			{
				pingReceivedInvoker -= value;
			}
		}
		static NetWorker.PingReceived pingReceivedInvoker;    // Because iOS doesn't have a JIT - Multi-cast function pointer.

#if !UNITY_WEBGL
		// JM: added for threaded lan discovery
		public static event NetWorker.LANEndPointFound lanEndPointFound
		{
			add
			{
				lanEndPointFoundInvoker += value;
			}
			remove
			{
				lanEndPointFoundInvoker -= value;
			}
		}
		static NetWorker.LANEndPointFound lanEndPointFoundInvoker;    // Because iOS doesn't have a JIT - Multi-cast function pointer.
#endif

		public static event NetWorker.BasicEvent NetworkReset
		{
			add
			{
				NetworkResetInvoker += value;
			}
			remove
			{
				NetworkResetInvoker -= value;
			}
		}
		static NetWorker.BasicEvent NetworkResetInvoker;    // Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// A getter for the current primary socket.  Usually in games you will have one socket that does the main communication
		/// with the game and all of it's events, this would be the reference to that particular NetWorker(socket)
		/// </summary>
		public static NetWorker PrimarySocket { get; private set; }

		/// <summary>
		/// The type of protocol being used for the <see cref="Networking.PrimarySocket" /> object
		/// </summary>
		public static ProtocolType PrimaryProtocolType { get; private set; }

		/// <summary>
		/// Tell if the system is running under Bare Metal mode or not
		/// </summary>
		public static bool IsBareMetal { get; private set; }

		/// <summary>
		/// The list of callbacks that are fired for a Network instantiate
		/// </summary>
		private static Dictionary<int, Action<SimpleNetworkedMonoBehavior>> instantiateCallbacks = new Dictionary<int, Action<SimpleNetworkedMonoBehavior>>();
		private static int callbackCounter = 1;

		/// <summary>
		/// Tell the system if we are currently running it as Bare Metal or not - Mainly internal
		/// </summary>
		/// <param name="isBareMetal">If this is a Bare Metal instance</param>
		public static void SetBareMetal(bool isBareMetal)
		{
			IsBareMetal = isBareMetal;
		}

		/// <summary>
		/// Used to assign the <see cref="Networking.PrimarySocket"/> object to the specified <see cref="NetWorker"/>
		/// </summary>
		/// <param name="NetWorker">The NetWorker that will be the primary socket</param>
		public static void SetPrimarySocket(NetWorker NetWorker)
		{
			PrimarySocket = NetWorker;

#if !UNITY_WEBGL
			if (PrimarySocket is CrossPlatformUDP)
				PrimaryProtocolType = ProtocolType.UDP;
			else
#endif
				PrimaryProtocolType = ProtocolType.TCP;
		}

		/// <summary>
		/// Will setup a new server on this machine
		/// </summary>
		/// <param name="port">This is the port you want to bind the server to</param>
		/// <param name="comType">The particular transportation protocol <see cref="Networking.TransportationProtocolType"/> you wish to be used for this server</param>
		/// <param name="maxConnections">The maximum connections (players) allowed on the server at one point in time</param>
		/// <param name="winRT">If this is Windows Phone or Windows Store, this should be true, otherwise default to false</param>
		/// <param name="allowWebplayerConnection">Allow web player connections to server</param>
		/// <param name="relayToAll">Used to determine if messages should be relayed to client (normally true) - Mainly internal</param>
		/// <returns>The NetWorker server that was created (Which may not have established a connection yet <see cref="NetWorker.connected"/></returns>
		/// <example>
		/// public int port = 15937;																				// Port number
		/// public Networking.TransportationProtocolType protocolType = Networking.TransportationProtocolType.UDP;	// Communication protocol
		/// public int playerCount = 31;
		/// 
		/// #if NetFX_CORE && !UNITY_EDITOR
		///		private bool isWinRT = true;
		/// #else
		///		private bool isWinRT = false;
		/// #endif
		/// public void StartServer()
		/// {
		///		NetWorker socket = Networking.Host((ushort)port, protocolType, playerCount, isWinRT);	
		///	}
		/// </example>
		public static NetWorker Host(ushort port, TransportationProtocolType comType, int maxConnections, bool winRT = false, string overrideIP = null, bool allowWebplayerConnection = false, bool relayToAll = true, bool useNat = false, NetWorker.NetworkErrorEvent errorCallback = null)
		{
#if UNITY_WEBGL
            Debug.LogWarning("Cannot host in WebGL, try standalone");
			return null;
#else
			Threading.ThreadManagement.Initialize();
#if !BARE_METAL
			Unity.MainThreadManager.Create();
#endif

			if (Sockets == null) Sockets = new Dictionary<ushort, NetWorker>();

			if (Sockets.ContainsKey(port) && Sockets[port].Connected)
				throw new NetworkException(8, "Socket has already been initialized on that port");


			if (comType == TransportationProtocolType.UDP)
				Sockets.Add(port, new CrossPlatformUDP(true, maxConnections));
			else
			{
				if (winRT)
					Sockets.Add(port, new WinMobileServer(maxConnections));
				else
				{
					Sockets.Add(port, new DefaultServerTCP(maxConnections));
					((DefaultServerTCP)Sockets[port]).RelayToAll = relayToAll;
				}
			}

			// JM: added error callback in args in case Connect() below fails
			if (errorCallback != null)
			{
				Sockets[port].error += errorCallback;
			}

			Sockets[port].connected += delegate()
			{
				Sockets[port].AssignUniqueId(0);

				if (connectedInvoker != null)
					connectedInvoker(Sockets[port]);
			};

			Sockets[port].UseNatHolePunch = useNat;
			Sockets[port].Connect(overrideIP, port);

#if !NetFX_CORE && !BARE_METAL
			// TODO:  Allow user to pass in the variables needed to pass into this begin function
			if (allowWebplayerConnection)
				SocketPolicyServer.Begin();
#endif

			SimpleNetworkedMonoBehavior.Initialize(Sockets[port]);
			return Sockets[port];
#endif
		}

		/// <summary>
		/// This will force a firewall request by the users machine to allow for
		/// Network communications with this particular application
		/// </summary>
		/// <param name="port">Port to be allowed</param>
		/// <remarks>
		/// When a connection is first initialized with forge, typically a user will be prompted to give access to the application (unity) to use the Network.
		/// This can be problematic if your game requires for any reason the user not to look out of the game, it is also industry standard to prompt the user with
		/// anything like this when the application is starting up. In order to do this, you can use this helpful static method to force this prompt to popup on 
		/// startup. This method would best be used:
		/// <ul>
		///     <li>in the scene you load on startup</li>
		///     <li>in a gameobject that is enabled when the scene is loaded</li>
		///     <li>and in a method such as Awake() or Start(), both methods used by unity</li>
		/// </ul>
		/// If your game used multiple ports or may be hosted on a different port, just specify the default port you intend to use. Once your application has been
		/// allowed through the firewall it won't need to keep requesting access. Moving the location, renaming the file or rebuilding the .exe may cause you to have
		/// to reallow the application.
		/// </remarks>
		public static void InitializeFirewallCheck(ushort port)
		{
#if !UNITY_WEBGL
			DefaultServerTCP firewallSocket = new DefaultServerTCP(1);

			NetWorker.BasicEvent disconnect = null;
			disconnect = () =>
			{
				firewallSocket.Disconnect();
				firewallSocket.connected -= disconnect;
				firewallSocket = null;
			};

			firewallSocket.connected += disconnect;
			firewallSocket.Connect("127.0.0.1", (ushort)(port + 55));
#endif
		}

		/// <summary>
		/// Create and connect a client to the specified server ip and port
		/// </summary>
		/// <param name="ip">The host (usually ip address or domain name) to connect to</param>
		/// <param name="port">The port for the particular server that this connection is attempting</param>
		/// <param name="comType">The transportation protocol type that is to be used <see cref="Networking.TransportationProtocolType"/></param>
		/// <param name="winRT">If this is Windows Phone or Windows Store, this should be true, otherwise default to false</param>
		/// <returns>The NetWorker client that was created (Which may not have established a connection yet <see cref="NetWorker.connected"/></returns>
		/// <example>
		/// public string host = "127.0.0.1";																		// IP address
		/// public int port = 15937;																				// Port number
		/// public Networking.TransportationProtocolType protocolType = Networking.TransportationProtocolType.UDP;	// Communication protocol
		/// 
		/// #if NetFX_CORE && !UNITY_EDITOR
		///		private bool isWinRT = true;
		/// #else
		///		private bool isWinRT = false;
		/// #endif
		/// public void StartServer()
		/// {
		///		NetWorker socket = Networking.Connect(host, (ushort)port, protocolType, isWinRT);	
		///	}
		/// </example>
		public static NetWorker Connect(string ip, ushort port, TransportationProtocolType comType, bool winRT = false, bool useNat = false, bool standAlone = false)
		{
#if !UNITY_WEBGL
			Threading.ThreadManagement.Initialize();
#endif
			Unity.MainThreadManager.Create();

			if (Sockets == null) Sockets = new Dictionary<ushort, NetWorker>();

			if (Sockets.ContainsKey(port))
			{
#if UNITY_IOS || UNITY_IPHONE
				if (comType == TransportationProtocolType.UDP)
					Sockets[port] = new CrossPlatformUDP(false, 0);
				else
					Sockets[port] = new DefaultClientTCP();
#else
				if (Sockets[port].Connected)
					throw new NetworkException(8, "Socket has already been initialized on that port");
				else if (Sockets[port].Disconnected)
					Sockets.Remove(port);
				else
					return Sockets[port]; // It has not finished connecting yet
#endif
			}
#if !UNITY_WEBGL
			else if (comType == TransportationProtocolType.UDP)
				Sockets.Add(port, new CrossPlatformUDP(false, 0));
#endif
			else
			{
				if (winRT)
					Sockets.Add(port, new WinMobileClient());
				else
					Sockets.Add(port, new DefaultClientTCP());
			}

			Sockets[port].connected += delegate()
			{
				if (connectedInvoker != null)
					connectedInvoker(Sockets[port]);
			};

			Sockets[port].UseNatHolePunch = useNat;
			Sockets[port].Connect(ip, port);

			if (!standAlone) {
				SimpleNetworkedMonoBehavior.Initialize(Sockets[port]);
			}

			return Sockets[port];
		}

		/// <summary>
		/// Finds the first host on the Network on the specified port number in the local area Network and makes a connection to it
		/// </summary>
		/// <param name="port">The port to connect to</param>
		/// <param name="listenWaitTime">The time in milliseconds to wait for a discovery</param>
		/// <param name="protocol">The protocol type for the server</param>
		/// <param name="winRT">If this is Windows Phone or Windows Store, this should be true, otherwise default to false</param>
		/// <returns>The <see cref="NetWorker"/> that has been bound for this communication, null if none were found</returns>
#if NetFX_CORE
		public static void LanDiscovery(ushort port, int listenWaitTime = 10000, TransportationProtocolType protocol = TransportationProtocolType.UDP, bool winRT = false)
#else
		public static void LanDiscovery(ushort port, int listenWaitTime = 10000, TransportationProtocolType protocol = TransportationProtocolType.UDP, bool winRT = false)
#endif
		{
#if !UNITY_WEBGL
		   Task.Run(() => LanDiscoveryThread(new object[] {port, listenWaitTime}));
#endif
		}

		// JM: made threaded to not freeze main thread
		private static void LanDiscoveryThread(object args)
		{
#if !NetFX_CORE && !UNITY_WEBPLAYER && !UNITY_WEBGL
			ushort port = (ushort)((object[])args)[0];
			int listenWaitTime = (int)((object[])args)[1];
			// JM: brought in shavedrat's changes posted on EpicJoin to fix OSX
			List<string> localSubNet = new List<string>();
			NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var NetInterface in interfaces)
			{
				if ((NetInterface.OperationalStatus == OperationalStatus.Up ||
					NetInterface.OperationalStatus == OperationalStatus.Unknown) &&
					(NetInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
						NetInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet))

				{
					foreach (var d in NetInterface.GetIPProperties().UnicastAddresses)
					{
						if (d.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							var ipAddress = d.Address;
							if (ipAddress.ToString().Contains("."))
								localSubNet.Add(ipAddress.ToString().Remove(ipAddress.ToString().LastIndexOf('.')));
						}
					}
				}
			}

			UdpClient Client = new UdpClient();
			IPEndPoint foundEndpoint = new IPEndPoint(IPAddress.Any, 0);
			bool found = false;


			foreach (string s in localSubNet)
				Client.Send(new byte[1], 1, new IPEndPoint(IPAddress.Parse(s + ".255"), port));

			int counter = 0;
			do
			{
				if (Client.Available != 0)
				{
					Client.Receive(ref foundEndpoint);
					found = true;
					break;
				}

				if (counter++ > listenWaitTime / 50)
					break;

				System.Threading.Thread.Sleep(50);
				foreach (string s in localSubNet)
					Client.Send(new byte[1], 1, new IPEndPoint(IPAddress.Parse(s + ".255"), port));
			} while (true);

			Client.Close();

			if (found && lanEndPointFoundInvoker != null)
				lanEndPointFoundInvoker(foundEndpoint);


#elif NetFX_CORE
			// TODO:  Implement
			Debug.LogWarning("LanDiscovery not yet implemented");
#elif UNITY_WEBPLAYER
			Debug.LogError("Unable to find local at this time for webplayer");
#endif

#if !UNITY_WEBGL
			if (lanEndPointFoundInvoker != null)
				lanEndPointFoundInvoker(null);
#endif
		}

		/// <summary>
		/// Disconnects a player on a given port
		/// </summary>
		/// <param name="port">Port to disconnect from</param>
		/// <param name="player">Player to disconnect</param>
		/// <exception cref="NetworkException">Thrown when there is not a <see cref="NetWorker"/> on the supplied port</exception>
		/// <exception cref="NetworkException">Thrown when the <see cref="NetWorker"/> on the specified port is not a server</exception>
		public static void Disconnect(ushort port, NetworkingPlayer player)
		{
			if (!Sockets.ContainsKey(port))
				throw new NetworkException("There isn't a server running using the specified port on this machine");

			if (!Sockets[port].IsServer)
				throw new NetworkException("Disconnecting players can only be managed by the server, the NetWorker on the specified port is not a server");

			Sockets[port].Disconnect(player);
		}

		/// <summary>
		/// Disconnect a player on a given NetWorker(Socket)
		/// </summary>
		/// <param name="socket">NetWorker(Socket) to be disconnected from</param>
		/// <param name="player">The player reference to disconnect</param>
		/// <exception cref="NetworkException">Thrown when the <see cref="NetWorker"/> on the specified port is not a server</exception>
		/// <code>
		/// // Disconnect the first player on the primary socket
		/// Networking.Disconnect(Networking.PrimarySocket, Networking.PrimarySocket.Players[0]);
		/// </code>
		public static void Disconnect(NetWorker socket, NetworkingPlayer player)
		{
			if (!socket.IsServer)
				throw new NetworkException("Disconnecting players can only be managed by the server, the NetWorker on the specified port is not a server");

			socket.Disconnect(player);
		}

		/// <summary>
		/// Disconnects (on this machine) either a client or a server on the specified port
		/// </summary>
		/// <param name="port">Port of the local server/client to be disconnected from</param>
		public static void Disconnect(ushort port)
		{
			if (Sockets[port] == null)
				return;

#if !NetFX_CORE && !UNITY_WEBGL
			if (!ReferenceEquals(NetworkingManager.Instance, null) && NetworkingManager.Instance.OwningNetWorker != null && NetworkingManager.Instance.OwningNetWorker.IsServer)
				SocketPolicyServer.End();
#endif

			try
			{
				Sockets[port].Disconnect();
			}
			catch { }

			if (Sockets[port] == PrimarySocket && NetworkingManager.Instance != null)
				NetworkingManager.Instance.Disconnect();

			Sockets[port] = null;
			Sockets.Remove(port);

			// TODO:  Go through all the NetworkedMonoBehaviors and clean them up
		}

		/// <summary>
		/// Disconnect the specified <see cref="NetWorker"/> (socket) and remove it from the <see cref="Networking.Sockets"/> lookup
		/// </summary>
		/// <param name="socket">The socket <see cref="NetWorker"/> to be shut down</param>
		public static void Disconnect(NetWorker socket)
		{
			ushort[] keys = new ushort[Sockets.Keys.Count];
			Sockets.Keys.CopyTo(keys, 0);
			for (int i = 0; i < keys.Length; i++)
			{
				if (Sockets[keys[i]] == socket)
				{
					if (socket == PrimarySocket && NetworkingManager.Instance != null)
						NetworkingManager.Instance.Disconnect();

					socket.Disconnect();
					Sockets[keys[i]] = null;
					Sockets.Remove(keys[i]);
					break;
				}
			}
		}

		/// <summary>
		/// Disconnects all Sockets and clients for this machine running under this system and
		/// removes them all from the lookup (calls <see cref="Networking.NetworkingReset"/>
		/// </summary>
		public static void Disconnect()
		{
#if !NetFX_CORE && !UNITY_WEBGL
			if (!ReferenceEquals(NetworkingManager.Instance, null) && NetworkingManager.Instance.OwningNetWorker.IsServer) // JM: NetworkingManager.Instance cannot be server if null
				SocketPolicyServer.End();
#endif

			NetworkingReset();

			// TODO:  Go through all the NetworkedMonoBehaviors and clean them up
		}

		/// <summary>
		/// Writes a <see cref="NetworkingStream"/> to a particular <see cref="NetWorker"/> that is
		/// running on a particular port directly to a player (if the port is a server)
		/// </summary>
		/// <param name="port">The port that the <see cref="NetWorker"/> is listening on</param>
		/// <param name="player">The player that this server will be writing this message to</param>
		/// <param name="stream">The data stream that is to be written to the player</param>
		/// <exception cref="NetworkException">Thrown when there is not a <see cref="NetWorker"/> on the supplied port</exception>
		/// <exception cref="NetworkException">Thrown when the <see cref="NetWorker"/> on the specified port is not a server</exception>
		public static void Write(ushort port, NetworkingPlayer player, NetworkingStream stream)
		{
			if (!Sockets.ContainsKey(port))
				throw new NetworkException("There isn't a server running using the specified port on this machine");

			if (!Sockets[port].IsServer)
				throw new NetworkException("Writing to particular players can only be done by the server, the NetWorker on the specified port is not a server");

			Sockets[port].Write(player, stream);
		}

		/// <summary>
		/// Writes a <see cref="NetworkingStream"/> to a particular <see cref="NetWorker"/> directly to a player (if the port is a server)
		/// </summary>
		/// <param name="socket">NetWorker(Socket) to write with</param>
		/// <param name="player">Player to be written to server</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <exception cref="NetworkException">Thrown when there is not a <see cref="NetWorker"/> on the supplied port</exception>
		/// <exception cref="NetworkException">Thrown when the <see cref="NetWorker"/> on the specified port is not a server</exception>
		public static void Write(NetWorker socket, NetworkingPlayer player, NetworkingStream stream)
		{
			if (!socket.IsServer)
				throw new NetworkException("Writing to particular players can only be done by the server, the NetWorker on the specified port is not a server");

			socket.Write(player, stream);
		}

		/// <summary>
		/// Writes a <see cref="NetworkingStream"/> to a particular <see cref="NetWorker"/> that is
		/// running on a particular port
		/// </summary>
		/// <param name="port">Port of the given NetWorker(Socket)</param>
		/// <param name="identifier">Unique identifier to be used</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this be a reliable UDP</param>
		public static void WriteUDP(ushort port, string identifier, NetworkingStream stream, bool reliable = false)
		{
			if (!Sockets.ContainsKey(port))
				throw new NetworkException("There isn't a server running using the specified port on this machine");

			Sockets[port].Write(identifier, stream, reliable);
		}

		/// <summary>
		/// Writes a <see cref="NetworkingStream"/> to a particular <see cref="NetWorker"/>
		/// </summary>
		/// <param name="socket">NetWorker(Socket) to write with</param>
		/// <param name="identifier">Unique identifier to be used</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this be a reliable UDP</param>
		public static void WriteUDP(NetWorker socket, string identifier, NetworkingStream stream, bool reliable = false)
		{
			socket.Write(identifier, stream, reliable);
		}

		/// <summary>
		/// Write to the TCP given a NetWorker(Socket) with a stream of data
		/// </summary>
		/// <param name="port">Port of the given NetWorker(Socket)</param>
		/// <param name="stream">The stream of data to be written</param>
		public static void WriteTCP(ushort port, NetworkingStream stream)
		{
			Sockets[port].Write(stream);
		}

		/// <summary>
		/// Write to the TCP given a NetWorker(Socket) with a stream of data
		/// </summary>
		/// <param name="socket">The NetWorker(Socket) to write with</param>
		/// <param name="stream">The stream of data to be written</param>
		public static void WriteTCP(NetWorker socket, NetworkingStream stream)
		{
			socket.Write(stream);
		}

		/// <summary>
		/// Write to the UDP given a ip and port with a identifier and a stream of data
		/// </summary>
		/// <param name="ip">IpAddress of the given NetWorker(Socket)</param>
		/// <param name="port">Port of the given NetWorker(Socket)</param>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">The stream of data to be written</param>
		/// <param name="reliable">If this be a reliable UDP</param>
		[Obsolete("Static calls to the UDP library are no longer supported")]
		public static void WriteUDP(string ip, ushort port, string updateidentifier, NetworkingStream stream, bool reliable = false)
		{
			//CrossPlatformUDP.Write(ip, port, updateidentifier, stream, reliable);
		}

#region Object Instantiation
		private static bool ValidateNetworkedObject(string name, out SimpleNetworkedMonoBehavior NetBehavior)
		{
			NetBehavior = null;

#if !BARE_METAL
			if (NetworkingManager.Instance == null)
			{
				Debug.LogError("The NetworkingManager object could not be found.");
				return false;
			}

			GameObject o = NetworkingManager.Instance.PullObject(name);

			if (o == null)
				return false;

			NetBehavior = o.GetComponent<SimpleNetworkedMonoBehavior>();
#else
			//TODO: Pull the NetBehavior to see if it in the list
			return true;
#endif


			if (NetBehavior == null)
			{
#if !BARE_METAL
				Debug.LogError("Instantiating on the Network is only for objects that derive from BaseNetworkedMonoBehavior, " +
					"if object does not need to be serialized consider using a RPC with GameObject.Instantiate");
#else
				Console.WriteLine("Instantiating on the Network is only for objects that derive from BaseNetworkedMonoBehavior, " +
					"if object does not need to be serialized consider using a RPC with GameObject.Instantiate");
#endif

				return false;
			}

			return true;
		}

		private static void CallInstantiate(string obj, NetworkReceivers receivers, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			SimpleNetworkedMonoBehavior NetBehavior;
			if (ValidateNetworkedObject(obj, out NetBehavior))
			{
				if (callback != null)
				{
					instantiateCallbacks.Add(callbackCounter, callback);

#if !BARE_METAL
					NetworkingManager.Instantiate(receivers, obj, NetBehavior.transform.position, NetBehavior.transform.rotation, callbackCounter);
#else
					NetworkingManager.Instantiate(receivers, obj, Vector3.zero, Quaternion.identity, callbackCounter);
#endif
					callbackCounter++;

					if (callbackCounter == 0)
						callbackCounter++;
				}
				else
				{
#if !BARE_METAL
					NetworkingManager.Instantiate(receivers, obj, NetBehavior.transform.position, NetBehavior.transform.rotation, 0);
#else
					// TODO:  Put the position and rotation of the object in the scene JSON data
					NetworkingManager.Instantiate(receivers, obj, Vector3.zero, Quaternion.identity, 0);
#endif
				}
			}
		}

		private static void CallInstantiate(string obj, Vector3 position, Quaternion rotation, NetworkReceivers receivers, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			SimpleNetworkedMonoBehavior NetBehavior;
			if (ValidateNetworkedObject(obj, out NetBehavior))
			{
				if (callback != null)
				{
					instantiateCallbacks.Add(callbackCounter, callback);
					NetworkingManager.Instantiate(receivers, obj, position, rotation, callbackCounter);
					callbackCounter++;

					if (callbackCounter == 0)
						callbackCounter++;
				}
				else
					NetworkingManager.Instantiate(receivers, obj, position, rotation, 0);
			}
		}

		public static bool RunInstantiateCallback(int index, SimpleNetworkedMonoBehavior spawn)
		{
			if (instantiateCallbacks.ContainsKey(index))
			{
				instantiateCallbacks[index](spawn);
				instantiateCallbacks.Remove(index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Instantiate an object on the Network
		/// </summary>
		/// <param name="obj">Object to be instantiated by object name, prefab must be in the NetworkingManager.NetworkInstantiates array to be found</param>
		/// <param name="receivers">Recipients who will receive this instantiate call</param>
		/// <remarks>
		/// For an object to be instantiated across all connected clients. The player who calls the method locally will be the owner of the object. The object
		/// must have an SNMB or NMB attatched to it. This polymorphic variant of the method will instantiate the object at the default zero position.
		/// 
		/// If receivers NetworkReceivers.AllBuffered is specified, any clients who connect after the call has been made on all clients, will immediately call the
		/// buffered Network instantiates that they missed before connecting. This can be very important for making a system where a player can join in late.
		/// 
		/// If an object with the same name but "(remote)" after the name, can be found in NetworkingManager.NetworkInstantiates
		/// it will be spawned on all clients other than the client that calls instantiate. If no object with that name can be found, 
		/// the same object will be instantiated. If the prefab "player" is spawned and "player(remote)" is in the list, "player(remote)"
		/// will be used instead of "player".
		/// </remarks>
		public static void Instantiate(string obj, NetworkReceivers receivers, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			if (!NetworkingManager.IsOnline)
			{
				SimpleNetworkedMonoBehavior Netout;
				if (ValidateNetworkedObject(obj, out Netout))
				{
					// JM: offline fixes
					SimpleNetworkedMonoBehavior snmb = (GameObject.Instantiate(Netout.gameObject) as GameObject).GetComponent<SimpleNetworkedMonoBehavior>();
					snmb.OfflineStart();
					callback(snmb);
					return;
				}
			}

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					CallInstantiate(obj, receivers, callback: callback);
				});
			}
			else
				CallInstantiate(obj, receivers, callback: callback);
		}

		/// <summary>
		/// Instantiate an object on the Network
		/// </summary>
		/// <param name="obj">Object to be instantiated by object name</param>
		/// <param name="position">Position of instantiated object</param>
		/// <param name="rotation">Rotation of instantiated object</param>
		/// <param name="receivers">Recipients who will receive this instantiate call</param>
		/// <remarks>
		/// For an object to be instantiated across all connected clients. The player who calls the method locally will be the owner of the object. The object
		/// must have an SNMB or NMB attatched to it. This polymorphic variant of the method will instantiate the object at the specified position and rotation.
		/// 
		/// If receivers NetworkReceivers.AllBuffered is specified, any clients who connect after the call has been made on all clients, will immediately call the
		/// buffered Network instantiates that they missed before connecting. This can be very important for making a system where a player can join in late.
		/// 
		/// If an object with the same name but "(remote)" after the name, can be found in NetworkingManager.NetworkInstantiates
		/// it will be spawned on all clients other than the client that calls instantiate. If no object with that name can be found, 
		/// the same object will be instantiated. If the prefab "player" is spawned and "player(remote)" is in the list, "player(remote)"
		/// will be used instead of "player".
		/// </remarks>
		public static void Instantiate(string obj, Vector3 position, Quaternion rotation, NetworkReceivers receivers, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			if (!NetworkingManager.IsOnline)
			{
				SimpleNetworkedMonoBehavior Netout;
				if (ValidateNetworkedObject(obj, out Netout))
				{
					// JM: offline fixes
					SimpleNetworkedMonoBehavior snmb = (GameObject.Instantiate(Netout.gameObject, position, rotation) as GameObject).GetComponent<SimpleNetworkedMonoBehavior>();
					snmb.OfflineStart();
					callback(snmb);
					return;
				}
			}

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					CallInstantiate(obj, position, rotation, receivers, callback);
				});
			}
			else
				CallInstantiate(obj, position, rotation, receivers, callback);
		}

		/// <summary>
		/// Instantiate an object on the Network
		/// </summary>
		/// <param name="obj">Object to be instantiated by object name</param>
		/// <param name="receivers">Recipients who will receive this instantiate call (Default: All)</param>
		/// <remarks>
		/// For an object to be instantiated across all connected clients. The player who calls the method locally will be the owner of the object. The object
		/// must have an SNMB or NMB attatched to it. This polymorphic variant of the method will instantiate the object at the zero position.
		/// 
		/// If receivers NetworkReceivers.AllBuffered is specified, any clients who connect after the call has been made on all clients, will immediately call the
		/// buffered Network instantiates that they missed before connecting. This can be very important for making a system where a player can join in late.
		/// 
		/// If an object with the same name but "(remote)" after the name, can be found in NetworkingManager.NetworkInstantiates
		/// it will be spawned on all clients other than the client that calls instantiate. If no object with that name can be found, 
		/// the same object will be instantiated. If the prefab "player" is spawned and "player(remote)" is in the list, "player(remote)"
		/// will be used instead of "player".
		/// </remarks>
		public static void Instantiate(GameObject obj, NetworkReceivers receivers = NetworkReceivers.All, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			if (!NetworkingManager.IsOnline)
			{
				// JM: offline fixes
				SimpleNetworkedMonoBehavior snmb = (GameObject.Instantiate(obj) as GameObject).GetComponent<SimpleNetworkedMonoBehavior>();
				snmb.OfflineStart();
				callback(snmb);
				return;
			}

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					Instantiate(obj, receivers, callback);
				});
			}
			else
				CallInstantiate(obj.name, receivers, callback: callback);
		}

		/// <summary>
		/// Instantiate an object on the Network
		/// </summary>
		/// <param name="obj">Object to be instantiated by object name</param>
		/// <param name="position">Position of instantiated object</param>
		/// <param name="rotation">Rotation of instantiated object</param>
		/// <param name="receivers">Recipients who will receive this instantiate call (Default: All)</param>
		/// <remarks>
		/// For an object to be instantiated across all connected clients. The player who calls the method locally will be the owner of the object. The object
		/// must have an SNMB or NMB attatched to it. This polymorphic variant of the method will instantiate the object at the specified position and rotation.
		/// 
		/// If receivers NetworkReceivers.AllBuffered is specified, any clients who connect after the call has been made on all clients, will immediately call the
		/// buffered Network instantiates that they missed before connecting. This can be very important for making a system where a player can join in late.
		/// 
		/// If an object with the same name but "(remote)" after the name, can be found in NetworkingManager.NetworkInstantiates
		/// it will be spawned on all clients other than the client that calls instantiate. If no object with that name can be found, 
		/// the same object will be instantiated. If the prefab "player" is spawned and "player(remote)" is in the list, "player(remote)"
		/// will be used instead of "player".
		/// </remarks>
		public static void Instantiate(GameObject obj, Vector3 position, Quaternion rotation, NetworkReceivers receivers = NetworkReceivers.All, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			if (!NetworkingManager.IsOnline)
			{
				// JM: offline fixes
				SimpleNetworkedMonoBehavior snmb = (GameObject.Instantiate(obj, position, rotation) as GameObject).GetComponent<SimpleNetworkedMonoBehavior>();
				snmb.OfflineStart();
				if (callback != null)
					callback(snmb);
				return;
			}

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					Instantiate(obj, position, rotation, receivers, callback);
				});
			}
			else
				CallInstantiate(obj.name, position, rotation, receivers, callback);
		}

		/// <summary>
		/// Instantiate an object on the Network from the resources folder
		/// </summary>
		/// <param name="resourcePath">Location of the resource</param>
		/// <param name="receivers">Recipients will receive this instantiate call (Default: All)</param>
		public static void InstantiateFromResources(string resourcePath, NetworkReceivers receivers = NetworkReceivers.All, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			GameObject obj = Resources.Load<GameObject>(resourcePath);

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					CallInstantiate(obj.name, obj.transform.position, obj.transform.rotation, receivers, callback);
				});
			}
			else
				CallInstantiate(obj.name, obj.transform.position, obj.transform.rotation, receivers, callback);
		}

		/// <summary>
		/// Instantiate an object on the Network from the resources folder
		/// </summary>
		/// <param name="resourcePath">Location of the resource</param>
		/// <param name="position">Position of instantiated object</param>
		/// <param name="rotation">Rotation of instantiated object</param>
		/// <param name="receivers">Recipients will receive this instantiate call (Default: All)</param>
		public static void InstantiateFromResources(string resourcePath, Vector3 position, Quaternion rotation, NetworkReceivers receivers = NetworkReceivers.All, Action<SimpleNetworkedMonoBehavior> callback = null)
		{
			GameObject obj = Resources.Load<GameObject>(resourcePath);

			if (NetworkingManager.Instance == null || !NetworkingManager.Instance.IsSetup)
			{
				NetworkingManager.setupActions.Add(() =>
				{
					CallInstantiate(obj.name, position, rotation, receivers, callback);
				});
			}
			else
				CallInstantiate(obj.name, position, rotation, receivers, callback);
		}
#endregion

		/// <summary>
		/// Destroy a simple Networked object
		/// </summary>
		/// <param name="NetBehavior">Networked behavior to destroy</param>
		/// <remarks>
		/// This destroys a SNMB across the Network for all clients, the opposite of Networking.Instantiate() which creates an object.
		/// SimpleNetworkedMonoBehavior.NetworkDestroy() can also be used to delete a SNMB using a SimpleNetworkedMonoBehavior.NetworkedId.
		/// </remarks>
		public static void Destroy(SimpleNetworkedMonoBehavior NetBehavior)
		{
			if (!NetworkingManager.IsOnline)
			{
#if !BARE_METAL
				GameObject.Destroy(NetBehavior.gameObject);
#else
				Destroy(NetBehavior);
#endif
				return;
			}

			if (!NetBehavior.IsOwner && !NetBehavior.OwningNetWorker.IsServer)
				return;

			if (!ReferenceEquals(NetworkingManager.Instance, null))
				NetworkingManager.Instance.RPC("DestroyOnNetwork", NetBehavior.NetworkedId);
		}

#region Raw Writes
		/// <summary>
		/// Write a custom raw byte message with a 1 byte header across the Network
		/// </summary>
		/// <param name="id"></param>
		/// <param name="NetWorker"></param>
		/// <param name="data"></param>
		public static void WriteRaw(NetWorker NetWorker, BMSByte data, string uniqueId, bool reliable)
		{
			if (data == null)
			{
				NetWorker.ThrowException(new NetworkException(1000, "The data being written can not be null"));
				return;
			}

			if (data.Size == 0)
			{
				NetWorker.ThrowException(new NetworkException(1001, "The data being sent can't be empty"));
				return;
			}

			NetWorker.WriteRaw(data, uniqueId, true, reliable);
		}

		/// <summary>
		/// Allows the server to send a raw message to a particular player
		/// </summary>
		/// <param name="NetWorker"></param>
		/// <param name="targetPlayer"></param>
		/// <param name="data"></param>
		public static void WriteRaw(NetWorker NetWorker, NetworkingPlayer targetPlayer, BMSByte data, string uniqueId, bool reliable = false)
		{
			data.InsertRange(0, new byte[1] { 1 });
			NetWorker.WriteRaw(targetPlayer, data, uniqueId, reliable);
		}
#endregion

#region Custom Writes
		/// <summary>
		/// This allows you to write custom data across the Network and is useful for serializing entire classes if needed
		/// </summary>
		/// <param name="id">Unique identifier to be used</param>
		/// <param name="port">Port to be written to</param>
		/// <param name="data">Data to send over</param>
		/// <remarks>
		/// This very useful method allows you to end a BMSByte to a given grouping of receivers. You can also use a version of WriteCustom() to
		/// send a BMSByte to send directly to a specific NetworkingPlayer. WriteCustom requires all clients to be subscribed to Networking.PrimarySocket.AddCustomDataReadEvent().
		/// You must specify the same ID for each corresponding method on each client. WriteCustom can be used a wide variety of ways,
		/// see <A HREF="http://developers.forgepowered.com/Tutorials/WriteCustom/Write-Custom-Sending-Classes-Across-the-Network">this</A> for an example of one way it can be used.
		/// </remarks>
		public static void WriteCustom(uint id, ushort port, BMSByte data, NetworkReceivers recievers = NetworkReceivers.All)
		{
			WriteCustom(id, Sockets[port], data, false, recievers);
		}

		/// <summary>
		/// This allows you to write custom data across the Network and is useful for serializing entire classes if needed
		/// </summary>
		/// <param name="id">Unique identifier to be used</param>
		/// <param name="NetWorker">The NetWorker(Socket) to write with</param>
		/// <param name="data">Data to send over</param>
		/// <param name="reliableUDP">If this be a reliable UDP</param>
		public static void WriteCustom(uint id, NetWorker NetWorker, BMSByte data, bool reliableUDP = false, NetworkReceivers recievers = NetworkReceivers.All)
		{
#if !UNITY_WEBGL
			NetworkingStream stream = new NetworkingStream(NetWorker is CrossPlatformUDP ? ProtocolType.UDP : ProtocolType.TCP).Prepare(NetWorker, NetworkingStream.IdentifierType.Custom, 0,
				data, recievers, NetWorker is CrossPlatformUDP && reliableUDP, id, noBehavior: true);
#else
			NetworkingStream stream = new NetworkingStream(ProtocolType.TCP).Prepare(NetWorker, NetworkingStream.IdentifierType.Custom, 0,
				data, recievers, reliableUDP, id, noBehavior: true);
#endif
			if (NetWorker.IsServer)
			{
				switch (recievers)
				{
					case NetworkReceivers.Server:
					case NetworkReceivers.All:
					case NetworkReceivers.AllBuffered:
					case NetworkReceivers.AllProximity:
						BMSByte returnBytes = stream.Bytes;
						NetWorker.ExecuteCustomRead(id, NetWorker.Me, new NetworkingStream().Consume(NetWorker, NetWorker.Me, returnBytes));
						break;
				}

				if (recievers == NetworkReceivers.Server || recievers == NetworkReceivers.ServerAndOwner) // If only sending to the server, then just execute it itself.
					return;
			}

#if !UNITY_WEBGL
			if (NetWorker is CrossPlatformUDP)
				NetWorker.Write("BMS_INTERNAL_Write_Custom_" + id.ToString(), stream, reliableUDP);
			else
#endif
				NetWorker.Write(stream);

		}

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="id">Unique identifier to be used</param>
		/// <param name="NetWorker">The NetWorker(Socket) to write with</param>
		/// <param name="data">Data to send over</param>
		/// <param name="reliableUDP">If this be a reliable UDP</param>
		public static void WriteCustom(uint id, NetWorker NetWorker, BMSByte data, NetworkingPlayer target, bool reliableUDP = false)
		{
			if (!NetWorker.IsServer)
				throw new NetworkException("Currently this overload of WriteCustom is only supported being called on the server.");

#if !UNITY_WEBGL
			if (NetWorker is CrossPlatformUDP)
			{
				NetWorker.Write(id, target, new NetworkingStream(ProtocolType.UDP).Prepare(
					NetWorker, NetworkingStream.IdentifierType.Custom, 0, data, NetworkReceivers.Others, reliableUDP, id, noBehavior: true
				), reliableUDP);
			}
			else
			{
#endif
				NetWorker.Write(target, new NetworkingStream(ProtocolType.TCP).Prepare(
					NetWorker, NetworkingStream.IdentifierType.Custom, 0, data, NetworkReceivers.Others, reliableUDP, id, noBehavior: true
				));
#if !UNITY_WEBGL
			}
#endif
		}
#endregion

		public static void DynamicCommand(NetWorker socket, string command, bool relayOnServer = true, bool reliable = true)
		{
			BMSByte data = new BMSByte();
			data.Append(new byte[] { 7 });
			ObjectMapper.MapBytes(data, command);

			socket.WriteRaw(data, "BMS_INTERNAL_Command_" + command, relayOnServer, reliable);
		}

#region Player States
		public static void ClientReady(NetWorker socket)
		{
			DynamicCommand(socket, "ready");
		}
#endregion

#if !NetFX_CORE && !UNITY_WEBGL
		/// <summary>
		/// Get the local Ip address
		/// </summary>
		/// <returns>The Local Ip Address</returns>
		public static string GetLocalIPAddress()
		{
			IPHostEntry host;
			string localIP = "";
			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork && IsPrivateIP(ip)) // JM: check for all local ranges
				{
					localIP = ip.ToString();
					break;
				}
			}

			return localIP;
		}

		private static bool IsPrivateIP(IPAddress myIPAddress)
		{
			if (myIPAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			{
				byte[] ipBytes = myIPAddress.GetAddressBytes();

				// 10.0.0.0/24 
				if (ipBytes[0] == 10)
				{
					return true;
				}
				// 172.16.0.0/16
				else if (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31)
				{
					return true;
				}
				// 192.168.0.0/16
				else if (ipBytes[0] == 192 && ipBytes[1] == 168)
				{
					return true;
				}
			}

			return false;
		}

		// JM: hack added to get external IP
		static string externalIP;
		public static string GetExternalIPAddress()
		{
			if (externalIP != null) {
				return externalIP;
			}
			HTTP getIP = new HTTP ("http://icanhazip.com");
			getIP.Get ((page) => {
				externalIP = page.ToString();
			});
			return null;
		}

#endif

			/// <summary>
			/// To reset the Network by clearing the Sockets and disconnecting them if possible
			/// </summary>
		public static void NetworkingReset()
		{
			if (Sockets == null)
				return;

			ushort[] keys = new ushort[Sockets.Keys.Count];
			Sockets.Keys.CopyTo(keys, 0);
			foreach (ushort key in keys)
			{
				if (Sockets[key] != null)
				{
					Sockets[key].Disconnect();
					Sockets[key] = null;
				}
			}

			Sockets.Clear();

			PrimarySocket = null;
			SimpleNetworkedMonoBehavior.ResetAll();

			if (NetworkResetInvoker != null)
				NetworkResetInvoker();

			NetworkResetInvoker = null;
		}

#region Message Groups
		/// <summary>
		/// This will set the message group for the specified socket connection
		/// </summary>
		/// <param name="socket">The NetWorker to assign the message group for</param>
		/// <param name="groupId">The unique identifier for the message group</param>
		public static void SetMyMessageGroup(NetWorker socket, ushort groupId)
		{
			socket.Me.SetMessageGroup(groupId);

			BMSByte data = new BMSByte();
			data.Append(new byte[] { 6 });
			ObjectMapper.MapBytes(data, groupId);

			socket.WriteRaw(data, "BMS_INTERNAL_Set_MessageGroup", true, true);
		}
#endregion

#region Change Client Scene
		/// <summary>
		/// Tells the client to change their scene to the given scene.  This is often called
		/// after the server has changed to that scene to ensure that the server will always
		/// load up the scene before the client does
		/// </summary>
		/// <param name="NetWorker"></param>
		/// <param name="sceneName">The name of the scene in which the client should load</param>
		public static void ChangeClientScene(NetWorker NetWorker, string sceneName)
		{
			if (!NetWorker.IsServer) throw new NetworkException("Only the server can call this method, the specified NetWorker is not a server");

			BMSByte data = new BMSByte();
			data.Append(new byte[] { 2 });
			ObjectMapper.MapBytes(data, sceneName);

			NetWorker.WriteRaw(data, "BMS_INTERNAL_Change_Client_Scene", false, true);
		}

		/// <summary>
		/// Tells the client to change their scene to the given scene.  This is often called
		/// after the server has changed to that scene to ensure that the server will always
		/// load up the scene before the client does
		/// </summary>
		/// <param name="NetWorker">The current <see cref="NetWorker"/> that will be sending the message</param>
		/// <param name="targetPlayer">The particular player that will be receiving this message</param>
		/// <param name="sceneName">The name of the scene in which the client should load</param>
		public static void ChangeClientScene(NetWorker NetWorker, NetworkingPlayer targetPlayer, string sceneName)
		{
			if (!NetWorker.IsServer) throw new NetworkException("Only the server can call this method, the specified NetWorker is not a server");

			BMSByte data = new BMSByte();
			data.Append(new byte[] { 2 });
			ObjectMapper.MapBytes(data, sceneName);

			NetWorker.WriteRaw(targetPlayer, data, "BMS_INTERNAL_Change_Client_Scene", true);
		}

		/// <summary>
		/// Tells the client to change their scene to the given scene.  This is often called
		/// after the server has changed to that scene to ensure that the server will always
		/// load up the scene before the client does
		/// </summary>
		/// <param name="port">The port of the <see cref="NetWorker"/> that is to send the message</param>
		/// <param name="sceneName">The name of the scene in which the client should load</param>
		public static void ChangeClientScene(ushort port, string sceneName)
		{
			if (!Sockets.ContainsKey(port)) throw new NetworkException("There isn't a server running using the specified port on this machine");
			if (!Sockets[port].IsServer) throw new NetworkException("Only the server can call this method, the NetWorker on the specified port is not a server");

			BMSByte data = new BMSByte();
			data.Append(new byte[] { 2 });
			ObjectMapper.MapBytes(data, sceneName);

			Sockets[port].WriteRaw(data, "BMS_INTERNAL_Change_Client_Scene", false, true);
		}

		/// <summary>
		/// Tells the client to change their scene to the given scene.  This is often called
		/// after the server has changed to that scene to ensure that the server will always
		/// load up the scene before the client does
		/// </summary>
		/// <param name="port">The port of the <see cref="NetWorker"/> that is to send the message</param>
		/// <param name="targetPlayer">The particular player that will be receiving this message</param>
		/// <param name="sceneName">The name of the scene in which the client should load</param>
		public static void ChangeClientScene(ushort port, NetworkingPlayer targetPlayer, string sceneName)
		{
			if (!Sockets.ContainsKey(port)) throw new NetworkException("There isn't a server running using the specified port on this machine");
			if (!Sockets[port].IsServer) throw new NetworkException("Writing to particular players can only be done by the server, the NetWorker on the specified port is not a server");

			BMSByte data = new BMSByte();
			data.Append(new byte[] { 2 });
			ObjectMapper.MapBytes(data, sceneName);

			Sockets[port].WriteRaw(targetPlayer, data, "BMS_INTERNAL_Change_Client_Scene", true);
		}
#endregion

		/// <summary>
		/// Ping a particular host to get its response time in milliseconds
		/// </summary>
		/// <param name="host">The host object to ping</param>
        /// <param name="callback">Called when it has finished the ping</param>
		public static void Ping(HostInfo host, System.Action<HostInfo> callback = null)
		{
#if NetFX_CORE

#else
			System.Threading.Thread pingThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ThreadPing));
			pingThread.Start(new object[] { host, callback });
#endif
		}

		private static void ThreadPing(object hostObj)
		{
#if NetFX_CORE

#elif UNITY_WEBGL

#else
			HostInfo host = (HostInfo)((object[])hostObj)[0];
            System.Action<HostInfo> callback = (System.Action<HostInfo>)((object[])hostObj)[1];
			IPAddress address = IPAddress.Parse(host.ipAddress);
			UdpClient Client = new UdpClient();
			IPEndPoint ep = new IPEndPoint(address, host.port);

			Client.Send(new byte[1], 1, ep);
			DateTime start = DateTime.Now;
			int counter = 0;
			int maxTries = 50;

			do
			{
				if (Client.Available != 0)
				{
					Client.Receive(ref ep);

					if (ep.Address.ToString() == host.ipAddress && ep.Port == host.port)
						break;
				}

				if (counter++ >= maxTries)
					return; // TODO: Fire off a failed event

				System.Threading.Thread.Sleep(1000);
				Client.Send(new byte[1], 1, new IPEndPoint(address, host.port));
				start = System.DateTime.Now;
			} while (true);

			int time = (int)(System.DateTime.Now - start).TotalMilliseconds;

			host.SetPing(time);

			if (pingReceivedInvoker != null)
				pingReceivedInvoker(host, time);

            if (callback != null)
                callback(host);
#endif
		}

		// JM: option for RPCs to run off fixed loop.  
		//fixed loop should probably be used in Networked games because different machines may have slower or faster frame rates which will change the timings of RPCs
		public static bool UseFixedUpdate = false;
	}
}
