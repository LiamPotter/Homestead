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
#endif
using System.Security.Cryptography;
using System.Text;
#endif

namespace BeardedManStudios.Network
{
	abstract public class TCPProcess : NetWorker
	{
		public TCPProcess() : base() { }
		public TCPProcess(int maxConnections) : base(maxConnections) { }

#if !NetFX_CORE
		protected int previousSize = 0;
		protected BMSByte readBuffer = new BMSByte();
		protected BMSByte backBuffer = new BMSByte();

		protected object rpcMutex = new object();

		protected NetworkingStream readStream = new NetworkingStream();

#if !UNITY_WEBGL
		protected BMSByte ReadBuffer(NetworkStream stream)
		{
			int count = 0;
			while (stream.DataAvailable)
			{
				previousSize = backBuffer.Size;
				backBuffer.SetSize(backBuffer.Size + 1024);

				count += stream.Read(backBuffer.byteArr, previousSize, 1024);
				backBuffer.SetSize(previousSize + count);
			}

			int size = BitConverter.ToInt32(backBuffer.byteArr, backBuffer.StartIndex());

			readBuffer.Clear();

			if (count == 0)
				return readBuffer;

			readBuffer.BlockCopy(backBuffer.byteArr, backBuffer.StartIndex(4), size);

			if (readBuffer.Size + 4 < backBuffer.Size)
				backBuffer.RemoveStart(size + 4);
			else
				backBuffer.Clear();

			return readBuffer;
		}
#endif

		private object tmp = new object();
		protected void StreamReceived(NetworkingPlayer sender, BMSByte bytes)
		{
			if (TrackBandwidth)
				BandwidthIn += (ulong)bytes.Size;

			lock (tmp)
			{
				bytes.MoveStartIndex(1);
				readStream.Reset();

				if (base.ProcessReceivedData(sender, bytes, bytes[0]))
					return;

				// TODO:  Not needed after initialization
				readStream.SetProtocolType(Networking.ProtocolType.TCP);
				if (readStream.Consume(this, sender, bytes) == null)
					return;

				CurrentStreamOwner = sender;

				// Do not process player because it is processed through the listener
				if (readStream.identifierType == NetworkingStream.IdentifierType.Player)
				{
					if (!Connected)
						OnConnected();

					return;
				}

				if (!readStream.Ready)
					return;

				// TODO:  These need to be done better since there are many of them
				if (readStream.Bytes.Size < 22)
				{
					try
					{
						if (ObjectMapper.Compare<string>(readStream, "update"))
							UpdateNewPlayer(sender);

						if (ObjectMapper.Compare<string>(readStream, "disconnect"))
						{
							// TODO:  If this eventually sends something to the player they will not exist
							Disconnect(sender);
							return;
						}
					}
					catch
					{
						throw new NetworkException(12, "Mal-formed defalut communication");
					}
				}

				if (ReadStream(sender, readStream) && IsServer)
					RelayStream(readStream);

				DataRead(sender, readStream);
			}
		}

		protected bool ReadStream(NetworkingPlayer sender, NetworkingStream stream)
		{
			if (IsServer)
			{
				if (stream.Receivers == NetworkReceivers.MessageGroup && Me.MessageGroup != stream.Sender.MessageGroup)
					return true;
			}

			// Don't execute this logic on the server if the server doesn't own the object
			if (!ReferenceEquals(stream.NetworkedBehavior, null) && stream.Receivers == NetworkReceivers.Owner)
				return true;

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC)
			{
				lock (rpcMutex)
				{
					if ((new NetworkingStreamRPC(stream)).FailedExecution)
						return false;
				}
			}

			return true;
		}
		
		protected string HeaderHashKey()
		{
			string headerHash = "";
			Random rand = new Random();
			char[] availableChars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();

			for (int i = 0; i < 9; i++)
				headerHash += availableChars[rand.Next(0, availableChars.Length)];

			headerHash = Convert.ToBase64String((new SHA1CryptoServiceProvider()).ComputeHash(Encoding.UTF8.GetBytes(headerHash)));

			return headerHash;
		}

		protected string HeaderHashKeyCheck(string data)
		{
			return Convert.ToBase64String((new SHA1CryptoServiceProvider()).ComputeHash(Encoding.UTF8.GetBytes(data + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
		}

		abstract public override void Connect(string hostAddress, ushort port);
		abstract public override void Disconnect();
		abstract public override void TimeoutDisconnect();
		abstract public override void Write(NetworkingStream stream);
		abstract public override void Write(NetworkingPlayer player, NetworkingStream stream);
#endif
	}
}