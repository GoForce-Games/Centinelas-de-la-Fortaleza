using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using UnityEngine.SceneManagement;

public class ClientManager : MonoBehaviour
{
    public static ClientManager instance = null;

    public event Action<string> OnChatMessageReceived;
    public event Action<List<string>> OnPlayerListUpdated;
    public event Action<GameStateData> OnGameSyncReceived;
    public event Action<GameOverData> OnGameOverReceived;

    public List<string> currentPlayers = new List<string>();
    public bool IsConnected { get; private set; } = false;

    private Thread receiveThread;
    private bool isRunning = false;
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private float lastSent = 0;
    [SerializeField] private float pingInterval = 0.5f;

    public string playerName;
    private readonly Queue<Action> actionQueue = new Queue<Action>();
    private NetMessage pingMsg = new NetMessage("PING", "client");

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

    void Update()
    {
        lock (actionQueue)
        {
            while (actionQueue.Count > 0)
            {
                actionQueue.Dequeue()?.Invoke();
            }
        }

        if (isRunning && udpClient != null)
        {
            if (Time.time - lastSent > pingInterval)
            {
                SendMessageToServer(pingMsg);
            }
        }
    }

    public void ConnectToServer(string ip)
    {
        try
        {
            if (udpClient != null) udpClient.Close();
            
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 8888);
            udpClient.Connect(serverEndPoint);

            isRunning = true;
            IsConnected = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            NetMessage msg = new NetMessage("CONNECT", playerName);
            SendMessageToServer(msg);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void ReceiveLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEP);
                string json = System.Text.Encoding.UTF8.GetString(data);
                NetMessage msg = JsonUtility.FromJson<NetMessage>(json);

                if (msg.opCode != "ACK" && msg.opCode != "PONG")
                {
                    SendAck(msg.messageId);
                }

                EnqueueToMainThread(() => ProcessMessage(msg));
            }
            catch (Exception) {}
        }
    }

    private void SendAck(string idReceived)
    {
        NetMessage ack = new NetMessage("ACK", idReceived);
        SendMessageToServer(ack);
    }

    private void ProcessMessage(NetMessage msg)
    {
        switch (msg.opCode)
        {
            case "CHAT":
                OnChatMessageReceived?.Invoke(msg.msgData);
                break;
            case "PLAYERLIST":
                string[] players = msg.msgData.Split(',');
                currentPlayers = new List<string>(players);
                OnPlayerListUpdated?.Invoke(currentPlayers);
                break;
            case "GAME_SYNC":
                GameStateData state = JsonUtility.FromJson<GameStateData>(msg.msgData);
                OnGameSyncReceived?.Invoke(state);
                break;
            case "GAME_OVER":
                GameOverData over = JsonUtility.FromJson<GameOverData>(msg.msgData);
                OnGameOverReceived?.Invoke(over);
                break;
            case "START_GAME":
                SceneManager.LoadScene("GameScene");
                break;
            case "ACK":
                break;
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

    public void SendGameInput(int slotIndex, float value)
    {
        PlayerInputData input = new PlayerInputData();
        input.slotIndex = slotIndex;
        input.playerName = playerName;
        input.inputValue = value;
        NetMessage msg = new NetMessage("GAME_INPUT", JsonUtility.ToJson(input));
        SendMessageToServer(msg);
    }

    public void SendMessageToServer(NetMessage msg)
    {
        if (udpClient == null) return;
        try
        {
            string json = JsonUtility.ToJson(msg);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            udpClient.Send(data, data.Length);
            lastSent = Time.time;
        }
        catch (Exception) { }
    }

    private void EnqueueToMainThread(Action action)
    {
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        receiveThread?.Abort();
        udpClient?.Close();
    }
}