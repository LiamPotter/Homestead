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



#if NetFX_CORE
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace BeardedManStudios.Network
{
	/// <summary>
	/// This is the base class for all objects that are to be established on the Network
	/// </summary>
	/// <remarks>
	/// The SimpleNetworkedMonoBehavior or SNMB is the first of the two main classes used heavily by the forge Networking features. 
	/// Typically your class will inherit from SNMB if you only need the functionality of RPCs. NetworkedMonoBehavior is a more out of the box
	/// solution it allows for syncronization of position, rotation and scale of classes that inherit from it, interpolation of those values, 
	/// RPCs, Proximity Based Updates, Authoritative Control, advanced management of message send rates (throttling) and the use of forge's NetworkControls.
	/// NetworkedMonoBehavior is however more resource intensive and should not be used if you do not need the additional features, and
	/// are only intending on using RPC calls from that objects.
	/// </remarks>
	[AddComponentMenu("Forge Networking/Simple Networked MonoBehavior")]
#if BARE_METAL
	public class SimpleNetworkedMonoBehavior : BareMetalMonoBehavior, INetworkingSerialized
#else
	public class SimpleNetworkedMonoBehavior : MonoBehaviour, INetworkingSerialized
#endif
	{
#if BARE_METAL
		public SimpleNetworkedMonoBehavior(string name, string type) : base(name, type) { }
#endif

		/// <summary>
		/// This is the main attribute class that is used to describe RPC methods
		/// </summary>
		/// <remarks>
		/// The BRPC attribute can be added to methods in derivative classes of the SimpleNetworkedMonoBehavior (including derivatives of the NetworkedMonoBehavior),
		/// to designate a method as a Remote Procedure Call (RPC). This then allows the method to be called across the Network, in ForgeNetworking we do so using
		/// the method's name (as a string). The method used to do is SimpleNetworkedMonoBehavior.RPC(), an overload method also allows you to specify 
		/// Network.NetworkReceivers, this can be used to control who in the Network executes the RPC.
		/// 
		/// To designate a method as an RPC you add the BRPC attribute as follows:
		/// <code>
		/// [BRPC]
		/// void MethodA(){
		/// 
		/// }
		/// </code>
		/// A method can have any access modifier (private/protected allowed). Methods can also have parameters, the following parameter types are supported:
		/// <ul>
		/// <li>byte</li>
		/// <li>char</li>
		/// <li>short</li>
		/// <li>ushort</li>
		/// <li>bool</li>
		/// <li>int</li>
		/// <li>uint</li>
		/// <li>float</li>
		/// <li>long</li>
		/// <li>ulong</li>
		/// <li>double</li>
		/// <li>string</li>
		/// <li>Vector2</li>
		/// <li>Vector3</li>
		/// <li>Vector4</li>
		/// <li>Color</li>
		/// <li>Quaternion</li>
		/// </ul>
		/// </remarks>
		protected sealed class BRPC : Attribute
		{
			public Type interceptorType;

			public BRPC() { }

			public BRPC(Type interceptorType)
			{
				this.interceptorType = interceptorType;
			}
		}

		/// <summary>
		/// Determine if the initial setup has been done or not
		/// </summary>
		private static bool initialSetup = false;

		/// <summary>
		/// Used for when creating new Networked behaviors
		/// </summary>
		public static object NetworkedBehaviorsMutex = new object();

		/// <summary>
		/// A list of all of the current Networked behaviors
		/// </summary>
		/// <remarks>
		/// A dictionary that stores all Networked objects in the scene, allowing you to find an SNMB using an ID, see SimpleNetworkedMonoBehavior.NetworkedId 
		/// for more information on that unique id.
		/// </remarks>
		public static Dictionary<ulong, SimpleNetworkedMonoBehavior> NetworkedBehaviors = new Dictionary<ulong, SimpleNetworkedMonoBehavior>();

		/// <summary>
		/// A number that is used in assigning the unique id for a Networked object
		/// </summary>
		public static ulong ObjectCounter { get; private set; }

		/// <summary>
		/// Determine if the object calling this boolean is the owner of this Networked object
		/// </summary>
		/// <remarks>
		/// OwnerId can be used to establish if the OwnerId is equal to the
		/// id of the local player.
		/// Please see OwnerUpdate() method to execute code 
		/// every frame, this is better than using Update() method.
		/// See Networking.Instantiate() to see more about ownership.
		/// </remarks>
		public bool IsOwner { get; protected set; }

		/// <summary>
		/// The player id who currently owns this Networked object
		/// </summary>
		/// <remarks>
		/// OwnerId can be used to establish which client owns a SNMB/NMB.
		/// Please note rather than trying to workout if OwnerId is the current
		/// client's id, IsOwner should be used.
		/// OwnerId is better accessed with OwnerUpdate() than Update()
		/// See Networking.Instantiate() to see more about ownership.
		/// </remarks>
		public ulong OwnerId { get; protected set; }

		/// <summary>
		/// Whether this object is allowed to be added to the buffer
		/// </summary>
		/// <remarks>
		/// Requests won't automatically buffer, the RPC calls have to be set to buffer, but for an object to buffer it must have
		/// this set to <c>true</c>. This is only used internally by forge, .
		/// </remarks>
		public bool IsClearedForBuffer { get; protected set; }

		/// <summary>
		/// Used to determine if the server owns this object, the server id
		/// will always be 0 in Forge Networking
		/// </summary>
		public bool IsServerOwner { get { return OwnerId == 0; } }

		private Dictionary<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>> rpcs = null;

		/// <summary>
		/// Used to map BRPC methods to integers for bandwidth compression
		/// </summary>
		protected Dictionary<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>> RPCs
		{
			get
			{
				if (rpcs == null)
					Reflect();

				return rpcs;
			}
		}

		/// <summary>
		/// A list of RPC (remote methods) that are pending to be called for this object
		/// </summary>
		protected List<NetworkingStreamRPC> rpcStack = new List<NetworkingStreamRPC>();
		private object rpcStackMutex = new object();
		private string rpcStackExceptionMethodName = string.Empty;

		/// <summary>
		/// The sender of the RPC (player who requested the RPC to be called)
		/// </summary>
		/// <remarks>
		/// In an RPC method you may want to access the information of the player who originally called the RPC, this allows quick access
		/// to all of that information. See NetworkingPlayer for the type of information that can be accessed.
		/// </remarks>
		protected NetworkingPlayer CurrentRPCSender { get; set; }

		[SerializeField, HideInInspector]
		private int sceneNetworkedId = 0;

		public int GetSceneNetworkedId() { return sceneNetworkedId; }

		// TODO:  Optimization when an object is removed there needs to be a way to replace its spot
		/// <summary>
		/// The Network ID of this Simple Networked Monobehavior
		/// </summary>
		/// <remarks>
		/// This is the unique ID of the SNMB (not to be confused with the NetworkingPlayer.NetworkId, this represents a unique id for every SNMB object even if
		/// owned by the same player), this unique ID is assigned to every SNMB when an SNMB is successfully instantiated or connected to the Network.
		/// It is required for the SNMB to start calling RPCs or Sync'ing variables.
		/// </remarks>
		public ulong NetworkedId { get; private set; }

		public void SetSceneNetworkedId(int id)
		{
			if (this is NetworkingManager || id < 1)
				return;

			sceneNetworkedId = id;
		}

		/// <summary>
		/// If this object has been setup on the Network
		/// </summary>
		/// <remarks>
		/// This IsSetup represents the initial setup and synchronization with the server. 
		/// Sync'ed Variables and RPCs are not ready to begin being sent before this is flagged
		/// true. If you try to call RPCs before the SNMB/NMB is setup errors may be thrown.
		/// IsSetup typically takes at least a few frames to be setup depending on the distance
		/// of the connection, this will mean methods like Awake() and probably Start() will always
		/// report IsSetup to be false. If you want to use the functionality of those method 
		/// use NetworkStart() instead.
		/// </remarks>
		public bool IsSetup { get; protected set; }

		/// <summary>
		/// The owning socket (Net Worker) for this Simple Networked Monobehavior
		/// </summary>
		/// <remarks>
		/// A stored reference to the NetWorker socket the Networked object is connected to.
		/// See NetWorker for more...
		/// </remarks>
		public NetWorker OwningNetWorker { get; protected set; }

		/// <summary>
		/// This is used by the server in order to easily associate this object to its owning player
		/// </summary>
		/// <remarks>
		/// This gives direct access to the NetworkingPlayer object that represents the client of
		/// the SNMB/NMB. This is a faster way to access the NetworkingPlayer than storing the 
		/// OwnerId and then searching the Networking.Sockets[port].NetworkingPlayers.
		/// 
		/// See NetworkingPlayer for the type of information that can be accessed.
		/// </remarks>
		public NetworkingPlayer OwningPlayer { get; protected set; }

		/// <summary>
		/// The stream that is re-used for the RPCs that are being sent from this object
		/// </summary>
		private NetworkingStream rpcNetworkingStream = new NetworkingStream();

		/// <summary>
		/// The main cached buffer the RPC Network stream
		/// </summary>
		private BMSByte getStreamBuffer = new BMSByte();

		/// <summary>
		/// Used to lock the initialization thread
		/// </summary>
		private static object initializeMutex = new object();

		/// <summary>
		/// If this is marked as true then this object will not be cleaned up by the Network on level load
		/// </summary>
		/// <remarks>
		/// This ensures an SNMB will not be destroyed when loading a new scene, and that all information in the SNMB will be preserved. This can
		/// be set in the inspector or by script. See ResetForScene()... 
		/// </remarks>
		public bool dontDestroyOnLoad = false;

		/// <summary>
		/// If this is true, then a Network destroy will be called on disconnect
		/// </summary>
		/// <remarks>
		/// This can be set to make sure the GameObject will be destroyed across the Network for all clients if the SimpleNetworkedMonoBehavior.OwningPlayer,
		/// disconnects or looses connection to the server.
		/// </remarks>
		public bool destroyOnDisconnect = false;

		/// <summary>
		/// Determines if this object has the ability to change owners
		/// </summary>
		/// <remarks>
		/// If set <c>true</c> the SimpleNetworkedMonoBehavior.OwningPlayer or the server can change the owner of this SNMB by using the ChangeOwner() method.
		/// </remarks>
		public bool allowOwnershipChange = true;

		/// <summary>
		/// Get a generated Unique ID for the next simple Networked mono behavior or its derivitive
		/// </summary>
		/// <returns>A Unique unsigned long ID</returns>
		/// <remarks>
		/// Used internally by forge to get a new NetworkedId for a new SNMB, see SimpleNetworkedMonoBehaviour.NetworkedId.
		/// </remarks>
		public static ulong GenerateUniqueId()
		{
			return ++ObjectCounter;
		}

		/// <summary>
		/// Locate a Simple Networked Monobehavior given a ID
		/// </summary>
		/// <param name="id">ID of the Simple Networked Monobehavior</param>
		/// <returns>The Simple Networked Monobehavior found or <c>null</c> if not found</returns>
		/// <remarks>
		/// This useful static method finds the SimpleNetworkedMonoBehavior in the scene using the unique SNMB id, see SimpleNetworkedMonoBehavior.NetworkedId for
		/// more information on the id used to find the SNMB. This method can be very useful for finding the GameObject of a SNMB in a scene when transfering 
		/// information over the Network.
		/// </remarks>
		public static SimpleNetworkedMonoBehavior Locate(ulong id)
		{
			if (NetworkedBehaviors.ContainsKey(id))
				return NetworkedBehaviors[id];

			return null;
		}

		/// <summary>
		/// Destroy a Simple Networked Monobehavior or any of its derivitives with the given Network ID
		/// </summary>
		/// <param name="NetworkId">Network ID to be destroyed, see SimpleNetworkedMonoBehavior.NetworkedId</param>
		/// <returns><c>True</c> if the Network behavoir was destroy, otherwise <c>False</c> if no SNMB is found</returns>
		/// <remarks>
		/// NetworkDestroy() will search for a SNMB using Locate(), this method searches using the unique id for each SNMB 
		/// (See SimpleNetworkedMonoBehavior.NetworkedId for more information). Once the SNMB is found the SNMB is deleted across the Network
		/// for all clients. If you want to destroy a SNMB using a reference to the SNMB not the NetworkedId use Networking.Destroy().
		/// </remarks>
		public static bool NetworkDestroy(ulong NetworkId)
		{
			// Make sure the object exists on the Network before calling destroy
			SimpleNetworkedMonoBehavior behavior = Locate(NetworkId);

			if (behavior == null)
				return false;

			// Destroy the object from the scene and remove it from the lookup

#if !BARE_METAL
			GameObject.Destroy(behavior.gameObject);
#else
			Destroy(behavior);
#endif

			lock (NetworkedBehaviorsMutex)
			{
				NetworkedBehaviors.Remove(NetworkId);
			}

			if (Networking.PrimarySocket.IsServer)
				Networking.PrimarySocket.ClearBufferedInstantiateFromID(NetworkId);

			return true;
		}

		/// <summary>
		/// Finds the RPC method, using the index for the storred RPCs
		/// </summary>
		/// <param name="id">The id of the RPC in the RPCs dictionary</param>
		/// <remarks>
		/// All RPCs are stored in SimpleNetworkedBehavior.RPCs, this method
		/// allows you to access the RPC's method info. The id passed in is
		/// effectively an index. This method is mostly used internally and
		/// would only likely be needed to extend the core functionality of
		/// forge.
		/// </remarks>
		public MethodInfo GetRPC(int id)
		{
			if (!RPCs.ContainsKey(id))
				return null;

			return RPCs[id].Key;
		}

		private static Dictionary<ulong, List<NetworkingStreamRPC>> missingIdBuffer = new Dictionary<ulong, List<NetworkingStreamRPC>>();
		private static object missingIdMutex = new object();
		/// <summary>
		/// Used to make sure an RPC call on an SNMB that hasn't been instantiated gets called once the SNMB has been instantiated.
		/// </summary>
		/// <param name="id">the Networking stream id</param>
		/// <param name="stream">Networking stream of the RPC</param>
		/// <remarks>
		/// Internally used by forge, the purpose is to handle RPCs on SNMBs being instantiated. If an SNMB is instantiated and an RPC called 
		/// before the SNMB has time to be instantiated across the Network, QueRPCForInstantiate allows an RPC call to be delayed to allow the SNMB to
		/// be instantiated first.
		/// </remarks>
		public static void QueueRPCForInstantiate(ulong id, NetworkingStream stream)
		{
			if (id < ObjectCounter)
				return;

			lock (missingIdMutex)
			{
				if (!missingIdBuffer.ContainsKey(id))
					missingIdBuffer.Add(id, new List<NetworkingStreamRPC>());

				missingIdBuffer[id].Add(new NetworkingStreamRPC(stream, true));
			}
		}

		/// <summary>
		/// Get the RPC's of the simple Networked monobehavior
		/// </summary>
		/// <remarks>
		/// Reflect is used internally by Forge Networking to get all RPCs in a SNMB, the found RPCs are stored for use when actually calling RPCs. 
		/// if RPC("method_name") fails to find an RPC it is likely because Reflect failed to find the RPC.
		/// </remarks>
		protected virtual void Reflect()
		{
			IsClearedForBuffer = true;
			rpcs = new Dictionary<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>>();
#if NetFX_CORE
			IEnumerable<MethodInfo> methods = this.GetType().GetRuntimeMethods();
#else
			MethodInfo[] methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
			foreach (MethodInfo method in methods)
			{
				BRPC[] attributes = null;
#if NetFX_CORE
				attributes = method.GetCustomAttributes<BRPC>().ToList().ToArray();
				//if (method.GetCustomAttribute<BRPC>() != null)
#else
				attributes = method.GetCustomAttributes(typeof(BRPC), true) as BRPC[];
#endif
				if (attributes != null && attributes.Length != 0)
				{
					RPCs.Add(RPCs.Count, new KeyValuePair<MethodInfo, List<IBRPCIntercept>>(method, new List<IBRPCIntercept>()));

					foreach (BRPC brpc in attributes)
					{
						if (brpc.interceptorType == null)
							continue;

						object interceptor = Activator.CreateInstance(brpc.interceptorType);

						if (interceptor == null || !(interceptor is IBRPCIntercept))
							throw new NetworkException("The type " + brpc.interceptorType.ToString() + " does not implement IBRPCIntercept");

						RPCs[RPCs.Count - 1].Value.Add((IBRPCIntercept)interceptor);
					}
				}
			}
		}

		/// <summary>
		/// Removes unity calls (Update and FixedUpdate) encase thread manager is destroyed.
		/// </summary>
		/// <remarks>
		/// Forge used to override using init, now forge hooks into the events with MainThreadManager. The events need to be unregistered
		/// before the MainThreadManager gameobject is destroyed (such as when swapping scene) encase the MainThreadManager is destroyed also.
		/// </remarks>
		public void Cleanup()
		{
			Unity.MainThreadManager.unityUpdate -= UnityUpdate;
			Unity.MainThreadManager.unityFixedUpdate -= UnityFixedUpdate;
		}

		/// <summary>
		/// Prepares the scene for loading a new scene by removing SNMBs
		/// </summary>
		/// <param name="skip">You can specify a List of SNMBs to not destroy</param>
		/// <remarks>
		/// Destroys all SNMBs and properly removes them from the system using Clear(). SNMBs in the skip list are not destroyed, SNMBs with
		/// SimpleNetworkedMonoBehavior.dontDestroyOnLoad set to <c>true</c> are also not destroyed.
		/// </remarks>
		public static void ResetForScene(List<SimpleNetworkedMonoBehavior> skip)
		{
			initialSetup = false;

			lock (NetworkedBehaviorsMutex)
			{
				foreach (SimpleNetworkedMonoBehavior behavior in NetworkedBehaviors.Values)
				{
					if (!skip.Contains(behavior))
					{
						if (behavior.dontDestroyOnLoad)
							skip.Add(behavior);
						else
							behavior.Disconnect();
					}
				}

				NetworkedBehaviors.Clear();
			}

			for (int i = skip.Count - 1; i >= 0; --i)
				if (skip[i] == null)
					skip.RemoveAt(i);

			lock (NetworkedBehaviorsMutex)
			{
				foreach (SimpleNetworkedMonoBehavior behavior in skip)
					NetworkedBehaviors.Add(behavior.NetworkedId, behavior);
			}
		}

		/// <summary>
		/// Similar to ResetForScene(), ResetAll() doesn't skip any SNMBs and detroys and resets everything.
		/// </summary>
		/// <remarks>
		/// See ResetForScene(), ResetAll() destroys everything, even SNMBS with SimpleNetworkedMonoBehavior.dontDestroyOnLoad set to <c>true</c>.
		/// </remarks>
		public static void ResetAll()
		{
			initialSetup = false;

			lock (NetworkedBehaviorsMutex)
			{
				foreach (SimpleNetworkedMonoBehavior behavior in NetworkedBehaviors.Values)
				{
					behavior.Disconnect();
					behavior.OwningNetWorker = null;
				}

				NetworkedBehaviors.Clear();
			}

			ObjectCounter = 0;
		}

		/// <summary>
		/// Resets the buffer to be clear again so that it can start buffering
		/// </summary>
		/// <remarks>
		/// Generally only used internally, this resets the buffer. For example if we buffered instantiate and destroy something,
		/// a new client connecting will instantiate and destroy the object. As instantiating something and destroying it straighht away is
		/// wasteful and pointless it can be useful to sometimes reset the buffer.
		/// </remarks>
		public void ResetBufferClear()
		{
			IsClearedForBuffer = true;
		}

		/// <summary>
		/// An initial setup to make sure that a Networking manager exists before running any core logic
		/// </summary>
		/// <remarks>
		/// A way for making sure the Networking Manager exists and then setting up all Networked objects after the Networking socket is successfully connected. 
		/// If it isn't connected, DelayedInitialize() will call when the Networking socket is connected.
		/// </remarks>
		public static void Initialize(NetWorker socket)
		{
			lock (initializeMutex)
			{
				if (!initialSetup) {
					initialSetup = true;
					
#if !BARE_METAL
					if (ReferenceEquals(NetworkingManager.Instance, null)) {
						Unity.MainThreadManager.Run(delegate {
							Instantiate(Resources.Load<GameObject>("BeardedManStudios/Networking Manager"));
							if (!NetworkingManager.Instance.Populate(socket)) {
								Networking.connected += DelayedInitialize;
							}

						});

						return;
					}   
#else
					NetworkingManager.Instance.BareMetalAwake();
#endif
					
					if (!NetworkingManager.Instance.Populate(socket)) {
						Networking.connected += DelayedInitialize;
					}
				}
			}
		}

		/// <summary>
		/// Used to attempt to initialize once the Networking socket is connected, see Initialize()...
		/// </summary>
		/// <param name="socket"></param>
		/// <remarks>
		/// DelayedInitialize() is called when the Networking socket connects, if the Networking socket wasn't connected when Initialize() was called.
		/// This allows the Networked objects to be setup once the Networking socket is connected. See Initialize() for more...
		/// </remarks>
		private static void DelayedInitialize(NetWorker socket)
		{
			initialSetup = false;
			Initialize(socket);
			Networking.connected -= DelayedInitialize;
		}

		private void ThrowNetworkerException()
		{
#if UNITY_EDITOR && !BARE_METAL
			Debug.Log("Try using the Forge Quick Start Menu and setting the \"Scene Name\" on the \"Canvas\" to the scene you are loading. Then running from that scene.");
#endif

			throw new NetworkException("The NetWorker doesn't exist. Is it possible that a connection hasn't been made?");
		}

		/// <summary>
		/// A start method that is called after the object has been setup on the Network
		/// </summary>
		/// <remarks>
		/// A method that is called when the object is fully setup and connected to the Network,
		/// methods overriding this must call base.NetworkStart(), as it is required to fully connect the object to the Network.
		/// This can be used instead of unity's Start() method when you want to execute code once the object is connected to the Network, as
		/// opposed to when it is first instantiated.
		/// base.NetworkStart() must be called first before doing any code relating to the Network.
		/// </remarks>
		protected virtual void NetworkStart()
		{
			IsSetup = true;

			lock (missingIdMutex)
			{
				if (missingIdBuffer.ContainsKey(NetworkedId))
				{
					foreach (NetworkingStreamRPC rpcStream in missingIdBuffer[NetworkedId])
					{
						rpcStream.AssignBehavior(this);
						InvokeRPC(rpcStream);
					}

					missingIdBuffer.Remove(NetworkedId);
				}
			}


#if !BARE_METAL
			Unity.MainThreadManager.unityUpdate += UnityUpdate;
			Unity.MainThreadManager.unityFixedUpdate += UnityFixedUpdate;

			// Just make sure that Unity doesn't destroy this objeect on load
			if (dontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);
#endif
		}

		// JM: added for offline
		public virtual void OfflineStart()
		{
			Unity.MainThreadManager.unityUpdate += UnityUpdate;
			Unity.MainThreadManager.unityFixedUpdate += UnityFixedUpdate;
			IsOwner = true;
		}

		/// <summary>
		/// Setup the Simple Networked Monobehavior stack with a NetWorker
		/// </summary>
		/// <param name="owningSocket">The NetWorker to be setup with</param>
		/// <remarks>
		/// This method allows you to specify one or more SNMB (or SNMB derivatives), and make sure each of them is correctly setup with their own
		/// unique Id, has proper owner information setup, and the SNMB is properly associated with the NetWorker (See SimpleNetworkedMonoBehavior.UniqueId/ 
		/// SimpleNetworkedMonoBehavior.OwnerId for more). This is done by calling Setup() on each of the SNMBs and NetworkingManager.Setup().
		/// </remarks>
		public static void SetupObjects(SimpleNetworkedMonoBehavior[] behaviors, NetWorker owningSocket)
		{
			if (ObjectCounter == 0)
				GenerateUniqueId();

			NetworkingManager.Instance.Setup(owningSocket, owningSocket.IsServer, 0, 0);

			// TODO:  Got through all objects in NetworkingManager stack and set them up
			foreach (SimpleNetworkedMonoBehavior behavior in behaviors)
				if (!(behavior is NetworkingManager) && behavior != null)
					behavior.Setup(owningSocket, owningSocket.IsServer, GenerateUniqueId(), 0, true);
		}

		/// <summary>
		/// Setup the Simple Networked Monobehavior stack with a NetWorker, owner, Network id, and owner id
		/// </summary>
		/// <param name="owningSocket">The NetWorker to be setup with</param>
		/// <param name="isOwner">If this object is the owner</param>
		/// <param name="NetworkId">The NetworkID for this Simple Networked Monobehavior</param>
		/// <param name="ownerId">The OwnerID for this Simple Networked Monobehavior</param>
		/// <remarks>
		/// This is used to setup all the Attributes of the SNMB, such as the owner information, ids, RPCs and socket references. 
		/// Forge calls this on all SNMBs after SetupObjects() and Initialize() (look there for more information on usage).
		/// </remarks>
#if NetFX_CORE
		public virtual async void Setup(NetWorker owningSocket, bool isOwner, ulong NetworkId, ulong ownerId, bool isSceneObject = false)
#else
		public virtual void Setup(NetWorker owningSocket, bool isOwner, ulong NetworkId, ulong ownerId, bool isSceneObject = false)
#endif
		{
			Reflect();

			if (owningSocket == null)
				ThrowNetworkerException();

			int count = 0;
#if BARE_METAL
			while (!(this is NetworkingManager) && !NetworkingManager.Instance.IsSetup)
#else
			while (NetworkingManager.Instance != this && !NetworkingManager.Instance.IsSetup)
#endif
			{
#if NetFX_CORE
				await Task.Delay(TimeSpan.FromMilliseconds(25));
#else
				System.Threading.Thread.Sleep(25);
#endif

				if (++count == 20)
					throw new NetworkException("The NetworkingManager could not be found");
			}

			OwningNetWorker = owningSocket;
			IsOwner = isOwner;
			OwnerId = ownerId;

			if (!isSceneObject || sceneNetworkedId == 0)
				NetworkedId = NetworkId;
			else
				NetworkedId = (ulong)sceneNetworkedId;

			if (NetworkedId != 0 || !NetworkedBehaviors.ContainsKey(0))
				NetworkedBehaviors.Add(NetworkedId, this);

			if (OwningNetWorker.Me != null && ownerId == OwningNetWorker.Me.NetworkId)
				OwningPlayer = OwningNetWorker.Me;
			else if (OwningNetWorker.IsServer)
			{
				foreach (NetworkingPlayer player in OwningNetWorker.Players)
				{
					if (ownerId == player.NetworkId)
					{
						OwningPlayer = player;
						break;
					}
				}
			}

			if (OwningPlayer != null && OwningNetWorker.IsServer && this is NetworkedMonoBehavior)
				OwningPlayer.SetMyBehavior((NetworkedMonoBehavior)this);

			NetworkStart();
		}

		public void BareMetalUpdate()
		{
#if !BARE_METAL
			return;
#else
			UnityUpdate();
#endif
		}

		public int MaxRPCBatch = 10000;
		public void ExecuteRPCStack()
		{
			// If there are any pending RPC calls, then do them now on the main thread
			if (rpcStack.Count != 0)
			{
				int currentRPCBatch = Mathf.Clamp(rpcStack.Count, 1, MaxRPCBatch);
				bool found = false;

				for (int i = 0; i < currentRPCBatch; i++)
				{
					found = false;
					NetworkingStreamRPC stream = rpcStack[i];

					if (stream == null)
						continue;

					rpcStackExceptionMethodName = stream.MethodName;

					foreach (KeyValuePair<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>> rpc in RPCs)
					{
						if (rpc.Value.Key.Name == stream.MethodName)
						{
							CurrentRPCSender = stream.Sender;
							rpc.Value.Key.Invoke(this, stream.Arguments);
							CurrentRPCSender = null;
							found = true;
							break;
						}
					}

					if (!found)
					{
						throw new NetworkException(13, "Invoked Network method " + rpcStackExceptionMethodName + " not found or not marked with [BRPC]");
					}
				}

				lock (rpcStackMutex)
				{
					rpcStack.RemoveRange(0, currentRPCBatch);
				}
			}
		}

		/// <summary>
		/// Used to change the NetworkingPlayer who owns the SNMB.
		/// </summary>
		/// <param name="newOwnerPlayerId">The new NetworkingPlayer.NetworkId of the player who will now own this SNMB</param>
		/// <remarks>
		/// This method requires the object of the SNMB to have SimpleNetworkedMonoBehaviour.allowOwnershipChange to be set to <c>true</c>, which
		/// can be set in the inspector as it is a public attribute. Additionally, only the player who owns the SNMB and the server can change the owner.
		/// </remarks>
		public void ChangeOwner(ulong newOwnerPlayerId)
		{
			if (!allowOwnershipChange)
				return;

			// Only the current owner or server can change the owner of this object
			if (!IsOwner && !OwningNetWorker.IsServer)
			{
#if UNITY_EDITOR
				Debug.LogError("Only the current owner or server can change the owner of this object");
#endif
				return;
			}

			if (!OwningNetWorker.IsServer)
				RPC("ServerChangeOwner", NetworkReceivers.Server, newOwnerPlayerId);
			else
				ServerChangeOwner(newOwnerPlayerId);
		}

		// JM: made public to fix "NetworkException: No method marked with [BRPC] was found by the name ServerChangeOwner"
		[BRPC]
		public void ServerChangeOwner(ulong newOwnerPlayerId)
		{
			if (!OwningNetWorker.IsServer || !allowOwnershipChange)
				return;

			NetworkingPlayer player = null;
			player = OwningNetWorker.Players.Find(x => x.NetworkId == newOwnerPlayerId);

			if (player != null || newOwnerPlayerId == 0)
				RPC("AssignNewOwner", newOwnerPlayerId);
			else
				Debug.LogError("No such player with id " + newOwnerPlayerId + " currently connected.");
		}

		[BRPC]
		protected void AssignNewOwner(ulong newOwnerPlayerId)
		{
			if (!allowOwnershipChange)
				return;

			OwnerId = newOwnerPlayerId;

			if (OwningNetWorker.Uniqueidentifier == newOwnerPlayerId)
				IsOwner = true;
			else
				IsOwner = false;
		}

		private void DoOwnerUpdate()
		{
			if (IsOwner)
				OwnerUpdate();
			else
				NonOwnerUpdate();
		}

		private void DoOwnerFixedUpdate()
		{
			if (IsOwner)
				OwnerFixedUpdate();
			else
				NonOwnerFixedUpdate();
		}

		protected virtual void UnityUpdate()
		{
			if (!Networking.UseFixedUpdate) // JM: option for RPCs to run off fixed loop
			{
				ExecuteRPCStack();
			}

			DoOwnerUpdate();
		}

		protected virtual void UnityFixedUpdate()
		{
			if (Networking.UseFixedUpdate) // JM: option for RPCs to run off fixed loop
			{
				ExecuteRPCStack();
			}
			DoOwnerFixedUpdate();
		}
		/// <summary>
		/// Called every frame on SNMB/NMBs that belong to the client.
		/// </summary>
		/// <remarks>
		/// A typical usage would be:
		/// <code>
		///protected override void OwnerUpdate(){
		///   base.OwnerUpdate();
		///
		///   if (Input.GetKey(KeyCode.UpArrow))
		///        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
		///
		///   if (Input.GetKey(KeyCode.DownArrow))
		///        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
		///
		///    if (Input.GetKey(KeyCode.RightArrow))
		///        transform.Rotate(Vector3.up, 5.0f);
		///
		///   if (Input.GetKey(KeyCode.LeftArrow))
		///        transform.Rotate(Vector3.right, 5.0f);
		///}
		/// </code>
		/// You may want to execute code on SNMB/NMBs that belong to the local player,
		/// rather than using the Update method in combination with if(IsOwner),
		/// OwnerUpdate() should be used.
		/// 
		/// If you want to execute code every frame on SNMB/NMBs that do NOT belong to
		/// the client use NonOwnerUpdate(). See Networking.Instantiate() to see more about ownership.
		/// </remarks>
		protected virtual void OwnerUpdate() { }

		/// <summary>
		/// Called every frame on SNMB/NMBs that do NOT belong to the client.
		/// </summary>
		/// <remarks>
		/// You may want to execute code on SNMB/NMBs that do NOT belong to the local player,
		/// rather than using the Update method in combination with if(!IsOwner),
		/// NonOwnerUpdate() should be used.
		/// 
		/// If you want to execute code every frame on SNMB/NMBs that do belong to
		/// the client use NonOwnerUpdate().
		/// </remarks>
		protected virtual void NonOwnerUpdate() { }

		/// <summary>
		/// This offers the same functionality as OwnerUpdate(), but executes 60 times per second regardless of frame rate. See OwnerUpdate() for more.
		/// </summary>
		protected virtual void OwnerFixedUpdate() { }

		/// <summary>
		/// This offers the same functionality as NonOwnerUpdate(), but executes 60 times per second regardless of frame rate. See NonOwnerUpdate() for more.
		/// </summary>
		protected virtual void NonOwnerFixedUpdate() { }

		private int TranslateChildRPC(int currentId)
		{
#if BARE_METAL
			string snmb = typeof(SimpleNetworkedMonoBehavior).ToString();
			string nmb = typeof(NetworkedMonoBehavior).ToString();
			string nm = typeof(NetworkingManager).ToString();

			if (ClassType == snmb || ClassType == nmb || ClassType == nm)
				return currentId;

			string method = ClassMap.ValidRPC(currentId, ClassType);

			if (string.IsNullOrEmpty(method))
				return -1;

			string parentType = ClassMap.GetParent(ClassType);

			string[] parentMethods = null;
			if (parentType == nmb || parentType == snmb)
			{
				if (parentType == nmb)
					parentMethods = ClassMap.GetMethodList(nmb);
				else if (parentType == snmb)
					parentMethods = ClassMap.GetMethodList(snmb);

				for (int i = 0; i < parentMethods.Length; i++)
				{
					if (method == parentMethods[i])
					{
						currentId = i;
						break;
					}
				}
			}
#endif

			return currentId;
		}

		private int TranslateParentRPC(int currentId)
		{
#if BARE_METAL
			string snmb = typeof(SimpleNetworkedMonoBehavior).ToString();
			string nmb = typeof(NetworkedMonoBehavior).ToString();
			string nm = typeof(NetworkingManager).ToString();

			if (ClassType == snmb || ClassType == nmb || ClassType == nm)
				return currentId;

			string method = ClassMap.ValidRPC(currentId, nmb);

			if (string.IsNullOrEmpty(method))
				return -1;

			string[] childMethods = ClassMap.GetMethodList(ClassType);

			for (int i = 0; i < childMethods.Length; i++)
			{
				if (method == childMethods[i])
				{
					currentId = i;
					break;
				}
			}
#endif

			return currentId;
		}

		int tmpRPCMapId = 0;
		/// <summary>
		/// To Invoke an RPC on a given Networking Stream RPC
		/// </summary>
		/// <param name="stream">Networking Stream RPC to read from</param>
		/// <remarks>
		/// InvokeRPC() is primarily used internally, it validates the RPC being called to make sure that the RPC exists and the parameters given are
		/// compatible. If the RPC is validated the method returns <c>true</c> and the RPC is added to the stack which executes the RPC on the next frame.
		/// The NetworkingStreamRPC is the stream created that can be passed over the Network to represent the request, parameters and all other meta information
		/// needed by the Network.
		/// </remarks>
		public bool InvokeRPC(NetworkingStreamRPC stream)
		{
			tmpRPCMapId = ObjectMapper.Map<int>(stream);

#if BARE_METAL
				tmpRPCMapId = TranslateChildRPC(tmpRPCMapId);

				if (tmpRPCMapId < 0)
					return false;

				// TODO:  Use Bare Metal RPC intercept plugin
#endif

			if (!RPCs.ContainsKey(tmpRPCMapId))
				return true;

			stream.SetName(RPCs[tmpRPCMapId].Key.Name);

			List<object> args = new List<object>();

			MethodInfo invoke = null;
			List<IBRPCIntercept> attributes = null;
			foreach (KeyValuePair<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>> m in RPCs)
			{
				if (m.Value.Key.Name == stream.MethodName)
				{
					invoke = m.Value.Key;
					attributes = m.Value.Value;
					break;
				}
			}

			int start = stream.Bytes.StartIndex(stream.ByteReadIndex);
			ParameterInfo[] pars = invoke.GetParameters();
			foreach (ParameterInfo p in pars)
			{
				if (p.ParameterType == typeof(MessageInfo))
					args.Add(new MessageInfo(stream.RealSenderId, stream.FrameIndex));
				else
					args.Add(ObjectMapper.Map(p.ParameterType, stream));
			}

			stream.SetArguments(args.ToArray());

			if (ReferenceEquals(this, NetworkingManager.Instance))
			{
				if (OwningNetWorker.IsServer)
				{
					if (stream.MethodName == NetworkingStreamRPC.INSTANTIATE_METHOD_NAME)
						stream.Arguments[1] = stream.SetupInstantiateId(stream, start);
					else if (stream.MethodName == NetworkingStreamRPC.DESTROY_METHOD_NAME)
					{
						if (OwningNetWorker.ClearBufferedInstantiateFromID((ulong)args[0]))
						{
							// Set flag if method removed instantiate
							IsClearedForBuffer = !stream.BufferedRPC;
						}
					}
				}
			}

			foreach (IBRPCIntercept interceptor in attributes)
			{
				if (!interceptor.ValidateRPC(stream))
					return false;
			}

			lock (rpcStackMutex)
			{
				rpcStack.Add(stream);
			}

			return true;
		}

		/// <summary>
		/// Creates a Network stream for the method with the specified string name and returns the method info
		/// </summary>
		/// <param name="methodName">The name of the method to call from this class</param>
		/// <param name="receivers">The players on the Network that will be receiving RPC</param>
		/// <param name="arguments">The list of arguments that will be sent for the RPC</param>
		/// <returns></returns>
		private int GetStreamRPC(string methodName, NetworkReceivers receivers, params object[] arguments)
		{
			foreach (KeyValuePair<int, KeyValuePair<MethodInfo, List<IBRPCIntercept>>> rpc in RPCs)
			{
				if (rpc.Value.Key.Name == methodName)
				{
					if (!NetworkingManager.IsOnline)
						return rpc.Key;

					int rpcId = rpc.Key;

#if BARE_METAL
					rpcId = TranslateParentRPC(rpc.Key);
#endif

					getStreamBuffer.Clear();
					ObjectMapper.MapBytes(getStreamBuffer, rpcId);

#if UNITY_EDITOR
					int argCount = 0;
					bool matcheTypes = true;

					ParameterInfo[] parameters = rpc.Value.Key.GetParameters();
					for (int i = 0; i < parameters.Length; i++)
					{
						if (parameters[i].ParameterType != typeof(MessageInfo))
						{
							if (parameters[i].ParameterType != arguments[i].GetType())
							{
								matcheTypes = false;
								break;
							}
							argCount++;
						}
					}

					if (!matcheTypes)
						throw new NetworkException("There is no BRPC matching signature (" + string.Join(", ", arguments.Select(x => x.GetType().ToString()).ToArray()) + ") for method '" + methodName + "'");
					
					if (arguments.Length != argCount)
						throw new NetworkException("The number of arguments [" + arguments.Length + "] provided for the " + methodName + " RPC call do not match the method signature argument count [" + rpc.Value.Key.GetParameters().Length + "]");
#endif

					if (arguments != null && arguments.Length > 0)
						ObjectMapper.MapBytes(getStreamBuffer, arguments);

					bool buffered = receivers == NetworkReceivers.AllBuffered || receivers == NetworkReceivers.OthersBuffered;

#if !UNITY_WEBGL
					rpcNetworkingStream.SetProtocolType(OwningNetWorker is CrossPlatformUDP ? Networking.ProtocolType.UDP : Networking.ProtocolType.TCP);
#else
					rpcNetworkingStream.SetProtocolType(Networking.ProtocolType.TCP);
#endif

					rpcNetworkingStream.Prepare(OwningNetWorker, NetworkingStream.IdentifierType.RPC, this.NetworkedId, getStreamBuffer, receivers, buffered);

					return rpc.Key;
				}
			}

			throw new NetworkException(14, "No method marked with [BRPC] was found by the name " + methodName);
		}

		private void AuthRPC(string methodName, NetWorker socket, NetworkingPlayer player, bool runOnServer, string uniqueIdentifier, bool reliable, params object[] arguments)
		{
			int rpcId = GetStreamRPC(methodName, NetworkReceivers.All, arguments);

#if !UNITY_WEBGL
			if (socket is CrossPlatformUDP)
				((CrossPlatformUDP)socket).Write(uniqueIdentifier + methodName, player, rpcNetworkingStream, reliable);
			else
#endif
				socket.Write(player, rpcNetworkingStream);

			if (socket.IsServer && runOnServer)
			{
				Unity.MainThreadManager.Run(() =>
				{
					bool failedValidate = false;

					foreach (IBRPCIntercept intercept in RPCs[rpcId].Value)
					{
						if (!intercept.ValidateRPC(RPCs[rpcId].Key))
						{
							failedValidate = true;
							break;
						}
					}

					if (!failedValidate)
						RPCs[rpcId].Key.Invoke(this, arguments);
				});
			}
		}

		/// <summary>
		/// Used for the server to call an RPC method on a NetWorker(Socket) on a particular player
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="player">The NetworkingPlayer who will execute this RPC</param>
		/// <param name="arguments">The RPC function parameters to be passed in</param>
		/// <remarks>
		/// Authorative RPC allows a client or the server to call an RPC on the specified NetworkingPlayer, as opposed to RPC which can call on all or a specified
		/// group of players. The Authorative RPC can be called on the server as well as the NetworkingPlayer who receives the specific message by using the runOnServer flag.
		/// </remarks>
		public void AuthoritativeRPC(string methodName, NetWorker socket, NetworkingPlayer player, bool runOnServer, params object[] arguments)
		{
			AuthRPC(methodName, socket, player, runOnServer, "BMS_INTERNAL_Rpc_", true, arguments);
		}

		/// <summary>
		/// Used for the server to call an URPC method on a NetWorker(Socket) on a particular player
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="player">The NetworkingPlayer who will execute this RPC</param>
		/// <param name="arguments">The RPC function parameters to be passed in</param>
		/// <remarks>
		/// AuthorativeURPC() is a combination of AuthoritativeRPC() and URPC(), it allows you to specify a specific Network.NetworkingPlayer to send the RPC to
		/// the RPC is also URPC which means the RPC may not arrive.
		/// </remarks>
		public void AuthoritativeURPC(string methodName, NetWorker socket, NetworkingPlayer player, bool runOnServer, params object[] arguments)
		{
			AuthRPC(methodName, socket, player, runOnServer, "BMS_INTERNAL_Urpc_", false, arguments);
		}

		/// <summary>
		/// Call an RPC method on a NetWorker(Socket) with receivers and arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		/// <param name="arguments">The RPC function parameters to be passed in</param>
		/// <remarks>
		/// See RPC()...
		/// 
		/// this overload executes an RPC() on the specified NetWorker, the Network.NetworkReceivers allows you to control
		/// the clients who will execute the RPC by specifying Network.NetworkReceivers.
		/// </remarks>
		public void RPC(string methodName, NetWorker socket, NetworkReceivers receivers, params object[] arguments)
		{
			_RPC(methodName, socket, receivers, true, arguments);
		}

		/// <summary>
		/// Call an Unreliable RPC method on a NetWorker(Socket) with receivers and arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		/// <param name="arguments">The RPC function parameters to be passed in</param>
		/// <remarks>
		/// URPC (Unreliable Remote Procedure Call) makes an unreliable RPC call across the Network. See SimpleNetworkedMonoBehavior.RPC() for more information
		/// on RPCs. The RPC is unreliable, this means it may not arrive every time, because of this the URPC would typically be used when sending many RPCs,
		/// in a situation where it won't matter (or break your system) if one of the RPCs are lost while sending across the Network.
		/// </remarks>
		public void URPC(string methodName, NetWorker socket, NetworkReceivers receivers, params object[] arguments)
		{
			_RPC(methodName, socket, receivers, false, arguments);
		}

		//Helper function - toreau
		private void _RPC(string methodName, NetWorker socket, NetworkReceivers receivers, bool reliable, params object[] arguments)
		{
			int rpcId = GetStreamRPC(methodName, receivers, arguments);

			// JM: offline fix
			if (NetworkingManager.IsOnline)
			{
#if !UNITY_WEBGL
				if (socket is CrossPlatformUDP)
					((CrossPlatformUDP)socket).Write("BMS_INTERNAL_Rpc_" + methodName, rpcNetworkingStream, reliable);
				else
#endif
					socket.Write(rpcNetworkingStream);
			}

			// JM: added offline check and similar change that was in the reliable RPC
			if ((!NetworkingManager.IsOnline || socket.IsServer) && receivers != NetworkReceivers.Others && receivers != NetworkReceivers.OthersBuffered && receivers != NetworkReceivers.OthersProximity)
			{
				Unity.MainThreadManager.Run(() =>
				{
					bool faildValidate = false;

					foreach (IBRPCIntercept intercept in RPCs[rpcId].Value)
					{
						if (!intercept.ValidateRPC(RPCs[rpcId].Key))
						{
							faildValidate = true;
							break;
						}
					}

					if (faildValidate)
						return;

					List<object> args = new List<object>();
					int argCount = 0;
					foreach (ParameterInfo info in RPCs[rpcId].Key.GetParameters())
					{
						if (info.ParameterType == typeof(MessageInfo))
							args.Add(new MessageInfo(OwningNetWorker.Me.NetworkId, NetworkingManager.Instance.CurrentFrame));
						else
							args.Add(arguments[argCount++]);
					}

					CurrentRPCSender = OwningPlayer;
					RPCs[rpcId].Key.Invoke(this, args.ToArray());
					CurrentRPCSender = null;
				});
			}
		}

		/// <summary>
		/// Call an RPC method with arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="arguments">Extra parameters passed in</param>
		/// <remarks>
		/// See BRPC for more information on Remote Procedure Calls...
		/// 
		/// This method executes the RPC across ALL clients in the Network (including the server). The method name is the string name of the RPC method.
		/// A set of parameters can be passed that correspond to the arguements of the target RPC method's arguements.
		/// </remarks>
		public void RPC(string methodName, params object[] arguments)
		{
			RPC(methodName, OwningNetWorker, NetworkReceivers.All, arguments);
		}

		/// <summary>
		/// Call an RPC method with a receiver and arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="rpcMode">Who shall receive the RPC</param>
		/// <param name="arguments">Extra parameters passed in</param>
		/// <remarks>
		/// See RPC()...
		/// 
		/// This overload allows you to specify Network.Receivers to control who in the Network executes the RPC.
		/// </remarks>
		public void RPC(string methodName, NetworkReceivers rpcMode, params object[] arguments)
		{
			RPC(methodName, OwningNetWorker, rpcMode, arguments);
		}

		/// <summary>
		/// Call an Unreliable RPC method with a receiver and arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="rpcMode">Who shall receive the RPC</param>
		/// <param name="arguments">Extra parameters passed in</param>
		/// <remarks>
		/// See URPC()...
		/// 
		/// This overload allows you to specify Network.Receivers to control who in the Network executes the URPC.
		/// </remarks>
		public void URPC(string methodName, NetworkReceivers rpcMode, params object[] arguments)
		{
			URPC(methodName, OwningNetWorker, rpcMode, arguments);
		}

		/// <summary>
		/// Call an RPC method with a NetWorker(Socket) and arguments
		/// </summary>
		/// <param name="methodName">Method(Function) name to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="arguments">Extra parameters passed in</param>
		/// <remarks>
		/// See RPC()...
		/// 
		/// This overload executes the RPC on ALL clients (including the server), on the specified NetWorker.
		/// </remarks>
		public void RPC(string methodName, NetWorker socket, params object[] arguments)
		{
			RPC(methodName, socket, NetworkReceivers.All, arguments);
		}

		/// <summary>
		/// Serialize the Simple Networked Monobehavior
		/// </summary>
		/// <returns></returns>
		public virtual BMSByte Serialized() { return null; }

		/// <summary>
		/// Deserialize the Networking Stream
		/// </summary>
		/// <param name="stream">Stream to be deserialized</param>
		public virtual void Deserialize(NetworkingStream stream) { }

		/// <summary>
		/// Used to do final cleanup when disconnecting. This gets called currently on application quit and scene resets
		/// </summary>
		/// <remarks>
		/// Cleans up object from the main thread manager and any remaining variables on the object. If object has DestroyOnDisconnect set <c>true</c>,
		/// object will be destroyed.
		/// </remarks>
		public virtual void Disconnect()
		{
			Cleanup();
			Unity.MainThreadManager.unityUpdate -= UnityUpdate;
			Unity.MainThreadManager.unityFixedUpdate -= UnityFixedUpdate;
		}

		/// <summary>
		/// Cleans up main thread manager hookups, called when object is destroyed
		/// </summary>
		/// <remarks>
		/// base.OnDestroy() must be called first in the method, this can be used to execute code when the Networked object is destroyed.
		/// </remarks>
		protected virtual void OnDestroy()
		{
			Cleanup();
		}

		/// <summary>
		/// Method called when application closes, calls Disconnect()
		/// </summary>
		/// <remarks>
		/// This method is called when the unity application is closed, base.OnApplicationQuit() must be called first.
		/// The method calls Disconnect(), but you can also use it to execute code on closing the unity application.
		/// </remarks>
		protected virtual void OnApplicationQuit() { Disconnect(); }

		/// <summary>
		/// Force destroys all buffers and Network information
		/// </summary>
		/// <remarks>
		/// Method used to force the Network object to disconnect, used if the server kicks the client out or if you need to forcibly disconnect a client.
		/// </remarks>
		protected virtual void NetworkDisconnect()
		{
			Disconnect();
			initialSetup = false;

			lock (NetworkedBehaviorsMutex)
			{
				NetworkedBehaviors.Clear();
			}

			ObjectCounter = 0;
			missingIdBuffer.Clear();
		}

		/// <summary>
		/// Call an RPC method with arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		public void RPC(Expression<Action> method)
		{
			RPC(method, OwningNetWorker);
		}

		/// <summary>
		/// Call an RPC method with a NetWorker(Socket) and arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		public void RPC(Expression<Action> method, NetWorker socket)
		{
			RPC(method, socket, NetworkReceivers.All);
		}

		/// <summary>
		/// Call an RPC method with a receiver and arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		public void RPC(Expression<Action> method, NetworkReceivers receivers)
		{
			RPC(method, OwningNetWorker, receivers);
		}

		/// <summary>
		/// Call an RPC method on a NetWorker(Socket) with receivers and arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		public void RPC(Expression<Action> method, NetWorker socket, NetworkReceivers receivers)
		{
			MethodCallExpression call = GetMethodCallExpression(method);

			RPC(call.Method.Name, socket, receivers, GetAttributesFromMethodCall(call));
		}

		/// <summary>
		/// Call an Unreliable RPC method with a receiver and arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		public void URPC(Expression<Action> method, NetworkReceivers receivers)
		{
			URPC(method, OwningNetWorker, receivers);
		}

		/// <summary>
		/// Call an Unreliable RPC method on a NetWorker(Socket) with receivers and arguments
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="receivers">Who shall receive the RPC</param>
		public void URPC(Expression<Action> method, NetWorker socket, NetworkReceivers receivers)
		{
			MethodCallExpression call = GetMethodCallExpression(method);

			URPC(call.Method.Name, socket, receivers, GetAttributesFromMethodCall(call));
		}

		/// <summary>
		/// Used for the server to call an RPC method on a NetWorker(Socket) on a particular player
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="player">The NetworkingPlayer who will execute this RPC</param>
		public void AuthoritativeRPC(Expression<Action> method, NetWorker socket, NetworkingPlayer player, bool runOnServer)
		{
			MethodCallExpression call = GetMethodCallExpression(method);

			AuthoritativeRPC(call.Method.Name, socket, player, runOnServer, GetAttributesFromMethodCall(call));
		}

		/// <summary>
		/// Used for the server to call an URPC method on a NetWorker(Socket) on a particular player
		/// </summary>
		/// <param name="method">Method(Function) to call</param>
		/// <param name="socket">The NetWorker(Socket) being used</param>
		/// <param name="player">The NetworkingPlayer who will execute this RPC</param>
		public void AuthoritativeURPC(Expression<Action> method, NetWorker socket, NetworkingPlayer player, bool runOnServer)
		{
			MethodCallExpression call = GetMethodCallExpression(method);

			AuthoritativeURPC(call.Method.Name, socket, player, runOnServer, GetAttributesFromMethodCall(call));
		}

		/// <summary>
		/// Goes through all the Arguments in a MethodCallExpression and gets all
		/// there values and returns them.
		/// </summary>
		/// <param name="call">The MethodCallExpression for which the Argument values should be obtained.</param>
		/// <returns>An object Array with all the values.</returns>
		private static object[] GetAttributesFromMethodCall(MethodCallExpression call)
		{
			object[] arguments = new object[call.Arguments.Count];

			for (int i = 0; i < call.Arguments.Count; i++)
			{
				arguments[i] = GetExpressionValue(call.Arguments[i]);
			}

			return arguments;
		}

		/// <summary>
		/// Creates a getter for the Expression and calls it to retrieve the value of said Expression.
		/// </summary>
		/// <param name="expression">The Expression for which the value should be obtained.</param>
		/// <returns>The value of the Expression.</returns>
		private static object GetExpressionValue(Expression expression)
		{
			UnaryExpression unaryExpression = Expression.Convert(expression, typeof(object));
			Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(unaryExpression);
			Func<object> getter = lambda.Compile();

			return getter();
		}

		/// <summary>
		/// Checks if the expression contains a MethodCallExpression in its Body and returns it,
		/// otherwise an Exception gets thrown.
		/// </summary>
		/// <param name="expression">The Expression to check.</param>
		/// <returns>The MethodCallExpression if it exists.</returns>
		private static MethodCallExpression GetMethodCallExpression(LambdaExpression expression)
		{
			MethodCallExpression outermostExpression = expression.Body as MethodCallExpression;

			//The Lambda didn't contain a method call
			if (outermostExpression == null)
			{
				throw new ArgumentException("Expression must have a MethodCall as its Body");
			}

			return outermostExpression;
		}
	}
}
