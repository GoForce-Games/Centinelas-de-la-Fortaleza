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
    private readonly Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private UdpClient udpListener;
    private readonly List<IPEndPoint> udpClients = new List<IPEndPoint>();
    private readonly Dictionary<IPEndPoint, string> udpClientNames = new Dictionary<IPEndPoint, string>();

    void Start()
    {
        listenerThread = new Thread(ListenForUdpPackets);
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

                string jsonMessage = NetworkGlobals.ENCODING.GetString(data); 
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
            udpClientNames[(IPEndPoint)sender] = senderName;
            HandlePlayerJoin(senderName);
        }
        else if (msg.msgType == "CHAT")
        {
            string senderName = "Desconocido";
            senderName = udpClientNames.ContainsKey((IPEndPoint)sender) ? udpClientNames[(IPEndPoint)sender] : senderName;

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
        
        {
            byte[] data = NetworkGlobals.ENCODING.GetBytes(jsonMessage);
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
            playerNames = udpClientNames.Values.ToList();

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
        
        if (udpListener != null) udpListener.Close();
    }
}