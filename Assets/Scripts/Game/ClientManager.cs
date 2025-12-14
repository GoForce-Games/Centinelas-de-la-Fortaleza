using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Game;
using UnityEngine.SceneManagement;

public class ClientManager : MonoBehaviour
{
    public static ClientManager instance = null;

    public event Action<string> OnChatMessageReceived;
    public event Action<List<string>> OnPlayerListUpdated;
    public event Action<string> OnGameStarted; 
    public event Action<GameStateData> OnGameSyncReceived;
    public event Action<GameOverData> OnGameOverReceived;

    private Thread receiveThread;
    public LobbyUI lobbyUI;
    private bool isRunning = false;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private CancellationTokenSource cancelToken = new CancellationTokenSource();

    private float lastSent = 0;
    private float lastReceived = 0;
    [SerializeField] private float pingInterval = 0.5f;
    [SerializeField] private float pingTimeout = 3.0f;

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
        
        isRunning = true;
        playerName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Jugador" + UnityEngine.Random.Range(0,999));
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
                    OnGameStarted?.Invoke("GameScene");
                    SceneManager.LoadScene("GameScene");
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
                
                case NetworkGlobals.MODULE_MANAGER_EVENT_KEY:
                    ModuleManager.ClientProcessReceive(msg);
                    break;

                case "GAME_SYNC":
                    GameStateData gData = JsonUtility.FromJson<GameStateData>(msg.msgData);
                    OnGameSyncReceived?.Invoke(gData);
                    break;

                case "GAME_OVER":
                    GameOverData overData = JsonUtility.FromJson<GameOverData>(msg.msgData);
                    OnGameOverReceived?.Invoke(overData);
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

    public void SendGameInput(int slotIndex)
    {
        PlayerInputData input = new PlayerInputData();
        input.slotIndex = slotIndex;
        input.playerName = playerName;
        NetMessage msg = new NetMessage("GAME_INPUT", JsonUtility.ToJson(input));
        SendMessageToServer(msg);
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