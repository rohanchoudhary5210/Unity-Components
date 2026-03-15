using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Define custom request/response to include our game specific data
public struct CornholeServerRequest : NetworkMessage { }

public struct CornholeServerResponse : NetworkMessage 
{
    public long serverId;
    public System.Uri uri;
    public System.Net.IPEndPoint EndPoint { get; set; }
    public string roomName;
}

// [System.Serializable]
// public class DiscoveryEvent : UnityEvent<CornholeServerResponse> { }

public class CornholeNetworkDiscovery : NetworkDiscoveryBase<CornholeServerRequest, CornholeServerResponse>
{
    public static CornholeNetworkDiscovery Instance { get; private set; }

    // public DiscoveryEvent OnServerFound = new DiscoveryEvent();

    private Dictionary<long, CornholeServerResponse> discoveredServers = new Dictionary<long, CornholeServerResponse>();
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject wifiManager;
    private AndroidJavaObject multicastLock;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        SetupAndroidMulticastLock();
    }

    void OnDestroy()
    {
        ReleaseAndroidMulticastLock();
    }

    protected override CornholeServerRequest GetRequest() => new CornholeServerRequest();

    protected override CornholeServerResponse ProcessRequest(CornholeServerRequest request, System.Net.IPEndPoint endpoint)
    {
        try
        {
            return new CornholeServerResponse
            {
                serverId = ServerId,
                uri = transport.ServerUri(),
                roomName = CornholeNetworkManager.Instance.currentRoomName
            };
        }
        catch (System.NotImplementedException)
        {
            Debug.LogError($"Transport {transport} does not support network discovery");
            throw;
        }
    }

    protected override void ProcessResponse(CornholeServerResponse response, System.Net.IPEndPoint endpoint)
    {
        // Ignore our own server broadcast on host to prevent self-join buttons.
        if (NetworkServer.active && response.serverId == ServerId)
        {
            return;
        }

        response.EndPoint = endpoint;

        System.UriBuilder realUri = new System.UriBuilder(response.uri)
        {
            Host = response.EndPoint.Address.ToString()
        };
        response.uri = realUri.Uri;

        if (!discoveredServers.ContainsKey(response.serverId))
        {
            discoveredServers[response.serverId] = response;
            OnServerFound.Invoke(response);
            
            // Add to Network Manager's list
            if (CornholeNetworkManager.Instance != null)
            {
                CornholeNetworkManager.Instance.discoveredServers[response.serverId] = response;
            }
            
            Debug.Log($"Found server at {endpoint.Address}");
        }
    }

    public void StartSearching()
    {
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            Debug.LogWarning("Cannot search for games while hosting or already connected.");
            return;
        }

        AcquireAndroidMulticastLock();
        discoveredServers.Clear();
        StartDiscovery();
    }

    public void StartAdvertising()
    {
        StopDiscovery();
        AcquireAndroidMulticastLock();
        AdvertiseServer();
    }

    private void SetupAndroidMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
                if (wifiManager != null)
                {
                    multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "cornhole_discovery_lock");
                    multicastLock.Call("setReferenceCounted", false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to setup Android multicast lock: {ex.Message}");
        }
#endif
    }

    private void AcquireAndroidMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (multicastLock != null)
            {
                bool held = multicastLock.Call<bool>("isHeld");
                if (!held)
                {
                    multicastLock.Call("acquire");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to acquire Android multicast lock: {ex.Message}");
        }
#endif
    }

    private void ReleaseAndroidMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (multicastLock != null)
            {
                bool held = multicastLock.Call<bool>("isHeld");
                if (held)
                {
                    multicastLock.Call("release");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to release Android multicast lock: {ex.Message}");
        }
#endif
    }
}
