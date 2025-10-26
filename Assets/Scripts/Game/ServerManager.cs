using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Game;

public class ServerManager : MonoBehaviour
{
    public static ServerManager instance = null;
    
    private Thread listenerThread;
    public LobbyUI lobbyUI;
    private NetworkChoice.Protocol serverMode;
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private TcpListener tcpListener;
    private readonly List<TcpClient> tcpClients = new List<TcpClient>();
    private readonly Dictionary<TcpClient, string> tcpClientNames = new Dictionary<TcpClient, string>();

    private readonly Dictionary<TcpClient, StreamWriter> tcpClientWriters = new Dictionary<TcpClient, StreamWriter>();

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
        
        instance = this;

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
        StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);


        lock(tcpClientWriters)
        {
            tcpClientWriters.Add(tcpClient, writer);
        }
        
        try
        {

             string jsonMessage;
             while ((jsonMessage = reader.ReadLine()) != null)
             {
                 ProcessMessage(jsonMessage, tcpClient);
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
            lock (tcpClientWriters)
            {
                tcpClientWriters.Remove(tcpClient);
            }
            
            writer.Close();
            reader.Close();
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

                string jsonMessage = Encoding.ASCII.GetString(data); 
                ProcessMessage(jsonMessage, clientEndPoint);
            }
        }
        catch (System.Exception e) { EnqueueToMainThread(() => Debug.LogError("Error en listener UDP: " + e.Message)); }
    }
    
    private void ProcessMessage(string jsonMsg, object sender)
    {
        Debug.Log("<color=yellow>SERVER [RECV]:</color> " + jsonMsg);
        NetMessage msg;
        try
        {

            msg = JsonUtility.FromJson<NetMessage>(jsonMsg);
            if (msg == null) return;
        }
        catch (System.Exception e)
        {
            EnqueueToMainThread(() => Debug.LogWarning("Error al deserializar mensaje de cliente: " + e.Message + " | JSON: " + jsonMsg));
            return;
        }

        if (msg.msgType == "JOIN")
        {
            string senderName = msg.msgData;
            if (serverMode == NetworkChoice.Protocol.TCP) tcpClientNames[(TcpClient)sender] = senderName;
            else udpClientNames[(IPEndPoint)sender] = senderName;

            HandlePlayerJoin(senderName);
        }
        else if (msg.msgType == "CHAT")
        {
            string senderName = "Desconocido";
            if (serverMode == NetworkChoice.Protocol.TCP) senderName = tcpClientNames.ContainsKey((TcpClient)sender) ? tcpClientNames[(TcpClient)sender] : senderName;
            else senderName = udpClientNames.ContainsKey((IPEndPoint)sender) ? udpClientNames[(IPEndPoint)sender] : senderName;

            string formattedMessage = $"{senderName}: {msg.msgData}";
            EnqueueToMainThread(() => lobbyUI.AddChatMessage(formattedMessage));


            BroadcastMessage(new NetMessage("CHAT", formattedMessage));
        }
        else if (msg.msgType == "UI_Update" || msg.msgType == "Accion1_Clicked")
        {
            BroadcastMessage(msg);
        }
        else if (msg.msgType == NetworkGlobals.MODULE_MANAGER_EVENT_KEY)
        {
            EnqueueToMainThread(()=> ModuleManager.ServerProcessReceive(msg));
        }
        else
        {
            EnqueueToMainThread(()=> Debug.LogWarning($"Comando desconocido: {msg.msgType}\nDatos:\n{msg.msgData}"));
        }
    }
    
    public void HandleHostMessage(string message)
    {
        string hostName = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY, "Host");
        string formattedMessage = $"{hostName}: {message}";
        lobbyUI.AddChatMessage(formattedMessage);
        

        BroadcastMessage(new NetMessage("CHAT", formattedMessage));
    }

    private void HandlePlayerJoin(string name)
    {
        EnqueueToMainThread(() => {
            string joinMessage = $"{name} se ha unido a la sala.";
            lobbyUI.AddChatMessage(joinMessage);
            

            BroadcastMessage(new NetMessage("CHAT", joinMessage));
            UpdatePlayerListForAll();
        });
    }

    private void HandlePlayerDisconnect(string name)
    {
        string leaveMessage = $"{name} ha abandonado la sala.";
        lobbyUI.AddChatMessage(leaveMessage);
        

        BroadcastMessage(new NetMessage("CHAT", leaveMessage));
    }

    public void BroadcastMessage(NetMessage msg)
    {
        string jsonMessage = JsonUtility.ToJson(msg);

        if (serverMode == NetworkChoice.Protocol.TCP)
        {
            lock (tcpClientWriters)
            {

                var writers = tcpClientWriters.Values.ToList();
                foreach (var writer in writers)
                {
                    try
                    {

                        writer.WriteLine(jsonMessage);
                        writer.Flush();
                    } catch (System.Exception e) {
                        EnqueueToMainThread(() => Debug.LogWarning("Error al broadcastear a cliente TCP: " + e.Message));
                    }
                }
            }
        }
        else
        {
            byte[] data = Encoding.ASCII.GetBytes(jsonMessage);
            lock (udpClients)
            {
                foreach (var clientEndPoint in udpClients)
                {
                    try
                    {
                        udpListener.Send(data, data.Length, clientEndPoint);
                    } catch (System.Exception e) {
                        EnqueueToMainThread(() => Debug.LogWarning("Error al broadcastear a cliente UDP: " + e.Message));
                    }
                }
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

    void OnApplicationQuit()
    {
        listenerThread?.Abort();
        if (tcpListener != null) tcpListener.Stop();
        

        lock(tcpClientWriters)
        {
            foreach(var writer in tcpClientWriters.Values) writer.Close();
        }
        
        if (udpListener != null) udpListener.Close();
    }
}