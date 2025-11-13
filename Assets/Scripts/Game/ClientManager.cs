using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO; 

public class ClientManager : MonoBehaviour
{
    public static ClientManager instance = null;
    
    private Thread receiveThread;
    public LobbyUI lobbyUI;
    private NetworkChoice.Protocol clientMode;
    private bool isRunning = false;

    private TcpClient tcpClient;

    private StreamWriter tcpWriter;
    private StreamReader tcpReader;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    private UIManagerReceptor uiReceptor;

    public string playerName;
    private readonly Queue<string> messageQueue = new Queue<string>();

    public void ConnectToServer(string ipAddress)
    {
        uiReceptor = FindObjectOfType<UIManagerReceptor>();
        if (uiReceptor == null)
        {
            Debug.LogError("ClientManager no pudo encontrar UIManagerReceptor.");
        }
        clientMode = NetworkChoice.ChosenProtocol;
        isRunning = true;
        playerName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY);
        
        try
        {
            Debug.Log($"Iniciando conexión en modo {clientMode} hacia {ipAddress}...");
            receiveThread = new Thread(() => {
                if (clientMode == NetworkChoice.Protocol.TCP) ConnectAndReceiveTCP(ipAddress);
                else ConnectAndReceiveUDP(ipAddress);
            });
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al iniciar conexión: " + e.Message);
        }

        //Sets the active client instance to this
        instance = this;
    }
    
    private void ConnectAndReceiveTCP(string ip)
    {
        try
        {
            tcpClient = new TcpClient(ip, NetworkGlobals.GAME_PORT_TCP);
            NetworkStream stream = tcpClient.GetStream();
            tcpWriter = new StreamWriter(stream, NetworkGlobals.ENCODING);
            tcpReader = new StreamReader(stream, NetworkGlobals.ENCODING);
            
            EnqueueToMainThread(() => Debug.Log("Conectado al servidor TCP."));
            

            NetMessage joinMsg = new NetMessage("JOIN", playerName);
            SendMessageToServer(joinMsg);


            string jsonMessage;
            while (isRunning && (jsonMessage = tcpReader.ReadLine()) != null)
            {
                EnqueueToMainThread(jsonMessage);
            }
        }
        catch (System.Exception e)
        {
            if (isRunning) EnqueueToMainThread(() => Debug.LogError("Error en cliente TCP: " + e.Message));
        }
    }
    
    private void ConnectAndReceiveUDP(string ip)
    {
        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), NetworkGlobals.GAME_PORT_UDP);
            EnqueueToMainThread(() => Debug.Log("Cliente UDP listo."));
            

            NetMessage joinMsg = new NetMessage("JOIN", playerName);
            SendMessageToServer(joinMsg);
            
            while(isRunning)
            {
                IPEndPoint anyIp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref anyIp);
                string jsonMessage = NetworkGlobals.ENCODING.GetString(data); 
                EnqueueToMainThread(jsonMessage);
            }

            while (isRunning)
            {
                var resultTask = udpClient.ReceiveAsync();
                if (await Task.WhenAny(resultTask, Task.Delay((int)(pingTimeout * 1000))) == resultTask)
                {
                    string jsonMessage = NetworkGlobals.ENCODING.GetString(resultTask.Result.Buffer);
                    EnqueueToMainThread(jsonMessage);
                }
                else
                {
                    EnqueueToMainThread(() => Debug.LogError("Error en cliente UDP: Timeout"));
                    isRunning = false;
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
            if (clientMode == NetworkChoice.Protocol.TCP)
            {

                tcpWriter?.WriteLine(jsonMessage);
                tcpWriter?.Flush();
            }
            else
            {

                byte[] data = NetworkGlobals.ENCODING.GetBytes(jsonMessage);
                udpClient?.Send(data, data.Length, serverEndPoint);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al enviar mensaje: " + e.Message);
        }
    }

    private void EnqueueToMainThread(System.Action action)
    {
        lock (messageQueue)
        {
             if(action != null) action.Invoke();
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
        
        tcpWriter?.Close(); 
        tcpReader?.Close(); 
        tcpClient?.Close();
        udpClient?.Close();
    }
}