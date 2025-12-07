using System;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Game;

public class ServerManager : MonoBehaviour
{
    public static ServerManager instance = null;
    
    private Thread listenerThread;
    public LobbyUI lobbyUI;
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private UdpClient udpListener;

    private List<ClientConnection> clients = new List<ClientConnection>();
    
    [SerializeField] private float pingInterval = 0.5f;
    [SerializeField] private float pingTimeout = 3.0f;
    [SerializeField] private short maxRetries = 3;
    
    private byte[] pingMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PING\",\"msgData\":\"host\"}");
    private byte[] pongMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PONG\",\"msgData\":\"host\"}");
    
    void Start()
    {
        listenerThread = new Thread(ListenForUdpPackets);
        listenerThread.IsBackground = true;
        listenerThread.Start();
        
        instance = this;

        UpdatePlayerListForAll();
    }

    void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue().Invoke();
            }
        }
    }

    private void ListenForUdpPackets()
    {
        try
        {
            udpListener = new UdpClient(NetworkGlobals.GAME_PORT_UDP);
            EnqueueToMainThread(() => Debug.Log($"Servidor UDP escuchando en el puerto {NetworkGlobals.GAME_PORT_UDP}"));

            ClientConnection connection = null;
            while (true)
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.Receive(ref clientEndPoint);
                
                lock(clients)
                {
                    connection = clients.FirstOrDefault(c => c.endPoint.Equals(clientEndPoint));
                    if (connection == null)
                    {
                        connection = new ClientConnection(clientEndPoint);
                        clients.Add(connection);

                        EnqueueToMainThread(() => OnClientConnected(connection));
                    }
                }

                NetMessage msg = null;
                try
                {
                    msg = NetMessage.FromBytes(data);
                }
                catch (Exception e)
                {
                    EnqueueToMainThread(() => Debug.LogError("<color=red>SERVER [RECV]:</color> Error deserializing client message: " + e.Message));
                }
                
                if (msg != null)
                {
                    EnqueueToMainThread(()=> OnMessageReceived(msg, connection));
                    ProcessMessage(msg, connection);
                }
                else
                    EnqueueToMainThread(() => Debug.LogError("<color=red>SERVER [RECV]:</color> Error deserializing client message: Message is null"));

            }
        }
        catch (System.Exception e) { EnqueueToMainThread(() => Debug.LogError("<color=red>SERVER [RECV]:</color> Unhandled error in UDP listener: " + e.Message)); }
    }
    
    private void ProcessMessage(NetMessage msg, ClientConnection sender)
    {
        Debug.Log("<color=yellow>SERVER [RECV]:</color> " + msg);

        if (msg.msgType == "JOIN")
        {
            string senderName = msg.msgData;

            lock (clients)
            {
                sender.name = senderName;
                EnqueueToMainThread(() =>
                {
                    lock (clients) sender.lastReceived = Time.time;
                });
            }

            HandlePlayerJoin(sender);
        }
        //TODO leave the game manually
        else if (msg.msgType == "LEAVE")
        {
            
        }
        else if (msg.msgType == "PING")
        {
            Debug.Log($"<color=cyan>SERVER [PING]:</color> Ping received from {sender.name}. Sending pong response");
            SendToClient(pongMsg, sender);
        }
        else if (msg.msgType == "PONG")
        {
            Debug.Log($"<color=cyan>SERVER [PONG]:</color> Ping response received from {sender.name}");
        }
        else if (msg.msgType == "CHAT")
        {
            string formattedMessage = $"{sender.name}: {msg.msgData}";
            EnqueueToMainThread(() => lobbyUI.AddChatMessage(formattedMessage));

            BroadcastMessage(new NetMessage("CHAT", formattedMessage));
        }
        else if (msg.msgType == "UI_Update" || msg.msgType == "Accion1_Clicked")
        {
            BroadcastMessage(msg);
        }
        else if (msg.msgType == NetworkGlobals.MODULE_MANAGER_EVENT_KEY)
        {
            EnqueueToMainThread(()=> ModuleManager.ServerProcessReceive(msg));
        }
        else
        {
            EnqueueToMainThread(()=> Debug.LogWarning($"Comando desconocido: {msg.msgType}\nDatos:\n{msg.msgData}"));
        }
    }
    
    public void HandleHostMessage(string message)
    {
        string hostName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Host");
        string formattedMessage = $"{hostName}: {message}";
        lobbyUI.AddChatMessage(formattedMessage);
        

        BroadcastMessage(new NetMessage("CHAT", formattedMessage));
    }

    private void HandlePlayerJoin(ClientConnection cc)
    {
        EnqueueToMainThread(() => {
            string joinMessage = $"{cc.name} se ha unido a la sala.";
            lobbyUI.AddChatMessage(joinMessage);
            

            BroadcastMessage(new NetMessage("CHAT", joinMessage));
            UpdatePlayerListForAll();
        });
    }

    private void HandlePlayerDisconnect(ClientConnection cc)
    {
        string leaveMessage = $"{cc.name} ha abandonado la sala.";
        lobbyUI.AddChatMessage(leaveMessage);
        
        cc.connectionToken.Cancel();
        
        lock (clients)
            clients.Remove(cc);

        BroadcastMessage(new NetMessage("CHAT", leaveMessage));
    }

    public void SendToClient(NetMessage msg, ClientConnection cc)
    {
        byte[] data = msg.ToBytes();
        SendToClient(data, cc);
    }

    public void SendToClient(byte[] bytes, ClientConnection cc)
    {
        udpListener.Send(bytes, bytes.Length, cc.endPoint);
        EnqueueToMainThread(()=> OnSendMessageToClient(cc));
    }
    
    public void BroadcastMessage(NetMessage msg)
    {
        byte[] data =  msg.ToBytes();
        
        lock (clients)
        {
            foreach (var clientConnection in clients)
            {
                try
                {
                    SendToClient(data, clientConnection);
                }
                catch (Exception e)
                {
                    EnqueueToMainThread(() => Debug.LogWarning($"Error al broadcastear a cliente UDP {clientConnection.name}: {e.Message}"));
                }
            }
        }
        
    }

    private void UpdatePlayerListForAll()
    {
        EnqueueToMainThread(() => {
            string hostName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Host");
            List<string> playerNames;
            lock(clients)
            {
                playerNames = clients.Select(c => c.name).ToList();
            }

            // TODO remove this line when host is also treated as client
            playerNames.Insert(0, hostName);
            
            lobbyUI.UpdatePlayerList(playerNames);
            


            string listData = string.Join(",", playerNames);
            BroadcastMessage(new NetMessage("PLAYERLIST", listData));
        });
    }

    private void EnqueueToMainThread(System.Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    // Must be called on main thread
    void OnClientConnected(ClientConnection clientConnection)
    {
        IEnumerator ClientPing()
        {
            while (true)
            {
                if (clientConnection.connectionToken.IsCancellationRequested)
                {
                    break;
                }

                yield return new WaitForSecondsRealtime(pingInterval);
                
                    if (clientConnection.lastSent + pingTimeout < Time.time)
                    {
                        Debug.Log($"<color=cyan>SERVER [SENT]:</color> Sent ping to {clientConnection.endPoint}");
                        lock (clients)
                            SendToClient(pingMsg, clientConnection);
                    }
                    else
                    {
                        OnClientTimeout(clientConnection);
                    }
                
            }
        }
                    
        StartCoroutine(ClientPing());
        Debug.Log($"Nuevo cliente UDP contactó desde: {clientConnection.endPoint}");
    }

    // Must be called in main thread
    void OnMessageReceived(NetMessage msg, ClientConnection sender)
    {
        sender.lastReceived = Time.time;
    }

    // Must be called on main thread
    void OnSendMessageToClient(ClientConnection clientConnection)
    {
        lock(clients)
            clientConnection.lastSent = Time.time;

        clientConnection.consecutiveTimeouts = 0;
    }
    
    void OnClientTimeout(ClientConnection cc)
    {
        
        if (cc.consecutiveTimeouts >= maxRetries)
        {
            Debug.LogWarning($"<color=yellow>SERVER [WARN]:</color> Client {cc.name} from IP {cc.endPoint.Address}:{cc.endPoint.Port} has disconnected (TIMEOUT)");
            HandlePlayerDisconnect(cc);
        }
        else
        {
            cc.consecutiveTimeouts++;
            Debug.LogWarning($"<color=yellow>SERVER [WARN]:</color> Client {cc.name} from IP {cc.endPoint.Address}:{cc.endPoint.Port} timed out {cc.consecutiveTimeouts}/{maxRetries} times");
        }
        
    }

    void OnApplicationQuit()
    {
        listenerThread?.Abort();
        
        if (udpListener != null) udpListener.Close();
    }
}