using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    private object timeLock = new object();
    private float _currentTime = 0;
    [SerializeField] private float pingInterval = 0.5f;
    [SerializeField] private float pingTimeout = 3.0f;
    [SerializeField] private float ackTimeout = 1.0f;
    [SerializeField] [Min(2)] private short maxRetries = 3; // Applies to both timeouts and missed packages
    
    private byte[] pingMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PING\",\"msgData\":\"host\"}");
    private byte[] pongMsg = NetworkGlobals.ENCODING.GetBytes("{\"msgType\":\"PONG\",\"msgData\":\"host\"}");

    // Self-locking -> thread-safe
    private float CurrentTime
    {
        get { lock(timeLock) return _currentTime; }
        set { lock(timeLock) _currentTime = value; }
    }

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
        SceneManager.LoadScene("GameScene");
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
        
        // Send accumulated packet acknowledgements
        lock(clients)
            foreach (var clientConnection in clients)
            {
                if (clientConnection.ackedIds.Count > 0)
                {
                    SendToClient(new NetMessage("ACK", JsonHelper.ToJson(clientConnection.ackedIds.ToArray())), clientConnection);
                    clientConnection.ackedIds.Clear();
                }
            }

        CurrentTime = Time.time;
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
        // If packet is received a second time, skip it
        if (sender.recentIds.Contains(msg.ID)) return;
        
        switch (msg.msgType)
        {
            case "JOIN":
                string senderName = msg.msgData;
                lock (clients)
                {
                    sender.name = senderName; 
                    sender.lastReceived = CurrentTime;
                }
                HandlePlayerJoin(sender);
                break;

            case "LEAVE":
                HandlePlayerDisconnect(sender);
                break;

            case "PING":
                // pongMsg is an ACK package itself, no need to check if target receives it 
                SendToClient(pongMsg, sender);
                break;

            case "PONG":
                break;
            
            case "ACK":
                int[] acknowledgedPackets = JsonHelper.FromJson<int>(msg.msgData);
                sender.ackPending.RemoveAll(p => acknowledgedPackets.Contains(p.msg.ID));
                break;

            case "CHAT":
                string formattedMessage = $"{sender.name}: {msg.msgData}";
                BroadcastMessage(new NetMessage("CHAT", formattedMessage));
                break;

            case "GAME_INPUT":
                PlayerInputData inputData = JsonUtility.FromJson<PlayerInputData>(msg.msgData);
                EnqueueToMainThread(() => {
                    if (ServerGameController.instance != null)
                    {
                        ServerGameController.instance.ProcessInput(inputData.playerName, inputData.slotIndex, inputData.inputValue);
                    }
                });
                break;

            case "UI_Update":
            case "Accion1_Clicked":
                BroadcastMessage(msg);
                break;

            case "ModuleAction":
                EnqueueToMainThread(()=> ModuleManager.ServerProcessReceive(msg));
                break;

            default:
                Debug.LogWarning($"Comando desconocido: {msg.msgType}");
                break;
        }

        // Prevent ACK loop. PONG is ACK without data and acts as PING's acknowledgement packet
        if (msg.msgType != "ACK" && msg.msgType != "PONG" && msg.msgType != "PING")
        {
            sender.ackedIds.Add(msg.ID);
            sender.recentIds.Enqueue(msg.ID);
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

    //Send to individual client, check for ACK packet afterwards in main loop
    public void SendToClient(NetMessage msg, ClientConnection cc)
    {
        byte[] data = msg.ToBytes();
        SendToClient(data, cc);
        cc.ackPending.Add(new PendingMessage(msg, CurrentTime));
    }

    // Calling this directly by itself WILL NOT check for an ACK packet
    public void SendToClient(byte[] bytes, ClientConnection cc)
    {
        if (udpListener == null) return;
        
        // Resend pending packets first
        if (cc.ackPending.Count > 0)
        {
            foreach (var pendingMessage in cc.ackPending)
            {
                if (pendingMessage.retryCount >= maxRetries)
                {
                    // Max retries reached, show error in console
                    if (pendingMessage.msg.msgType != "ACK")
                        Debug.LogWarning($"<color=red>SERVER [ERROR]:</color> Packet lost with ID {pendingMessage.msg.ID}, target client {cc.endPoint} and type {pendingMessage.msg.msgType} with contents: {pendingMessage.msg.msgData}");
                }
                else if (pendingMessage.timeStamp + ackTimeout <= CurrentTime)
                {
                    // Can't call SendToClient recursively, would result in stack overflow
                    byte[] data = pendingMessage.msg.ToBytes();
                    udpListener.Send(data, data.Length, cc.endPoint);
                    pendingMessage.timeStamp = CurrentTime;
                    pendingMessage.retryCount++;
                }
            }
            cc.ackPending.RemoveAll(p => p.retryCount > maxRetries );
        }
        
        udpListener.Send(bytes, bytes.Length, cc.endPoint);
        EnqueueToMainThread(()=> OnSendMessageToClient(cc));
    }
    
    // Same as SendToClient(NetMessage, ClientConnection), but for all clients
    public void BroadcastMessage(NetMessage msg)
    {
        byte[] data =  msg.ToBytes();
        
        lock (clients)
        {
            foreach (var clientConnection in clients)
            {
                try
                {
                    // Send data and save it to ack pending list to check later
                    SendToClient(data, clientConnection);
                    clientConnection.ackPending.Add(new PendingMessage(msg, CurrentTime));
                }
                catch (Exception e)
                {
                    EnqueueToMainThread(() => Debug.LogWarning($"Error broadcast a {clientConnection.name}: {e.Message}"));
                }
            }
        }
    }

    public List<string> GetConnectedPlayers()
    {
        List<string> names = new List<string>();
        lock (clients)
        {
            foreach (var c in clients)
            {
                if (!string.IsNullOrEmpty(c.name)) names.Add(c.name);
            }
        }
        return names;
    }

    public void UpdatePlayerListForAll()
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
                
                if (clientConnection.lastSent + pingTimeout < CurrentTime)
                {
                    // pingMsg already has a dedicated ACK message equivalent to prevent spam
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
        sender.lastReceived = CurrentTime;
    }

    void OnSendMessageToClient(ClientConnection clientConnection)
    {
        clientConnection.lastSent = CurrentTime;
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