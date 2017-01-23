#if !NetFX_CORE && !UNITY_IPHONE && !UNITY_ANDROID && !UNITY_WEBGL

using BeardedManStudios.Threading;
using System;
using System.Reflection;
using UnityEngine;

namespace BeardedManStudios.Network.Unit
{
	public class InterceptorTest : IBRPCIntercept
	{
		private bool Validate(string name)
		{
			if (name == "PassRPC")
				return true;
			else
				return false;
		}

		public bool ValidateRPC(MethodInfo method)
		{
			return Validate(method.Name);
		}

		public bool ValidateRPC(NetworkingStreamRPC method)
		{
			return Validate(method.MethodName);
		}
	}

	public class ForgeUnitException : NetworkException
	{
		public ForgeUnitException(string message) : base(message) { }
		public ForgeUnitException(ushort code, string message) : base(code, message) { }
	}

	public class UnitRPC : SimpleNetworkedMonoBehavior
	{
		private Task NetworkCallTask = null;

		private sbyte tSbyte = -9;
		private byte tByte = 9;
		private short tShort = -9;
		private ushort tUshort = 9;
		private int tInt = -9;
		private uint tUint = 9;
		private long tLong = -9;
		private ulong tUlong = 9;
		private char tChar = 'B';
		private bool tBool = true;
		private float tFloat = 9.93f;
		private double tDouble = 9.93;
		private string tString = "Forge Networking!";
		private Vector2 tVector2 = new Vector2(9.93f, 3.39f);
		private Vector3 tVector3 = new Vector3(9.93f, 3.39f, 1.23f);
		private Vector4 tVector4 = new Vector4(9.93f, 3.39f, 1.23f, 2.24f);
		private Quaternion tQuaternion = new Quaternion(9.93f, 3.39f, 1.23f, 2.24f);
		private Color tColor = new Color(0.25f, 0.35f, 0.56f, 0.93f);
		private byte[] tByteArray = new byte[] { 233, 3, 64, 128 };

		private void Success() { Debug.Log("SUCCESS: " + new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name + " called successfully"); }
		private void Success(string message) { Debug.Log("SUCCESS: " + message); }
		private void Failure(string message)
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif

#if !BARE_METAL
			GetComponent<Camera>().backgroundColor = Color.red;
#endif

			throw new ForgeUnitException("ERROR: " + message);
		}

		private void AllSuccess()
		{
#if !BARE_METAL
			GetComponent<Camera>().backgroundColor = Color.green;
#endif
		}

		protected override void NetworkStart()
		{
			base.NetworkStart();

			Success("NetworkStart called");

			if (OwningNetWorker.IsServer && OwningNetWorker.Players.Count > 0)
				return;

			NetworkCall();
			RPC("PrivateMethod");
		}

		private void NetworkCall(Action overrideCall = null)
		{
			if (overrideCall == null)
				NetworkCallTask = Task.Run(CallFailed);
			else
				NetworkCallTask = Task.Run(overrideCall);
		}

		private void KillNetworkCall(Action nextCall = null, Action overrideCall = null)
		{
			if (OwningNetWorker.IsServer && OwningNetWorker.Players.Count > 0)
				return;

			NetworkCallTask.Kill();

			if (nextCall != null)
			{
				NetworkCall(overrideCall);
				nextCall();
			}
		}

		private void CallFailed()
		{
			System.Threading.Thread.Sleep(1000);
			throw new ForgeUnitException("Invoked call failed");
		}

		private void Update()
		{
			if (!IsSetup)
				Failure("The object is not setup before Update is called");
		}

		[BRPC]
		private void PrivateMethod()
		{
			Success();
			KillNetworkCall(() => { RPC("ProtectedMethod"); });
		}

		[BRPC]
		protected void ProtectedMethod()
		{
			Success();
			KillNetworkCall(() => { RPC("PublicMethod"); });
		}

		[BRPC]
		public void PublicMethod()
		{
			Success();
			KillNetworkCall(() => { RPC("SByteRPC", tSbyte); });
		}

		[BRPC]
		private void SByteRPC(sbyte x)
		{
			if (x != tSbyte)
				Failure("The value of sbyte is " + x + " and not " + tSbyte);

			Success();
			KillNetworkCall(() => { RPC("ByteRPC", tByte); });
		}

		[BRPC]
		private void ByteRPC(byte x)
		{
			if (x != tByte)
				Failure("The value of byte is " + x + " and not " + tByte);

			Success();
			KillNetworkCall(() => { RPC("CharRPC", tChar); });
		}

		[BRPC]
		private void CharRPC(char x)
		{
			if (x != tChar)
				Failure("The value of char is " + x + " and not '" + tChar + "'");

			Success();
			KillNetworkCall(() => { RPC("BoolRPC", tBool); });
		}

		[BRPC]
		private void BoolRPC(bool x)
		{
			if (x != tBool)
				Failure("The value of bool is " + x + " and not " + tBool);

			Success();
			KillNetworkCall(() => { RPC("ShortRPC", tShort); });
		}

		[BRPC]
		private void ShortRPC(short x)
		{
			if (x != tShort)
				Failure("The value of short is " + x + " and not " + tShort);

			Success();
			KillNetworkCall(() => { RPC("UShortRPC", tUshort); });
		}

		[BRPC]
		private void UShortRPC(ushort x)
		{
			if (x != tUshort)
				Failure("The value of ushort is " + x + " and not " + tUshort);

			Success();
			KillNetworkCall(() => { RPC("IntRPC", tInt); });
		}

		[BRPC]
		private void IntRPC(int x)
		{
			if (x != tInt)
				Failure("The value of int is " + x + " and not " + tInt);

			Success();
			KillNetworkCall(() => { RPC("UIntRPC", tUint); });
		}

		[BRPC]
		private void UIntRPC(uint x)
		{
			if (x != tUint)
				Failure("The value of uint is " + x + " and not " + tUint);

			Success();
			KillNetworkCall(() => { RPC("LongRPC", tLong); });
		}

		[BRPC]
		private void LongRPC(long x)
		{
			if (x != tLong)
				Failure("The value of long is " + x + " and not " + tLong);

			Success();
			KillNetworkCall(() => { RPC("ULongRPC", tUlong); });
		}

		[BRPC]
		private void ULongRPC(ulong x)
		{
			if (x != tUlong)
				Failure("The value of ulong is " + x + " and not " + tUlong);

			Success();
			KillNetworkCall(() => { RPC("FloatRPC", tFloat); });
		}

		[BRPC]
		private void FloatRPC(float x)
		{
			if (x != tFloat)
				Failure("The value of float is " + x + " and not " + tFloat);

			Success();
			KillNetworkCall(() => { RPC("DoubleRPC", tDouble); });
		}

		[BRPC]
		private void DoubleRPC(double x)
		{
			if (x != tDouble)
				Failure("The value of double is " + x + " and not " + tDouble);

			Success();
			KillNetworkCall(() => { RPC("StringRPC", tString); });
		}

		[BRPC]
		private void StringRPC(string x)
		{
			if (x != tString)
				Failure("The value of string is \"" + x + "\" and not \"" + tString + "\"");

			Success();
			KillNetworkCall(() => { RPC("Vector2RPC", tVector2); });
		}

		[BRPC]
		private void Vector2RPC(Vector2 x)
		{
			if (x != tVector2)
				Failure("The value of Vector2 is " + x.x + ", " + x.y + " and not " + tVector2.x + ", " + tVector2.y);

			Success();
			KillNetworkCall(() => { RPC("Vector3RPC", tVector3); });
		}

		[BRPC]
		private void Vector3RPC(Vector3 x)
		{
			if (x != tVector3)
				Failure("The value of Vector3 is " + x.x + ", " + x.y + ", " + x.z + " and not " + tVector3.x + ", " + tVector3.y + ", " + tVector3.z);

			Success();
			KillNetworkCall(() => { RPC("Vector4RPC", tVector4); });
		}

		[BRPC]
		private void Vector4RPC(Vector4 x)
		{
			if (x != tVector4)
				Failure("The value of Vector4 is " + x.x + ", " + x.y + ", " + x.z + ", " + x.w + " and not " + tVector4.x + ", " + tVector4.y + ", " + tVector4.z + ", " + tVector4.w);

			Success();
			KillNetworkCall(() => { RPC("QuaternionRPC", tQuaternion); });
		}

		[BRPC]
		private void QuaternionRPC(Quaternion x)
		{
			if (x != tQuaternion)
				Failure("The value of Quaternion is " + x.x + ", " + x.y + ", " + x.z + ", " + x.w + " and not " + tQuaternion.x + ", " + tQuaternion.y + ", " + tQuaternion.z + ", " + tQuaternion.w);

			Success();
			KillNetworkCall(() => { RPC("ColorRPC", tColor); });
		}

		[BRPC]
		private void ColorRPC(Color x)
		{
			if (x != tColor)
				Failure("The value of Color is " + x.r + ", " + x.g + ", " + x.b + ", " + x.a + " and not " + tColor.r + ", " + tColor.g + ", " + tColor.b + ", " + tColor.a);

			Success();
			KillNetworkCall(() => { RPC("ByteArrayRPC", tByteArray); });
		}

		[BRPC]
		private void ByteArrayRPC(byte[] x)
		{
			if (x[0] != tByteArray[0] && x[1] != tByteArray[1] && x[2] != tByteArray[2] && x[3] != tByteArray[3])
				Failure("The value of byte[] is " + x[0] + ", " + x[1] + ", " + x[2] + ", " + x[3] + " and not " + tByteArray[0] + ", " + tByteArray[1] + ", " + tByteArray[2] + ", " + tByteArray[3]);

			Success();
			KillNetworkCall(() =>
			{
				RPC("AllTypes",
					tInt, tByte, tShort, tUshort, tSbyte, tByteArray, tLong, tUlong,
					tChar, tBool, tFloat, tDouble, tString, tVector2, tVector3,
					tVector4, tQuaternion, tColor, tUint
				);
			});
		}

		[BRPC]
		private void AllTypes(int a, byte b, short c, ushort d, sbyte e, byte[] f, long g, ulong h, char i, bool j, float k, double l, string m, Vector2 n, Vector3 o, Vector4 p, Quaternion q, Color r, uint s)
		{
			if (a != tInt)
				Failure("The value of int is " + a + " and not " + tInt);
			if (b != tByte)
				Failure("The value of byte is " + b + " and not " + tByte);
			if (c != tShort)
				Failure("The value of short is " + c + " and not " + tShort);
			if (d != tUshort)
				Failure("The value of ushort is " + d + " and not " + tUshort);
			if (e != tSbyte)
				Failure("The value of sbyte is " + e + " and not " + tSbyte);
			if (f[0] != tByteArray[0] && f[1] != tByteArray[1] && f[2] != tByteArray[2] && f[3] != tByteArray[3])
				Failure("The value of byte[] is " + f[0] + ", " + f[1] + ", " + f[2] + ", " + f[3] + " and not " + tByteArray[0] + ", " + tByteArray[1] + ", " + tByteArray[2] + ", " + tByteArray[3]);
			if (g != tLong)
				Failure("The value of long is " + g + " and not " + tLong);
			if (h != tUlong)
				Failure("The value of ulong is " + h + " and not " + tUlong);
			if (i != tChar)
				Failure("The value of char is " + i + " and not " + tChar);
			if (j != tBool)
				Failure("The value of bool is " + j + " and not " + tBool);
			if (k != tFloat)
				Failure("The value of float is " + k + " and not " + tFloat);
			if (l != tDouble)
				Failure("The value of double is " + l + " and not " + tDouble);
			if (m != tString)
				Failure("The value of string is " + m + " and not " + tString);
			if (n != tVector2)
				Failure("The value of Vector2 is " + n.x + ", " + n.y + " and not " + tVector2.x + ", " + tVector2.y);
			if (o != tVector3)
				Failure("The value of Vector3 is " + o.x + ", " + o.y + ", " + o.z + " and not " + tVector3.x + ", " + tVector3.y + ", " + tVector3.z);
			if (p != tVector4)
				Failure("The value of Vectorr is " + p.x + ", " + p.y + ", " + p.z + ", " + p.w + " and not " + tVector4.x + ", " + tVector4.y + ", " + tVector4.z + ", " + tVector4.w);
			if (q != tQuaternion)
				Failure("The value of Quaternion is " + q.x + ", " + q.y + ", " + q.z + ", " + q.w + " and not " + tQuaternion.x + ", " + tQuaternion.y + ", " + tQuaternion.z + ", " + tQuaternion.w);
			if (r != tColor)
				Failure("The value of Color is " + r.r + ", " + r.g + ", " + r.b + ", " + r.a + " and not " + tColor.r + ", " + tColor.g + ", " + tColor.b + ", " + tColor.a);
			if (s != tUint)
				Failure("The value of uint is " + s + " and not " + tUint);

			Success();
			KillNetworkCall(() => { RPC("HasMessageInfo"); });
		}

		[BRPC]
		private void HasMessageInfo(MessageInfo info)
		{
			// TODO:  Test input variables

			Debug.LogWarning("Sender ID: " + info.SenderId);
			Debug.LogWarning("Frame: " + info.Frame);

			Success();
			KillNetworkCall(() => { RPC("HasMessageInfoParams", tString, tByte); });
		}

		[BRPC]
		private void HasMessageInfoParams(string param, MessageInfo info, byte end)
		{
			// TODO:  Test input variables
			if (param != tString)
				Failure("Message is " + param + " and not " + tString);
			if (end != tByte)
				Failure("Byte is " + end + " and not " + tByte);

			Debug.LogWarning("Sender ID: " + info.SenderId);
			Debug.LogWarning("Frame: " + info.Frame);

			Success();
			KillNetworkCall(() => { RPC("PassRPC"); });
		}

		[BRPC(typeof(InterceptorTest))]
		private void PassRPC()
		{
			Success();
			KillNetworkCall(() => { RPC("FailRPC"); }, SkippedFailRPC);
		}

		[BRPC(typeof(InterceptorTest))]
		private void FailRPC()
		{
			Failure("This RPC is not suppose to get called FailRPC");
		}

		private void SkippedFailRPC()
		{
			System.Threading.Thread.Sleep(1000);

			Unity.MainThreadManager.Run(() =>
			{
				Success();
				AllSuccess();
			});
		}
	}
}
#endif