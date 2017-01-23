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



using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BeardedManStudios.Network {
    /// <summary>
    /// A serializable class that can be sent across the Network. Only the supported serialization Types (seen in NetSync) can be serialized and sent
    /// in a ForgeTransportObject. A class must inherit from the ForgeTransportObject to function as one, then you use the Send() method to send the object.
    /// You must subscribe to the event described in the documentation for Send(), you can also specify who receives the object with a parameter in Send().
    /// 
    /// Below is an example class of a ForgeTransportObject
    /// <code>
    /// public class ForgeExample_ObjectToTransport : ForgeTransportObject
    ///{
    ///	public int apple = 0;
    ///   public float brent = 9.93f;
    ///   public string cat = "cat";
    /// public bool dog = false;
    ///
    /// public ForgeExample_ObjectToTransport()
	///	: base()
	///{
    ///
    ///}
    ///
    ///public override string ToString() {
    ///    return "apple: " + apple.ToString() + "\n" +
    ///       "brent: " + brent.ToString() + "\n" +
    ///        "cat: " + cat + "\n" +
    ///        "dog: " + dog.ToString();
    ///}
    ///}
    /// </code>
    /// </summary>
public class ForgeTransportObject
	{
		public delegate void TransportFinished(ForgeTransportObject target);

		public event TransportFinished transportFinished
		{
			add
			{
				transportFinishedInvoker += value;
			}
			remove
			{
				transportFinishedInvoker -= value;
			}
		}
		TransportFinished transportFinishedInvoker;

#if NetFX_CORE
		IEnumerable<FieldInfo> fields;
#else
		FieldInfo[] fields;
#endif

		private static ulong currentId = 0;
		private ulong id = 0;
		private object serializerMutex = new Object();
		private BMSByte serializer = new BMSByte();

		public static Dictionary<ulong, ForgeTransportObject> transportObjects = new Dictionary<ulong, ForgeTransportObject>();

		public ForgeTransportObject()
		{
			id = currentId++;
			Initialize();
			transportObjects.Add(id, this);
		}

		public static ForgeTransportObject Locate(ulong identifier)
		{
			if (transportObjects.ContainsKey(identifier))
				return transportObjects[identifier];

			return null;
		}

		private void Initialize()
		{
			if (Networking.PrimarySocket == null)
				return;

#if NetFX_CORE
			fields = this.GetType().GetRuntimeFields();
#else
			fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		}

        /// <summary>
        /// Sends the forge transport object
        /// </summary>
        /// <param name="receivers">who will receive the transport object</param>
        /// <param name="reliable">send the packet reliably/unreliably</param>
        /// <remarks>
        /// Serializes the forge transport object, then sends it to all clients specified by the receivers. 
        /// Subscribe a method to ForgeTransportObject.transportObject.transportFinished to decide what method should
        /// be executed when the object is received.
        /// </remarks>
		public void Send(NetworkReceivers receivers = NetworkReceivers.Others, bool reliable = true)
		{
			lock (serializerMutex)
			{
				serializer.Clear();
				ObjectMapper.MapBytes(serializer, id);

				foreach (FieldInfo field in fields)
					ObjectMapper.MapBytes(serializer, field.GetValue(this));

				Networking.WriteCustom(WriteCustomMapping.TRANSPORT_OBJECT, Networking.PrimarySocket, serializer, reliable, receivers);
			}
		}

		public void ReadFromNetwork(NetworkingStream stream)
		{
			Deserialize(stream);
		}

		private void Deserialize(NetworkingStream stream)
		{
			lock (serializerMutex)
			{
				foreach (FieldInfo field in fields)
					field.SetValue(this, ObjectMapper.Map(field.FieldType, stream));

				if (transportFinishedInvoker != null)
					transportFinishedInvoker(this);
			}
		}
	}
}