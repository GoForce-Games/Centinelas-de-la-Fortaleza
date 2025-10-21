using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class ClientManager : MonoBehaviour
{
    private Thread receiveThread;
    public LobbyUI lobbyUI;
    private NetworkChoice.Protocol clientMode;
    private bool isRunning = false;

    private TcpClient tcpClient;
    private NetworkStream tcpStream;
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    private string playerName;
    private readonly Queue<string> messageQueue = new Queue<string>();

    public void ConnectToServer(string ipAddress)
    {
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
    }
    
    private void ConnectAndReceiveTCP(string ip)
    {
        try
        {
            tcpClient = new TcpClient(ip, NetworkGlobals.GAME_PORT_TCP);
            tcpStream = tcpClient.GetStream();
            EnqueueToMainThread(() => Debug.Log("Conectado al servidor TCP."));
            
            SendMessageToServer($"JOIN:{playerName}");

            byte[] buffer = new byte[4096];
            int bytesRead;
            while (isRunning && (bytesRead = tcpStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                EnqueueToMainThread(Encoding.ASCII.GetString(buffer, 0, bytesRead));
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
            
            SendMessageToServer($"JOIN:{playerName}");
            
            while(isRunning)
            {
                IPEndPoint anyIp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref anyIp);
                EnqueueToMainThread(Encoding.ASCII.GetString(data));
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

    private void ProcessServerMessage(string msg)
    {
        string[] parts = msg.Split(new char[] { ':' }, 2);
        string msgType = parts[0];
        string msgData = parts.Length > 1 ? parts[1] : "";

        if (msgType == "CHAT")
        {
            lobbyUI.AddChatMessage(msgData);
        }
        else if (msgType == "PLAYERLIST")
        {
            string[] playerNames = msgData.Split(',');
            lobbyUI.UpdatePlayerList(new List<string>(playerNames));
        }
    }

    public void SendChatMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            SendMessageToServer($"CHAT:{message}");
        }
    }

    private void SendMessageToServer(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        try
        {
            if (clientMode == NetworkChoice.Protocol.TCP)
            {
                tcpStream?.Write(data, 0, data.Length);
            }
            else
            {
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
        tcpClient?.Close();
        udpClient?.Close();
    }
}