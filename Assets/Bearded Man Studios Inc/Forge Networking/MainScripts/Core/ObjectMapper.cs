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



#if BMS_DEBUGGING
#define BMS_DEBUGGING_UNITY
#endif

using System;
using System.Text;
using UnityEngine;

namespace BeardedManStudios.Network
{
	public class ObjectMapper
	{
		private static int byteArrSize = 0;

		/// <summary>
		/// Map a type of object from a Networking stream to a object
		/// </summary>
		/// <param name="type">Type of object to map</param>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>Returns the mapped object</returns>
		public static object Map(Type type, NetworkingStream stream)
		{
			object obj = null;

			if (type == typeof(string))
				obj = MapString(stream);
			else if (type == typeof(Vector2))
				obj = MapVector2(stream);
			else if (type == typeof(Vector3))
				obj = MapVector3(stream);
			else if (type == typeof(Vector4) || type == typeof(Color) || type == typeof(Quaternion))
				obj = MapVector4(type, stream);
			else if (type == typeof(byte[]))
			{
				obj = MapByteArray(stream);
				byte[] tmp = new byte[byteArrSize];
				for (int i = 0; i < tmp.Length; i++) tmp[i] = ((byte[])obj)[i];
				obj = tmp;
			}
			else if (type == typeof(BMSByte))
				obj = MapBMSByte(stream);
			else if(type.IsEnum())
				obj = MapBasicType(Enum.GetUnderlyingType(type), stream);
			else
				obj = MapBasicType(type, stream);

			return obj;
		}

		/// <summary>
		/// Compares a type of object to the Networking Stream
		/// </summary>
		/// <typeparam name="T">Value type to get out of it</typeparam>
		/// <param name="stream">Stream to be used</param>
		/// <param name="o">Object being compared with</param>
		/// <returns>Returns the type of comparison passed</returns>
		public static bool Compare<T>(NetworkingStream stream, object o)
		{
			stream.StartPeek();
			object obj = null;
            var genericType = typeof (T);

            if (genericType == typeof(string))
                obj = MapString(stream);
            else if (genericType == typeof(Vector3))
				obj = MapVector3(stream);
            else if (genericType == typeof(Vector4) || genericType == typeof(Color) || genericType == typeof(Quaternion))
                obj = MapVector4(genericType, stream);
            else if (genericType == typeof(byte[]))
				obj = MapByteArray(stream);
            else if (genericType == typeof(BMSByte))
				obj = MapBMSByte(stream);
            else if (genericType.IsEnum())
                obj = MapBasicType(Enum.GetUnderlyingType(genericType), stream);
			else
                obj = MapBasicType(genericType, stream);

			stream.StopPeek();

			return Equals(o, obj);
		}

		/// <summary>
		/// Get a mapped value out of the Networking Stream
		/// </summary>
		/// <typeparam name="T">Value to get out of it</typeparam>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>Returns a mapped value from the Networking Stream</returns>
		public static T Map<T>(NetworkingStream stream)
		{
            object obj = null;
            var genericType = typeof(T);

            if (genericType == typeof(string))
			   obj = MapString(stream);
            else if (genericType == typeof(Vector2))
			   obj = MapVector2(stream);
            else if (genericType == typeof(Vector3))
			   obj = MapVector3(stream);
            else if (genericType == typeof(Vector4) || genericType == typeof(Color) || genericType == typeof(Quaternion))
               obj = MapVector4(genericType, stream);
            else if (genericType == typeof(byte[]))
			   obj = MapByteArray(stream);
            else if (genericType == typeof(BMSByte))
			   obj = MapBMSByte(stream);
            else if (genericType.IsEnum())
               obj = MapBasicType(Enum.GetUnderlyingType(genericType), stream);
			else
               obj = MapBasicType(genericType, stream);

			return (T)obj;
		}

		/// <summary>
		/// Get a mapped basic type of object from the Networking Stream
		/// </summary>
		/// <param name="type">Type of object to be mapped</param>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>Returns a mapped object of the given type</returns>
		public static object MapBasicType(Type type, NetworkingStream stream)
		{
			if (type == typeof(sbyte))
				return (sbyte)stream.Read(sizeof(sbyte))[0];
			else if (type == typeof(byte))
				return (byte)stream.Read(sizeof(byte))[0];
			else if (type == typeof(char))
				return (byte)stream.Read(sizeof(byte))[0];
			else if (type == typeof(short))
				return BitConverter.ToInt16(stream.Read(sizeof(short)), 0);
			else if (type == typeof(ushort))
				return BitConverter.ToUInt16(stream.Read(sizeof(short)), 0);
			else if (type == typeof(bool))
				return BitConverter.ToBoolean(stream.Read(sizeof(bool)), 0);
			else if (type == typeof(int))
				return BitConverter.ToInt32(stream.Read(sizeof(int)), 0);
			else if (type == typeof(uint))
				return BitConverter.ToUInt32(stream.Read(sizeof(int)), 0);
			else if (type == typeof(float))
				return BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			else if (type == typeof(long))
				return BitConverter.ToInt64(stream.Read(sizeof(long)), 0);
			else if (type == typeof(ulong))
				return BitConverter.ToUInt64(stream.Read(sizeof(long)), 0);
			else if (type == typeof(double))
				return BitConverter.ToDouble(stream.Read(sizeof(double)), 0);
			else
				throw new NetworkException(11, "The type " + type.ToString() + " is not allowed to be sent over the Network (yet)");
		}

		/// <summary>
		/// Map a string from the Networking Stream
		/// </summary>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>Returns a string out of the Networking Stream</returns>
		public static object MapString(NetworkingStream stream)
		{
			int length = BitConverter.ToInt32(stream.Read(sizeof(int)), 0);

			if (length <= 0)
				return string.Empty;

#if NetFX_CORE
			return Encoding.UTF8.GetString(stream.Read(length), 0, length);
#else
			if (length > stream.Bytes.Size - sizeof(int))
			{
				return string.Empty;
				//throw new NetworkException(12, "Attempted to read a string that doesn't exist");
			}

			return Encoding.UTF8.GetString(stream.Read(length), 0, length);
#endif
		}

		private static int size;
		private static float x, y, z, w;
		//private static byte[] readBytes = new byte[0];

		/// <summary>
		/// Get a Vector2 out of a Networking Stream
		/// </summary>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>A Vector2 out of the Networking Stream</returns>
		public static object MapVector2(NetworkingStream stream)
		{
			x = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			y = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			return new Vector2(x, y);
		}

		/// <summary>
		/// Get a Vector3 out of a Networking Stream
		/// </summary>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>A Vector3 out of the Networking Stream</returns>
		public static object MapVector3(NetworkingStream stream)
		{
			x = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			y = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			z = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);

			return new Vector3(x, y, z);
		}

		/// <summary>
		/// Get a Vector4 out of a Networking Stream
		/// </summary>
		/// <param name="type">Type of object to be mapped</param>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>A type of Vector4 (Vector4/Color/Quaternion) out of the Networking Stream</returns>
		public static object MapVector4(Type type, NetworkingStream stream)
		{
			x = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			y = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			z = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);
			w = BitConverter.ToSingle(stream.Read(sizeof(float)), 0);

			if (type == typeof(Vector4))
				return new Vector4(x, y, z, w);
			else if (type == typeof(Color))
				return new Color(x, y, z, w);
			else// if (type == typeof(Quaternion))
				return new Quaternion(x, y, z, w);
		}

		/// <summary>
		/// Get a byte array of a Networking Stream
		/// </summary>
		/// <param name="type">Type of object to be mapped</param>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>A byte array that was read from the Networking Stream</returns>
		public static object MapByteArray(NetworkingStream stream)
		{
			byteArrSize = Map<int>(stream);
			byte[] readBytes = stream.Read(byteArrSize);

			byte[] value = new byte[byteArrSize];
			for (int i = 0; i < value.Length; i++)
				value[i] = readBytes[i];

			return value;
		}

		/// <summary>
		/// Get a BMSByte out of a Networking Stream
		/// </summary>
		/// <param name="type">Type of object to be mapped</param>
		/// <param name="stream">Networking Stream to be used</param>
		/// <returns>A BMSByte that was read from the Networking Stream</returns>
		public static object MapBMSByte(NetworkingStream stream)
		{
			size = Map<int>(stream);
			return new BMSByte().Clone(stream.Read(size), size);
		}

		/// <summary>
		/// Get a byte[] out of a Networking Stream
		/// </summary>
		/// <param name="args">Arguments passed through to get mapped</param>
		/// <returns>A byte[] of the mapped arguments</returns>
		public static BMSByte MapBytes(BMSByte bytes, params object[] args)
		{
			foreach (object o in args)
			{
#if UNITY_EDITOR
				if (o == null)
					throw new NetworkException("You are trying to serialize a null object, null objects have no dimentions and can not be mapped across the Network");
#endif

				Type type = o.GetType();

				GetBytes(o, type, ref bytes);
			}

			return bytes;
		}

		/// <summary>
		/// Gets the bytes for the Instance of an Object and appends them to a <c>BMSByte</c>.
		/// </summary>
		/// <param name="o">The Instance of the Object.</param>
		/// <param name="type">The Type of the Object.</param>
		/// <param name="bytes"><c>BMSByte</c> to which the bytes should be added.</param>
		private static void GetBytes(object o, Type type, ref BMSByte bytes)
		{
			if (type == typeof(string))
			{
				var strBytes = Encoding.UTF8.GetBytes((string)o);
				// TODO:  Need to make custom string serialization to binary
				bytes.Append(BitConverter.GetBytes(strBytes.Length));

				if (strBytes.Length > 0)
					bytes.Append(strBytes);
			}
			else if (type == typeof(sbyte))
				bytes.BlockCopy<sbyte>(o, 1);
			else if (type == typeof(byte))
				bytes.BlockCopy<byte>(o, 1);
			else if (type == typeof(char))
				bytes.BlockCopy<char>(o, 1);
			else if (type == typeof(bool))
				bytes.Append(BitConverter.GetBytes((bool)o));
			else if (type == typeof(short))
				bytes.Append(BitConverter.GetBytes((short)o));
			else if (type == typeof(ushort))
				bytes.Append(BitConverter.GetBytes((ushort)o));
			else if (type == typeof(int))
				bytes.Append(BitConverter.GetBytes((int)o));
			else if (type == typeof(uint))
				bytes.Append(BitConverter.GetBytes((uint)o));
			else if (type == typeof(long))
				bytes.Append(BitConverter.GetBytes((long)o));
			else if (type == typeof(ulong))
				bytes.Append(BitConverter.GetBytes((ulong)o));
			else if (type == typeof(float))
				bytes.Append(BitConverter.GetBytes((float)o));
			else if (type == typeof(double))
				bytes.Append(BitConverter.GetBytes((double)o));
			else if (type == typeof(Vector2))
			{
				bytes.Append(BitConverter.GetBytes(((Vector2)o).x));
				bytes.Append(BitConverter.GetBytes(((Vector2)o).y));
			}
			else if (type == typeof(Vector3))
			{
				bytes.Append(BitConverter.GetBytes(((Vector3)o).x));
				bytes.Append(BitConverter.GetBytes(((Vector3)o).y));
				bytes.Append(BitConverter.GetBytes(((Vector3)o).z));
			}
			else if (type == typeof(Vector4))
			{
				bytes.Append(BitConverter.GetBytes(((Vector4)o).x));
				bytes.Append(BitConverter.GetBytes(((Vector4)o).y));
				bytes.Append(BitConverter.GetBytes(((Vector4)o).z));
				bytes.Append(BitConverter.GetBytes(((Vector4)o).w));
			}
			else if (type == typeof(Color))
			{
				bytes.Append(BitConverter.GetBytes(((Color)o).r));
				bytes.Append(BitConverter.GetBytes(((Color)o).g));
				bytes.Append(BitConverter.GetBytes(((Color)o).b));
				bytes.Append(BitConverter.GetBytes(((Color)o).a));
			}
			else if (type == typeof(Quaternion))
			{
				bytes.Append(BitConverter.GetBytes(((Quaternion)o).x));
				bytes.Append(BitConverter.GetBytes(((Quaternion)o).y));
				bytes.Append(BitConverter.GetBytes(((Quaternion)o).z));
				bytes.Append(BitConverter.GetBytes(((Quaternion)o).w));
			}
			else if (type == typeof(byte[]))
			{
				bytes.Append(BitConverter.GetBytes(((byte[])o).Length));
				bytes.Append((byte[])o);
			}
			else if (type == typeof(BMSByte))
			{
				bytes.Append(BitConverter.GetBytes(((BMSByte)o).Size));
				bytes.BlockCopy(((BMSByte)o).byteArr, ((BMSByte)o).StartIndex(), ((BMSByte)o).Size);
			}
			else if (type.IsEnum())
				GetBytes(o, Enum.GetUnderlyingType(type), ref bytes);
			else
				throw new NetworkException(11, "The type " + type.ToString() + " is not allowed to be sent over the Network (yet)");
		}
	}
}
