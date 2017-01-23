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



#if !UNITY_WEBGL
#if !NetFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
#endif

#pragma warning disable 0219 //disables the bytes read warning

namespace BeardedManStudios.Network
{
	public class DefaultServerTCP : TCPProcess
	{
#if NetFX_CORE
		public bool RelayToAll { get; set; }
		public DefaultServerTCP(int maxConnections) : base(maxConnections) { }
		public override void Connect(string hostAddress, ushort port) { }
		public override void Disconnect() { }
		public override void TimeoutDisconnect() { }
		public override void Disconnect(NetworkingPlayer player, string reason = "") { }
		public void WriteTo(NetworkingPlayer player, NetworkingStream stream) { }
		public override void Write(NetworkingStream stream) { }
		public override void Write(NetworkingPlayer player, NetworkingStream stream) { }
		public override void Send(byte[] data, int length, object endpoint = null) { }
#else
		private TcpListener listener = null;
		private IPAddress ipAddress = null;

		private object clientMutex = new object();
		private Thread connectionThread = null;
		private Thread readThread = null;
		private bool readThreadCancel = false;


		private NetworkingStream staticWriteStream = new NetworkingStream();

		/// <summary>
		/// Should the messages be relayed to all
		/// </summary>
		public bool RelayToAll { get; set; }

		private object removalMutex = new object();

		/// <summary>
		/// Constructor with a given Maximum allowed connections
		/// </summary>
		/// <param name="maxConnections">The Maximum connections allowed</param>
		public DefaultServerTCP(int maxConnections) : base(maxConnections) { RelayToAll = true; }
		~DefaultServerTCP() { }

		private Thread connector;

		public override void Send(byte[] data, int length, object endpoint = null)
		{
			if (endpoint == null)
				return;

			byte[] send = EncodeMessageToSend(data, length);
			
			lock (endpoint)
			{
				((TcpClient)endpoint).GetStream().Write(send, 0, send.Length);
			}
		}

		/// <summary>
		/// Host to a Ip Address with a supplied port
		/// </summary>
		/// <param name="hostAddress">Ip Address to host from</param>
		/// <param name="port">Port to allow connections from</param>
		public override void Connect(string hostAddress, ushort port)
		{
			Host = hostAddress;

			connector = new Thread(new ParameterizedThreadStart(ThreadedConnect));
			connector.Start(new object[] { hostAddress, port });
		}

		private void ThreadedConnect(object hostAndPort)
		{
			string hostAddress = (string)((object[])hostAndPort)[0];
			ushort port = (ushort)((object[])hostAndPort)[1];

			// Create an instance of the TcpListener class.
			server = null;
			if (string.IsNullOrEmpty(hostAddress) || hostAddress == "0.0.0.0" || hostAddress == "127.0.0.1" || hostAddress == "localhost")
				ipAddress = IPAddress.Any;
			else
				ipAddress = IPAddress.Parse(hostAddress);

			try
			{
				// Set the listener on the local IP address 
				// and specify the port.
				listener = new TcpListener(ipAddress, port);
				listener.Start();

				Players = new List<NetworkingPlayer>();
				Me = new NetworkingPlayer(ServerPlayerCounter++, "127.0.0.1", listener, "SERVER");

				connectionThread = new Thread(new ParameterizedThreadStart(ConnectionLoop));
				readThread = new Thread(new ThreadStart(ReadClients));

				connectionThread.Start(listener);
				readThread.Start();

				OnConnected();
			}
			catch (Exception e)
			{
#if !BARE_METAL
				UnityEngine.Debug.LogException(e);
#endif
				Disconnect();
			}
		}

		/// <summary>
		/// DisconNet a player from the server
		/// </summary>
		/// <param name="player">Player to be removed from the server</param>
		public override void Disconnect(NetworkingPlayer player, string reason = null)
		{
			lock (removalMutex)
			{
				base.Disconnect(player);

				OnPlayerDisconnected(player);
				CleanRPCForPlayer(player);
			}
		}

		/// <summary>
		/// Disconnect the server
		/// </summary>
		public override void Disconnect()
		{
			if (Players != null)
			{
				lock (Players)
				{
					foreach (NetworkingPlayer player in Players)
						((TcpClient)player.SocketEndpoint).Close();

					Players.Clear();
				}
			}

			if (connectionThread != null)
				connectionThread.Abort();

			readThreadCancel = true;
			listener.Stop();

			OnDisconnected();
		}

		public override void TimeoutDisconnect()
		{
			// TODO:  Implement
		}

		private void WriteAndClose(TcpClient targetSocket, NetworkingStream stream)
		{
			Send(stream.Bytes.Compress().byteArr, stream.Bytes.Size, targetSocket);
			targetSocket.Close();
		}

		/// <summary>
		/// Write the Players data and Networking stream sent to the server
		/// </summary>
		/// <param name="player">Player to write from</param>
		/// <param name="stream">Networking Stream to be used</param>
		public override void Write(NetworkingPlayer player, NetworkingStream stream)
		{
			Send(stream.Bytes.Compress().byteArr, stream.Bytes.Size, player.SocketEndpoint);
		}

		/// <summary>
		/// Write the Networking Stream to the server
		/// </summary>
		/// <param name="stream">Networking Stream to be used</param>
		public override void Write(NetworkingStream stream)
		{
			// TODO:  Find out if this was a relay
			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && (stream.Receivers == NetworkReceivers.AllBuffered || stream.Receivers == NetworkReceivers.OthersBuffered))
				ServerBufferRPC(stream);

			if (stream.Receivers == NetworkReceivers.Server || stream.Receivers == NetworkReceivers.ServerAndOwner)
				return;

			byte[] sendData = stream.Bytes.Compress().byteArr;
			for (int i = 0; i < Players.Count; i++)
			{
				if (!Players[i].Connected)
					continue;

				if ((stream.Receivers == NetworkReceivers.Others || stream.Receivers == NetworkReceivers.OthersBuffered) && Players[i] == stream.Sender)
					continue;

				if (!((TcpClient)Players[i].SocketEndpoint).Connected)
				{
					Disconnect(Players[i]);
					continue;
				}
				
				Send(sendData, sendData.Length, Players[i].SocketEndpoint);
			}
		}

		private void ReadClients()
		{
			while (true)
			{
				try
				{
					if (readThreadCancel)
						return;

					try
					{
						lock (clientMutex)
						{
							for (int i = 0; i < Players.Count; i++)
							{
								if (readThreadCancel)
									return;

								TcpClient playerClient = (TcpClient)Players[i].SocketEndpoint;
								NetworkStream playerStream = playerClient.GetStream();

								if (!playerClient.Connected)
								{
									Disconnect(Players[i--]);
									continue;
								}

								if (!playerStream.DataAvailable)
									continue;

								if (!Players[i].Connected)
								{
									if (!Players[i].WebsocketHeaderPrepared)
									{
										byte[] bytes = new byte[playerClient.Available];
										playerStream.Read(bytes, 0, bytes.Length);

										string data = Encoding.UTF8.GetString(bytes);

										if (new Regex("^GET").IsMatch(data))
										{
											byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
												+ "Connection: Upgrade" + Environment.NewLine
												+ "Upgrade: websocket" + Environment.NewLine
												+ "Sec-WebSocket-Accept: " + Convert.ToBase64String((new SHA1CryptoServiceProvider()).ComputeHash(Encoding.UTF8.GetBytes(
															new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
												))) + Environment.NewLine + Environment.NewLine);

											playerStream.Write(response, 0, response.Length);
										}

										Players[i].WebsocketHeaderPrepared = true;
									}
									else
									{
										int length = 0;
										byte[] bytes = DecodeMessage(GetNextBytes(playerClient, playerStream, out length));

										lock (writeMutex)
										{
											writeBuffer.Clear();
											ObjectMapper.MapBytes(writeBuffer, Players[i].NetworkId);

											writeStream.SetProtocolType(Networking.ProtocolType.TCP);
											Write(Players[i], writeStream.Prepare(this,
												NetworkingStream.IdentifierType.Player, 0, writeBuffer, noBehavior: true));

											OnPlayerConnected(Players[i]);
										}
									}
								}
								else
								{
									int length = 0;
									byte[] bytes = DecodeMessage(GetNextBytes(playerClient, playerStream, out length));

									if (bytes[0] == 136)
									{
										Disconnect(Players[i--]);
										continue;
									}

									readBuffer.Clear();
									readBuffer.Clone(bytes);
									StreamReceived(Players[i], readBuffer);
								}
							}
						}
					}
					catch (Exception ex)
					{
#if !BARE_METAL
						UnityEngine.Debug.LogException(ex);
#endif
					}

					Thread.Sleep(ThreadSpeed);
				}
				catch (Exception ex)
				{
#if !BARE_METAL
						UnityEngine.Debug.LogException(ex);
#endif
				}
			}
		}

		private BMSByte writeBuffer = new BMSByte();

		private void ConnectionLoop(object server)
		{
			while (true)
			{
				try
				{
					TcpClient client = ((TcpListener)server).AcceptTcpClient();

					if (Connections >= MaxConnections)
					{
						lock (writeMutex)
						{
							writeBuffer.Clear();
							ObjectMapper.MapBytes(writeBuffer, "Max Players Reached On Server");

							staticWriteStream.SetProtocolType(Networking.ProtocolType.TCP);
							WriteAndClose(client, staticWriteStream.Prepare(
								this, NetworkingStream.IdentifierType.Disconnect, 0, writeBuffer, noBehavior: true));
						}

						return;
					}

					// TODO:  Set the name
					string name = string.Empty;

					NetworkingPlayer player = new NetworkingPlayer(ServerPlayerCounter++, client.Client.RemoteEndPoint.ToString(), client, name);

					lock (clientMutex)
					{
						Players.Add(player);
					}
				}
				catch (Exception exception)
				{
#if !BARE_METAL
					UnityEngine.Debug.LogException(exception);
#endif
					Disconnect();
				}
			}
		}
#endif

		private byte[] GetNextBytes(TcpClient playerClient, NetworkStream NetStream, out int length)
		{
			byte[] bytes = new byte[playerClient.Available];
			NetStream.Read(bytes, 0, 2);

			int dataLength = bytes[1] & 127;
			int indexFirstMask = 2;
			if (dataLength == 126)
				indexFirstMask = 4;
			else if (dataLength == 127)
				indexFirstMask = 10;

			length = dataLength;
			if (indexFirstMask != 2)
			{
				NetStream.Read(bytes, 2, indexFirstMask - 2);

				// Need to reverse the endien order
				if (indexFirstMask == 4)
					length = BitConverter.ToUInt16(bytes, 2);
				else
					length = (int)BitConverter.ToUInt32(bytes, 2);
			}

			// Read the mask
			NetStream.Read(bytes, indexFirstMask, 4);
			NetStream.Read(bytes, indexFirstMask + 4, length);

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

			IEnumerable<byte> keys = bytes.Skip(indexFirstMask).Take(4);
			int indexFirstDataByte = indexFirstMask + 4;
			
			byte[] decoded = new byte[bytes.Length - indexFirstDataByte];
			for (int i = indexFirstDataByte, j = 0; i < bytes.Length; i++, j++)
				decoded[j] = (byte)(bytes[i] ^ keys.ElementAt(j % 4));

			return decoded;
		}

		private byte[] EncodeMessageToSend(byte[] bytesRaw, int length = -1)
		{
			byte[] response;
			byte[] frame = new byte[10];

			int indexStartRawData = -1;
			length = length == -1 ? bytesRaw.Length : length;

			frame[0] = 130;
			if (length <= 125)
			{
				frame[1] = (byte)length;
				indexStartRawData = 2;
			}
			else if (length >= 126 && length <= 65535)
			{
				frame[1] = 126;
				frame[2] = (byte)((length >> 8) & 255);
				frame[3] = (byte)(length & 255);
				indexStartRawData = 4;
			}
			else
			{
				frame[1] = 127;
				frame[2] = (byte)((length >> 56) & 255);
				frame[3] = (byte)((length >> 48) & 255);
				frame[4] = (byte)((length >> 40) & 255);
				frame[5] = (byte)((length >> 32) & 255);
				frame[6] = (byte)((length >> 24) & 255);
				frame[7] = (byte)((length >> 16) & 255);
				frame[8] = (byte)((length >> 8) & 255);
				frame[9] = (byte)(length & 255);

				indexStartRawData = 10;
			}

			response = new byte[indexStartRawData + length];

			int i, reponseIdx = 0;

			// Add the frame bytes to the reponse
			for (i = 0; i < indexStartRawData; i++)
			{
				response[reponseIdx] = frame[i];
				reponseIdx++;
			}

			// Add the data bytes to the response
			for (i = 0; i < length; i++)
			{
				response[reponseIdx] = bytesRaw[i];
				reponseIdx++;
			}

			return response;
		}
	}
}
#endif