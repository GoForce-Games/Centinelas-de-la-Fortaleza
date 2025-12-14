using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public Transform gridParent;
    public GameObject modulePrefab;
    public TMP_Text timerText;
    public TMP_Text mistakesText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverTitle;
    public TMP_Text gameOverStats;
    public Button returnToLobbyButton;

    private List<ModuleUI> modules = new List<ModuleUI>();
    private const int SLOT_COUNT = 16;

    void Start()
    {
        gameOverPanel.SetActive(false);
        if(returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(OnReturnClicked);
            returnToLobbyButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            GameObject obj = Instantiate(modulePrefab, gridParent);
            ModuleUI mod = obj.GetComponent<ModuleUI>();
            mod.Setup(i, OnModuleClick);
            mod.Refresh("", false);
            modules.Add(mod);
        }

        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnGameSyncReceived += UpdateUI;
            ClientManager.instance.OnGameOverReceived += ShowGameOver;
        }
    }

    void OnDestroy()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnGameSyncReceived -= UpdateUI;
            ClientManager.instance.OnGameOverReceived -= ShowGameOver;
        }
    }

    void OnModuleClick(int slotIndex)
    {
        ClientManager.instance.SendGameInput(slotIndex);
    }

    void UpdateUI(GameStateData data)
    {
        timerText.text = $"{Mathf.Ceil(data.timeRemaining)}s";
        mistakesText.text = $"{data.currentMistakes}/{data.maxMistakes}";

        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].Refresh("", false);
        }

        foreach (var task in data.tasks)
        {
            if (task.slotIndex >= 0 && task.slotIndex < modules.Count)
            {
                modules[task.slotIndex].Refresh(task.description, true);
            }
        }
    }

    void ShowGameOver(GameOverData data)
    {
        gameOverPanel.SetActive(true);
        gameOverTitle.text = data.isWin ? "MISION CUMPLIDA" : "NAVE DESTRUIDA";
        gameOverStats.text = $"MVP: {data.mvpName}\n\n{data.scores}";
        
        if(returnToLobbyButton != null) returnToLobbyButton.gameObject.SetActive(true);
    }

    void OnReturnClicked()
    {
         SceneManager.LoadScene("Lobby");
    }
}