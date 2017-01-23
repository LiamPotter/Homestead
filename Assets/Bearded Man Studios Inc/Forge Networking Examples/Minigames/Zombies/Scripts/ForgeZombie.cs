using BeardedManStudios.Network;
using UnityEngine;

/// <summary>
/// This zombie is created by the server and is only controlled by the server
/// </summary>
public class ForgeZombie : NetworkedMonoBehavior
{
	[NetSync]
	public int Health = 100; // Zombie health

	private bool _dead = false;
	private Renderer _thisRenderer;
	public bool destroy;

	private void Awake()
	{
#if !BARE_METAL
		_thisRenderer = GetComponent<Renderer>();
#endif
	}

	private void Update()
	{
		if (destroy)
		{
			Networking.Destroy(this);
			return;
		}

		if (NetworkingManager.IsOnline && !OwningNetWorker.IsServer)
			return;

		// To check if we are dead
		if (_dead)
			return;

		if (Health <= 0) // Zombie died!
		{
			_dead = true;
			Networking.Destroy(this); // Destroy it! :)
		}
		else
		{
			ForgePlayer_Zombie closestPlayer = null;
			float dist = 100; // Min distance

			// Check for the closest player
			foreach (ForgePlayer_Zombie player in ForgePlayer_Zombie.ZombiePlayers)
			{
				float distance = Vector3.Distance(player.transform.position, transform.position);
				if (distance < dist)
				{
					closestPlayer = player;
					dist = distance;
				}
			}

			if (closestPlayer != null && dist > 2) // Move towards the closest player
				transform.position -= (transform.position - closestPlayer.transform.position) * Time.deltaTime;
		}
	}

	public void Damage(ForgeZombieBullet bullet)
	{
		if (bullet != null)
		{
			if (!NetworkingManager.IsOnline || OwningNetWorker.IsServer) // Only the server cares about the damage
				Health -= bullet.BulletDamage;

			if (Health < 35)
			{
				_thisRenderer.material.color = Color.red;
			}
		}
	}
}