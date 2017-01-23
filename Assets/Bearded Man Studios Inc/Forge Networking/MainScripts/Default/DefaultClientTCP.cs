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



#if !NetFX_CORE
using System;
#if !UNITY_WEBGL
using System.Net.Sockets;
using System.Threading;
using System.Text;
#else
using System.Runtime.InteropServices;
#endif
#endif

namespace BeardedManStudios.Network
{
	public class DefaultClientTCP : TCPProcess
	{
#if NetFX_CORE
		public override void Connect(string hostAddress, ushort port) { }
		public override void Disconnect() { }
		public override void TimeoutDisconnect() { }
		public override void Disconnect(NetworkingPlayer player, string reason = "") { }
		public override void Write(NetworkingStream stream) { }
		public override void Write(NetworkingPlayer player, NetworkingStream stream) { }
		public override void Send(byte[] data, int length, object endpoint = null) { }
#else

#if !UNITY_WEBGL
		private NetworkStream NetStream = null;
		private TcpClient client = null;
		private string headerHash = "";
		private bool headerExchanged = false;
		private Thread connector;
		private Thread readWorker = null;
		protected new object writeMutex = new object();
#endif
		~DefaultClientTCP() { Disconnect(); }

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern void ForgeConnect(string host, ushort port);

		[DllImport("__Internal")]
		private static extern void ForgeWrite(byte[] data, int length);

		[DllImport("__Internal")]
		private static extern IntPtr ForgeShiftDataRead();

		[DllImport("__Internal")]
		private static extern int ForgeContainsData();

		[DllImport("__Internal")]
		private static extern void ForgeClose();

		[DllImport("__Internal")]
		private static extern void ForgeLog(string data);
#endif

		public override void Send(byte[] data, int length, object endpoint = null)
		{
			byte[] send = EncodeMessageToSend(data, length);
#if !UNITY_WEBGL

			lock (writeMutex)
			{
				NetStream.Write(send, 0, send.Length);
			}
#else
			ForgeWrite(send, send.Length);
#endif
		}

		/// <summary>
		/// Connect to a Ip Address with a supplied port
		/// </summary>
		/// <param name="hostAddress">Ip Address to connect to</param>
		/// <param name="port">Port to connect from</param>
		public override void Connect(string hostAddress, ushort port)
		{
			Host = hostAddress;
#if UNITY_WEBPLAYER && !BARE_METAL
			if (UnityEngine.Application.isWebPlayer)
				UnityEngine.Security.PrefetchSocketPolicy(hostAddress, 843);	// TODO:  Make this configurable
#endif
#if UNITY_WEBGL
			ForgeConnect(hostAddress, port);
			Unity.MainThreadManager.unityFixedUpdate += ReadAsync;
#else
            connector = new Thread(new ParameterizedThreadStart(ThreadedConnect));
			connector.Start(new object[] { hostAddress, port });
#endif
		}

#if !UNITY_WEBGL
		private void ThreadedConnect(object hostAndPort)
		{
			string hostAddress = (string)((object[])hostAndPort)[0];
			ushort port = (ushort)((object[])hostAndPort)[1];
			
			try
			{
				// Create a TcpClient. 
				// The client requires a TcpServer that is connected 
				// to the same address specified by the server and port 
				// combination.
				client = new TcpClient(hostAddress, port);

				// Get a client stream for reading and writing. 
				// Stream stream = client.GetStream();
				NetStream = client.GetStream();

				readWorker = new Thread(new ThreadStart(ReadAsync));
				readWorker.Start();

				headerHash = HeaderHashKey();

				byte[] connectHeader = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\n" +
"Host: http://developers.forgepowered.com:" + port.ToString() + "\r\n" +
"Upgrade: websocket\r\n" +
"Connection: Upgrade\r\n" +
"Sec-WebSocket-Key: " + headerHash + "\r\n" +
"Sec-WebSocket-Version: 13\r\n");

				server = new NetworkingPlayer(0, client.Client.RemoteEndPoint.ToString(), client, "SERVER");
				NetStream.Write(connectHeader, 0, connectHeader.Length);
			}
			catch
			{
				throw new NetworkException(1, "Host is invalid or not found");
			}
		}
#endif

		/// <summary>
		/// Disconnect from the server
		/// </summary>
		public override void Disconnect()
		{
			BMSByte tmp = new BMSByte();
			ObjectMapper.MapBytes(tmp, "disconnect");
			
			lock (writeMutex)
			{
				writeStream.SetProtocolType(Networking.ProtocolType.TCP);
				writeStream.Prepare(this, NetworkingStream.IdentifierType.Disconnect, 0, tmp, NetworkReceivers.Server, noBehavior: true);

				Write(writeStream);
			}

#if UNITY_WEBGL
			ForgeClose();
#else
			if (readWorker != null)
#if UNITY_IOS
				readWorker.Interrupt();
#else
				readWorker.Abort();
#endif


			if (NetStream != null)
				NetStream.Close();

			if (client != null)
				client.Close();
#endif

			OnDisconnected();
		}

		public override void TimeoutDisconnect()
		{
			// TODO:  Implement
			OnTimeoutDisconnected();
		}

		public override void Write(NetworkingPlayer player, NetworkingStream stream)
		{
			throw new NetworkException(11, "This is a method planned for the future and has not been implemented yet.");
		}

		/// <summary>
		/// Write to the server with a Networking Stream
		/// </summary>
		/// <param name="stream">Networking Stream to write</param>
		public override void Write(NetworkingStream stream)
		{
			if (!Connected)
				throw new NetworkException(5, "The Network could not be written to because no connection has been opened");
#if !UNITY_WEBGL
			if (!NetStream.CanWrite)
				return;
#endif

			// Send the message to the connected TcpServer.
			Send(stream.Bytes.Compress().byteArr, stream.Bytes.Size);
			OnDataSent(stream);
		}

		/// <summary>
		/// Get all the new player updates
		/// </summary>
		public override void GetNewPlayerUpdates()
		{
			Me = new NetworkingPlayer(Uniqueidentifier, "127.0.0.1", null, string.Empty);

			BMSByte tmp = new BMSByte();
			ObjectMapper.MapBytes(tmp, "update");

			lock(writeMutex)
			{
				writeStream.SetProtocolType(Networking.ProtocolType.TCP);
				writeStream.Prepare(this, NetworkingStream.IdentifierType.None, 0, tmp, NetworkReceivers.Server, noBehavior: true);

				Write(writeStream);
			}
		}

		private void ReadAsync()
		{
			try
			{
#if !UNITY_WEBGL
				while (true)
				{
#endif
#if UNITY_WEBGL
				int length = ForgeContainsData();
				if (length == 0)
					return;

				IntPtr ptr = ForgeShiftDataRead();
				byte[] bytes = new byte[length];
				Marshal.Copy(ptr, bytes, 0, bytes.Length);

				if (bytes.Length > 0)
				{
					readBuffer.Clear();
					readBuffer.Clone(bytes);
					StreamReceived(server, readBuffer);
				}
#else
					if (!NetStream.CanRead)
						break;

					if (!NetStream.DataAvailable)
					{
						Thread.Sleep(10);
						continue;
					}

					if (!headerExchanged)
					{
						byte[] bytes = new byte[client.Available];
						NetStream.Read(bytes, 0, bytes.Length);

						string tmp = Encoding.UTF8.GetString(bytes);
						string[] headers = tmp.Replace("\r", "").Split('\n');

						if (headers.Length < 4)
							continue;

						if (headers[0] == "HTTP/1.1 101 Switching Protocols" &&
							headers[1] == "Connection: Upgrade" &&
							headers[2] == "Upgrade: websocket" &&
							headers[3].StartsWith("Sec-WebSocket-Accept: "))
						{
							string hash = headers[3].Substring(headers[3].IndexOf(' ') + 1);
							if (hash == HeaderHashKeyCheck(headerHash))
							{
								headerExchanged = true;

								// Ping the server to finalize the player's connection
								Send(new byte[] { 0 }, 1);
							}
						}
					}
					else
					{
						int length = 0;
						byte[] bytes = DecodeMessage(GetNextBytes(NetStream, out length));

						readBuffer.Clear();
						readBuffer.Clone(bytes, length);
						StreamReceived(server, readBuffer);
					}
#endif
#if !UNITY_WEBGL
				}
#endif
			}
			catch (Exception e)
			{
#if !BARE_METAL
				UnityEngine.Debug.LogException(e);
#endif
				Disconnect();
			}
		}
#endif

#if !UNITY_WEBGL
		private byte[] GetNextBytes(NetworkStream NetStream, out int length)
		{
			byte[] bytes = new byte[client.Available];
			NetStream.Read(bytes, 0, 2);

			int dataLength = bytes[1] & 127;
			int indexFirstMask = 2;
			if (dataLength == 126)
				indexFirstMask = 4;
			else if (dataLength == 127)
				indexFirstMask = 10;

			length = bytes[1];
			if (indexFirstMask != 2)
			{
				NetStream.Read(bytes, 2, indexFirstMask - 2);

				// Need to reverse the endien order
				if (indexFirstMask == 4)
					length = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
				else
					length = BitConverter.ToInt32(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
			}

			NetStream.Read(bytes, indexFirstMask, length);

			return bytes;
		}

		protected byte[] DecodeMessage(byte[] bytes)
		{
			int dataLength = bytes[1] & 127;
			int indexFirstMask = 2;
			if (dataLength == 126)
				indexFirstMask = 4;
			else if (dataLength == 127)
				indexFirstMask = 10;

			int indexFirstDataByte = indexFirstMask;

			byte[] decoded = new byte[bytes.Length - indexFirstDataByte];
			for (int i = indexFirstDataByte, j = 0; i < bytes.Length; i++, j++)
				decoded[j] = bytes[i];

			return decoded;
		}
#endif

#if UNITY_WEBGL
		protected byte[] EncodeMessageToSend(byte[] bytesRaw, int length = -1)
		{
			if (length < 0 || bytesRaw.Length == length)
				return bytesRaw;

			byte[] transformed = new byte[length];
			for (int i = 0; i < length; i++)
				transformed[i] = bytesRaw[i];

			return transformed;
		}
#else
		protected byte[] EncodeMessageToSend(byte[] bytesRaw, int length = -1)
		{
			byte[] response;
			byte[] frame = new byte[10];

			int indexStartRawData = -1;
			length = length == -1 ? bytesRaw.Length : length;

			frame[0] = 130;
			if (length <= 125)
			{
				// Inverse the length for the server
				frame[1] = (byte)((byte)length & 127);
				indexStartRawData = 2;
			}
			else if (length >= 126 && length <= 65535)
			{
				frame[1] = 126;
				frame[3] = (byte)((length >> 8) & 255);
				frame[2] = (byte)(length & 255);
				indexStartRawData = 4;
			}
			else
			{
				frame[1] = 127;
				frame[9] = (byte)((length >> 56) & 255);
				frame[8] = (byte)((length >> 48) & 255);
				frame[7] = (byte)((length >> 40) & 255);
				frame[6] = (byte)((length >> 32) & 255);
				frame[5] = (byte)((length >> 24) & 255);
				frame[4] = (byte)((length >> 16) & 255);
				frame[3] = (byte)((length >> 8) & 255);
				frame[2] = (byte)(length & 255);

				indexStartRawData = 10;
			}

			byte[] mask = BitConverter.GetBytes(new Random().Next(int.MinValue, int.MaxValue));
			indexStartRawData += mask.Length;

			response = new byte[indexStartRawData + length];

			int i, reponseIdx = 0, j = 0;

			// Add the frame bytes to the reponse
			for (i = 0; i < indexStartRawData - mask.Length; i++)
				response[reponseIdx++] = frame[i];

			for (i = 0; i < 4; i++)
				response[reponseIdx++] = mask[i];
			
			// Add the data bytes to the response
			for (i = 0; i < length; i++)
				response[reponseIdx++] = bytesRaw[i];

			for (i = indexStartRawData, j = 0; i < response.Length; i++, j++)
				response[i] = (byte)(response[i] ^ mask[j % 4]);

			return response;
		}
#endif
	}
}