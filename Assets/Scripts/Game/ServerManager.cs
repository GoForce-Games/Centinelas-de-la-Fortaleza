using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System;

public class ServerManager : MonoBehaviour
{
    public static ServerManager instance;

    private UdpClient udpServer;
    private List<IPEndPoint> clients = new List<IPEndPoint>();
    private List<string> playerNames = new List<string>();
    private bool isRunning;
    private Thread listenThread;
    
    private readonly Queue<Action> actionQueue = new Queue<Action>();

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

    void Start()
    {
        StartServer();
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
    }

    void StartServer()
    {
        if (udpServer != null) return;

        udpServer = new UdpClient(8888);
        isRunning = true;
        listenThread = new Thread(ListenLoop);
        listenThread.IsBackground = true;
        listenThread.Start();
        
        if (GetComponent<ServerGameController>() == null)
            gameObject.AddComponent<ServerGameController>();
    }

    void ListenLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref remoteEP);
                string json = System.Text.Encoding.UTF8.GetString(data);
                NetMessage msg = JsonUtility.FromJson<NetMessage>(json);

                EnqueueToMainThread(() => ProcessMessage(msg, remoteEP));
            }
            catch (Exception) { }
        }
    }

    void ProcessMessage(NetMessage msg, IPEndPoint sender)
    {
        bool newClient = true;
        foreach(var c in clients)
        {
            if(c.ToString() == sender.ToString()) 
            {
                newClient = false;
                break;
            }
        }
        if(newClient) clients.Add(sender);

        switch (msg.opCode)
        {
            case "CONNECT":
                if (!playerNames.Contains(msg.msgData))
                    playerNames.Add(msg.msgData);
                SendPlayerList();
                break;
            case "CHAT":
                BroadcastMessage(msg);
                break;
            case "GAME_INPUT":
                PlayerInputData input = JsonUtility.FromJson<PlayerInputData>(msg.msgData);
                if(ServerGameController.instance != null)
                    ServerGameController.instance.ProcessInput(input.playerName, input.slotIndex, input.inputValue);
                break;
            case "PING":
                break;
        }
    }

    void SendPlayerList()
    {
        string list = string.Join(",", playerNames);
        BroadcastMessage(new NetMessage("PLAYERLIST", list));
    }

    public void StartGame()
    {
        BroadcastMessage(new NetMessage("START_GAME", ""));
        if(ServerGameController.instance != null)
            ServerGameController.instance.Initialize(this, playerNames);
    }

    public void BroadcastMessage(NetMessage msg)
    {
        string json = JsonUtility.ToJson(msg);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
        foreach (var client in clients)
        {
            udpServer.Send(data, data.Length, client);
        }
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
        udpServer?.Close();
        listenThread?.Abort();
    }
}