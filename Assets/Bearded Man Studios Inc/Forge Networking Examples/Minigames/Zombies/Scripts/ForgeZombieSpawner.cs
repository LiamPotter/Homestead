#if !BARE_METAL

using BeardedManStudios.Network;
using System.Collections.Generic;
using UnityEngine;

public class ForgeZombieSpawner : MonoBehaviour
{
	public GameObject Zombie;
	public GameObject Powerup;
	public Transform[] SpawnLocations;
	public Transform[] PowerupSpawnLocations;
	public int MaxZombies = 30;
	public float SpawnTimer = 30;

	public int MaxPowerups = 1;
	public float PowerupSpawnTimer = 5;

	private bool SpawnAllowed;
	private float _spawnTimespan = 0;
	private float _powerupSpawnTimespan = 0;
	private List<GameObject> _zombies = new List<GameObject>();
	private List<GameObject> _powerups = new List<GameObject>();

	private void Start()
	{
		if (!NetworkingManager.IsOnline)
		{
			SpawnAllowed = true;
			return;
		}

		Networking.PrimarySocket.disconnected += ExitGame;
		if (Networking.PrimarySocket.Connected)
			SpawnAllowed = true;
		else
		{
			Networking.PrimarySocket.connected += delegate()
			{
				SpawnAllowed = true;
			};
		}
	}

	// Update is called once per frame
	private void Update()
	{
		for (int i = _zombies.Count - 1; i >= 0; i--)
		{
			if (_zombies[i] == null)
				_zombies.Remove(_zombies[i]);
		}

		for (int i = _powerups.Count - 1; i >= 0; i--)
		{
			if (_powerups[i] == null)
				_powerups.Remove(_powerups[i]);
		}

		if (!NetworkingManager.IsOnline || (SpawnAllowed && NetworkingManager.Socket.IsServer))
		{
			if (_zombies.Count < MaxZombies)
			{
				_spawnTimespan -= Time.deltaTime;
				if (_spawnTimespan <= 0)
				{
					_spawnTimespan = SpawnTimer;
					Networking.Instantiate(Zombie, SpawnLocations[Random.Range(0, SpawnLocations.Length)].position, Quaternion.identity,
						NetworkReceivers.AllBuffered, ZombieSpawned);
				}
			}

			if (_powerups.Count < MaxPowerups)
			{
				_powerupSpawnTimespan -= Time.deltaTime;

				if (_powerupSpawnTimespan <= 0)
				{
					_powerupSpawnTimespan = PowerupSpawnTimer;

					Networking.Instantiate(Powerup, PowerupSpawnLocations[Random.Range(0, SpawnLocations.Length)].position,
						Quaternion.identity,
						NetworkReceivers.AllBuffered, PowerupSpawned);
				}
			}
		}
}

	private void ExitGame()
	{
		Networking.PrimarySocket.disconnected -= ExitGame;

		BeardedManStudios.Network.Unity.MainThreadManager.Run(() =>
		{
			Debug.Log("Quit game");
#if UNITY_4_6 || UNITY_4_7
            Application.LoadLevel("ForgeQuickStartMenu");
#else
			BeardedManStudios.Network.Unity.UnitySceneManager.LoadScene("ForgeQuickStartMenu");
#endif
		});
	}

	private void ZombieSpawned(SimpleNetworkedMonoBehavior zombie)
	{
		_zombies.Add(zombie.gameObject);
	}

	private void PowerupSpawned(SimpleNetworkedMonoBehavior powerup)
	{
		_powerups.Add(powerup.gameObject);
	}
}
#endif