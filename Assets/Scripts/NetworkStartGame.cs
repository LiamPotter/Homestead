using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
using BeardedManStudios.Network.Unity;
#if !UNITY_WEBGL
using System.Net;
#endif
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkStartGame : MonoBehaviour {

    public string host = "127.0.0.1";
    public InputField IP_Field;
    public ushort port = 15937;
    public Networking.TransportationProtocolType protocolType = Networking.TransportationProtocolType.UDP;
    public int playerCount = 31;
    public string sceneName = "Default";
    private NetWorker socket = null;
    public int NetworkLatencySimulationTime = 0;
    public float packetDropSimulationChance = 0.0f;
    private bool ready = false,isBusyFindingLan=false;
    public bool useNatHolePunching,proximityBasedUpdates,showBandwidth;
    public float proximityDistance;
    public string masterServerIp;
#if UNITY_WINRT && !UNITYEDITOR
    private bool isWinRT = true;
#else
#endif
    private bool isWinRT = false;

    void Start ()
    {
        IP_Field.text = host;
        Networking.InitializeFirewallCheck(port);
	}

    public void StartServer()
    {
        // Create a host connection
        socket = Networking.Host((ushort)port, protocolType, playerCount, isWinRT, useNat: useNatHolePunching);

        if (socket == null)
        {
            Debug.LogError("Socket failed to initialize");
            return;
        }
        socket.TrackBandwidth = showBandwidth;

#if !NetFX_CORE && !UNITY_WEBGL
        if (socket is CrossPlatformUDP)
        {
            ((CrossPlatformUDP)socket).packetDropSimulationChance = packetDropSimulationChance;
            ((CrossPlatformUDP)socket).NetworkLatencySimulationTime = NetworkLatencySimulationTime;
        }
#endif

        if (!string.IsNullOrEmpty(masterServerIp))
        {
            socket.connected += delegate ()
            {
                ForgeMasterServer.RegisterServer(masterServerIp, (ushort)port, playerCount, "My Awesome Game Name", "Deathmatch", "Thank you for your support!", sceneName: sceneName);
            };

            socket.playerConnected += UpdatePlayerCount;
            socket.playerDisconnected += UpdatePlayerCount;
        }

        Go();
    }
    public void StartClient()
    {
        host = IP_Field.text;
        Debug.Log("Doing StartClient");
        if (string.IsNullOrEmpty(host.Trim()))
        {
            Debug.Log("No ip address provided to connect to");
            return;
        }

        socket = Networking.Connect(host, (ushort)port, protocolType, isWinRT, useNatHolePunching);

        if (!socket.Connected)
        {
            socket.ConnectTimeout = 5000;
            socket.connectTimeout += ConnectTimeout;
        }

#if !NetFX_CORE && !UNITY_WEBGL
        if (socket is CrossPlatformUDP)
            ((CrossPlatformUDP)socket).NetworkLatencySimulationTime = NetworkLatencySimulationTime;
#endif

        Go();
    }

	void Update ()
    {
        if(socket!=null)Debug.Log("Is socket connected?: " + socket.Connected);
	    if(socket!=null&&socket.Connected)
        {
            Networking.SetPrimarySocket(socket);
            LoadScene();
        }	
	}
    private void UpdatePlayerCount(NetworkingPlayer player)
    {
        ForgeMasterServer.UpdateServer(masterServerIp, socket.Port, socket.Players.Count);
    }
#if !UNITY_WEBGL
    public void StartClientLan()
    {
        Debug.Log("Doing StartClientLan");
        if (isBusyFindingLan)
            return;

        isBusyFindingLan = true;
        Networking.lanEndPointFound += FoundEndpoint;
        Networking.LanDiscovery((ushort)port, 5000, protocolType, isWinRT);
    }
    private void FoundEndpoint(IPEndPoint endpoint)
    {
        Debug.Log("Doing foundEndpoint");
        isBusyFindingLan = false;
        Networking.lanEndPointFound -= FoundEndpoint;
        if (endpoint == null)
        {
            Debug.Log("No server found on LAN");
            return;
        }

        string ipAddress = string.Empty;
        ushort targetPort = 0;

#if !NetFX_CORE
        ipAddress = endpoint.Address.ToString();
        targetPort = (ushort)endpoint.Port;
#else
						ipAddress = endpoint.ipAddress;
						targetPort = (ushort)endpoint.port;
#endif
   
    socket = Networking.Connect(ipAddress, targetPort, protocolType, isWinRT, useNatHolePunching);
    Go();
    }
#endif
    private void RemoveSocketReference()
    {
        Debug.Log("Doing removeSocketReference");
        socket = null;
        Networking.NetworkReset -= RemoveSocketReference;
    }
    private void ConnectTimeout()
    {
        Debug.LogWarning("Connection could not be established");
        Networking.Disconnect();
    }
    private void Go()
    {
        Debug.Log("Doing Go");
        Networking.NetworkReset += RemoveSocketReference;
        if (proximityBasedUpdates)
            socket.MakeProximityBased(proximityDistance);
        if (socket.Connected)
            LoadScene();
        else
            socket.connected += LoadScene;
    }
    public void LoadScene()
    {
       SceneManager.LoadScene(sceneName);
    }
    public void OnIPChange()
    {
        host = IP_Field.text;
    }
}
