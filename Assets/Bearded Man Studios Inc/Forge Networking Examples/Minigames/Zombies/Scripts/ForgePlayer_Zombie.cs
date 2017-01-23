using BeardedManStudios.Network;
using System.Collections.Generic;
using UnityEngine;

public class ForgePlayer_Zombie : NetworkedMonoBehavior
{
#if !BARE_METAL
	public Transform Bullet;
#endif

	[NetSync]
	public int Health = 100; //Player health

	public int Damage = 30;

	[NetSync]
	public bool Shooting = false; //If the player is attacking

	public bool RapidFire = false;
	public float RapidFireTimespan = 3;
	private float _firingRate = 0.5f; //Local variable only the server controls
	private float _timespan = 0;

	public static List<ForgePlayer_Zombie> ZombiePlayers = new List<ForgePlayer_Zombie>(); 

#if !BARE_METAL
	//Debug font
	GUIStyle blackFont = new GUIStyle();
#endif

	private void Awake()
	{
#if !BARE_METAL
		blackFont.normal.textColor = Color.black;
#endif

		ZombiePlayers.Add(this);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			Networking.Disconnect();

		if (Shooting) //Local bullet shooting!
		{
			_timespan -= Time.deltaTime;
			if (_timespan <= 0)
			{
				_timespan = RapidFire ? _firingRate * 0.5f : _firingRate;

				float lifespan = 5;
				Ray gunRay = new Ray(transform.position, transform.forward);
#if !BARE_METAL
				ForgeZombie hitZombie = null;
#endif
				RaycastHit[] gunHit;
#if UNITY_EDITOR
				Debug.DrawRay(transform.position, transform.forward * 100, Color.green);
#endif

				gunHit = Physics.RaycastAll(gunRay, 100);
				if (gunHit != null && gunHit.Length > 0)
				{
					foreach (RaycastHit hit in gunHit)
					{
						if (hit.collider != null)
						{
							if (hit.collider.name.Contains("Zombie"))
							{
#if !BARE_METAL
								hitZombie = hit.collider.GetComponent<ForgeZombie>();
#endif
								//We hit a zombie! woot!
								float distance = Vector3.Distance(transform.position, hit.collider.transform.position);
								lifespan = distance * 0.02f;
								break;
							}
						}
					}
				}

#if !BARE_METAL
				//Shoot the bullet!
				Transform bullet = Instantiate(Bullet, transform.position + (transform.forward * 1.2f), transform.rotation) as Transform;

				if (hitZombie != null)
					bullet.GetComponent<ForgeZombieBullet>().Setup(hitZombie, lifespan);
#endif
			}
		}

		if (RapidFire)
		{
			RapidFireTimespan -= Time.deltaTime;
			if (RapidFireTimespan <= 0)
			{
				RapidFireTimespan = 3;
				RapidFire = false;
			}
		}
	}

	protected override void OwnerUpdate()
	{
		base.OwnerUpdate();

		//Follow controls
		//if (Input.GetKey(KeyCode.W))
		//	transform.position += transform.forward * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.S))
		//	transform.position += -transform.forward * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.A))
		//	transform.position += -transform.right * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.D))
		//	transform.position += transform.right * 5.0f * Time.deltaTime;

		//Non-Forward Controls
		if (Input.GetKey(KeyCode.W))
			transform.position += Vector3.forward * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.S))
			transform.position += -Vector3.forward * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.A))
			transform.position += -Vector3.right * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.D))
			transform.position += Vector3.right * 5.0f * Time.deltaTime;

		Vector3 mousePos = Input.mousePosition;
		Vector3 objectPos = Camera.main.WorldToScreenPoint(transform.position);
		mousePos.x = mousePos.x - objectPos.x;
		mousePos.y = mousePos.y - objectPos.y;
		float playerRotationAngle = Mathf.Atan2(mousePos.x, mousePos.y) * Mathf.Rad2Deg;

		transform.rotation = Quaternion.Euler(new Vector3(0, playerRotationAngle, 0));

		if (Input.GetMouseButton(0))
			Shooting = true;
		else
			Shooting = false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		ZombiePlayers.Remove(this);
	}

	[BRPC]
	public void EnableRapidFire()
	{
		RapidFireTimespan = 3;
		RapidFire = true;
	}

	public override void Disconnect()
	{
		base.Disconnect();

		if (IsOwner)
			ZombiePlayers.Clear();
	}

#if !BARE_METAL
	private void OnGUI()
	{
		if (!IsOwner)
			return;

#if UNITY_IOS || UNITY_IPHONE
		if (Networking.PrimarySocket == null)
			return;

		if (NetworkingManager.Instance == null)
			return;
#endif
//		if (GUILayout.Button("Disconnect", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(Screen.height * 0.2f)))
//		{
//			Networking.Disconnect();
//			return;
//		}

		if (Networking.PrimarySocket == null || !Networking.PrimarySocket.TrackBandwidth)
			return;

		// The server NetworkingManager object controls how fast the client's times are updated
		GUILayout.BeginArea(new Rect(Screen.width * 0.35f, Screen.height * 0.8f, Screen.width * 35f, Screen.height * 0.2f));
		GUILayout.Label("The current server time is: " + NetworkingManager.Instance.ServerTime, blackFont);
		GUILayout.Label("Bytes In: " + NetWorker.BandwidthIn, blackFont);
		GUILayout.Label("Bytes Out: " + NetWorker.BandwidthOut, blackFont);
		GUILayout.EndArea();
	}
#endif
}
