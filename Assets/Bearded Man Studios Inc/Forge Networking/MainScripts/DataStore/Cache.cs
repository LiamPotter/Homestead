using System;
using System.Collections.Generic;
using UnityEngine;

#if NetFX_CORE
using System.Threading.Tasks;

#else
#endif

namespace BeardedManStudios.Network
{
	/// <summary>
	/// The main class for managing and communicating data from the server cache
	/// </summary>
    /// <remarks>
    /// Cache is used to store any arbitrary data for your game, you can deposit supported data types to the server with a key, then access them again
    /// on any client by using the key to access the specific data stored. Cache.Set() and Cache.Request() being the two primary ways to use Cache.
    /// </remarks>
	public static class Cache
	{
		// Default expiry datetime for a cached object.
		readonly static DateTime maxDateTime = DateTime.MaxValue;

		/// <summary>
		/// Determines if the chache has been initialized yet or not
		/// </summary>
		private static bool initialized = false;

		/// <summary>
		/// The memory cache for the data
		/// </summary>
		static Dictionary<string, CachedObject> memory = new Dictionary<string, CachedObject>();

		/// <summary>
		/// The main socket for communicating the cache back and forth
		/// </summary>
		public static NetWorker Socket { get { return Networking.PrimarySocket; } }

		// TODO:  Possibly make this global
		/// <summary>
		/// The set of types that are allowed and a byte mapping to them
		/// </summary>
		private static Dictionary<byte, Type> typeMap = new Dictionary<byte, Type>() {
			{ 0, typeof(byte) },
			{ 1, typeof(char) },
			{ 2, typeof(short) },
			{ 3, typeof(ushort) },
			{ 4, typeof(int) },
			{ 5, typeof(uint) },
			{ 6, typeof(long) },
			{ 7, typeof(ulong) },
			{ 8, typeof(bool) },
			{ 9, typeof(float) },
			{ 10, typeof(double) },
			{ 11, typeof(string) },
			{ 12, typeof(Vector2) },
			{ 13, typeof(Vector3) },
			{ 14, typeof(Vector4) },
			{ 15, typeof(Quaternion) },
			{ 16, typeof(Color) }
		};

		/// <summary>
		/// The current id that the callback stack is on
		/// </summary>
		private static int responseHookIncrementer = 0;

		/// <summary>
		/// The main callback stack for when requesting data
		/// </summary>
		private static Dictionary<int, Action<object>> responseHooks = new Dictionary<int, Action<object>>();

		private static void CheckSetup(bool resetOnDisconnect = true)
		{
			RemoveExpiredObjects();

			if (initialized)
				return;

			initialized = true;

			Socket.AddCustomDataReadEvent(WriteCustomMapping.CACHE_READ_SERVER, NetworkReadServer);
			Socket.AddCustomDataReadEvent(WriteCustomMapping.CACHE_READ_CLIENT, NetworkReadClient);

			if (resetOnDisconnect)
			{
				// JM: reset variables in case app disconnects and starts up new session
				Socket.disconnected += DisconnectReset;
			}
		}

		static void RemoveExpiredObjects()
		{
			foreach (KeyValuePair<string, CachedObject> entry in memory)
				if (entry.Value.IsExpired())
					memory.Remove(entry.Key);
		}

		private static void DisconnectReset()
		{
			Socket.disconnected -= DisconnectReset;

			initialized = false;
			memory = new Dictionary<string, CachedObject>();
			responseHookIncrementer = 0;
			responseHooks = new Dictionary<int, Action<object>>();
		}

		/// <summary>
		/// Called when the Network as interpreted that a cache message has been sent from the client
		/// </summary>
		/// <param name="player">The player that requested data from the cache</param>
		/// <param name="stream">The data that was received</param>
		public static void NetworkReadServer(NetworkingPlayer player, NetworkingStream stream)
		{
			byte type = ObjectMapper.Map<byte>(stream);
			int responseHookId = ObjectMapper.Map<int>(stream);
			string key = ObjectMapper.Map<string>(stream);

			object obj = Get(key);

			// TODO:  Let the client know it is null
			if (obj == null)
				return;

			BMSByte data = new BMSByte();
			ObjectMapper.MapBytes(data, type, responseHookId, obj);

			Networking.WriteCustom(WriteCustomMapping.CACHE_READ_CLIENT, Socket, data, player, true);
		}

		/// <summary>
		/// Called when the Network as interpreted that a cache message has been sent from the server
		/// </summary>
		/// <param name="player">The server</param>
		/// <param name="stream">The data that was received</param>
		private static void NetworkReadClient(NetworkingPlayer player, NetworkingStream stream)
		{
			byte type = ObjectMapper.Map<byte>(stream);
			int responseHookId = ObjectMapper.Map<int>(stream);

			object obj = null;

			if (typeMap[type] == typeof(Vector2))
				obj = ObjectMapper.Map<Vector2>(stream);
			else if (typeMap[type] == typeof(Vector3))
				obj = ObjectMapper.Map<Vector3>(stream);
			else if (typeMap[type] == typeof(Vector4))
				obj = ObjectMapper.Map<Vector4>(stream);
			else if (typeMap[type] == typeof(Color))
				obj = ObjectMapper.Map<Color>(stream);
			else if (typeMap[type] == typeof(Quaternion))
				obj = ObjectMapper.Map<Quaternion>(stream);
			else if (typeMap[type] == typeof(string))
				obj = ObjectMapper.Map<string>(stream);
			else
				obj = ObjectMapper.Map(typeMap[type], stream);

			if (responseHooks.ContainsKey(responseHookId))
			{
				responseHooks[responseHookId](obj);
				responseHooks.Remove(responseHookId);
			}
		}

		/// <summary>
		/// Get an object from cache
		/// </summary>
		/// <typeparam name="T">The type of object to store</typeparam>
		/// <param name="key">The name variable used for storing the desired object</param>
		/// <returns>Return object from key otherwise return the default value of the type or null</returns>
		private static T Get<T>(string key)
		{
			CheckSetup();

			if (!Socket.IsServer)
				return default(T);

			if (!memory.ContainsKey(key))
				return default(T);

			if (memory[key] is T)
				return (T)memory[key].Value;

			return default(T);
		}

		/// <summary>
		/// Used on the server to get an object at a given key from cache
		/// </summary>
		/// <param name="key">The key to be used in the dictionary lookup</param>
		/// <returns>The object at the given key in cache otherwise null</returns>
		private static object Get(string key)
		{
			CheckSetup();

			if (!Socket.IsServer)
				return null;

			if (memory.ContainsKey(key))
				return memory[key].Value;

			return null;
		}

		/// <summary>
		/// Get an object from cache
		/// </summary>
		/// <param name="key">The name variable used for storing the desired object</param>
		/// <returns>The string data at the desired key or null</returns>
        /// <remarks>
        /// Allows a client (or the server) to get a value from the Cache, the value is read directly from the server.
        /// A callback must be specified, this is because the code has to be executed after a moment when the response from the server
        /// is received. Request can be done like this:
        /// <code>
        /// void getServerDescription(){
        /// Cache.Request<string>("server_description", delegate (object response){
        ///     Debug.Log(((string) response));
        /// });
        /// }
        /// </code>
        /// The Cache only supports Forge's supported data Types, you can find a list of supported data Types in the NetSync documentation...
        /// </remarks>
		public static void Request<T>(string key, Action<object> callback)
		{
			CheckSetup();

			if (callback == null)
				throw new NetworkException("A callback is needed when requesting data from the server");

			if (Socket.IsServer)
			{
				callback(Get<T>(key));
				return;
			}

			responseHooks.Add(responseHookIncrementer, callback);

			BMSByte data = new BMSByte();
			byte targetType = byte.MaxValue;

			foreach (KeyValuePair<byte, Type> kv in typeMap)
			{
				if (typeof(T) == kv.Value)
				{
					targetType = kv.Key;
					break;
				}
			}

			if (targetType == byte.MaxValue)
				throw new NetworkException("Invalid type specified");

			ObjectMapper.MapBytes(data, targetType, responseHookIncrementer, key);

			Networking.WriteCustom(WriteCustomMapping.CACHE_READ_SERVER, Socket, data, true, NetworkReceivers.Server);
			responseHookIncrementer++;
		}

		/// <summary>
		/// Inserts a NEW key/value into cache
		/// </summary>
		/// <typeparam name="T">The serializable type of object</typeparam>
		/// <param name="key">The name variable used for storing the specified object</param>
		/// <param name="value">The object that is to be stored into cache</param>
		/// <returns>True if successful insert or False if the key already exists</returns>
		public static bool Insert<T>(string key, T value)
		{
			return Insert(key, value, maxDateTime);
		}

		/// <summary>
		/// Inserts a NEW key/value into cache
		/// </summary>
		/// <typeparam name="T">The serializable type of object</typeparam>
		/// <param name="key">The name variable used for storing the specified object</param>
		/// <param name="value">The object that is to be stored into cache</param>
		/// <param name="expireAt">The DateTime defining when the cached object should expire</param>
		/// <returns>True if successful insert or False if the key already exists</returns>
		public static bool Insert<T>(string key, T value, DateTime expireAt)
		{
			CheckSetup();

			if (!Socket.IsServer)
				throw new NetworkException("Inserting cache values is not yet supported for clients!");

			if (!memory.ContainsKey(key))
				return false;

			memory.Add(key, new CachedObject(value, expireAt));

			return true;
		}

		/// <summary>
		/// Inserts a new key/value or updates a key's value in cache
		/// </summary>
		/// <typeparam name="T">The serializable type of object</typeparam>
		/// <param name="key">The name variable used for storing the specified object</param>
		/// <param name="value">The object that is to be stored into cache</param>
        /// <remarks>
        /// This inputs a value into the cache, this can only be called on the server, you can only input Types forge supports (See NetSync for supported Types).
        /// </remarks>
		public static void Set<T>(string key, T value)
		{
			Set(key, value, maxDateTime);
		}

		/// <summary>
		/// Inserts a new key/value or updates a key's value in cache
		/// </summary>
		/// <typeparam name="T">The serializable type of object</typeparam>
		/// <param name="key">The name variable used for storing the specified object</param>
		/// <param name="value">The object that is to be stored into cache</param>
		/// <param name="expireAt">The DateTime defining when the cached object should expire</param>
		public static void Set<T>(string key, T value, DateTime expireAt)
		{
			CheckSetup();

			if (!Socket.IsServer)
				throw new NetworkException("Setting cache values is not yet supported for clients!");

			var cachedObject = new CachedObject(value, expireAt);

			if (!memory.ContainsKey(key))
				memory.Add(key, cachedObject);
			else
				memory[key] = cachedObject;
		}

		/// <summary>
		/// CachedObject class.
		/// </summary>
		public class CachedObject
		{
			public object Value { get; private set; }

			public DateTime ExpireAt { get; private set; }

			public CachedObject(object value, DateTime expireAt)
			{
				Value = value;
				ExpireAt = expireAt;
			}

			public bool IsExpired()
			{
				return DateTime.Now >= ExpireAt;
			}
		}
	}
}
