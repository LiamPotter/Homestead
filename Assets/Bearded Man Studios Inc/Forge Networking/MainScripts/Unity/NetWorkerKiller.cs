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



using System.Collections.Generic;
using UnityEngine;

namespace BeardedManStudios.Network.Unity
{
	/// <summary>
	/// This class is responsible for doing any late cleanup of threads and Networked objects
	/// </summary>
	public class NetWorkerKiller : MonoBehaviour
	{
		private static NetWorkerKiller instance = null;

		/// <summary>
		/// This will create the Networker Killer if it hasn't been created already
		/// </summary>
		public static void Create()
		{
			if (!ReferenceEquals(instance, null))
				return;

#if !UNITY_WEBGL
			if (Threading.ThreadManagement.IsMainThread)
			{
#endif
				instance = new GameObject("NetWorker Authority").AddComponent<NetWorkerKiller>();
				DontDestroyOnLoad(instance.gameObject);
#if !UNITY_WEBGL
			}
			else
			{
				MainThreadManager.Run(() =>
				{
					instance = new GameObject("NetWorker Authority").AddComponent<NetWorkerKiller>();
					DontDestroyOnLoad(instance.gameObject);
				});
			}
#endif
		}

		/// <summary>
		/// Get the instance of the NetWorkerKiller
		/// </summary>
		public static NetWorkerKiller Instance
		{
			get
			{
				if (ReferenceEquals(instance, null))
					Create();

				return instance;
			}
			private set
			{
				instance = value;
			}
		}

		/// <summary>
		/// Get a list of all the NetWorkers
		/// </summary>
		public static List<NetWorker> NetWorkers { get; private set; }

		/// <summary>
		/// Add a NetWorker to this list
		/// </summary>
		/// <param name="NetWorker"></param>
		public static void AddNetWorker(NetWorker NetWorker)
		{
			if (ReferenceEquals(Instance, null) || NetWorkers == null)
				NetWorkers = new List<NetWorker>();

			if (!NetWorkers.Contains(NetWorker))
				NetWorkers.Add(NetWorker);
		}

		/// <summary>
		/// Clean all the Sockets and connections
		/// </summary>
		private void OnApplicationQuit()
		{
			if (NetWorkers == null || NetWorkers.Count == 0)
				return;

			Networking.Disconnect();

			UnityEventObject.Cleanup();
#if !UNITY_WEBGL
			Threading.Task.KillAll();
#endif

			NetWorkers.Clear();
			NetWorkers = null;
			instance = null;
		}
	}
}