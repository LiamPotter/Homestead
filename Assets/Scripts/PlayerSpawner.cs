using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;

public class PlayerSpawner : MonoBehaviour {

    public bool SpawnAtStart;

    public GameObject objectToSpawn;
	void Start ()
    {

        if (SpawnAtStart)
        {
         
            if (Networking.PrimarySocket.Connected)
                Spawn();
            else Networking.PrimarySocket.connected += Spawn;
        }
    }
    public void Spawn()
    {
        if (Networking.PrimarySocket!=null) Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered);    
        //if (NetworkingManager.Socket == null || NetworkingManager.Socket.Connected)
        //    Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered, PlayerSpawned);
        //else
        //{
        //    NetworkingManager.Instance.OwningNetWorker.connected += delegate ()
        //    {
        //        Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered, PlayerSpawned);
        //    };
        //}
    }
    private void PlayerSpawned(SimpleNetworkedMonoBehavior playerObject)
    {
        Debug.Log("The player object " + playerObject.name + " has spawned at " +
            "X: " + playerObject.transform.position.x +
            "Y: " + playerObject.transform.position.y +
            "Z: " + playerObject.transform.position.z);
    }
}
