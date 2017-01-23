using System.Collections.Generic;
using System.Linq;

namespace BeardedManStudios.Network
{
	public class ClientManager
	{
		public delegate void RunActionWithClient(NetworkingPlayer player);
		public delegate bool RunActionWithClients(NetworkingPlayer player);

		public int Count { get { return clientSockets.Count; } }

		/// <summary>
		/// Dictionary of all the client Sockets on the CrossPlatformUDP (NetWorker)
		/// </summary>
		private Dictionary<string, NetworkingPlayer> clientSockets = new Dictionary<string, NetworkingPlayer>();

		private object clientSocketMutex = new object();

		/// <summary>
		/// Players conencted to this NetWorker(Socket)
		/// </summary>
		public List<NetworkingPlayer> Players { get { lock (clientSocketMutex) { return clientSockets.Values.ToList(); } } }

		/// <summary>
		/// Add a client to the CrossPlatformUDP (NetWorker)
		/// </summary>
		/// <param name="ip">Ip address of the player to add</param>
		/// <param name="player">Player we are adding</param>
		public bool AddClient(string ip, NetworkingPlayer player)
		{
			lock (clientSocketMutex)
			{
				if (!clientSockets.ContainsKey(ip))
				{
					clientSockets.Add(ip, player);
					return true;
				}
			}

			return false;
		}

		public bool RemoveClient(NetworkingPlayer player)
		{
			lock (clientSocketMutex)
			{
				if (player == null)
				{
					List<string> removePlayers = new List<string>();
					foreach (KeyValuePair<string, NetworkingPlayer> kv in clientSockets)
					{
						if (kv.Value == null)
							removePlayers.Add(kv.Key);
					}

					foreach (string ip in removePlayers)
						if (clientSockets.ContainsKey(ip))
							clientSockets.Remove(ip);
				}
				else if (clientSockets.ContainsKey(player.Ip))
				{
					clientSockets.Remove(player.Ip);
					return true;
				}
			}

			return false;
		}

		public void RemoveAllClients()
		{
			lock(clientSocketMutex)
			{
				clientSockets.Clear();
			}
		}

		/// <summary>
		/// Run an action on all of the clients, if the RunActionWithClients returns false then discontinue the iteration
		/// </summary>
		/// <param name="action">The action to be performed with the NetworkingPlayer</param>
		public void Iterate(RunActionWithClients action)
		{
			// Do not allow for null actions
			// TODO:  Throw exception
			if (action == null)
				return;

			lock (clientSocketMutex)
			{
				foreach (KeyValuePair<string, NetworkingPlayer> kv in clientSockets)
				{
					if (!action(kv.Value))
						break;
				}
			}
		}

		public bool RunActionOnPlayerEndpoint(string endpoint, RunActionWithClient action)
		{
			lock (clientSocketMutex)
			{
				if (clientSockets.ContainsKey(endpoint))
					action(clientSockets[endpoint]);
			}

			return false;
		}

		public NetworkingPlayer GetClientFromEndpoint(string endpoint)
		{
			lock (clientSocketMutex)
			{
				if (clientSockets.ContainsKey(endpoint))
					return clientSockets[endpoint];
			}

			return null;
		}

		public bool HasEndpoint(string endpoint)
		{
			lock (clientSocketMutex)
			{
				return clientSockets.ContainsKey(endpoint);
			}
		}
	}
}