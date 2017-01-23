using BeardedManStudios.Network;
using UnityEngine;

public class ForgeZombiePowerup : NetworkedMonoBehavior 
{
	public void OnTriggerEnter(Collider other)
	{
		if (NetworkingManager.IsOnline && !NetworkingManager.Socket.IsServer)
			return;

		if (other.gameObject.name.Contains("CubePlayerGuy"))
		{
#if !BARE_METAL
			ForgePlayer_Zombie player = other.GetComponent<ForgePlayer_Zombie>();

			//A player hit this powerup!
			player.RPC("EnableRapidFire", NetworkReceivers.All);
			Debug.Log("Powerup Triggered for " + other.gameObject.name);

			Networking.Destroy(this);
#endif
		}
	}
}
