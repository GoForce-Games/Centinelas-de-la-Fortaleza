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

    public event Action<string> OnServerLog;
    
    private Thread listenerThread;
    
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private UdpClient udpListener;

    private List<ClientConnection> clients = new List<ClientConnection>();
    
    [SerializeField] private float pingInterval = 0.5f;
    [SerializeField] private float pingTimeout = 3.0f;
    [SerializeField] private short maxRetries = 3;
    
    private byte[] pingMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PING\",\"msgData\":\"host\"}");
    private byte[] pongMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PONG\",\"msgData\":\"host\"}");
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    } 
    
    void Start()
    {
        listenerThread = new Thread(ListenForUdpPackets);
        listenerThread.IsBackground = true;
        listenerThread.Start();
        
        Log("Servidor iniciado.");
    }

    public void StartGame()
    {
        NetMessage startMsg = new NetMessage("START_GAME", "MainGame");
        BroadcastMessage(startMsg);
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
                    EnqueueToMainThread(() => Debug.LogError("<color=red>SERVER [RECV]:</color> Error deserializing: " + e.Message));
                }
                
                if (msg != null)
                {
                    EnqueueToMainThread(()=> OnMessageReceived(msg, connection));
                    ProcessMessage(msg, connection);
                }
            }
        }
        catch (System.Exception e) { EnqueueToMainThread(() => Debug.LogError("<color=red>SERVER [RECV]:</color> Error UDP listener: " + e.Message)); }
    }
    
    private void ProcessMessage(NetMessage msg, ClientConnection sender)
    {
        // Debug.Log("<color=yellow>SERVER [RECV]:</color> " + msg.msgType);

        if (msg.msgType == "JOIN")
        {
            string senderName = msg.msgData;
            lock (clients)
            {
                sender.name = senderName; 
                EnqueueToMainThread(() => { lock (clients) sender.lastReceived = Time.time; });
            }
            HandlePlayerJoin(sender);
        }
        else if (msg.msgType == "LEAVE")
        {
            HandlePlayerDisconnect(sender);
        }
        else if (msg.msgType == "PING")
        {
            SendToClient(pongMsg, sender);
        }
        else if (msg.msgType == "PONG")
        {

        }
        else if (msg.msgType == "CHAT")
        {
            string formattedMessage = $"{sender.name}: {msg.msgData}";
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
            Debug.LogWarning($"Comando desconocido: {msg.msgType}");
        }
    }
    
    private void HandlePlayerJoin(ClientConnection cc)
    {
        EnqueueToMainThread(() => {
            string joinMessage = $"{cc.name} se ha unido a la sala.";
            Log(joinMessage);
            
            BroadcastMessage(new NetMessage("CHAT", joinMessage));
            
            UpdatePlayerListForAll();
        });
    }

    private void HandlePlayerDisconnect(ClientConnection cc)
    {
        string leaveMessage = $"{cc.name} ha abandonado la sala.";
        Log(leaveMessage);

        cc.connectionToken.Cancel();
        
        lock (clients)
            clients.Remove(cc);

        BroadcastMessage(new NetMessage("CHAT", leaveMessage));
        UpdatePlayerListForAll();
    }

    public void SendToClient(NetMessage msg, ClientConnection cc)
    {
        byte[] data = msg.ToBytes();
        SendToClient(data, cc);
    }

    public void SendToClient(byte[] bytes, ClientConnection cc)
    {
        if (udpListener != null)
        {
            udpListener.Send(bytes, bytes.Length, cc.endPoint);
            EnqueueToMainThread(()=> OnSendMessageToClient(cc));
        }
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
                    EnqueueToMainThread(() => Debug.LogWarning($"Error broadcast a {clientConnection.name}: {e.Message}"));
                }
            }
        }
    }

    private void UpdatePlayerListForAll()
    {
        EnqueueToMainThread(() => {
            List<string> playerNames;
            lock(clients)
            {
                playerNames = clients.Select(c => c.name).ToList();
            }

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

    private void Log(string msg)
    {
        OnServerLog?.Invoke(msg);
        Debug.Log("[SERVER] " + msg);
    }

    void OnClientConnected(ClientConnection clientConnection)
    {
        IEnumerator ClientPing()
        {
            while (true)
            {
                if (clientConnection.connectionToken.IsCancellationRequested) break;
                yield return new WaitForSecondsRealtime(pingInterval);
                
                if (clientConnection.lastSent + pingTimeout < Time.time)
                {
                    lock (clients) SendToClient(pingMsg, clientConnection);
                }
                else
                {
                    OnClientTimeout(clientConnection);
                }
            }
        }       
        StartCoroutine(ClientPing());
        Log($"Nuevo cliente desde: {clientConnection.endPoint}");
    }

    void OnMessageReceived(NetMessage msg, ClientConnection sender)
    {
        sender.lastReceived = Time.time;
    }

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
            Debug.LogWarning($"<color=yellow>SERVER [WARN]:</color> Cliente {cc.name} ({cc.endPoint}) desconectado (TIMEOUT)");
            HandlePlayerDisconnect(cc);
        }
        else
        {
            cc.consecutiveTimeouts++;
        }
    }

    void OnApplicationQuit()
    {
        listenerThread?.Abort();
        
        if (udpListener != null) udpListener.Close();
    }
}