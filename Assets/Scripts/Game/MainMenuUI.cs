using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class MainMenuUI : MonoBehaviour
{
    public TMP_InputField nameInputField;

    [Header("Botones")]
    public Button createUdpButton;
    public Button joinUdpButton;

    void Start()
    {
        createUdpButton.onClick.AddListener(OnCreateUDP);
        joinUdpButton.onClick.AddListener(OnJoinUDP);

        if (PlayerPrefs.HasKey(NetworkGlobals.PLAYER_NAME_KEY))
        {
            nameInputField.text = PlayerPrefs.GetString(NetworkGlobals.PLAYER_NAME_KEY);
        }
    }

    private void SavePlayerName()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Guardián" + Random.Range(100, 999);
        }
        PlayerPrefs.SetString(NetworkGlobals.PLAYER_NAME_KEY, playerName);
        PlayerPrefs.Save();
    }

    public void OnCreateUDP()
    {
        SavePlayerName();
        NetworkChoice.ChosenProtocol = NetworkChoice.Protocol.UDP;
        CreateHostMarkerAndLoadLobby();
    }

    public void OnJoinUDP()
    {
        SavePlayerName();
        NetworkChoice.ChosenProtocol = NetworkChoice.Protocol.UDP;
        SceneManager.LoadScene("Lobby");
    }
    
    private void CreateHostMarkerAndLoadLobby()
    {
        GameObject hostMarkerObject = new GameObject("HostMarker");
        hostMarkerObject.AddComponent<HostMarker>();
        DontDestroyOnLoad(hostMarkerObject);
        SceneManager.LoadScene("Lobby");
    }
}