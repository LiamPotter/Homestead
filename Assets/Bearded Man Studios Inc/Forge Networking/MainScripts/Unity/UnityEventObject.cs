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

namespace BeardedManStudios.Network.Unity
{
	public class UnityEventObject : MonoBehaviour
	{
		public static UnityEventObject Instance { get; private set; }

		public delegate void BasicCallback();
		public static event BasicCallback onDestroy
		{
			add
			{
				onDestroyInvoker += value;
			}
			remove
			{
				onDestroyInvoker -= value;
			}
		}
		static BasicCallback onDestroyInvoker;

		private bool skipCalls = false;

		private void Awake()
		{
			if (Instance != null)
			{
				skipCalls = true;
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (skipCalls)
				return;

			if (onDestroyInvoker != null)
				onDestroyInvoker();
		}

		public static void Cleanup()
		{
			onDestroyInvoker = null;
		}
	}
}