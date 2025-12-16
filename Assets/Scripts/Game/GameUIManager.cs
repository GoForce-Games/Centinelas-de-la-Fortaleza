using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public Transform gridParent;
    
    [Header("Prefabs de Modulos")]
    public GameObject buttonModulePrefab;
    public GameObject sliderModulePrefab;
    public GameObject toggleModulePrefab;

    public TMP_Text timerText;
    public TMP_Text mistakesText;
    public TMP_Text instructionsLabel; 
    public GameObject gameOverPanel;
    public TMP_Text gameOverTitle;
    public TMP_Text gameOverStats;
    public Button returnToLobbyButton;

    private List<ModuleUI> modules = new List<ModuleUI>();
    private const int SLOT_COUNT = 16;

    void Start()
    {
        gameOverPanel.SetActive(false);
        gridParent.gameObject.SetActive(true);

        if(returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(OnReturnClicked);
            returnToLobbyButton.gameObject.SetActive(false);
        }

        foreach(Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            GameObject prefabToUse = buttonModulePrefab;
            int type = i % 3;
            
            if (type == 1) prefabToUse = sliderModulePrefab;
            else if (type == 2) prefabToUse = toggleModulePrefab;

            GameObject obj = Instantiate(prefabToUse, gridParent);
            ModuleUI mod = obj.GetComponent<ModuleUI>();

            if (mod != null)
            {
                mod.Setup(i, OnModuleClick);
                mod.Refresh("", false);
                modules.Add(mod);
            }
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

    void OnModuleClick(int slotIndex, float value)
    {
        ClientManager.instance.SendGameInput(slotIndex, value);
    }

    void UpdateUI(GameStateData data)
    {
        if(timerText != null) timerText.text = $"{Mathf.Ceil(data.timeRemaining)}s";
        if(mistakesText != null) mistakesText.text = $"{data.currentMistakes}/{data.maxMistakes}";

        string instructions = "";

        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].Refresh("", false);
        }

        if (data.tasks.Count > 0)
        {
            instructions = "| ";
            foreach (var task in data.tasks)
            {
                if (task.slotIndex >= 0 && task.slotIndex < modules.Count)
                {
                    modules[task.slotIndex].Refresh(task.description, true);
                    
                    string flatDescription = task.description.Replace("\n", " ");
                    instructions += $"{flatDescription} (Mod {task.slotIndex}) | ";
                }
            }
        }
        else
        {
            instructions = "ESPERANDO ORDENES...";
        }

        if(instructionsLabel != null) instructionsLabel.text = instructions;
    }

    void ShowGameOver(GameOverData data)
    {
        gridParent.gameObject.SetActive(false);
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