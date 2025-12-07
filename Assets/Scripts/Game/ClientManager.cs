using System;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class ClientManager : MonoBehaviour
{
    public static ClientManager instance = null;
    
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

            // Ping if no data has been sent within pingInterval
            IEnumerator PingTimer()
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    if ((lastSent + pingInterval) < Time.time && !string.IsNullOrEmpty(playerName))
                        SendMessageToServer(pingMsg);
                    yield return new WaitForSecondsRealtime(pingInterval);
                }
            }
            StartCoroutine(PingTimer());
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al iniciar conexión: " + e.Message);
        }

        //Sets the active client instance to this
        instance = this;
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
                else
                {
                    EnqueueToMainThread(() => Debug.LogError("Error en cliente UDP: Timeout"));
                    isRunning = false;
                    cancelToken.Cancel();
                }
            }
        }
        catch (System.Exception e)
        {
            if (isRunning) EnqueueToMainThread(() => Debug.LogError("Error en cliente UDP: " + e.Message));
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

            if (msg.msgType == "CHAT")
            {
                lobbyUI.AddChatMessage(msg.msgData);
            }
            else if (msg.msgType == "PLAYERLIST")
            {
                string[] playerNames = msg.msgData.Split(',');
                lobbyUI.UpdatePlayerList(new List<string>(playerNames));
            }
            else if (msg.msgType == "UI_Update" || msg.msgType == "Accion1_Clicked")
            {
                if (uiReceptor != null)
                {
                    // Le pasamos el JSON completo para que él lo procese
                    uiReceptor.RecibirMensaje(jsonMsg);
                }
            }
            else if (msg.msgType == "PING")
            {
                Debug.Log($"<color=blue>CLIENT [PING]:</color> Ping received from {msg.msgData}. Sending pong response");
                SendMessageToServer(new NetMessage("PONG", msg.msgData));
            }
            else if (msg.msgType == "PONG")
            {
                Debug.Log($"<color=blue>CLIENT [PONG]:</color> Ping response received from {msg.msgData}");
            }
            else
            {
                EnqueueToMainThread(()=> Debug.LogWarning($"Comando desconocido: {msg.msgType}\nDatos:\n{msg.msgData}"));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error al deserializar mensaje del servidor: " + e.Message + " | JSON: " + jsonMsg);
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

        Debug.Log("<color=cyan>CLIENT [SENT]:</color> " + jsonMessage);
        
        try
        {
            lock (udpClient)
            {
                byte[] data = NetworkGlobals.ENCODING.GetBytes(jsonMessage);
                udpClient?.Send(data, data.Length, serverEndPoint);
                EnqueueToMainThread(() => lastSent = Time.time); // Time.time can only be called in main thread
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