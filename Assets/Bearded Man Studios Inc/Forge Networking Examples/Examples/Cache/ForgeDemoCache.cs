using BeardedManStudios.Network;
using UnityEngine;

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeDemoCache : MonoBehaviour
	{
		public string dummyData = string.Empty;
		private string dummyCacheKey = "dummyData";

		private void Start()
		{
			if (NetworkingManager.Instance.OwningNetWorker.IsServer)
			{
				Cache.Set(dummyCacheKey, dummyData);
				Debug.Log(dummyData.Length);
				Debug.Log(dummyData);
			}
			else
			{
				Cache.Request<string>(dummyCacheKey, (object x) =>
				{
					string response = (string)x;
					Debug.Log(response.Length);
					Debug.Log(response);

					if (response == dummyData)
						Debug.Log("Contents are identical");
				});
			}
		}
	}
}