using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;

public class LobbyUI : MonoBehaviour
{
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
    
    [Header("Indicador de Modo")]
    public TMP_Text protocolModeText;

    private ServerManager serverManager;
    private ClientManager clientManager;
    private bool isHost;

    void Start()
    {
        isHost = FindObjectOfType<HostMarker>() != null;
        if (isHost) Destroy(FindObjectOfType<HostMarker>().gameObject);
        
        if (protocolModeText != null)
        {
            protocolModeText.text = $"Modo: {NetworkChoice.ChosenProtocol.ToString()}";
        }

        if (isHost) SetupHost();
        else SetupClient();

        connectButton.onClick.AddListener(OnConnectClicked);
        sendMessageButton.onClick.AddListener(OnSendMessageClicked);
    }

    private void SetupHost()
    {
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        serverManager = gameObject.AddComponent<ServerManager>();
        serverManager.lobbyUI = this;
        serverInfoText.text = $"Tu IP es: {GetLocalIPAddress()}";
        serverInfoText.gameObject.SetActive(true);
    }

    private void SetupClient()
    {
        connectPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        serverInfoText.gameObject.SetActive(false);
        clientManager = gameObject.AddComponent<ClientManager>();
        clientManager.lobbyUI = this;
    }

    private void OnConnectClicked()
    {
        string ip = ipAddressInput.text;
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
        clientManager.ConnectToServer(ip);
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void OnSendMessageClicked()
    {
        string message = chatMessageInput.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            if (isHost)
            {
                serverManager.HandleHostMessage(message);
            }
            else
            {
                clientManager.SendChatMessage(message);
            }
            chatMessageInput.text = "";
        }
    }

    public void UpdatePlayerList(List<string> playerNames)
    {
        string protocolString = NetworkChoice.ChosenProtocol.ToString();
        playerListText.text = $"Jugadores en la sala ({protocolString}):\n";
        
        foreach (var name in playerNames)
        {
            playerListText.text += "- " + name + "\n";
        }
    }

    public void AddChatMessage(string message)
    {
        chatHistoryText.text += message + "\n";
    }

    public void ShowConnectionError(string message)
    {
        connectPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        errorText.text = message;
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