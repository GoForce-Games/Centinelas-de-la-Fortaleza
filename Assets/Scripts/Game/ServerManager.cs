using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Game;

public class ServerManager : MonoBehaviour
{
    public static ServerManager instance;

    private UdpClient udpServer;
    private List<IPEndPoint> clients = new List<IPEndPoint>();
    private List<string> playerNames = new List<string>();
    private bool isRunning;
    private Thread listenThread;

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

    void StartServer()
    {
        if (udpServer != null) return;

        udpServer = new UdpClient(NetworkGlobals.GAME_PORT_UDP);
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
                string msgJson = NetworkGlobals.ENCODING.GetString(data);
                NetMessage msg = JsonUtility.FromJson<NetMessage>(msgJson);

                HandleMessage(msg, remoteEP);
            }
            catch { }
        }
    }

    void HandleMessage(NetMessage msg, IPEndPoint sender)
    {
        MainThreadDispatcher.Enqueue(() => {
            switch (msg.msgType)
            {
                case "JOIN":
                    bool exists = false;
                    foreach(var c in clients) if(c.Equals(sender)) exists = true;
                    
                    if (!exists)
                    {
                        clients.Add(sender);
                        playerNames.Add(msg.msgData);
                        SendPlayerList();
                    }
                    break;
                case "CHAT":
                    BroadcastMessage(msg);
                    break;
                case "GAME_INPUT":
                    PlayerInputData input = JsonUtility.FromJson<PlayerInputData>(msg.msgData);
                    if(ServerGameController.instance != null)
                        ServerGameController.instance.ProcessInput(input.playerName, input.slotIndex);
                    break;
                case "PING":
                    break;
                case "UI_Update":
                case "Button_Clicked":
                case NetworkGlobals.MODULE_MANAGER_EVENT_KEY:
                    ModuleManager.ServerProcessReceive(msg);
                    break;
            }
        });
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
        byte[] data = NetworkGlobals.ENCODING.GetBytes(json);
        foreach (var client in clients)
        {
            if(udpServer != null)
                udpServer.Send(data, data.Length, client);
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        udpServer?.Close();
        listenThread?.Abort();
    }
}