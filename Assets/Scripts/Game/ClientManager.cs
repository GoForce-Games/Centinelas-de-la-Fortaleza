using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientManager : MonoBehaviour
{
    public static ClientManager instance = null;

    public event Action<string> OnChatMessageReceived;
    public event Action<List<string>> OnPlayerListUpdated;
    public event Action<string> OnGameStarted;
    
    public event Action<string> OnGameSyncReceived;
    public event Action<string> OnGameOverReceived;
    
    private Thread receiveThread;
    private bool isRunning = false;

    public bool IsConnected => isRunning;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private CancellationTokenSource cancelToken = new CancellationTokenSource();

    private float lastSent = 0;
    private float lastReceived = 0;
    [SerializeField] private float pingInterval = 0.5f;
    [SerializeField] private float pingTimeout = 3.0f;
    [SerializeField] private short consecutiveTimeouts = 0;
    [SerializeField] private short maxTimeouts = 0;

    private UIManagerReceptor uiReceptor;

    public string playerName;
    private readonly Queue<Action> actionQueue = new Queue<Action>();
    private readonly Queue<string> messageQueue = new Queue<string>();

    private NetMessage pingMsg = new NetMessage("PING", "client");
    private NetMessage pongMsg = new NetMessage("PONG", "client");


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
        }
    }
    
    public void ConnectToServer(string ipAddress)
    {
        uiReceptor = FindObjectOfType<UIManagerReceptor>();
        if (uiReceptor == null)
        {
            Debug.LogError("ClientManager no pudo encontrar UIManagerReceptor.");
        }
        isRunning = true;
        playerName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY);
        pingMsg.msgData = pongMsg.msgData = playerName;
        
        try
        {
            Debug.Log($"Iniciando conexión en modo UDP hacia {ipAddress}...");
            receiveThread = new Thread(() => {
                ConnectAndReceiveUDP(ipAddress);
            });
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            StartCoroutine(PingTimer());
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al iniciar conexión: " + e.Message);
        }

        instance = this;
    }
    
    public void RegisterGameUI(UIManagerReceptor receptor)
    {
        uiReceptor = receptor;
    }

    public void SendGameInput(int slotIndex, float value)
    {
        PlayerInputData inputData = new PlayerInputData();
        inputData.playerName = this.playerName;
        inputData.slotIndex = slotIndex;
        inputData.inputValue = value;

        string json = JsonUtility.ToJson(inputData);
        NetMessage msg = new NetMessage("GAME_INPUT", json);
        SendMessageToServer(msg);
    }

    IEnumerator PingTimer()
    {
        while (!cancelToken.IsCancellationRequested)
        {
            if ((lastSent + pingInterval) < Time.time && !string.IsNullOrEmpty(playerName))
                SendMessageToServer(pingMsg);
            yield return new WaitForSecondsRealtime(pingInterval);
        }
    }
    
    private async void ConnectAndReceiveUDP(string ip)
    {
        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), NetworkGlobals.GAME_PORT_UDP);
            EnqueueToMainThread(() => Debug.Log("Cliente UDP listo."));

            NetMessage joinMsg = new NetMessage("JOIN", playerName);
            SendMessageToServer(joinMsg);

            while (isRunning)
            {
                var resultTask = udpClient.ReceiveAsync();
                if (await Task.WhenAny(resultTask, Task.Delay((int)(pingTimeout * 1000))) == resultTask)
                {
                    string jsonMessage = NetworkGlobals.ENCODING.GetString(resultTask.Result.Buffer);
                    EnqueueToMainThread(jsonMessage); 
                    EnqueueToMainThread(() => lastReceived = Time.time);
                    consecutiveTimeouts = 0;
                }
                else
                {
                    EnqueueToMainThread(() => Debug.LogError($"Error en cliente UDP: Timeout (Try {++consecutiveTimeouts}/{maxTimeouts}"));
                    if (consecutiveTimeouts >= maxTimeouts)
                    {
                        isRunning = false;
                        cancelToken.Cancel();
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (isRunning) EnqueueToMainThread(() => Debug.LogError("Error UDP: " + e.Message));
        }
    }

    void Update()
    {
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                ProcessServerMessage(messageQueue.Dequeue());
            }
        }

        lock (actionQueue)
        {
            while (actionQueue.Count > 0)
            {
                actionQueue.Dequeue()?.Invoke();
            }
        }
    }

    private void ProcessServerMessage(string jsonMsg)
    {
        try
        {
            NetMessage msg = JsonUtility.FromJson<NetMessage>(jsonMsg);
            if (msg == null) return;

            switch (msg.msgType)
            {
                case "CHAT":
                    OnChatMessageReceived?.Invoke(msg.msgData);
                    break;

                case "PLAYERLIST":
                    string[] playerNames = msg.msgData.Split(',');
                    OnPlayerListUpdated?.Invoke(new List<string>(playerNames));
                    break;
                
                case "START_GAME":
                    OnGameStarted?.Invoke(msg.msgData);
                    SceneManager.LoadScene("GameScene");
                    break;

                case "GAME_SYNC":
                    OnGameSyncReceived?.Invoke(msg.msgData);
                    break;

                case "GAME_OVER":
                    OnGameOverReceived?.Invoke(msg.msgData);
                    break;

                case "UI_Update":
                case "Accion1_Clicked":
                    if (uiReceptor == null) uiReceptor = FindObjectOfType<UIManagerReceptor>();
                    if (uiReceptor != null) uiReceptor.RecibirMensaje(jsonMsg);
                    break;

                case "PING":
                    SendMessageToServer(new NetMessage("PONG", msg.msgData));
                    break;

                case "PONG":
                    break;
                
                case "ModuleAction":
                    ModuleManager.ClientProcessReceive(msg);
                    break;

                default:
                    Debug.LogWarning($"Comando desconocido: {msg.msgType}");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error deserializar: " + e.Message);
        }
    }

    public void SendChatMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            NetMessage chatMsg = new NetMessage("CHAT", message);
            SendMessageToServer(chatMsg);
        }
    }

    public void SendMessageToServer(NetMessage msg)
    {
        string jsonMessage = JsonUtility.ToJson(msg); 

        if (udpClient == null) return;

        if (msg.msgType != "PING" && msg.msgType != "PONG" && msg.msgType != "GAME_INPUT")
        {
            Debug.Log("<color=cyan>CLIENT [SENT]:</color> " + jsonMessage);
        }
        
        try
        {
            lock (udpClient)
            {
                byte[] data = NetworkGlobals.ENCODING.GetBytes(jsonMessage);
                udpClient?.Send(data, data.Length, serverEndPoint);
                EnqueueToMainThread(() => lastSent = Time.time); 
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al enviar mensaje: " + e.Message);
        }
    }

    private void EnqueueToMainThread(System.Action action)
    {
        lock (actionQueue)
        {
             if(action != null) actionQueue.Enqueue(action);
        }
    }
    
    private void EnqueueToMainThread(string message)
    {
        lock (messageQueue)
        {
            messageQueue.Enqueue(message);
        }
    }

   void OnApplicationQuit()
    {
        isRunning = false;
        receiveThread?.Abort();
        udpClient?.Close();
    }
}