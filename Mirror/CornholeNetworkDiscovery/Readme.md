# Cornhole Network Discovery (Mirror + Unity)

This document explains how the **LAN server discovery system** works for the Cornhole game using **Mirror Networking** in Unity. It allows players on the same Wi-Fi network to automatically find and join hosted games.

---

# 1. Namespaces (The Toolboxes)

These namespaces provide the required networking, collections, and Unity functionality.

```csharp
using Mirror;                     // Core Mirror networking library
using Mirror.Discovery;           // LAN server discovery tools
using System.Collections.Generic; // Lists and Dictionaries
using UnityEngine;                // Standard Unity features
using UnityEngine.Events;         // UI event system
```

**Purpose**

* `Mirror` → Core multiplayer networking.
* `Mirror.Discovery` → Handles LAN server discovery using UDP multicast.
* `System.Collections.Generic` → Used for data storage like dictionaries.
* `UnityEngine` → Base Unity functionality.
* `UnityEngine.Events` → Allows UI updates when servers are discovered.

---

# 2. Data Structures (The “Envelopes”)

Before sending network data, you define **message structures** that act as packets.

## Request Message — “Hello?”

```csharp
public struct CornholeServerRequest : NetworkMessage { }
```

**Purpose**

* Sent by a client when searching for available games.
* It is intentionally **empty** because the client only needs to ask:

  > “Is there any server available?”

---

## Response Message — “I'm Here!”

```csharp
public struct CornholeServerResponse : NetworkMessage
{
    public long serverId;                       // Unique identifier for server
    public System.Uri uri;                      // Connection URI (e.g. kcp://192.168.1.5:7777)
    public System.Net.IPEndPoint EndPoint { get; set; } // Actual IP + Port
    public string roomName;                     // Server name shown in UI
}
```

**Fields**

| Field      | Description                                   |
| ---------- | --------------------------------------------- |
| `serverId` | Prevents duplicate entries of the same server |
| `uri`      | Connection address used by Mirror             |
| `EndPoint` | Actual IP address and port                    |
| `roomName` | Friendly name displayed in the UI             |

---

# 3. Main Class & Singleton

```csharp
public class CornholeNetworkDiscovery : 
NetworkDiscoveryBase<CornholeServerRequest, CornholeServerResponse>
{
    public static CornholeNetworkDiscovery Instance { get; private set; }
}
```

### Inheritance

The class inherits from:

```
NetworkDiscoveryBase<Request, Response>
```

Mirror handles the complicated parts such as:

* UDP messaging
* LAN broadcasting
* packet serialization

### Singleton

The singleton allows any script to access discovery easily:

```csharp
CornholeNetworkDiscovery.Instance.StartSearching();
```

Example usage from a **Main Menu script**.

---

# 4. The Dictionary (Server Memory)

```csharp
private Dictionary<long, CornholeServerResponse> discoveredServers
    = new Dictionary<long, CornholeServerResponse>();
```

This dictionary stores **all discovered servers**.

**Key**

```
serverId
```

**Value**

```
CornholeServerResponse
```

### Why use a Dictionary?

Servers repeatedly broadcast their presence.

Example:

```
Server -> shout
Server -> shout
Server -> shout
```

Without the dictionary, the UI would show the same server **many times**.

The dictionary ensures **only one entry per server**.

---

# 5. Android Multicast Lock (Battery Optimization Fix)

```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject wifiManager;
    private AndroidJavaObject multicastLock;
#endif
```

### The Problem

Android devices often **block multicast packets** to save battery.

This prevents LAN discovery from working.

### The Solution

A **Multicast Lock** tells Android:

> “Do not block these packets — my app requires them.”

This ensures reliable LAN discovery across Android devices.

---

# 6. The Handshake Logic (Discovery Flow)

The discovery process works as a **3-step handshake**.

---

## Step 1 — Client Sends Request

```csharp
protected override CornholeServerRequest GetRequest() 
    => new CornholeServerRequest();
```

When the client starts searching:

```
Client → Broadcasts request
```

Equivalent message:

```
"Hello! Any servers available?"
```

---

## Step 2 — Server Processes Request

```csharp
protected override CornholeServerResponse ProcessRequest(
    CornholeServerRequest request,
    System.Net.IPEndPoint endpoint)
{
    return new CornholeServerResponse
    {
        serverId = ServerId,
        uri = transport.ServerUri(),
        roomName = CornholeNetworkManager.Instance.currentRoomName
    };
}
```

When the server receives the request:

1. It prepares a response packet.
2. Adds server information.
3. Sends it back to the requesting client.

The response includes:

* Unique server ID
* Connection URI
* Room name

---

## Step 3 — Client Processes Response

```csharp
protected override void ProcessResponse(
    CornholeServerResponse response,
    System.Net.IPEndPoint endpoint)
{
    if (NetworkServer.active && response.serverId == ServerId) return;

    System.UriBuilder realUri = new System.UriBuilder(response.uri)
    {
        Host = endpoint.Address.ToString()
    };

    response.uri = realUri.Uri;

    if (!discoveredServers.ContainsKey(response.serverId))
    {
        discoveredServers[response.serverId] = response;
        OnServerFound.Invoke(response);
    }
}
```

### What happens here?

1. **Ignore self-host**

If the discovered server is your own host, ignore it.

2. **Fix localhost address**

Servers often report:

```
localhost
```

But the client needs the **actual LAN IP**.

So the code replaces it with:

```
endpoint.Address
```

3. **Add server to discovered list**

If the server is new:

* Add it to `discoveredServers`
* Trigger UI update

```
OnServerFound.Invoke(response);
```

The UI can then create a **Join button**.

---

# 7. Public Controls (UI Buttons)

These methods are typically triggered from **menu buttons**.

---

## Start Searching (Join Game)

```
StartSearching()
```

Actions:

1. Clears previous server list.
2. Requests Android multicast lock.
3. Starts listening for LAN broadcasts.

Result:

```
Client waits for server announcements.
```

---

## Start Advertising (Host Game)

```
StartAdvertising()
```

Actions:

1. Stops searching.
2. Starts broadcasting server presence.

Result:

```
Server repeatedly announces:
"I am hosting a game!"
```

---

# 8. Android Deep Integration (JNI)

Methods like:

```
SetupAndroidMulticastLock()
AcquireMulticastLock()
ReleaseMulticastLock()
```

use **JNI (Java Native Interface)**.

This allows C# code to call Android’s **native Java APIs**.

Purpose:

* Access Android Wi-Fi system services
* Enable multicast traffic
* Ensure compatibility across Android devices

---

# Complete Flow Summary

```
Host presses "Host"
    ↓
StartAdvertising()
    ↓
Server sends UDP multicast announcements

Client presses "Join"
    ↓
StartSearching()
    ↓
Client listens for announcements

Network delivers broadcast
    ↓
Client receives response
    ↓
ProcessResponse()
    ↓
Server appears in UI list

Player clicks Join
    ↓
Client connects using server IP
```

---

# Result

Players on the same Wi-Fi network can:

* Automatically discover servers
* See available game rooms
* Join games without manually entering IP addresses

This creates a **smooth LAN multiplayer experience** using **Mirror Networking**.
