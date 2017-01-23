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



using UnityEngine;
using BeardedManStudios.Network;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
#if UNITY_EDITOR && !UNITY_WEBPLAYER
using System.Collections;
#endif

namespace BeardedManStudios.Network.Unity
{
	/// <summary>
	/// A script that can be used to automatically launch a set of clients and connect to a local server for testing purposes
	/// </summary>
	/// <remarks>
	/// Autostart can be attatched to allow for easy testing of Net code that uses forge. It allows you to auto launch several unity instances and auto connect them.
	/// This allows you to not only test the Net code, but test it for a given number of players. This may be very heavy on your computer for more complex games, as running more than ~3-4 instances will 
	/// be a lot of computation.
	/// Please see <A HREF="http://developers.forgepowered.com/Tutorials/MasterClassIntermediate/Auto-Launcher">this</A> page for information on how to use the autostart.
	/// </remarks>
	public class Autostart : MonoBehaviour
	{
		/// <summary>
		/// IP address to automatically connect to
		/// </summary>
		public string host = "127.0.0.1";                                                                       // IP address
		/// <summary>
		/// port to attempt to host the server on, and connect client instances to
		/// </summary>
		public int port = 15937;                                                                                // Port number
		/// <summary>
		/// The protocol type (either UDP reliable or TCP unreliable)
		/// </summary>
		public Networking.TransportationProtocolType protocolType = Networking.TransportationProtocolType.UDP;  // Communication protocol
		/// <summary>
		/// Max number of players that can connect to the server
		/// </summary>
		public int playerCount = 31;                                                                            // Maximum player count -- excluding this server
		/// <summary>
		/// Name of game scene to be loaded once instances launch
		/// </summary>
		public string sceneName = "Game";                                                                       // Scene to load
		/// <summary>
		/// Should proximity based updates be automatically enabled
		/// </summary>
		public bool proximityBasedUpdates = false;                                                              // Only send other player updates if they are within range
		/// <summary>
		/// If Autostart.proximityBasedUpdates is enabled, the distance for proximity updates to register
		/// </summary>
		public float proximityDistance = 5.0f;																	// The range for the players to be updated within

		private NetWorker socket = null;																		// The initial connection socket

		public string executePath = null;
		/// <summary>
		/// Number of client instances to be launched
		/// </summary>
		public int clientCount = 5;

		/// <summary>
		/// Determine if the current system is within the "WinRT" ecosystem
		/// </summary>
		private bool IsWinRT
		{
			get
			{
#if UNITY_4_6 || UNITY_4_7 || BARE_METAL
				return Application.platform == RuntimePlatform.MetroPlayerARM ||
					Application.platform == RuntimePlatform.MetroPlayerX86 ||
					Application.platform == RuntimePlatform.MetroPlayerX64;
#else
				return Application.platform == RuntimePlatform.WSAPlayerARM ||
					Application.platform == RuntimePlatform.WSAPlayerX86 ||
					Application.platform == RuntimePlatform.WSAPlayerX64;
#endif
			}
		}

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR && !UNITY_WEBPLAYER && !UNITY_WEBGL
            SceneManager.sceneLoaded += LevelLoaded;
#endif
        }
        private void OnDestroy()
        {
#if UNITY_EDITOR && !UNITY_WEBPLAYER && !UNITY_WEBGL
            SceneManager.sceneLoaded -= LevelLoaded;
#endif
        }
		public void Start()
		{
			// These devices have no reason to fire off a firewall check as they are not behind a local firewall
#if !UNITY_IPHONE && !UNITY_WP_8_1 && !UNITY_ANDROID
			// Check to make sure that the user is allowing this connection through the local OS firewall
			Networking.InitializeFirewallCheck((ushort)port);
#endif

#if UNITY_EDITOR
			StartServer();
#else
			StartClient();
#endif
		}

		/// <summary>
		/// This method is called when the host server button is clicked
		/// </summary>
		public void StartServer()
		{
			// Create a host connection
			socket = Networking.Host((ushort)port, protocolType, playerCount, IsWinRT);
			Go();
		}

		public void StartClient()
		{
			socket = Networking.Connect(host, (ushort)port, protocolType, IsWinRT);
			Go();
		}

		private void RemoveSocketReference()
		{
			socket = null;
		}

		private void Go()
		{
			Networking.NetworkReset += RemoveSocketReference;

			if (proximityBasedUpdates)
				socket.MakeProximityBased(proximityDistance);

			socket.serverDisconnected += delegate(string reason)
			{
				MainThreadManager.Run(() =>
				{
					Debug.Log("The server kicked you for reason: " + reason);
					Application.Quit();
#if UNITY_EDITOR
					UnityEditor.EditorApplication.isPlaying = false;
#endif
				});
			};

			if (socket.Connected)
				MainThreadManager.Run(LoadScene);
			else
				socket.connected += LoadScene;
		}

		private void LoadScene()
		{
			Networking.SetPrimarySocket(socket);
			#if UNITY_4_6 || UNITY_4_7
			Application.LoadLevel(sceneName);
			#else
			UnitySceneManager.LoadScene(sceneName);
			#endif
		}

#if UNITY_EDITOR && !UNITY_WEBPLAYER && !UNITY_WEBGL
		private void LevelLoaded(Scene scene,LoadSceneMode mode)
		{
            if (string.IsNullOrEmpty(executePath) || !System.IO.File.Exists(executePath))
                return;

			StartCoroutine(LoadClients());
		}

		private IEnumerator LoadClients()
		{
			for (int i = 0; i < clientCount; i++)
			{
				System.Diagnostics.Process.Start(executePath, "-screen-fullscreen 0 -screen-width 640 -screen-height 480");
				yield return new WaitForSeconds(1.5f);
			}
		}
#endif
	}
}
