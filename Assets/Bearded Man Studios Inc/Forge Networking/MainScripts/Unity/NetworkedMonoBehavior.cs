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



#if NetFX_CORE
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BeardedManStudios.Network
{
	/// <summary>
	/// This is a class that can serialize class variables across the Network
	/// </summary>
	/// <remarks>
	/// NetworkedMonoBehavior or NMB is the second of the two main classes used heavily by the forge Networking features. NetworkedMonoBehavior allows for
	/// syncronization of position, rotation and scale of classes that inherit from it, interpolation of those values, RPCs, Proximity Based Updates, Authoritative Control, 
	/// advanced management of message send rates (throttling) and the use of forge's NetworkControls.
	/// If you only need to use RPCs your class should inherit from SimpleNetworkedMonoBehavior instead, the additional features of the NetworkedMonoBehavior 
	/// are slightly more resource intensive.
	/// </remarks>
	[AddComponentMenu("Forge Networking/Networked MonoBehavior")]
	public class NetworkedMonoBehavior : SimpleNetworkedMonoBehavior
	{
#if BARE_METAL
		public NetworkedMonoBehavior(string name, string type) : base(name, type) { }
#endif
		///@{
		/// <summary>
		/// A delegate fired on the server by using InputCheck()
		/// </summary>
		/// <param name="keyCode">The input key that was pressed</param>
		/// <param name="frame">The frame that this was requested on</param>
		protected delegate void InputRequest(KeyCode keyCode, int frame);
		/// <summary>
		/// A delegate for execuing input events from the client on the server
		/// </summary>
		/// <param name="keyCode">The input key that was pressed</param>
		protected delegate void FramelessInputRequest(KeyCode keyCode);

		/// <summary>
		/// A delegate for execuing mouse input events from the client on the server
		/// </summary>
		/// <param name="buttonIndex">The index of the button that was pressed</param>
		/// <param name="frame">The frame that this was requested on</param>
		protected delegate void MouseInputRequest(int buttonIndex, int frame);

		/// <summary>
		/// A delegate for execuing mouse input events from the client on the server
		/// </summary>
		/// <param name="buttonIndex">The index of the button that was pressed</param>
		protected delegate void FramelessMouseInputRequest(int buttonIndex);
		///@}


		///<summary>
		///This type of delegate describes a delegate type, used in the NetworkedMonoBehavior.enteredProximity
		///event and the NetworkedMonoBehavior.exitedProximity it simply describes a signature with two players.
		///The player who's perspective is being handled and the player who has entered or exited proximity with
		///the other player.
		///</summary>
		public delegate void ProximityEvent(NetworkedMonoBehavior myPlayer, NetworkedMonoBehavior otherPlayer);

		/// <summary>
		/// An event that is fired for when one player has entered "my players" proximity
		/// </summary>
		/// <remarks>
		/// This method can be used to display a player only once they are within a given proximity distance of the player.
		/// This event can be fired like so:
		/// <code>
		/// // This is an event that will fire whenever the other object enters my proximity
		/// enteredProximity += (mine, other) => { Debug.Log(other.name + " entered my (" + mine.name + ") proximity"); };
		/// </code>
		/// </remarks>
		public event ProximityEvent enteredProximity
		{
			add
			{
				enteredProximityInvoker += value;
			}
			remove
			{
				enteredProximityInvoker -= value;
			}
		}
		ProximityEvent enteredProximityInvoker;

		/// <summary>
		/// An event that is fired for when one player has left "my players" proximity
		/// </summary>
		/// <remarks>
		/// See NetworkedMonoBehavior.enteredProximity, this method does the same, but handles an NMB going beyond the proximity of the NMB
		/// owned by the local player.
		/// </remarks>
		public event ProximityEvent exitedProximity
		{
			add
			{
				exitedProximityInvoker += value;
			}
			remove
			{
				exitedProximityInvoker -= value;
			}
		}
		ProximityEvent exitedProximityInvoker;

		/// <summary>
		/// A list of other "isPlayer" behaviors that are currently in this behaviors proximity
		/// </summary>
		[HideInInspector]
		public List<ulong> proximityBehaviors = new List<ulong>();
		
		/// <summary>
		/// Attribute for easily replicating properties and fields across the Network
		/// </summary>
		/// <remarks>
		/// NetSync is an attribute that can easily be assigned to any variable, the variable will then be syncronized across the Network for all clients.
		/// An attribute can be assigned to a variable as follows:
		/// <code>
		/// [NetSync]
		/// public int playerHealth;
		/// </code>
		/// Additionally you can specify a method to be called when the variable is updated (this allows you to design behavior similar to that of a property),
		/// this is done so as follows:
		/// <code>
		/// [NetSync("methodA", NetworkCallers.Everyone)]
		/// public int playerHealth;
		/// 
		/// public void methodA(){
		/// 
		/// }
		/// </code>
		/// The NetSync attribute has two parameters, the first is a string representing the method's name. The second is a Network.NetworkCallers value,
		/// this defines which clients in the Network execute the method.
		/// </remarks>
		protected class NetSync : Attribute
		{
			/// <summary>
			/// Just to make it more visual rather than just a boolean
			/// </summary>
			public enum Interpolate
			{
				True,
				False
			}

			/// <summary>
			/// The name of the method to execute when the value has changed
			/// </summary>
			public string method;

			/// <summary>
			/// Used to determine who is to call the callback method when the value has changed
			/// </summary>
			public NetworkCallers callers;

			/// <summary>
			/// This is uesd to determine if this value ignores interpolation
			/// </summary>
			/// <remarks>
			/// This defines if a value will be interpolated as it is updated across the Network. Typically you'll want to set this with a constructor.
			/// This can often be needed if a value needs to be interpolated smoothly as the increments across the Network tend to be a little too large
			/// for things like movement.
			/// </remarks>
			public Interpolate interpolate;

			public NetSync()
			{
				method = string.Empty;
				callers = NetworkCallers.Everyone;
				interpolate = Interpolate.True;
			}

			/// <summary>
			/// This constructor allows you to specify if the NetSync should be interpolated, see NetSync.interpolate
			/// </summary>
			/// <param name="ignoreInterpolation">Interpolation setting</param>
			public NetSync(Interpolate ignoreInterpolation)
			{
				interpolate = ignoreInterpolation;
			}

			/// <summary>
			/// Allows you to specify a method that will be called when the NetSync is updated and who should call the method.
			/// </summary>
			/// <param name="method">Method name</param>
			/// <param name="callers">Who in the Network should call the method when it is updated</param>
			public NetSync(string method, NetworkCallers callers)
			{
				this.method = method;
				this.callers = callers;
				interpolate = Interpolate.True;
			}

			/// <summary>
			/// Allows you to specify a method to call when the value is updated, who in the Network should call the method and the type of interpolation on the 
			/// value of the NetSync.
			/// </summary>
			/// <param name="method">Method name</param>
			/// <param name="callers">Who in the Network should call the method when it is updated</param>
			/// <param name="ignoreInterpolation">Interpolation setting</param>
			public NetSync(string method, NetworkCallers callers, Interpolate ignoreInterpolation)
			{
				this.method = method;
				this.callers = callers;
				interpolate = ignoreInterpolation;
			}
		}

		/// <summary>
		/// Attribute for manually replicating properties and fields across the Network
		/// </summary>
		/// <remarks>
		/// Similar to NetSync, ManualNetSync allows you to decide when the variable syncs as opposed to the variable automatically
		/// syncing. SerializeManualProperties() method is used to sync the variable manually. To use ManualNetSync,
		/// apply it as an attribute to a variable.
		/// <code>
		/// [ManualNetSync]
		/// public float energy;
		/// 
		/// void Start(){
		///     SerializeManualProperties(true, NetworkReceivers.All);
		/// }
		/// </code>
		/// </remarks>
		protected sealed class ManualNetSync : NetSync { }

		/// <summary>
		/// Attribute for easily replicating properties and fields across the Network only to the server
		/// </summary>
		/// <remarks>
		/// Similar to NetSync, NetSyncToServer only syncronizes the variable on the owner client and the server. To use NetSyncToServer,
		/// apply it as an attribute to a variable.
		/// <code>
		/// [NetSyncToServer]
		/// public float energy;
		/// </code>
		/// </remarks>
		protected sealed class NetSyncToServer : NetSync { }

		/// <summary>
		/// An enum for easy visual serialization of properties
		/// </summary>
		public enum SerializeVector3Properties
		{
			None,
			X,
			Y,
			Z,
			XY,
			XZ,
			YZ,
			XYZ
		}

		/// <summary>
		/// Used to determine what fields to serialize for the position
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializePosition = SerializeVector3Properties.XYZ;

		/// <summary>
		/// Used to determine if the position should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpPosition = true;

		/// <summary>
		/// Used to determine what fields to serialize for the rotation
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializeRotation = SerializeVector3Properties.XYZ;

		/// <summary>
		/// Used to determine if the rotation should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpRotation = true;

		/// <summary>
		/// Used to determine what fields to serialize for the scale
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializeScale = SerializeVector3Properties.None;

		/// <summary>
		/// Used to determine if the scale should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpScale = true;

		/// <summary>
		/// If this is a reliable NetworkedMonoBehavior object
		/// </summary>
		[HideInInspector]
		public bool isReliable = false;

		/// <summary>
		/// If you want to Interpolate the values across the Network for smooth movement
		/// </summary>
		[HideInInspector]
		public bool interpolateFloatingValues = true;

		/// <summary>
		/// The lerp time it will take
		/// </summary>
		[HideInInspector]
		public float lerpT = 0.25f;

		/// <summary>
		/// The cutoff point to when it will stop lerping
		/// </summary>
		[HideInInspector]
		public float lerpStopOffset = 0.01f;

		/// <summary>
		/// The cutoff point for when it will stop lerping
		/// </summary>
		[HideInInspector]
		public float lerpAngleStopOffset = 1.0f;

		/// <summary>
		/// The delay it takes to send to the server
		/// </summary>
		[HideInInspector]
		public float NetworkTimeDelay = 0.1f;

		/// <summary>
		/// The current time for the delay counter
		/// </summary>
		private float timeDelayCounter = 0;

		/// <summary>
		/// A list of all of the properties that are to be serialized across the Network
		/// </summary>
		private List<NetRef<object>> Properties = new List<NetRef<object>>();

		/// <summary>
		/// A list of all of the properties that are to be manually serialized across the Network
		/// </summary>
		private List<NetRef<object>> ManualProperties = new List<NetRef<object>>();

		/// <summary>
		/// Get whether this is a player or not
		/// </summary>
		[HideInInspector]
		public bool isPlayer = false;

		/// <summary>
		/// When this is true, the client can only send inputs to the server via the Request() method
		/// </summary>
		/// <remarks>
		/// See <A HREF="http://developers.forgepowered.com/Tutorials/MasterClassIntermediate/Working-With-Authoritative-Server-Option">this</A> for more...
		/// </remarks>
		[HideInInspector]
		public bool serverIsAuthority = false;

		/// <summary>
		/// Used with <see cref="serverIsAuthority"/> in order to simulate inputs on the cilent side
		/// </summary>
		[HideInInspector]
		public bool clientSidePrediction = false;

		/// <summary>
		/// This is the maximum distance offset that the player can be from the server before it is syncronized
		/// </summary>
		/// <remarks>
		/// Used when the server is using the authoritative option, this essentially is a distance that the server will allow the local client to be from
		/// the server's version of the client. If the distance is very low, the client will likely snap back to the server's representation of the client
		/// a lot. Setting the option to be higher will allow the client to have a local simulation of the client that feels very responsive while the server's
		/// version of the client is slightly behind.
		/// See <A HREF="http://developers.forgepowered.com/Tutorials/MasterClassIntermediate/Authoritative-Sync-Thresholds">this</A> for a demonstration.
		/// </remarks>
		public float authoritativeSyncDistance = 0.25f;

		/// <summary>
		/// This is the maximum distance offset that the player can be from the server before it is syncronized
		/// </summary>
		public float authoritativeTeleportSyncDistance = 3.0f;

		/// <summary>
		/// This is the maximum rotation in degrees that the player can be from the server before it is syncronized
		/// </summary>
		/// <remarks>
		/// Works exactly the same as NetworkedMonoBehavior.authoritativeSyncDistance, but for the rotation of the client.
		/// </remarks>
		public float authoritativeSyncRotation = 5.0f;

		#region Authoritative Frame History

		/// <summary>
		/// The mutex for locking the logic for the authoritative frame
		/// </summary>
		private object _authoritativeFrameMutex;

		/// <summary>
		/// The type of authoritative frame
		/// </summary>
		protected enum AuthoritativeFrameType
		{
			Position,
			Rotation,
			Scale
		}

		/// <summary>
		/// The authoritative frame with it's frame number and value
		/// </summary>
		protected class AuthoritativeFrame
		{
			public Vector3 FramePrevious;
			public Vector3 FrameActualValue;
			public Vector3 FrameValue;
			public byte Frame;

			public AuthoritativeFrame(byte frame, Vector3 actual, Vector3 value, Vector3 previous)
			{
				Frame = frame;
				FrameActualValue = actual;
				FrameValue = value;
				FramePrevious = new Vector3(previous.x, previous.y, previous.z);
			}

			public void UpdateDelta()
			{
				FrameValue = FrameActualValue - FramePrevious;
			}

			public override string ToString()
			{
				return "Authoritative Frame { \"Frame\":" + Frame + ", \"Actual\":\"" + FrameActualValue + "\", \"Previous\":\"" +
					   FramePrevious + "\", \"Delta\":\"" + FrameValue + "\" }";
			}
		}

		/// <summary>
		/// The Authoritative frame history
		///		This will do all the magic for handling frame history
		/// </summary>
		protected class AuthoritativeFrameHistory
		{
			#region Private Variables
			private List<AuthoritativeFrame> _positionFrameHistory = null; 
			private List<AuthoritativeFrame> _rotationFrameHistory = null; 
			private List<AuthoritativeFrame> _scaleFrameHistory = null;

			private bool _trackPos;
			private bool _trackRotation;
			private bool _trackScale;

			private byte _previousPosFrame = 0;
			private byte _previousRotFrame = 0;
			private byte _previousScaFrame = 0;

			private float _posTimeStamp;
			private float _rotTimeStamp;
			private float _scaTimeStamp;

			private byte _posLastFrame = 255;
			private byte _rotLastFrame = 255;
			private byte _scaLastFrame = 255;

			#endregion

			#region Public API
			/// <summary>
			/// Sets up the authoritative history for what we want to track
			/// </summary>
			/// <param name="trackPos">Do we want to track the position?</param>
			/// <param name="trackRotation">Do we want to track the rotation?</param>
			/// <param name="trackScale">Do we want to track the scale?</param>
			public void Setup(bool trackPos, bool trackRotation, bool trackScale)
			{
				_trackPos = trackPos;
#if BARE_METAL
				_posTimeStamp = (float)BareMetal.BareMetalTime.time;
#else
				_posTimeStamp = Time.time;
#endif
				_trackRotation = trackRotation;
				_trackScale = trackScale;

				_positionFrameHistory = !_trackPos ? null : new List<AuthoritativeFrame>(byte.MaxValue);
				_rotationFrameHistory = !_trackRotation ? null : new List<AuthoritativeFrame>(byte.MaxValue);
				_scaleFrameHistory = !_trackScale ? null : new List<AuthoritativeFrame>(byte.MaxValue);
			}

			/// <summary>
			/// Gets the updated frame of a given type, number, target value, and current value
			/// </summary>
			/// <param name="type">Type of frame we want to update</param>
			/// <param name="frameNumber">The frame number of what the server is</param>
			/// <param name="value">The target value of the server</param>
			/// <param name="currentValue">The current value of the client</param>
			/// <returns></returns>
			public Vector3 UpdateFrame(NetWorker owningNetworker, AuthoritativeFrameType type, Vector3 currentValue, byte serverFrame, Vector3 serverValue, ref Vector3 previousValue)
			{
				switch (type)
				{
					case AuthoritativeFrameType.Position:
						if (!_trackPos)
							return currentValue;
						break;
					case AuthoritativeFrameType.Rotation:
						if (!_trackRotation)
							return currentValue;
						break;
					case AuthoritativeFrameType.Scale:
						if (!_trackScale)
							return currentValue;
						break;
				}
				//ADD/UPDATE CLIENT FRAME
				HandleClient(type, currentValue, ref previousValue);
				
				return FinishFrame(owningNetworker, type, currentValue, serverFrame, serverValue);
			}
			#endregion

			#region Private API

			private void HandleClient(AuthoritativeFrameType type, Vector3 currentValue, ref Vector3 previousValue)
			{
				byte currentFrame = NetworkingManager.Instance.CurrentFrame;
				bool valueChanged = Vector3.Distance(currentValue, previousValue) > 0.01f;
				bool frameFound = false;
				switch (type)
				{
					case AuthoritativeFrameType.Position:
						if (currentFrame != _previousPosFrame || valueChanged)
						{
							if (valueChanged)
								_posTimeStamp = Time.time;

							Vector3 delta = Vector3.zero;
							Vector3 previous = previousValue;

							_previousPosFrame = currentFrame;

							if (_positionFrameHistory.Count > 0)
							{
								for (int i = _positionFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_positionFrameHistory[i].Frame == currentFrame)
									{
										frameFound = true;
										_positionFrameHistory[i].FrameActualValue = currentValue;
										_positionFrameHistory[i].UpdateDelta();
										break;
									}
								}

								if (!frameFound)
								{
									delta = currentValue - _positionFrameHistory[_positionFrameHistory.Count - 1].FrameActualValue;

									previousValue = currentValue;
								}

								previous = _positionFrameHistory[_positionFrameHistory.Count - 1].FrameActualValue;
							}

							if (!frameFound)
							{
								AuthoritativeFrame tempNewFrame = new AuthoritativeFrame(currentFrame, currentValue, delta, previous);
								_positionFrameHistory.Add(tempNewFrame);
							}

							if (_positionFrameHistory.Count > byte.MaxValue)
								_positionFrameHistory.RemoveRange(0, (int)(_positionFrameHistory.Count * 0.8f));
						}
						break;
					case AuthoritativeFrameType.Rotation:
						if (currentFrame != _previousRotFrame || valueChanged)
						{
							if (valueChanged)
								_rotTimeStamp = Time.time;

							Vector3 delta = Vector3.zero;
							Vector3 previous = previousValue;

							_previousRotFrame = currentFrame;

							if (_rotationFrameHistory.Count > 0)
							{
								for (int i = _rotationFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_rotationFrameHistory[i].Frame == currentFrame)
									{
										frameFound = true;
										_rotationFrameHistory[i].FrameActualValue = currentValue;
										_rotationFrameHistory[i].UpdateDelta();
										break;
									}
								}

								if (!frameFound)
								{
									delta = currentValue - _rotationFrameHistory[_rotationFrameHistory.Count - 1].FrameActualValue;

									previousValue = currentValue;
								}

								previous = _rotationFrameHistory[_rotationFrameHistory.Count - 1].FrameActualValue;
							}

							if (!frameFound)
							{
								AuthoritativeFrame tempNewFrame = new AuthoritativeFrame(currentFrame, currentValue, delta, previous);
								_rotationFrameHistory.Add(tempNewFrame);
							}

							if (_rotationFrameHistory.Count > byte.MaxValue)
								_rotationFrameHistory.RemoveRange(0, (int)(_rotationFrameHistory.Count * 0.8f));
						}
						break;
					case AuthoritativeFrameType.Scale:
						if (currentFrame != _previousScaFrame || valueChanged)
						{
							if (valueChanged)
								_scaTimeStamp = Time.time;

							Vector3 delta = Vector3.zero;
							Vector3 previous = previousValue;

							_previousScaFrame = currentFrame;

							if (_scaleFrameHistory.Count > 0)
							{
								for (int i = _scaleFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_scaleFrameHistory[i].Frame == currentFrame)
									{
										frameFound = true;
										_scaleFrameHistory[i].FrameActualValue = currentValue;
										_scaleFrameHistory[i].UpdateDelta();
										break;
									}
								}

								if (!frameFound)
								{
									delta = currentValue - _scaleFrameHistory[_scaleFrameHistory.Count - 1].FrameActualValue;

									previousValue = currentValue;
								}

								previous = _scaleFrameHistory[_scaleFrameHistory.Count - 1].FrameActualValue;
							}

							if (!frameFound)
							{
								AuthoritativeFrame tempNewFrame = new AuthoritativeFrame(currentFrame, currentValue, delta, previous);
								_scaleFrameHistory.Add(tempNewFrame);
							}

							if (_scaleFrameHistory.Count > byte.MaxValue)
								_scaleFrameHistory.RemoveRange(0, (int)(_scaleFrameHistory.Count * 0.8f));
						}
						break;
				}
			}

			private Vector3 FinishFrame(NetWorker owningNetworker, AuthoritativeFrameType type, Vector3 currentValue, byte serverFrame, Vector3 serverValue)
			{
				bool frameFound = false;
				int iter = 0;
				Vector3 pos = currentValue;
				switch (type)
				{
					case AuthoritativeFrameType.Position:
						if (_posLastFrame != serverFrame)
						{
							_posLastFrame = serverFrame;
							_posTimeStamp = Time.time;
							
							if (_positionFrameHistory.Count > 0)
							{
								for (int i = _positionFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_positionFrameHistory[i].Frame == serverFrame)
									{
										frameFound = true;
										iter = i;
										break;
									}
								}

								if (frameFound)
								{
									_positionFrameHistory.RemoveRange(0, iter);

									_positionFrameHistory[0].FrameActualValue = serverValue;
									_positionFrameHistory[0].FrameValue = Vector3.zero;

									if (_positionFrameHistory.Count > 1)
									{
										_positionFrameHistory[1].FramePrevious = _positionFrameHistory[0].FrameActualValue;
										_positionFrameHistory[1].UpdateDelta();
									}

									pos = serverValue;
									for (int i = 0; i < _positionFrameHistory.Count; ++i)
										pos += _positionFrameHistory[i].FrameValue; //Get the delta distance
								}
							}

							currentValue = pos;
						}
						else
						{
							if ((Time.time - _posTimeStamp) * 1000 > owningNetworker.PreviousServerPing + 100)
							{
								_posTimeStamp = Time.time;
								if (Vector3.Distance(currentValue, serverValue) > 0.1f)
								{
									currentValue = serverValue;
									_positionFrameHistory.Clear();
								}
							}
						}
						break;
					case AuthoritativeFrameType.Rotation:
						if (_rotLastFrame != serverFrame)
						{
							_rotLastFrame = serverFrame;
							_rotTimeStamp = Time.time;

							if (_rotationFrameHistory.Count > 0)
							{
								for (int i = _rotationFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_rotationFrameHistory[i].Frame == serverFrame)
									{
										frameFound = true;
										iter = i;
										break;
									}
								}

								if (frameFound)
								{
									_rotationFrameHistory.RemoveRange(0, iter);

									_rotationFrameHistory[0].FrameActualValue = serverValue;
									_rotationFrameHistory[0].FrameValue = Vector3.zero;

									if (_rotationFrameHistory.Count > 1)
									{
										_rotationFrameHistory[1].FramePrevious = _rotationFrameHistory[0].FrameActualValue;
										_rotationFrameHistory[1].UpdateDelta();
									}

									pos = serverValue;
									for (int i = 0; i < _rotationFrameHistory.Count; ++i)
										pos += _rotationFrameHistory[i].FrameValue; //Get the delta distance
								}
							}

							currentValue = pos;
						}
						else
						{
							if ((Time.time - _rotTimeStamp) * 1000 > owningNetworker.PreviousServerPing + 100)
							{
								_rotTimeStamp = Time.time;
								if (Vector3.Distance(currentValue, serverValue) > 0.1f)
								{
									currentValue = serverValue;
									_rotationFrameHistory.Clear();
								}
							}
						}
						break;
					case AuthoritativeFrameType.Scale:
						if (_scaLastFrame != serverFrame)
						{
							_scaLastFrame = serverFrame;
							_scaTimeStamp = Time.time;

							if (_scaleFrameHistory.Count > 0)
							{
								for (int i = _scaleFrameHistory.Count - 1; i >= 0; --i)
								{
									if (_scaleFrameHistory[i].Frame == serverFrame)
									{
										frameFound = true;
										iter = i;
										break;
									}
								}

								if (frameFound)
								{
									_scaleFrameHistory.RemoveRange(0, iter);

									_scaleFrameHistory[0].FrameActualValue = serverValue;
									_scaleFrameHistory[0].FrameValue = Vector3.zero;

									if (_scaleFrameHistory.Count > 1)
									{
										_scaleFrameHistory[1].FramePrevious = _scaleFrameHistory[0].FrameActualValue;
										_scaleFrameHistory[1].UpdateDelta();
									}

									pos = serverValue;
									for (int i = 0; i < _scaleFrameHistory.Count; ++i)
										pos += _scaleFrameHistory[i].FrameValue; //Get the delta distance
								}
							}

							currentValue = pos;
						}
						else
						{
							if ((Time.time - _scaTimeStamp) * 1000 > owningNetworker.PreviousServerPing + 100)
							{
								_scaTimeStamp = Time.time;
								if (Vector3.Distance(currentValue, serverValue) > 0.1f)
								{
									currentValue = serverValue;
									_scaleFrameHistory.Clear();
								}
							}
						}
						break;
				}

				return currentValue;
			}

			#endregion
		}
		#endregion

		/// <summary>
		/// The frame history for authoritative positions
		/// </summary>
		private AuthoritativeFrameHistory frameHistory = new AuthoritativeFrameHistory();

		/// <summary>
		/// The previous authoritative position
		/// </summary>
		private Vector3 previousAuthoritativePosition = Vector3.zero;

		/// <summary>
		/// The previous authoritative rotation
		/// </summary>
		private Vector3 previousAuthoritativeRotation = Vector3.zero;

		/// <summary>
		/// The previous authoritative scale
		/// </summary>
		private Vector3 previousAuthoritativeScale = Vector3.zero;

		/** @name Authorative Input events
		* The following events allow you to drive input on an authorative server.
		*/
		///@{
		/// <summary>
		/// An event that is fired on the server when an input down was 
		/// requested from a client.
		/// </summary>
		/// <remarks>
		/// This event is fired on the server when a client is using InputCheck(), 
		/// the keycode corresponds directly to the key pressed by the 
		/// client. A client needs to use InputCheck() in a method 
		/// such as OwnerUpdate(), in a NMB object:
		/// <code>
		/// protected override void OwnerUpdate(){
		///     InputCheck(KeyCode.RightArrow);
		///     InputCheck(KeyCode.LeftArrow);
		/// }
		/// </code>
		/// inputDownRequest() is then fired on the server, it can be used 
		/// to drive input to a character or system on the server side. 
		/// <code>
		/// //The event has to be subscribed on the server side
		/// protected override void NetworkStart(){
		///     inputDownRequest += InputDown;
		/// }
		/// 
		/// //The event has to be unsubscribed if the NMB object is destroyed
		/// protected override void OnDestroy(){
		///     inputDownRequest -= InputDown;
		/// }
		/// 
		/// //the event can be subscribed to a method and will pass the KeyCode pressed by the client
		/// //this is just an example method, the idea can be extended or used as needed.
		/// private void InputDown(KeyCode keyCode, int frame){
		///     switch(keyCode){
		///         case KeyCode.RightArrow:
		///             //do something when right arrow is held
		///             break;
		///         case KeyCode.LeftArrow:
		///             //do something when left arrow is held
		///             break;
		///     }
		/// }
		/// </code>
		/// inputDownRequest() specifically drives a key being pressed down, 
		/// inputRequest() and inputUpRequest() should be used for keys being 
		/// held or released.
		/// </remarks>
		protected event InputRequest inputDownRequest = null;

		/// <summary>
		/// An event that is fired on the server when an input up was requested from a client
		/// </summary>
		/// <remarks>
		/// This event is fired on the server when a client is using InputCheck(), 
		/// the keycode corresponds directly to the key pressed by the 
		/// client. A client needs to use InputCheck() in a method 
		/// such as OwnerUpdate(), in a NMB object:
		/// <code>
		/// protected override void OwnerUpdate(){
		///     InputCheck(KeyCode.RightArrow);
		///     InputCheck(KeyCode.LeftArrow);
		/// }
		/// </code>
		/// inputUpRequest() is then fired on the server, it can be used 
		/// to drive input to a character or system on the server side. 
		/// <code>
		/// //The event has to be subscribed on the server side
		/// protected override void NetworkStart(){
		///     inputUpRequest += InputUp;
		/// }
		/// 
		/// //The event has to be unsubscribed if the NMB object is destroyed
		/// protected override void OnDestroy(){
		///     inputUpRequest -= InputUp;
		/// }
		/// 
		/// //the event can be subscribed to a method and will pass the KeyCode pressed by the client
		/// //this is just an example method, the idea can be extended or used as needed.
		/// private void InputUp(KeyCode keyCode, int frame){
		///     switch(keyCode){
		///         case KeyCode.RightArrow:
		///             //do something when right arrow is held
		///             break;
		///         case KeyCode.LeftArrow:
		///             //do something when left arrow is held
		///             break;
		///     }
		/// }
		/// </code>
		/// inputUpRequest() specifically drives a key being released, 
		/// inputRequest() and inputDownRequest() should be used for keys being 
		/// held or pressed.
		/// </remarks>
		protected event InputRequest inputUpRequest = null;

		/// <summary>
		/// An event that is fired every update while in between a input 
		/// down request and an input up request
		/// </summary>
		/// <remarks>
		/// This event is fired on the server when a client is using InputCheck(), 
		/// the keycode corresponds directly to the key being pressed by the 
		/// client. A client needs to use InputCheck() in a method 
		/// such as OwnerUpdate(), in a NMB object:
		/// <code>
		/// protected override void OwnerUpdate(){
		///     InputCheck(KeyCode.RightArrow);
		///     InputCheck(KeyCode.LeftArrow);
		/// }
		/// </code>
		/// inputRequest() is then fired on the server, it can be used 
		/// to drive input to a character or system on the server side. 
		/// <code>
		/// //The event has to be subscribed on the server side
		/// protected override void NetworkStart(){
		///     inputRequest += InputHeld;
		/// }
		/// 
		/// //The event has to be unsubscribed if the NMB object is destroyed
		/// protected override void OnDestroy(){
		///     inputRequest -= InputHeld;
		/// }
		/// 
		/// //the event can be subscribed to a method and pass the KeyCode being pressed by the client
		/// //this is just an example method, the idea can be extended or used as needed.
		/// private void InputHeld(KeyCode keyCode){
		///     switch(keyCode){
		///         case KeyCode.RightArrow:
		///             //do something when right arrow is held
		///             break;
		///         case KeyCode.LeftArrow:
		///             //do something when left arrow is held
		///             break;
		///     }
		/// }
		/// </code>
		/// inputRequest() specifically drives a key being held down, 
		/// inputDownRequest() and inputUpRequest() should be used for 
		/// keys being pressed or released.
		/// </remarks>
		protected event FramelessInputRequest inputRequest = null;

		/// <summary>
		/// An event that is fired on the server when a mouse input down was requested from a client
		/// </summary>
		/// <remarks>
		/// For more information see inputDownRequest(), this handles mouse buttons being pressed.
		/// </remarks>
		protected event MouseInputRequest mouseDownRequest = null;

		/// <summary>
		/// An event that is fired on the server when a mouse input up was requested from a client
		/// </summary>
		/// <remarks>
		/// For more information see inputUpRequest(), this handles mouse buttons being released.
		/// </remarks>
		protected event MouseInputRequest mouseUpRequest = null;

		/// <summary>
		/// An event that is fired every update while in between a mouse down request and an mouse up request
		/// </summary>
		/// <remarks>
		/// For more information see inputRequest(), this handles mouse buttons being held.
		/// </remarks>
		protected event FramelessMouseInputRequest mouseRequest = null;
		///@}

		private List<KeyCode> currentKeys = new List<KeyCode>();
		private List<int> mouseIndices = new List<int>();
		private List<int> keyUpBuffer = new List<int>();
		private List<int> mouseUpBuffer = new List<int>();

		/// <summary>
		/// The single behavior that is marked as "isPlayer" that this client owns
		/// </summary>
		public static NetworkedMonoBehavior MyPlayer { get; private set; }

		private NetworkingPlayer serverTargetPlayer = null;

		/// <summary>
		/// A cached object that is constantly updated which is added here to optimize garbage collection
		/// </summary>
		private object valueGetter = null;

		/// <summary>
		/// The primary writing stream for this object to send data across the Network
		/// </summary>
		private NetworkingStream writeStream = new NetworkingStream();

		/// <summary>
		/// Used for lerping from the previous position to the new position
		/// </summary>
		private Vector3 previousPosition = Vector3.zero;

		/// <summary>
		/// Used for lerping from the previous rotation to the new rotation
		/// </summary>
		private Quaternion previousRotation = Quaternion.identity;

		/// <summary>
		/// Used for lerping from the previous scale to the new scale
		/// </summary>
		private Vector3 previousScale = Vector3.zero;

		/// <summary>
		/// The new destination position for this object as described from the Network
		/// </summary>
		private Vector3 targetPosition = Vector3.zero;

		/// <summary>
		/// The new rotation for this object as described from the Network
		/// </summary>
		private Vector3 targetRotation = Vector3.zero;

		/// <summary>
		/// The new scale for this object as described from the Network
		/// </summary>
		private Vector3 targetScale = Vector3.zero;

		/// <summary>
		/// The last frame id that was received
		/// </summary>
		private byte targetFrame = 0;

		/// <summary>
		/// Used for converting the target rotation (Vector3) into a Quaternion
		/// </summary>
		private Quaternion convertedTargetRotation = Quaternion.identity;

		/// <summary>
		/// Determines if this object has already serialized for its new set of values
		/// </summary>
		protected bool HasSerialized { get; private set; }

		private string myUniqueId = "BMS_INTERNAL_Properties_";

		/// <summary>
		/// If true then this object will teleport to where it is as soon as a client connects
		/// </summary>
		[HideInInspector]
		public bool teleportToInitialPositions = true;
		private bool skipInterpolation = false;
		
		/// <summary>
		/// Tells if the data has been initialized for this object across the Network
		/// </summary>
		public bool DataInitialized
		{
			get
			{
				return teleportToInitialPositions == false;
			}
			set
			{
				if (!teleportToInitialPositions || !value)
					return;

				teleportToInitialPositions = !value;
				skipInterpolation = true;
				NetworkInitialized();
			}
		}

		/// <summary>
		/// Called when the data has been initialized across the Network for this object (purpose is to override)
		/// </summary>
		protected virtual void NetworkInitialized() { }

		/// <summary>
		/// Determines if the collider has already been turned off for this object
		/// </summary>
		private bool turnedOffCollider = false;

		/// <summary>
		/// Unity5 Reference to the rigidbody
		/// </summary>
		protected Rigidbody rigidbodyRef = null;

		/// <summary>
		/// Unity5 Reference to the collider
		/// </summary>
		protected Collider colliderRef = null;

		/// <summary>
		/// The main byte buffer for serialization (sending across the Network)
		/// </summary>
		private BMSByte serializedBuffer = new BMSByte();

		private delegate object GetValueDelegate(object obj);
		private delegate void SetValueDelegate(object obj, object value);

		/// <summary>
		/// Locate a NetworkedMonoBehavior with a given ID
		/// </summary>
		/// <param name="id">ID of the NetworkedMonoBehavior</param>
		/// <returns></returns>
		new public static NetworkedMonoBehavior Locate(ulong id)
		{
			if (NetworkedBehaviors.ContainsKey(id))
			{
				if (NetworkedBehaviors[id] == null)
				{
#if UNITY_EDITOR
					Debug.LogError("Null id [" + id + "] found in the behaviors! Did you delete an object instead of calling NetworkDestroy or Transition Scenes with Multiple Network Managers?");
#endif
					NetworkedBehaviors.Remove(id); //Removing the ID as it is invalid.
					return null;
				}
				return (NetworkedMonoBehavior)NetworkedBehaviors[id];
			}

			return null;
		}

		protected override void Reflect()
		{
			if (NetworkingManager.IsOnline)
			{
#if !BARE_METAL
				rigidbodyRef = GetComponent<Rigidbody>();
				colliderRef = GetComponent<Collider>();

				if (colliderRef != null && colliderRef.enabled)
				{
					colliderRef.enabled = false;
					turnedOffCollider = true;
				}
#endif
			}

			base.Reflect();

#if NetFX_CORE
			// Get all of the fields for this class
			List<FieldInfo> fields = this.GetType().GetRuntimeFields().OrderBy(x => x.Name).ToList();
			// Get all of the properties for this class
			List<PropertyInfo> properties = this.GetType().GetRuntimeProperties().OrderBy(x => x.Name).ToList();
#else
			// Get all of the fields for this class
			FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
			// Get all of the properties for this class
			PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
#endif

			// Go throug all of the found fields and find any that are to be synced across the Network
#if NetFX_CORE
			for (int i = 0; i < fields.Count; i++)
#else
			for (int i = 0; i < fields.Length; i++)
#endif
			{
				// If this field has a [NetSync] attribute then add it to the variables to be synced
				NetSync[] NetSyncs = fields[i].GetCustomAttributes(typeof(NetSync), true) as NetSync[];
				if (NetSyncs.Length != 0)
				{
					// Create a temporary reference to this particular object to be used
					FieldInfo field = fields[i];

#if NetFX_CORE
					AddNetworkVariable(() => field.GetValue(this), x => field.SetValue(this, x), NetSyncs[0]);
#else
					GetValueDelegate get = (GetValueDelegate)Delegate.CreateDelegate(typeof(GetValueDelegate), field, "GetValue");
					SetValueDelegate set = (SetValueDelegate)Delegate.CreateDelegate(typeof(SetValueDelegate), field, typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) }));
					AddNetworkVariable(() => get(this), x => set(this, x), NetSyncs[0], NetSyncs[0] is ManualNetSync, NetSyncs[0] is NetSyncToServer);
#endif
				}
			}

			// Go throug all of the found propperties and find any that are to be synced across the Network
#if NetFX_CORE
			for (int i = 0; i < properties.Count; i++)
#else
			for (int i = 0; i < properties.Length; i++)
#endif
			{
				// If this property has a [NetSync] attribute then add it to the variables to be synced
#if NetFX_CORE
				if (properties[i].GetCustomAttribute<NetSync>() != null)
#else
				if (properties[i].GetCustomAttributes(typeof(NetSync), true).Length != 0)
#endif
				{
					// Make sure that the property is read and writeable otherwise there is no reason for it to sync
					if (!properties[i].CanWrite || !properties[i].CanRead)
						throw new NetworkException("Properties marked with the [NetSync] attribute must be readable and writeable");

					// TODO:  Getter and setter should be bound to delegate as fields are

					// Create a temporary reference to this particular object to be used
					PropertyInfo property = properties[i];
					AddNetworkVariable(() => property.GetValue(this, null), x => property.SetValue(this, x, null));
				}
			}

			SetupPreviousTransform();
		}

		private void SetupPreviousTransform()
		{
			previousPosition = transform.position;
			previousRotation = transform.rotation;
			previousScale = transform.localScale;

			targetPosition = transform.position;
			targetRotation = transform.eulerAngles;
			targetScale = transform.localScale;
		}

		/// <summary>
		/// Setup this NetworkedMonoBehavior with the owner of this object along with the Networked ID
		/// </summary>
		/// <param name="owningSocket">The socket that owns this object</param>
		/// <param name="isOwner">Is this the owner of this object</param>
		/// <param name="NetworkId">Network ID of who owns it</param>
		/// <param name="ownerId">The Network identifyer for the player who owns this object</param>
		public override void Setup(NetWorker owningSocket, bool isOwner, ulong NetworkId, ulong ownerId, bool isSceneObject = false)
		{
			base.Setup(owningSocket, isOwner, NetworkId, ownerId, isSceneObject);

#if !BARE_METAL
			bool foundServerAuthority = false, clientPrediction = false;

			foreach (NetworkedMonoBehavior behavior in GetComponents<NetworkedMonoBehavior>())
			{
				if (behavior.serverIsAuthority)
				{
					foundServerAuthority = true;
					clientPrediction = behavior.clientSidePrediction;
					break;
				}
			}

			if (rigidbodyRef != null)
			{
				if ((!OwningNetWorker.IsServer && foundServerAuthority && !clientPrediction) || (!IsOwner && !foundServerAuthority))
				{
					rigidbodyRef.constraints = RigidbodyConstraints.FreezeAll;
					rigidbodyRef.useGravity = false;
				}
			}

			if (isPlayer && OwningNetWorker.IsServer)
				serverTargetPlayer = OwningPlayer;

			if (turnedOffCollider)
			{
				if ((OwningNetWorker.IsServer && foundServerAuthority) || (IsOwner && !foundServerAuthority))
				{
					turnedOffCollider = false;
					colliderRef.enabled = true;
				}
			}
#endif
		}

		[BRPC]
		protected void InitializeObject()
		{
			if (!OwningNetWorker.IsServer)
				return;

			AutoritativeSerialize();
		}

		protected override void NetworkStart()
		{
			base.NetworkStart();

			// Statically assign the player to be this object to have access to it globally
			if (IsOwner && isPlayer && MyPlayer == null)
				MyPlayer = this;

			
			myUniqueId += NetworkedId.ToString();
			
			if (OwningNetWorker.IsServer)
			{
				// This was spawned by the server so the data will be initialized at spawn
				DataInitialized = true;
				return;
			}

			RPC("InitializeObject", NetworkReceivers.Server);

			_authoritativeFrameMutex = new object();
			frameHistory.Setup(serializePosition != SerializeVector3Properties.None, 
								serializeRotation != SerializeVector3Properties.None, 
								serializeScale != SerializeVector3Properties.None);
		}

		private BMSByte manualPropertyBytes = new BMSByte();

		/// <summary>
		/// Used to serialize manual properties across the Network for this object
		/// </summary>
		/// <param name="reliable">Determines if these properties should be reliably sent</param>
		/// <param name="receivers">The receivers for this data</param>
		public void SerializeManualProperties(bool reliable = false, NetworkReceivers receivers = NetworkReceivers.All)
		{
			if (ManualProperties == null || ManualProperties.Count == 0 || !IsSetup)
				return;

			manualPropertyBytes.Clear();
			ObjectMapper.MapBytes(manualPropertyBytes, NetworkedId);

			foreach (NetRef<object> obj in ManualProperties)
			{
				ObjectMapper.MapBytes(manualPropertyBytes, obj.Value);
				obj.Callback(this);
			}

			Networking.WriteCustom(WriteCustomMapping.NetWORKED_MONO_BEHAVIOR_MANUAL_PROPERTIES, OwningNetWorker, manualPropertyBytes, reliable, receivers);
		}

		public void DeserializeManualProperties(NetworkingStream stream)
		{
			foreach (NetRef<object> obj in ManualProperties)
			{
				if (!interpolateFloatingValues || obj.IgnoreLerp)
				{
					if (obj.Assign(ObjectMapper.Map(obj.Value.GetType(), stream)))
						obj.Callback(this, true);
				}
				else
					obj.Lerp(ObjectMapper.Map(obj.Value.GetType(), stream));
			}
		}

		/// <summary>
		/// Add a Network variable to the NetworkedMonoBehavior to use
		/// </summary>
		/// <param name="getter">Variable to get</param>
		/// <param name="setter">Variable to set</param>
		protected void AddNetworkVariable(Func<object> getter, Action<object> setter, NetSync NetSync = null, bool manualProperty = false, bool serverOnly = false)
		{
			if (IsSetup)
				throw new NetworkException(6, "Network variables can not be added after the Awake method of this MonoBehaviour");

			Action callback = null;
			NetworkCallers callers = NetworkCallers.Everyone;
			NetSync.Interpolate useInterpolation = NetSync.Interpolate.True;

			if (NetSync != null)
			{
				if (!string.IsNullOrEmpty(NetSync.method))
				{
#if NetFX_CORE
					callback = () => { this.GetType().GetRuntimeMethod(NetSync.method, null).Invoke(this, new object[] { }); };
#else
					callback = (Action)Delegate.CreateDelegate(typeof(Action), this, NetSync.method);
#endif
				}

				callers = NetSync.callers;
				useInterpolation = NetSync.interpolate;
			}

			if (manualProperty)
				ManualProperties.Add(new NetRef<object>(getter, setter, callback, callers, useInterpolation == NetSync.Interpolate.False));
			else
				Properties.Add(new NetRef<object>(getter, setter, callback, callers, useInterpolation == NetSync.Interpolate.False, serverOnly));
		}

		private void UpdateValues()
		{
			previousPosition = transform.position;
			previousRotation = transform.rotation;
			previousScale = transform.localScale;

			if (Properties == null)
				return;

			foreach (NetRef<object> property in Properties)
				property.Clean();
		}

		[BRPC]
		protected void KeyDownRequest(int keyCode, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (currentKeys.Contains((KeyCode)keyCode))
				return;

			currentKeys.Add((KeyCode)keyCode);

			if (inputDownRequest != null)
				inputDownRequest((KeyCode)keyCode, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + ((KeyCode)keyCode).ToString() + " was requested from the client but no input request inputDownRequest has not been assigned");
#endif

			//if (keyUpBuffer.Contains(keyCode))
			//{
			//	KeyUpRequest(keyCode, frame);
			//	keyUpBuffer.Remove(keyCode);
			//}
		}

		[BRPC]
		protected void KeyUpRequest(int keyCode, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (!currentKeys.Contains((KeyCode)keyCode))
			{
				keyUpBuffer.Add(keyCode);
				return;
			}

			if (inputUpRequest != null)
				inputUpRequest((KeyCode)keyCode, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + ((KeyCode)keyCode).ToString() + " was requested from the client but no input request inputUpRequest has not been assigned");
#endif

			currentKeys.Remove((KeyCode)keyCode);
		}

		[BRPC]
		protected void MouseDownRequest(int index, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (mouseIndices.Contains(index))
				return;

			mouseIndices.Add(index);

			if (mouseDownRequest != null)
				mouseDownRequest(index, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + index.ToString() + " was requested from the client but no mouse input request mouseDownRequest has not been assigned");
#endif

			if (mouseUpBuffer.Contains(index))
			{
				MouseUpRequest(index, frame);
				mouseUpBuffer.Remove(index);
			}
		}

		[BRPC]
		protected void MouseUpRequest(int index, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (!mouseIndices.Contains(index))
			{
				mouseUpBuffer.Add(index);
				return;
			}

			if (mouseUpRequest != null)
				mouseUpRequest(index, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The mouse index " + index.ToString() + " was requested from the client but no mouse input request mouseUpRequest has not been assigned");
#endif

			mouseIndices.Remove(index);
		}

		protected void InputCheck(KeyCode keyCode)
		{
			if (!IsOwner)
				return;

			if (Input.GetKeyDown(keyCode))
			{
				RPC("KeyDownRequest", NetworkReceivers.Server, (int)keyCode, (int)NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					KeyDownRequest((int)keyCode, NetworkingManager.Instance.CurrentFrame);
			}
			
			if (Input.GetKeyUp(keyCode))
			{
				RPC("KeyUpRequest", NetworkReceivers.Server, (int)keyCode, (int)NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					KeyUpRequest((int)keyCode, NetworkingManager.Instance.CurrentFrame);
			}
		}

		protected void MouseCheck(int index)
		{
			if (!IsOwner)
				return;

			if (Input.GetMouseButtonDown(index))
			{
				RPC("MouseDownRequest", NetworkReceivers.Server, index, (int)NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					MouseDownRequest(index, NetworkingManager.Instance.CurrentFrame);
			}
			
			if (Input.GetMouseButtonUp(index))
			{
				RPC("MouseUpRequest", NetworkReceivers.Server, index, (int)NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					MouseUpRequest(index, NetworkingManager.Instance.CurrentFrame);
			}
		}

#if BARE_METAL
		private Quaternion QuaternionFromEuler(Vector3 v)
		{
			float yaw = v.y;
			float pitch = -v.x;
			float roll = -v.z;
			Quaternion quaternion = new Quaternion();
			quaternion.x = (((float)Math.Cos(yaw * 0.5f) * (float)Math.Sin(pitch * 0.5f)) * (float)Math.Cos(roll * 0.5f)) + (((float)Math.Sin(yaw * 0.5f) * (float)Math.Cos(pitch * 0.5f)) * (float)Math.Sin(roll * 0.5f));
			quaternion.y = (((float)Math.Sin(yaw * 0.5f) * (float)Math.Cos(pitch * 0.5f)) * (float)Math.Cos(roll * 0.5f)) - (((float)Math.Cos(yaw * 0.5f) * (float)Math.Sin(pitch * 0.5f)) * (float)Math.Sin(roll * 0.5f));
			quaternion.z = (((float)Math.Cos(yaw * 0.5f) * (float)Math.Cos(pitch * 0.5f)) * (float)Math.Sin(roll * 0.5f)) - (((float)Math.Sin(yaw * 0.5f) * (float)Math.Sin(pitch * 0.5f)) * (float)Math.Cos(roll * 0.5f));
			quaternion.w = (((float)Math.Cos(yaw * 0.5f) * (float)Math.Cos(pitch * 0.5f)) * (float)Math.Cos(roll * 0.5f)) + (((float)Math.Sin(yaw * 0.5f) * (float)Math.Sin(pitch * 0.5f)) * (float)Math.Sin(roll * 0.5f));
			return quaternion;
		}
#endif

		// JM: added support to run Netsync and other Network updates in fixed loop
		protected override void UnityFixedUpdate ()
		{
			// TODO:  Look into this
			if (this == null)
				return;
			
			base.UnityFixedUpdate ();

			if (Networking.UseFixedUpdate) 
			{
				NetworkUpdate ();
			}
		}

		protected override void UnityUpdate()
		{
			// TODO:  Look into this
			if (this == null)
				return;

			base.UnityUpdate();

			if (!Networking.UseFixedUpdate) 
			{
				NetworkUpdate ();
			}
		}

		// JM: logic moved to function to be reused
		private void NetworkUpdate() 
		{
			if (!NetworkingManager.IsOnline)
				return;

			HasSerialized = false;

#if !BARE_METAL
			if (serverIsAuthority && (OwningNetWorker.IsServer || (IsOwner && clientSidePrediction)))
			{
				if (inputRequest != null)
				{
					foreach (KeyCode key in currentKeys)
						inputRequest(key);
				}

				if (mouseRequest != null)
				{
					foreach (int button in mouseIndices)
						mouseRequest(button);
				}
			}
#endif

			if ((Properties == null || Properties.Count == 0) &&
				serializePosition == SerializeVector3Properties.None &&
				serializeRotation == SerializeVector3Properties.None &&
				serializeScale == SerializeVector3Properties.None)
			{
				OwnedUpdate();
				return;
			}

			// TODO:  Examine other parts of removal for this
			//if (isPlayer && OwningNetWorker.IsServer && serverTargetPlayer != null)
			//	serverTargetPlayer.UpdatePosition(transform.position);

			if (OwningNetWorker.IsServer && serverTargetPlayer != null)
				serverTargetPlayer.Position = transform.position;

			if ((OwningNetWorker.IsServer && serverIsAuthority) || (!serverIsAuthority && IsOwner))
			{
				if (NetworkTimeDelay > 0)
				{
#if BARE_METAL
					timeDelayCounter += (float)BareMetal.BareMetalTime.deltaTime;
#else
					timeDelayCounter += Time.deltaTime;
#endif

					if (timeDelayCounter < NetworkTimeDelay)
					{
						OwnedUpdate();
						return;
					}

					timeDelayCounter = 0.0f;
				}

				if (Properties != null)
				{
					foreach (NetRef<object> obj in Properties)
					{
						if (obj.IsDirty)
						{
							DoSerialize();
							break;
						}
					}
				}

				if (!HasSerialized)
				{
					if ((serializePosition != SerializeVector3Properties.None && transform.position != previousPosition) ||
						(serializeRotation != SerializeVector3Properties.None && transform.rotation != previousRotation) ||
						(serializeScale != SerializeVector3Properties.None && transform.localScale != previousScale))
					{
						DoSerialize();
					}
				}
			}
			else
			{
				//if (newData.Ready)
				//	Deserialize(newData);

				if (clientSidePrediction && IsOwner)
				{
					lock (_authoritativeFrameMutex)
					{
						if (serverIsAuthority)
							TrackFrameHistory();
					}

					if (currentKeys.Count != 0 || mouseIndices.Count != 0)
					{
						// TODO:  If the player is too far from targetPosition then fix it
						OwnedUpdate();
						return;
					}
				}
				else
				{
					if (serializePosition != SerializeVector3Properties.None && transform.position != targetPosition)
					{
						if (!serverIsAuthority || !IsOwner || !clientSidePrediction)
						{
#if BARE_METAL
							transform.position = targetPosition;
#else
							if (!lerpPosition || skipInterpolation ||
								Vector3.Distance(transform.position, targetPosition) > authoritativeTeleportSyncDistance)
								transform.position = targetPosition;
							else
							{
								transform.position = Vector3.Lerp(transform.position, targetPosition, lerpT);

								if (Vector3.Distance(transform.position, targetPosition) <= lerpStopOffset)
									transform.position = targetPosition;
							}
#endif
						}
					}
				}

				if (serializeRotation != SerializeVector3Properties.None && transform.eulerAngles != targetRotation)
				{
#if BARE_METAL
					transform.eulerAngles = targetRotation;
					//convertedTargetRotation = QuaternionFromEuler(targetRotation);
#else
					convertedTargetRotation = Quaternion.Euler(targetRotation);
#endif

					if (!serverIsAuthority || !IsOwner || !clientSidePrediction || (Quaternion.Angle(transform.rotation, convertedTargetRotation) > authoritativeSyncRotation))
					{
#if BARE_METAL
						transform.rotation = convertedTargetRotation;
#else
						if (!lerpRotation || skipInterpolation)
							transform.rotation = convertedTargetRotation;
						else
						{
							transform.rotation = Quaternion.Slerp(transform.rotation, convertedTargetRotation, lerpT);

							if (Quaternion.Angle(transform.rotation, convertedTargetRotation) <= lerpAngleStopOffset)
								transform.rotation = convertedTargetRotation;
						}
#endif
					}
				}

				if (serializeScale != SerializeVector3Properties.None && transform.localScale != targetScale)
				{
#if BARE_METAL
					transform.localScale = targetScale;
#else
					if (!lerpScale || skipInterpolation)
						transform.localScale = targetScale;
					else
					{
						transform.localScale = Vector3.Lerp(transform.localScale, targetScale, lerpT);

						if (Vector3.Distance(transform.localScale, targetScale) <= lerpStopOffset)
							transform.localScale = targetScale;
					}
#endif
				}

				foreach (NetRef<object> obj in Properties)
				{
					if (!obj.Lerping)
						continue;

					UpdateRemoteNetRef(obj);
				}

				foreach (NetRef<object> obj in ManualProperties)
				{
					if (!obj.Lerping)
						continue;

					UpdateRemoteNetRef(obj);
				}
			}

			OwnedUpdate();
		}

		private void TrackFrameHistory()
		{
			if (OwningNetWorker.IsServer)
				return;
			
			byte serverFrame = (byte)(targetFrame - NetworkingManager.Instance.GetFrameCountFromTime(OwningNetWorker.PreviousServerPing));
			//Idea is to store the frames as they come down and
			Vector3 nextPos = frameHistory.UpdateFrame(OwningNetWorker, AuthoritativeFrameType.Position, transform.position, serverFrame, targetPosition, ref previousAuthoritativePosition);
			transform.position = nextPos;

			Vector3 nextRot = frameHistory.UpdateFrame(OwningNetWorker, AuthoritativeFrameType.Rotation, transform.rotation.eulerAngles, serverFrame, targetRotation, ref previousAuthoritativeRotation);
			transform.rotation = Quaternion.Euler(nextRot);

			Vector3 nextScale = frameHistory.UpdateFrame(OwningNetWorker, AuthoritativeFrameType.Scale, transform.localScale, serverFrame, targetScale, ref previousAuthoritativeScale);
			transform.localScale = nextScale;
		}

		private void UpdateRemoteNetRef(NetRef<object> obj)
		{
			valueGetter = obj.Value;

			bool finalize = false;

			if (valueGetter is float)
			{
				obj.Value = (float)Mathf.Lerp((float)valueGetter, (float)obj.LerpTo, lerpT);

				if (Math.Abs((float)obj.LerpTo - (float)valueGetter) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is double)
			{
				obj.Value = BeardedMath.Lerp((double)valueGetter, (double)obj.LerpTo, lerpT);

				if (Math.Abs((double)obj.LerpTo - (double)valueGetter) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector2)
			{
				obj.Value = Vector2.Lerp((Vector2)valueGetter, (Vector2)obj.LerpTo, lerpT);

				if (Vector2.Distance((Vector2)valueGetter, (Vector2)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector3)
			{
				obj.Value = Vector3.Lerp((Vector3)valueGetter, (Vector3)obj.LerpTo, lerpT);

				if (Vector3.Distance((Vector3)valueGetter, (Vector3)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector4)
			{
				obj.Value = Vector4.Lerp((Vector4)valueGetter, (Vector4)obj.LerpTo, lerpT);

				if (Vector4.Distance((Vector4)valueGetter, (Vector4)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Quaternion)
			{
				obj.Value = Quaternion.Slerp((Quaternion)valueGetter, (Quaternion)obj.LerpTo, lerpT);

				if (Quaternion.Angle((Quaternion)valueGetter, (Quaternion)obj.LerpTo) <= lerpAngleStopOffset)
					finalize = true;
			}
			else
				finalize = true;

			if (finalize)
			{
				obj.AssignToLerp();
				obj.Callback(this, true);
			}
		}

		private void OwnedUpdate()
		{
			if (newData.Ready)
			{
				newData.Reset();
#if !BARE_METAL
				if (turnedOffCollider) { turnedOffCollider = false; colliderRef.enabled = true; }
#endif
				if (skipInterpolation) skipInterpolation = false;
			}
		}

		public void AutoritativeSerialize()
		{
			if (OwningNetWorker.IsServer)
				RPC("AuthoritativeInitialize", NetworkReceivers.All, Serialized());
		}

		[BRPC]
		protected void AuthoritativeInitialize(BMSByte data)
		{
			newData.Reset();
			newData.Bytes.Clone(data);
			newData.ManualReady();

			try
			{
				Deserialize(newData);
			}
			catch (Exception ex)
			{
				if (ex is IndexOutOfRangeException)
					Debug.LogError("The object " + gameObject.name + "'s serialization patterns do not match");
				else
					throw ex;
			}

			DataInitialized = true;
		}

		private void DoSerialize()
		{
			serializedBuffer = Serialized();

#if !UNITY_WEBGL
			if (OwningNetWorker is CrossPlatformUDP)
			{
				writeStream.SetProtocolType(Networking.ProtocolType.UDP);
				Networking.WriteUDP(OwningNetWorker, myUniqueId, writeStream.Prepare(OwningNetWorker, NetworkingStream.IdentifierType.NetworkedBehavior, this.NetworkedId, serializedBuffer, (OwningNetWorker.ProximityBasedMessaging ? NetworkReceivers.OthersProximity : NetworkReceivers.Others)), isReliable);
			}
			else
			{
#endif
				writeStream.SetProtocolType(Networking.ProtocolType.TCP);
				Networking.WriteTCP(OwningNetWorker, writeStream.Prepare(OwningNetWorker, NetworkingStream.IdentifierType.NetworkedBehavior, this.NetworkedId, serializedBuffer, OwningNetWorker.ProximityBasedMessaging ? NetworkReceivers.OthersProximity : NetworkReceivers.Others));
#if !UNITY_WEBGL
		}
#endif

			HasSerialized = true;
			UpdateValues();
		}

		private void PrepareNextSerializedTransform(SerializeVector3Properties type, Vector3 value)
		{
			switch (type)
			{
				case SerializeVector3Properties.X:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					break;
				case SerializeVector3Properties.Y:
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					break;
				case SerializeVector3Properties.Z:
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.XY:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					break;
				case SerializeVector3Properties.XZ:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.YZ:
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.XYZ:
					ObjectMapper.MapBytes(serializedBuffer, value);
					break;
				default:
					return;
			}
		}

		/// <summary>
		/// Get the serialzed version of this NetworkedMonoBehavior
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Used to serialize the NetworkedMonoBehavior into a BMSByte which is a format that can be sent across the Network.
		/// </remarks>
		public override BMSByte Serialized()
		{
			serializedBuffer.Clear();

			PrepareNextSerializedTransform(serializePosition, transform.position);

			// Sending rotation across the Network as a Vector3 instead of Vector4 to save bandwidth
			PrepareNextSerializedTransform(serializeRotation, transform.eulerAngles);
			PrepareNextSerializedTransform(serializeScale, transform.localScale);

			if (Properties != null)
			{
				foreach (NetRef<object> obj in Properties)
				{
					ObjectMapper.MapBytes(serializedBuffer, obj.Value);
					obj.Callback(this);
				}
			}

			return serializedBuffer;
		}

		private NetworkingStream newData = new NetworkingStream();
		/// <summary>
		/// Prepare this to be Deserialized if it is not the owner
		/// </summary>
		/// <param name="stream">Stream of data to use</param>
		public void PrepareDeserialize(NetworkingStream stream)
		{
			if ((IsOwner && !serverIsAuthority) || (OwningNetWorker.IsServer && serverIsAuthority))
				return;

			newData.Reset();
			newData.Bytes.Clone(stream.Bytes);
			newData.ManualReady(stream.FrameIndex);

			Deserialize(newData);
		}

		private Vector3 GetNextSerializedTransform(SerializeVector3Properties type, NetworkingStream stream, Vector3 standard)
		{
			switch (type)
			{
				case SerializeVector3Properties.X:
					standard.x = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.Y:
					standard.y = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.Z:
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XY:
					standard.x = ObjectMapper.Map<float>(stream);
					standard.y = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XZ:
					standard.x = ObjectMapper.Map<float>(stream);
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.YZ:
					standard.y = ObjectMapper.Map<float>(stream);
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XYZ:
					return ObjectMapper.Map<Vector3>(stream);
			}

			return standard;
		}

		/// <summary>
		/// Only Deserialize the stream of data that is not the owner
		/// </summary>
		/// <param name="stream">Stream of data to use</param>
		/// <remarks>
		/// This deserializes a NetworkedMonoBehavior from a NetworkingStream, the NetworkingStream contains a BMSByte which is what actually stores
		/// the NMB. See Serialize() for more...
		/// </remarks>
		public override void Deserialize(NetworkingStream stream)
		{
			if ((IsOwner && !serverIsAuthority) || (OwningNetWorker.IsServer && serverIsAuthority))
				return;

			stream.ResetByteReadIndex();

			targetFrame = stream.FrameIndex;

			targetPosition = GetNextSerializedTransform(serializePosition, stream, targetPosition);
			targetRotation = GetNextSerializedTransform(serializeRotation, stream, targetRotation);
			targetScale = GetNextSerializedTransform(serializeScale, stream, targetScale);

			if (Properties == null)
				return;

			foreach (NetRef<object> obj in Properties)
			{
				// Only allow the server to replicate this variable across the Network
				if (obj.serverOnly && !OwningNetWorker.IsServer)
				{
					ObjectMapper.Map(obj.Value.GetType(), stream);
					continue;
				}

				if (!interpolateFloatingValues || obj.IgnoreLerp || !DataInitialized)
				{
					if (obj.Assign(ObjectMapper.Map(obj.Value.GetType(), stream)))
						obj.Callback(this, true);
				}
				else
					obj.Lerp(ObjectMapper.Map(obj.Value.GetType(), stream));
			}
		}

		public override void Disconnect()
		{
			base.Disconnect();

			if (isPlayer && !destroyOnDisconnect)
				Networking.Destroy(this);
		}

		[BRPC]
		protected virtual void EnteredProximity()
		{
			if (enteredProximityInvoker != null)
				enteredProximityInvoker(MyPlayer, this);
		}

		[BRPC]
		protected virtual void ExitedProximity()
		{
			if (exitedProximityInvoker != null)
				exitedProximityInvoker(MyPlayer, this);
		}

		public void ProximityInCheck(NetworkedMonoBehavior other)
		{
			if (!OwningNetWorker.IsServer)
				return;

			if (proximityBehaviors.Contains(other.NetworkedId))
				return;

			proximityBehaviors.Add(other.NetworkedId);
			other.proximityBehaviors.Add(this.NetworkedId);

			if (IsServerOwner)
				Unity.MainThreadManager.Run(other.EnteredProximity);
			else
				other.AuthoritativeRPC("EnteredProximity", OwningNetWorker, OwningPlayer, false);

			if (other.IsServerOwner)
				Unity.MainThreadManager.Run(EnteredProximity);
			else
				AuthoritativeRPC("EnteredProximity", OwningNetWorker, other.OwningPlayer, false);
		}

		public void ProximityOutCheck(NetworkedMonoBehavior other)
		{
			if (!OwningNetWorker.IsServer)
				return;

			if (!proximityBehaviors.Contains(other.NetworkedId))
				return;

			proximityBehaviors.Remove(other.NetworkedId);
			other.proximityBehaviors.Remove(this.NetworkedId);

			if (IsServerOwner)
				Unity.MainThreadManager.Run(other.ExitedProximity);
			else
				other.AuthoritativeRPC("ExitedProximity", OwningNetWorker, OwningPlayer, false);

			if (other.IsServerOwner)
				Unity.MainThreadManager.Run(ExitedProximity);
			else
				AuthoritativeRPC("ExitedProximity", OwningNetWorker, other.OwningPlayer, false);
		}

		protected override void NetworkDisconnect()
		{
			base.NetworkDisconnect();
			MyPlayer = null;
		}
	}
}
