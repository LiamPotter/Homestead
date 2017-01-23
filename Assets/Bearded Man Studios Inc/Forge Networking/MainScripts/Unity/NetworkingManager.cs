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



using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_5_3
#endif

namespace BeardedManStudios.Network
{
	/// <summary>
	/// This is a singleton manager for all of the Networked behaviors
	/// </summary>
	public class NetworkingManager : SimpleNetworkedMonoBehavior
	{
#if BARE_METAL
		//public delegate SimpleNetworkedMonoBehavior CreateObjectEvent(string name, string typeName);
		//public static CreateObjectEvent createObject = null;
		//public CreateObjectEvent CreateObject { get { return createObject; } set { createObject = value; } }
		public NetworkingManager(string name, string type) : base(name, type) { instance = this; }
		public ulong CallGenerateUniqueId() { return GenerateUniqueId(); }
#endif

		// TODO:  Invent ping commands
		private BMSByte loadLevelPing = new BMSByte();

		public delegate void PlayerEvent(NetworkingPlayer player);
		public event PlayerEvent clientLoadedLevel = null;
		public NetWorker.BasicEvent allClientsLoaded = null;
		private int currentClientsLoaded = 0;

		/// <summary>
		/// The controlling socket of this NetworkingManager
		/// </summary>
		public static NetWorker ControllingSocket { get; set; }

		private static NetworkingManager instance = null;

		/// <summary>
		/// The instance of the NetworkingManager
		/// </summary>
		public static NetworkingManager Instance { get { return instance; } }

		/// <summary>
		/// This is a list of all of the behaviors in the scene that need to be setup on the Network
		/// </summary>
		public SimpleNetworkedMonoBehavior[] startNetworkedSceneBehaviors = null;

		/// <summary>
		/// Used to determine if the main game socket is online (ever was connected)
		/// </summary>
		public static bool IsOnline
		{
			get
			{
				if (ReferenceEquals(Instance, null) || !Instance.IsSetup || ReferenceEquals(Instance.OwningNetWorker, null))
					return false;

				return true;
			}
		}

		/// <summary>
		/// This is a static reference to NetworkingManager.Instance.OwningNetWorker
		/// </summary>
		public static NetWorker Socket { get { return Instance.OwningNetWorker; } }

		/// <summary>
		/// All game objects to be instantiated whenever necessary
		/// </summary>
		public GameObject[] NetworkInstantiates = null;

		/// <summary>
		/// The list of all setup actions for when an object is instantiated
		/// </summary>
		public static List<Action> setupActions = new List<Action>();

		/// <summary>
		/// This is a list of the players that the client can access
		/// </summary>
		public List<NetworkingPlayer> Players { get { return OwningNetWorker.Players; } }

		private BMSByte playerPollData = new BMSByte();
		private Action<List<NetworkingPlayer>> pollPlayersCallback = null;
		private Action loadLevelFireOnConnect = null;

		private Dictionary<string, GameObject> _cachedLoadedObjects = new Dictionary<string, GameObject>();

		private float previousTime = 0.0f;
		private float serverTime = 0.0f;
		private float lastTimeUpdate = 0.0f;

		/// <summary>
		/// Gets the time for the server
		/// </summary>
		public float ServerTime
		{
			get
			{
				if (!IsOnline || OwningNetWorker.IsServer)
					return Time.time;

				return serverTime + (Time.time - lastTimeUpdate);
			}
		}

		public float SimulatedServerTime { get; private set; }

		/// <summary>
		/// This is the resources directory where Network prefabs are to be loaded from
		/// </summary>
		public string resourcesDirectory = string.Empty;

		// TODO:  Complete the ideas of frame intervals
		public int frameInterval = 100;

		// TODO:  Complete the ideas of frame intervals
		public byte CurrentFrame { get { return (byte)(SimulatedServerTime * 1000 / frameInterval); } }

		public byte GetFrameCountFromTime(double milliseconds) { return (byte)(milliseconds / frameInterval); }

		/// <summary>
		/// The amount of time in seconds to update the time from the server
		/// </summary>
		public float updateTimeInterval = 1.0f;

#if BARE_METAL
		public static Dictionary<string, string[]> BehaviorsAndRefCount = new Dictionary<string, string[]>();
		public Dictionary<string, string[]> behaviorsAndRefCount { get { return BehaviorsAndRefCount; } set { BehaviorsAndRefCount = value; } }
#else
		private static Dictionary<string, int> behaviorsAndRefCount = new Dictionary<string, int>();
#endif

		private void Begin()
		{

		}

		public void BareMetalAwake()
		{
#if BARE_METAL
			Awake();
#endif
		}

		private void Awake()
		{
#if !BARE_METAL
			bool callInitialize = false;

			if (instance != null)
			{
				instance.dontDestroyOnLoad = false;
				rpcStack = instance.rpcStack;

				NetWorker currentSocket = ControllingSocket;
				Destroy(instance.gameObject);
				
				ResetForScene(new List<SimpleNetworkedMonoBehavior>(new SimpleNetworkedMonoBehavior[] { this }));
				callInitialize = true;

				ControllingSocket = currentSocket;
			}
			else
				Unity.UnityEventObject.onDestroy += SkipResetOnDestroy;
#endif

			List<SimpleNetworkedMonoBehavior> allCurrentNetworkBehaviors = new List<SimpleNetworkedMonoBehavior>();

			if (startNetworkedSceneBehaviors != null)
				allCurrentNetworkBehaviors.AddRange(startNetworkedSceneBehaviors);

#if !BARE_METAL
			SimpleNetworkedMonoBehavior[] behaviors = FindObjectsOfType<SimpleNetworkedMonoBehavior>().Union(allCurrentNetworkBehaviors).ToArray();

			foreach (SimpleNetworkedMonoBehavior behavior in behaviors)
			{
				foreach (SimpleNetworkedMonoBehavior childBehavior in GetAllSimpleMonoBehaviors(behavior.gameObject))
					if (!allCurrentNetworkBehaviors.Contains(childBehavior))
						allCurrentNetworkBehaviors.Add(childBehavior);
			}

			startNetworkedSceneBehaviors = allCurrentNetworkBehaviors.ToArray();

			instance = this;

			DontDestroyOnLoad(gameObject);
#endif

			dontDestroyOnLoad = true;

#if !BARE_METAL
			CreateUnityEventObject();

            if (NetworkInstantiates != null)
            {
                foreach (GameObject obj in NetworkInstantiates)
                {
                    foreach (GameObject obj2 in NetworkInstantiates)
                    {
                        if (obj != obj2 && obj.name == obj2.name)
                            Debug.LogError("You have two or more objects in the Network Instantiate array with the name " + obj.name + ", these should be unique");
                    }
                }

                foreach (GameObject obj in NetworkInstantiates)
                {
                    if (behaviorsAndRefCount.ContainsKey(obj.name))
                        continue;

                    behaviorsAndRefCount.Add(obj.name, GetAllSimpleMonoBehaviors(obj).Length);

                    if (behaviorsAndRefCount[obj.name] == 0)
                        throw new NetworkException("The object " + obj.name + " in the prefabs list requires to have at least 1 SimpleNetworkedMonoBehavior attached to it or a child");
                }
            }
            else
            {
                NetworkInstantiates = new GameObject[0];
                Debug.LogWarning("Your network instantiates are empty");
            }

			if (!string.IsNullOrEmpty(resourcesDirectory))
			{
				foreach (GameObject obj in Resources.LoadAll<GameObject>(resourcesDirectory))
				{
					if (behaviorsAndRefCount.ContainsKey(obj.name))
						continue;

					behaviorsAndRefCount.Add(obj.name, GetAllSimpleMonoBehaviors(obj).Length);

					if (behaviorsAndRefCount[obj.name] == 0)
						throw new NetworkException("The object " + obj.name + " in the resources directory requires to have at least 1 SimpleNetworkedMonoBehavior attached to it or a child");
				}
			}

			if (callInitialize)
				Initialize(ControllingSocket);
#endif
		}

		/// <summary>
		/// This method is used to do all of the initial setup of objects
		/// </summary>
		public bool Populate(NetWorker socket)
		{
#if !BARE_METAL
			
			List<SimpleNetworkedMonoBehavior> allBehaviors = new List<SimpleNetworkedMonoBehavior>(startNetworkedSceneBehaviors);
			// Find all objects in the scene that have SNMB
			SimpleNetworkedMonoBehavior[] behaviors = FindObjectsOfType<SimpleNetworkedMonoBehavior>();
			
			foreach (SimpleNetworkedMonoBehavior behavior in behaviors)
			{
				if (!allBehaviors.Contains(behavior))
					allBehaviors.Add(behavior);
			}
			
			if (Networking.Sockets == null)
			{
				Debug.LogWarning("No connection has been established for this Network scene, preparing offline play...");
				IsSetup = true;

				foreach (Action execute in setupActions)
					execute();

				return false;
			}
			
			ControllingSocket = socket;
			OwningNetWorker = ControllingSocket;

			if (!OwningNetWorker.Connected)
			{
				NetWorker.BasicEvent handler = null;

				handler = () =>
				{
					SetupObjects(allBehaviors.ToArray(), OwningNetWorker);

					foreach (Action execute in setupActions)
						execute();

					OwningNetWorker.connected -= handler;
				};

				OwningNetWorker.connected += handler;
			}
			else
			{
				SetupObjects(allBehaviors.ToArray(), OwningNetWorker);

				foreach (Action execute in setupActions)
					execute();
			}
#else
			ControllingSocket = socket;
			OwningNetWorker = ControllingSocket;
#endif
			OwningNetWorker.AddCustomDataReadEvent(WriteCustomMapping.NetWORKING_MANAGER_POLL_PLAYERS, PollPlayersResponse, true);
			return true;
		}

		public static bool TryPullIdFromObject(string obj, ref ulong uniqueId)
		{
			if (Instance.OwningNetWorker.IsServer)
			{
				uniqueId = GenerateUniqueId();

#if BARE_METAL
				if (BehaviorsAndRefCount[obj].Length > 1)
				{
					for (int i = 1; i < BehaviorsAndRefCount[obj].Length; i++)
						GenerateUniqueId();
				}
#else
				// If there are multiple Network behaviors on this object then update the ids
				if (behaviorsAndRefCount[obj] > 1)
				{
					for (int i = 1; i < behaviorsAndRefCount[obj]; i++)
						GenerateUniqueId();
				}
#endif
			}

			return true;
		}

		/// <summary>
		/// This is the main instantiate method that is proxyed through the Networking.Instantiate method
		/// </summary>
		/// <param name="receivers">The receivers that will get this instantiate command</param>
		/// <param name="obj">The name of the object that is to be instantiated</param>
		/// <param name="position">The position where the object will be instantiated at</param>
		/// <param name="rotation">The rotation that the object will have when instantiated</param>
		/// <param name="callbackCounter">The id for the callback that is to be called when this object is instantiated</param>
		public static void Instantiate(NetworkReceivers receivers, string obj, Vector3 position, Quaternion rotation, int callbackCounter)
		{
			ulong uniqueId = 0;

			if (!TryPullIdFromObject(obj, ref uniqueId))
				return;
			
			if (Instance != null && Instance.OwningNetWorker != null)
				Instance.RPC("NetworkInstantiate", receivers, Instance.OwningNetWorker.Uniqueidentifier, Instance.OwningNetWorker.IsServer ? uniqueId : 0, obj, position, rotation, callbackCounter);
			else
			{
				setupActions.Add(() =>
				{
					Instance.RPC("NetworkInstantiate", receivers, Instance.OwningNetWorker.Uniqueidentifier, Instance.OwningNetWorker.IsServer ? uniqueId : 0, obj, position, rotation, callbackCounter);
				});
			}
		}

		protected override void NetworkStart()
		{
			base.NetworkStart();

			OwningNetWorker.AddCustomDataReadEvent(WriteCustomMapping.NetWORKING_MANAGER_PLAYER_LOADED_LEVEL, PlayerLoadedLevel, true);
			OwningNetWorker.AddCustomDataReadEvent(WriteCustomMapping.NetWORKED_MONO_BEHAVIOR_MANUAL_PROPERTIES, DeserializeManualProperties, true);
			OwningNetWorker.AddCustomDataReadEvent(WriteCustomMapping.TRANSPORT_OBJECT, ReadTransportObject, true);

			if (loadLevelFireOnConnect != null)
			{
				loadLevelFireOnConnect();
				loadLevelFireOnConnect = null;
			}
		}

		private void DeserializeManualProperties(NetworkingPlayer player, NetworkingStream stream)
		{
			ulong NetworkedId = ObjectMapper.Map<ulong>(stream);
			NetworkedMonoBehavior.Locate(NetworkedId).DeserializeManualProperties(stream);
		}

		private void ReadTransportObject(NetworkingPlayer player, NetworkingStream stream)
		{
			ForgeTransportObject.Locate(ObjectMapper.Map<ulong>(stream)).ReadFromNetwork(stream);
		}

		private void Update()
		{
			SimulatedServerTime = ServerTime;

			if (!IsOnline)
				return;

			if (!OwningNetWorker.IsServer)
				return;

			if (updateTimeInterval <= 0.01f)
				return;

			if (previousTime + updateTimeInterval <= Time.time)
			{
				URPC("UpdateServerTime", NetworkReceivers.Others, Time.time);
				serverTime = Time.time;
				previousTime = Time.time;
			}
		}

		public static SimpleNetworkedMonoBehavior[] GetAllSimpleMonoBehaviors(GameObject o)
		{
#if !BARE_METAL
			List<SimpleNetworkedMonoBehavior> behaviors = new List<SimpleNetworkedMonoBehavior>(o.GetComponents<SimpleNetworkedMonoBehavior>());

			for (int i = 0; i < o.transform.childCount; i++)
				behaviors.AddRange(GetAllSimpleMonoBehaviors(o.transform.GetChild(i).gameObject));
#else
			List<SimpleNetworkedMonoBehavior> behaviors = new List<SimpleNetworkedMonoBehavior>();
#endif
			return behaviors.ToArray();
		}

		[BRPC]
		public void NetworkInstantiate(ulong ownerId, ulong startNetworkId, string name, Vector3 position, Quaternion rotation, int callbackId = 0)
		{
			lock (NetworkedBehaviorsMutex)
			{
				if (NetworkedBehaviors.ContainsKey(startNetworkId))
					return;

				SimpleNetworkedMonoBehavior[] NetBehaviors = null;

#if BARE_METAL
				NetBehaviors = new SimpleNetworkedMonoBehavior[behaviorsAndRefCount[name].Length];

				for (int i = 0; i < NetBehaviors.Length; i++)
				{
					//if (CreateObject != null)
					//	NetBehaviors[i] = CreateObject(name + "(Clone)(Child)" + i, behaviorsAndRefCount[name][i]);

					if (behaviorsAndRefCount[name][i] == typeof(SimpleNetworkedMonoBehavior).ToString() || behaviorsAndRefCount[name][i] == typeof(NetworkedMonoBehavior).ToString())
					{
						NetBehaviors[i] = (SimpleNetworkedMonoBehavior)Activator.CreateInstance(typeof(SimpleNetworkedMonoBehavior).Assembly.GetType(behaviorsAndRefCount[name][i]), name, behaviorsAndRefCount[name][i]);
					}
					else
					{
						foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
						{
							if (assembly.FullName.Contains("ForgeBareMetalGame"))
							{
								NetBehaviors[i] = (SimpleNetworkedMonoBehavior)Activator.CreateInstance(assembly.GetType(behaviorsAndRefCount[name][i]), name, behaviorsAndRefCount[name][i]);
								break;
							}
						}
					}
				}
#else

				GameObject o = Instance.PullObject((ownerId != OwningNetWorker.Me.NetworkId ? name + "(Remote)" : name), name);
				NetBehaviors = GetAllSimpleMonoBehaviors(o);

				if (NetBehaviors.Length == 0)
				{
					Debug.LogError("Instantiating on the Network is only for objects that derive from BaseNetworkedMonoBehavior, " +
						"if object does not need to be serialized consider using a RPC with GameObject.Instantiate");

					return;
				}

				GameObject tmp = (Instantiate(o, position, rotation) as GameObject);
				NetBehaviors = GetAllSimpleMonoBehaviors(tmp);
#endif

				while (ObjectCounter < startNetworkId + (ulong)NetBehaviors.Length - 1)
				{
					GenerateUniqueId();
				}

				for (int i = 0; i < NetBehaviors.Length; i++)
					NetBehaviors[i].Setup(OwningNetWorker, OwningNetWorker.Uniqueidentifier == ownerId, startNetworkId + (ulong)i, ownerId);

#if !BARE_METAL
				if (ownerId == OwningNetWorker.Me.NetworkId)
					Networking.RunInstantiateCallback(callbackId, NetBehaviors[0].GetComponent<SimpleNetworkedMonoBehavior>());
#else
				if (ownerId == OwningNetWorker.Me.NetworkId)
					Networking.RunInstantiateCallback(callbackId, NetBehaviors[0]);
#endif
			}
		}

		[BRPC]
		private void DestroyOnNetwork(ulong NetworkId)
		{
			NetworkDestroy(NetworkId);
		}

		/// <summary>
		/// Get a game object of a given name in the NetworkingManager
		/// </summary>
		/// <param name="name">Name of the game object to pull</param>
		/// <returns>If the GameObject exists in the NetworkingManager, then it will return that. Otherwise <c>null</c></returns>
		public GameObject PullObject(string name, string fallback = "")
		{
			if (name == fallback)
				fallback = string.Empty;

			foreach (GameObject obj in NetworkInstantiates)
				if (obj.name == name)
					return obj;

			if (_cachedLoadedObjects.ContainsKey(name))
				return _cachedLoadedObjects[name];
			else
			{
				GameObject find = null;
				bool isRemote = name.Contains("(Remote)"); //Determining if this object is a remote, if we don't already have it stored
				//Then we will skip loading resources again to try and search for it.

				//If _cachedLoadedObjects.Count == 0, then this is our first time loading stuff from our resources directory
				if ((_cachedLoadedObjects.Count == 0 || !isRemote) && !string.IsNullOrEmpty(resourcesDirectory))
				{
					foreach (GameObject obj in Resources.LoadAll<GameObject>(resourcesDirectory))
					{
						if (!_cachedLoadedObjects.ContainsKey(obj.name))
							_cachedLoadedObjects.Add(obj.name, obj);

						if (obj.name == name)
							find = obj;
					}
				}


				if (find == null)
				{
					if (!string.IsNullOrEmpty(fallback))
						return PullObject(fallback);
#if UNITY_EDITOR
					else
						Debug.LogError("GameObject with name " + name + " was not found in the lookup. Make sure the object is in the resources folder or in the Networking Manager \"Network Instantiates\" array in the inspector.");
#endif
				}

				return find;
			}
		}

		/// <summary>
		/// Get the latest list of players from the server
		/// </summary>
		/// <param name="callback">The method to call once the player list has been received</param>
		public void PollPlayerList(Action<List<NetworkingPlayer>> callback = null)
		{
			if (OwningNetWorker.IsServer)
			{
				if (callback != null)
				{
					List<NetworkingPlayer> tmp = new List<NetworkingPlayer>(OwningNetWorker.Players.ToArray());
					tmp.Insert(0, OwningNetWorker.Me);
					callback(tmp);
				}

				return;
			}

			pollPlayersCallback = callback;
			RPC("PollPlayers", NetworkReceivers.Server);
		}

		/// <summary>
		/// Set the player name for the current running client or server
		/// </summary>
		/// <param name="name">The name to be assigned</param>
		public void SetName(string newName)
		{
			RPC("SetPlayerName", NetworkReceivers.Server, newName);
		}

		[BRPC]
		private void SetPlayerName(string newName)
		{
			if (!OwningNetWorker.IsServer)
				return;

			if (CurrentRPCSender == null)
				OwningNetWorker.Me.SetName(newName);
			else
				CurrentRPCSender.SetName(newName);
		}

		[BRPC]
		private void InitializeObject(ulong startObjectId, int count)
		{
			if (!OwningNetWorker.IsServer)
				return;

			for (int i = 0; i < count; i++)
			{
				if (NetworkedBehaviors.ContainsKey(startObjectId + (ulong)i))
				{
					if (NetworkedBehaviors[startObjectId + (ulong)i] is NetworkedMonoBehavior)
						((NetworkedMonoBehavior)NetworkedBehaviors[startObjectId + (ulong)i]).AutoritativeSerialize();
				}
			}
		}

		[BRPC]
		private void PollPlayers()
		{
			playerPollData.Clear();

			if (!OwningNetWorker.IsServer)
				return;

			ObjectMapper.MapBytes(playerPollData, OwningNetWorker.Players.Count + 1);

			// Send the server first
			ObjectMapper.MapBytes(playerPollData, OwningNetWorker.Me.NetworkId, OwningNetWorker.Me.Name);

			foreach (NetworkingPlayer player in OwningNetWorker.Players)
				ObjectMapper.MapBytes(playerPollData, player.NetworkId, player.Name);

			Networking.WriteCustom(WriteCustomMapping.NetWORKING_MANAGER_POLL_PLAYERS, OwningNetWorker, playerPollData, OwningNetWorker.CurrentStreamOwner, true);
		}

		private void PollPlayersResponse(NetworkingPlayer sender, NetworkingStream stream)
		{
			int count = ObjectMapper.Map<int>(stream);
			List<NetworkingPlayer> playerList = new List<NetworkingPlayer>();

			for (int i = 0; i < count; i++)
				playerList.Add(new NetworkingPlayer(ObjectMapper.Map<ulong>(stream), string.Empty, string.Empty, ObjectMapper.Map<string>(stream)));

			if (pollPlayersCallback != null)
				pollPlayersCallback(playerList);

			OwningNetWorker.AssignPlayers(playerList);
		}

		[BRPC]
		private void UpdateServerTime(float time)
		{
			serverTime = time;
			lastTimeUpdate = Time.time;
		}

		public override void Disconnect()
		{
			base.Disconnect();

			ControllingSocket = null;

			Unity.UnityEventObject.onDestroy -= SkipResetOnDestroy;

#if !UNITY_WEBGL
			if (Threading.ThreadManagement.IsMainThread)
#endif
				Destroy(gameObject);
#if !UNITY_WEBGL
			else
			{
				// JM: make sure this is run on main thread
				Unity.MainThreadManager.Run(() => {
						Destroy(gameObject);
				});
			}
#endif

			instance = null;
		}

		private void PlayerLoadedLevel(NetworkingPlayer player, NetworkingStream stream)
		{
			int levelLoaded = ObjectMapper.Map<int>(stream);

			Unity.MainThreadManager.Run(() =>
			{
				// The level that was loaded is not the current level
#if UNITY_4_6 || UNITY_4_7
				if (levelLoaded != Application.loadedLevel)
#else
				if (levelLoaded != Unity.UnitySceneManager.GetCurrentSceneBuildIndex())
#endif
					return;

				if (clientLoadedLevel != null)
					clientLoadedLevel(player);

				if (allClientsLoaded == null)
					return;

				if (++currentClientsLoaded >= OwningNetWorker.Players.Count)
				{
					allClientsLoaded();
					currentClientsLoaded = 0;
				}
			});
		}

		private void OnLevelWasLoaded(int level)
		{
			CreateUnityEventObject();

			if (OwningNetWorker == null)
				loadLevelFireOnConnect = () => { TellServerLevelLoaded(level); };
			else
				Initialize(OwningNetWorker);
		}

		private void TellServerLevelLoaded(int level)
		{
			if (OwningNetWorker == null || OwningNetWorker.IsServer)
				return;

			loadLevelPing.Clear();
			ObjectMapper.MapBytes(loadLevelPing, level);
			Networking.WriteCustom(WriteCustomMapping.NetWORKING_MANAGER_PLAYER_LOADED_LEVEL, OwningNetWorker, loadLevelPing, true, NetworkReceivers.Server);
		}

		private void CreateUnityEventObject()
		{
#if !BARE_METAL
			new GameObject("UnityEventObject").AddComponent<Unity.UnityEventObject>();
#endif
		}

		private void SkipResetOnDestroy()
		{
			ResetForScene(new List<SimpleNetworkedMonoBehavior>(new SimpleNetworkedMonoBehavior[] { Instance }));
		}
	}
}