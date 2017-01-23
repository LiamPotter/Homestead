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

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeChangeOwner : NetworkedMonoBehavior
	{
		private void Update()
		{
			if (OwningNetWorker.IsServer)
			{
				if (Input.GetKeyDown(KeyCode.Alpha0))
					ChangeOwner(0);
				else if (Input.GetKeyDown(KeyCode.Alpha1))
					ChangeOwner(1);
				else if (Input.GetKeyDown(KeyCode.Alpha2))
					ChangeOwner(2);
			}
		}

		protected override void OwnerUpdate()
		{
			base.OwnerUpdate();

			if (Input.GetKeyDown(KeyCode.UpArrow))
				transform.position += Vector3.up;

			if (Input.GetKeyDown(KeyCode.DownArrow))
				transform.position += Vector3.down;
		}
	}
}