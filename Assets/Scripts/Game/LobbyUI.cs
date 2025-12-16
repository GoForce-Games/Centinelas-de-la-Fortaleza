using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject connectPanel;
    public GameObject lobbyPanel;
    public TMP_InputField ipAddressInput;
    public Button connectButton;
    public TMP_Text playerListText;
    public TMP_Text chatHistoryText;
    public TMP_InputField chatMessageInput;
    public Button sendMessageButton;
    public Button startGameButton; 

    private bool isHost;

    void Start()
    {
        EnsureClientManagerExists();

        connectButton.onClick.AddListener(() => OnConnectClicked(ipAddressInput.text));
        sendMessageButton.onClick.AddListener(OnSendMessageClicked);
        
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(false); 
        }

        if (ServerManager.instance != null)
        {
            isHost = true;
            RestoreLobbyState();
        }
        else if (ClientManager.instance != null && ClientManager.instance.IsConnected)
        {
            isHost = false;
            RestoreLobbyState();
        }
        else if (FindObjectOfType<HostMarker>() != null)
        {
            isHost = true;
            Destroy(FindObjectOfType<HostMarker>().gameObject);
            SetupHost();
        }
        else
        {
            connectPanel.SetActive(true);
            lobbyPanel.SetActive(false);
        }

        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnPlayerListUpdated += UpdatePlayerListUI;
            ClientManager.instance.OnChatMessageReceived += AddChatMessage;
            
            if(ClientManager.instance.currentPlayers.Count > 0)
                UpdatePlayerListUI(ClientManager.instance.currentPlayers);
        }
    }

    void OnDestroy()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnPlayerListUpdated -= UpdatePlayerListUI;
            ClientManager.instance.OnChatMessageReceived -= AddChatMessage;
        }
    }

    private void EnsureClientManagerExists()
    {
        if (ClientManager.instance == null)
        {
            GameObject clientObj = new GameObject("ClientManager");
            ClientManager cm = clientObj.AddComponent<ClientManager>();
            
            if(PlayerPrefs.HasKey("PLAYER_NAME")) 
            {
                cm.playerName = PlayerPrefs.GetString("PLAYER_NAME");
            }
            else
            {
                cm.playerName = "Jugador" + Random.Range(100, 999);
            }
        }
    }

    public void SetupHost()
    {
        EnsureClientManagerExists();

        if (ServerManager.instance == null)
        {
            GameObject srv = new GameObject("ServerManager");
            srv.AddComponent<ServerManager>();
        }
        
        ClientManager.instance.ConnectToServer("127.0.0.1");
        
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        if(startGameButton != null) startGameButton.gameObject.SetActive(true);
    }

    private void RestoreLobbyState()
    {
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        
        if (isHost && startGameButton != null) 
        {
            startGameButton.gameObject.SetActive(true);
        }
    }

    private void OnConnectClicked(string ip)
    {
        EnsureClientManagerExists();

        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
        ClientManager.instance.ConnectToServer(ip);
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void OnStartGameClicked()
    {
        if(isHost && ServerManager.instance != null)
        {
            ServerManager.instance.StartGame(); 
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
        playerListText.text = "Jugadores en la sala:\n";
        foreach (var name in playerNames)
        {
            playerListText.text += "- " + name + "\n";
        }
    }

    private void AddChatMessage(string message)
    {
        chatHistoryText.text += message + "\n";
    }
}