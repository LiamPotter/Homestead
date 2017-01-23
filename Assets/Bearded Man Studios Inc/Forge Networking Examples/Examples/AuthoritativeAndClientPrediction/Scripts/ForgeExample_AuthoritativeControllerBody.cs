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

public class ForgeExample_AuthoritativeControllerBody : NetworkedMonoBehavior
{
	public float speed = 5.0f;
	public float horizontal = 0.0f;
	public float vertical = 0.0f;

	public ForgeExample_AuthoritativeControllerFloats inputController = null;

    //We leave it as a regular update because on the server we may not own this object
	private void Update()
	{
		horizontal = inputController.horizontal * speed * Time.deltaTime;
		vertical = inputController.vertical * speed * Time.deltaTime;

		transform.position += new Vector3(horizontal, vertical, 0);
	}
}