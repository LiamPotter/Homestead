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



using System;

#if BARE_METAL
using System.Runtime.Remoting;
using System.Runtime.Serialization;
#endif

namespace BeardedManStudios.Network
{
	/// <summary>
	/// The base exception class for Forge Networking
	/// </summary>
#if BARE_METAL
	[Serializable]
	public class NetworkException : RemotingException, ISerializable
#else
	public class NetworkException : Exception
#endif
	{
		/// <summary>
		/// Error code to return
		/// </summary>
		public ushort Code { get; private set; }

#if BARE_METAL
		private string _internalMessage;

		public NetworkException()
		{
			_internalMessage = string.Empty;
		}

		public NetworkException(string message)
		{
			_internalMessage = message;
		}

		public NetworkException(ushort code, string message)
		{
			_internalMessage = message;
			Code = code;
		}

		public NetworkException(SerializationInfo info, StreamingContext context)
		{
			_internalMessage = (string)info.GetValue("_internalMessage", typeof(string));
			Code = (ushort)info.GetValue("Code", typeof(ushort));
        }

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("_internalMessage", _internalMessage);
			info.AddValue("Code", Code);
		}

		// Returns the exception information.
		public override string Message
		{
			get
			{
				return "This is your custom remotable exception returning: \""
			 + _internalMessage
			 + "\"";
			}
		}
#else

		/// <summary>
		/// Constructor for a Networked exception
		/// </summary>
		/// <param name="message">Message of the exception</param>
		public NetworkException(string message) : base(message) { Code = 0; }

		/// <summary>
		/// Constructor for a Networked exception
		/// </summary>
		/// <param name="code">Error code of the exception</param>
		/// <param name="message">Message of the exception</param>
		public NetworkException(ushort code, string message) : base(message) { Code = code; }
#endif
	}
}