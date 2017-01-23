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



using BeardedManStudios.Network;
using UnityEngine;

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeExample_MakePlayer : MonoBehaviour
	{
		public GameObject objectToSpawn = null;

		private void Start()
		{
			if (NetworkingManager.Socket == null || NetworkingManager.Socket.Connected)
				Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered, PlayerSpawned);
			else
			{
				NetworkingManager.Instance.OwningNetWorker.connected += delegate()
				{
					Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered, PlayerSpawned);
				};
			}
		}

		private void PlayerSpawned(SimpleNetworkedMonoBehavior playerObject)
		{
			Debug.Log("The player object " + playerObject.name + " has spawned at " + 
				"X: " + playerObject.transform.position.x +
				"Y: " + playerObject.transform.position.y +
				"Z: " + playerObject.transform.position.z);
		}
	}
}