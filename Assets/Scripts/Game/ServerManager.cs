using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class ServerManager : MonoBehaviour
{
    private Thread listenerThread;
    public LobbyUI lobbyUI;
    private NetworkChoice.Protocol serverMode;
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private TcpListener tcpListener;
    private readonly List<TcpClient> tcpClients = new List<TcpClient>();
    private readonly Dictionary<TcpClient, string> tcpClientNames = new Dictionary<TcpClient, string>();

    private UdpClient udpListener;
    private readonly List<IPEndPoint> udpClients = new List<IPEndPoint>();
    private readonly Dictionary<IPEndPoint, string> udpClientNames = new Dictionary<IPEndPoint, string>();

    void Start()
    {
        serverMode = NetworkChoice.ChosenProtocol;
        
        listenerThread = new Thread(() => {
            if (serverMode == NetworkChoice.Protocol.TCP) ListenForTcpClients();
            else ListenForUdpPackets();
        });
        listenerThread.IsBackground = true;
        listenerThread.Start();

        UpdatePlayerListForAll();
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

    private void ListenForTcpClients()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, NetworkGlobals.GAME_PORT_TCP);
            tcpListener.Start();
            EnqueueToMainThread(() => Debug.Log($"Servidor TCP escuchando en el puerto {NetworkGlobals.GAME_PORT_TCP}"));

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                lock (tcpClients) { tcpClients.Add(client); }
                
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleTcpClientComm));
                clientThread.IsBackground = true;
                clientThread.Start(client);
            }
        }
        catch (System.Exception e) { EnqueueToMainThread(() => Debug.LogError("Error en listener TCP: " + e.Message)); }
    }

    private void HandleTcpClientComm(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;
        NetworkStream stream = tcpClient.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead;

        try
        {
             while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
             {
                 string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                 ProcessMessage(message, tcpClient);
             }
        }
        catch { }
        finally
        {
            string name = "Un jugador";
            if (tcpClientNames.TryGetValue(tcpClient, out name))
            {
                EnqueueToMainThread(() => HandlePlayerDisconnect(name));
            }
            lock (tcpClients) 
            {
                tcpClients.Remove(tcpClient);
                tcpClientNames.Remove(tcpClient);
            }
            tcpClient.Close();
            UpdatePlayerListForAll();
        }
    }

    private void ListenForUdpPackets()
    {
        try
        {
            udpListener = new UdpClient(NetworkGlobals.GAME_PORT_UDP);
            EnqueueToMainThread(() => Debug.Log($"Servidor UDP escuchando en el puerto {NetworkGlobals.GAME_PORT_UDP}"));

            while (true)
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.Receive(ref clientEndPoint);
                
                if (!udpClients.Contains(clientEndPoint))
                {
                    lock(udpClients) { udpClients.Add(clientEndPoint); }
                    EnqueueToMainThread(() => Debug.Log($"Nuevo cliente UDP contactó desde: {clientEndPoint}"));
                }

                string message = Encoding.ASCII.GetString(data);
                ProcessMessage(message, clientEndPoint);
            }
        }
        catch (System.Exception e) { EnqueueToMainThread(() => Debug.LogError("Error en listener UDP: " + e.Message)); }
    }
    
    private void ProcessMessage(string msg, object sender)
    {
        string[] parts = msg.Split(new char[] { ':' }, 2);
        string msgType = parts[0];
        string msgData = parts.Length > 1 ? parts[1] : "";

        if (msgType == "JOIN")
        {
            string senderName = msgData;
            if(serverMode == NetworkChoice.Protocol.TCP) tcpClientNames[(TcpClient)sender] = senderName;
            else udpClientNames[(IPEndPoint)sender] = senderName;
            
            HandlePlayerJoin(senderName);
        }
        else if (msgType == "CHAT")
        {
             string senderName = "Desconocido";
             if(serverMode == NetworkChoice.Protocol.TCP) senderName = tcpClientNames.ContainsKey((TcpClient)sender) ? tcpClientNames[(TcpClient)sender] : senderName;
             else senderName = udpClientNames.ContainsKey((IPEndPoint)sender) ? udpClientNames[(IPEndPoint)sender] : senderName;
            
            string formattedMessage = $"{senderName}: {msgData}";
            EnqueueToMainThread(() => lobbyUI.AddChatMessage(formattedMessage));
            BroadcastMessage($"CHAT:{formattedMessage}");
        }
    }
    
    public void HandleHostMessage(string message)
    {
        string hostName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Host");
        string formattedMessage = $"{hostName}: {message}";
        lobbyUI.AddChatMessage(formattedMessage);
        BroadcastMessage($"CHAT:{formattedMessage}");
    }

    private void HandlePlayerJoin(string name)
    {
        EnqueueToMainThread(() => {
            string joinMessage = $"{name} se ha unido a la sala.";
            lobbyUI.AddChatMessage(joinMessage);
            BroadcastMessage($"CHAT:{joinMessage}");
            UpdatePlayerListForAll();
        });
    }

    private void HandlePlayerDisconnect(string name)
    {
        string leaveMessage = $"{name} ha abandonado la sala.";
        lobbyUI.AddChatMessage(leaveMessage);
        BroadcastMessage($"CHAT:{leaveMessage}");
    }

    public void BroadcastMessage(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);

        if (serverMode == NetworkChoice.Protocol.TCP)
        {
            lock (tcpClients)
            {
                foreach (var client in tcpClients) client.GetStream().Write(data, 0, data.Length);
            }
        }
        else
        {
            lock (udpClients)
            {
                foreach (var clientEndPoint in udpClients) udpListener.Send(data, data.Length, clientEndPoint);
            }
        }
    }

    private void UpdatePlayerListForAll()
    {
        EnqueueToMainThread(() => {
            string hostName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Host");
            List<string> playerNames;
            if(serverMode == NetworkChoice.Protocol.TCP) playerNames = tcpClientNames.Values.ToList();
            else playerNames = udpClientNames.Values.ToList();

            playerNames.Insert(0, hostName);
            
            lobbyUI.UpdatePlayerList(playerNames);
            BroadcastMessage("PLAYERLIST:" + string.Join(",", playerNames));
        });
    }

    private void EnqueueToMainThread(System.Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    void OnApplicationQuit()
    {
        listenerThread?.Abort();
        if (tcpListener != null) tcpListener.Stop();
        if (udpListener != null) udpListener.Close();
    }
}