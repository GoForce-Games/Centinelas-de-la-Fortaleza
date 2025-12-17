using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject connectPanel;
    public GameObject lobbyPanel;
    public TMP_InputField ipAddressInput;
    public Button connectButton;
    public TMP_Text errorText;
    public TMP_Text playerListText;
    public TMP_Text chatHistoryText;
    public TMP_InputField chatMessageInput;
    public Button sendMessageButton;
    public TMP_Text serverInfoText;
    
    [Header("Game Control")]
    public Button startGameButton; 

    [Header("Indicador de Modo")]
    public TMP_Text protocolModeText;

    private ServerManager serverManager;
    private ClientManager clientManager;
    private bool isHost;

    void Start()
    {
        GameObject hostMarker = FindObjectOfType<HostMarker>()?.gameObject;
        bool serverExists = FindObjectOfType<ServerManager>() != null;
        
        isHost = (hostMarker != null) || serverExists;
        
        if (protocolModeText != null)
            protocolModeText.text = "Modo: UDP";

        connectButton.onClick.AddListener(OnConnectClicked);
        sendMessageButton.onClick.AddListener(OnSendMessageClicked);
        
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (isHost)
        {
            SetupHost();
            if (hostMarker != null) Destroy(hostMarker);
        }
        else
        {
            SetupClient();
        }
    }

    private void OnDestroy()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnChatMessageReceived -= AddChatMessage;
            ClientManager.instance.OnPlayerListUpdated -= UpdatePlayerListUI;
        }
    }

    private void SetupHost()
    {
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        if (startGameButton != null) 
        {
            startGameButton.gameObject.SetActive(true);
        }

        if (FindObjectOfType<ServerManager>() == null)
        {
            GameObject serverObj = new GameObject("ServerManager");
            serverManager = serverObj.AddComponent<ServerManager>();
            DontDestroyOnLoad(serverObj); 
        }
        else
        {
            serverManager = FindObjectOfType<ServerManager>();
        }

        if (serverInfoText != null)
        {
            serverInfoText.text = $"Tu IP es: {GetLocalIPAddress()}";
            serverInfoText.gameObject.SetActive(true);
        }

        SetupClient(); 
        
        if (ClientManager.instance != null && !ClientManager.instance.IsConnected) 
        {
            OnConnectClicked("127.0.0.1");
        }
        else if (ClientManager.instance == null)
        {
            OnConnectClicked("127.0.0.1");
        }
        else
        {
            serverManager.UpdatePlayerListForAll();
        }
    }

    private void SetupClient()
    {
        if (ClientManager.instance == null)
        {
            GameObject clientObj = new GameObject("ClientManager");
            clientManager = clientObj.AddComponent<ClientManager>();
            DontDestroyOnLoad(clientObj);
        }
        else
        {
            clientManager = ClientManager.instance;
            connectPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }

        ClientManager.instance.OnChatMessageReceived += AddChatMessage;
        ClientManager.instance.OnPlayerListUpdated += UpdatePlayerListUI;
        
        if (!isHost)
        {
            connectPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            if (serverInfoText != null) serverInfoText.gameObject.SetActive(false);
        }
    }

    private void OnConnectClicked()
    {
        string ip = ipAddressInput.text;
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
        OnConnectClicked(ip);
    }

    private void OnConnectClicked(string ip)
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.ConnectToServer(ip);
            connectPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }
    }

    private void OnStartGameClicked()
    {
        if (isHost)
        {
            if (serverManager != null)
            {
                serverManager.StartGame(); 
            }
            else
            {
                serverManager = FindObjectOfType<ServerManager>();
                if (serverManager != null)
                {
                    serverManager.StartGame();
                }
            }
        }
    }

    private void OnSendMessageClicked()
    {
        string message = chatMessageInput.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            ClientManager.instance.SendChatMessage(message);
            chatMessageInput.text = "";
        }
    }

    private void UpdatePlayerListUI(List<string> playerNames)
    {
        string protocolString = "UDP";
        if (playerListText != null)
        {
            playerListText.text = $"Jugadores en la sala ({protocolString}):\n";
            foreach (var name in playerNames)
            {
                playerListText.text += "- " + name + "\n";
            }
        }
    }

    private void AddChatMessage(string message)
    {
        if (chatHistoryText != null)
        {
            chatHistoryText.text += message + "\n";
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}