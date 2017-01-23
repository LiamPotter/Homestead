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
using System;
#if !UNITY_WEBGL
using System.Net.Sockets;
#endif
using UnityEngine;

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeExample_ErrorHandling : MonoBehaviour
	{
		private void Start()
		{
			Networking.PrimarySocket.error += PrimarySocket_error;
		}

		private void PrimarySocket_error(Exception exception)
		{
			if (exception is Exception)
				Debug.Log("It is a system exception");
			else if (exception is NetworkException)
				Debug.Log("This is a Forge Networking specific exception");
#if !NetFX_CORE && !UNITY_WEBGL
			else if (exception is SocketException)
				Debug.Log("This is somekind of socket exception, could be that the port is already in use?");
#endif
			else
				Debug.Log("What is this exception?");

			Debug.Log("We are now going to log the exception with Unity Debug.LogException");
			Debug.LogException(exception);
		}
	}
}