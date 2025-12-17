using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ServerGameController : MonoBehaviour
{
    public static ServerGameController instance;
    private ServerManager serverManager;

    [Header("Configuración del Juego")]
    public float totalTime = 300f;
    public float taskDuration = 25.0f; 
    public int baseMistakesLimit = 5;
    public float spawnRate = 2.0f;

    private float currentTime;
    private int currentMistakes;
    private int maxMistakes;
    private bool isRunning;
    private float spawnTimer;

    private Dictionary<int, TaskData> activeTasks = new Dictionary<int, TaskData>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    
    private string[] actions = { 
    "Izar", "Arriar", "Tensar", "Destensar", "Cargar", 
    "Disparar", "Prender", "Extinguir", "Verter", "Drenar", 
    "Reforzar", "Apuntalar", "Bloquear", "Desbloquear", "Engrasar", 
    "Limpiar", "Bendecir", "Vigilar", "Convocar", "Evacuar" 
    };

    private string[] systems = { 
        "Rastrillo Norte", "Rastrillo Sur", "Puente Levadizo", "Porton de Hierro", "Puerta Trasera", 
        "Catapulta Mayor", "Balista de Torre", "Trebuchet", "Mangonel", "Escorpion", 
        "Caldero de Aceite", "Caldero de Brea", "Foso Exterior", "Foso Interior", "Trampa de Pinchos"
    };

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (ServerManager.instance != null)
        {
            Initialize(ServerManager.instance, ServerManager.instance.GetConnectedPlayers());
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void Initialize(ServerManager manager, List<string> players)
    {
        serverManager = manager;
        currentTime = totalTime;
        currentMistakes = 0;
        maxMistakes = baseMistakesLimit;
        isRunning = true;
        spawnTimer = 1.0f;
        
        activeTasks.Clear();
        playerScores.Clear();
        
        if (players != null)
        {
            foreach(var p in players) 
            {
                if(!playerScores.ContainsKey(p)) playerScores.Add(p, 0);
            }
        }

        if (serverManager != null) serverManager.UpdatePlayerListForAll();

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(1.0f);

        while (isRunning && currentTime > 0 && currentMistakes < maxMistakes)
        {
            currentTime -= Time.deltaTime;
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0)
            {
                GenerateTask();
                spawnTimer = spawnRate;
            }

            CheckExpiredTasks();
            BroadcastState();
            yield return null;
        }

        EndGame(currentTime > 0 && currentMistakes < maxMistakes);
    }

    void GenerateTask()
    {
        if (activeTasks.Count >= 16) return;

        int slot = -1;
        int attempts = 0;
        do
        {
            slot = UnityEngine.Random.Range(0, 16);
            attempts++;
        } while (activeTasks.ContainsKey(slot) && attempts < 100);

        if (!activeTasks.ContainsKey(slot))
        {
            TaskData t = new TaskData();
            t.slotIndex = slot;
            t.timestamp = Time.time;
            
            int type = slot % 3;

            if (type == 1) 
            {
                int level = UnityEngine.Random.Range(1, 5); 
                t.targetValue = (float)level;
                string sysName = systems[UnityEngine.Random.Range(0, systems.Length)];
                t.description = $"Ajustar {sysName}\nal NIVEL {level}";
            }
            else if (type == 2) 
            {
                bool targetState = UnityEngine.Random.value > 0.5f;
                t.targetValue = targetState ? 1f : 0f;
                string sysName = systems[UnityEngine.Random.Range(0, systems.Length)];
                t.description = targetState ? $"ACTIVAR\n{sysName}" : $"DESACTIVAR\n{sysName}";
            }
            else 
            {
                t.targetValue = 1f; 
                t.description = $"{actions[UnityEngine.Random.Range(0, actions.Length)]}\n{systems[UnityEngine.Random.Range(0, systems.Length)]}";
            }

            activeTasks.Add(slot, t);
        }
    }

    void CheckExpiredTasks()
    {
        List<int> toRemove = new List<int>();
        foreach(var kvp in activeTasks)
        {
            if (Time.time - kvp.Value.timestamp > taskDuration)
            {
                toRemove.Add(kvp.Key);
                currentMistakes++;
                SendAnimTrigger("Todos", "sad"); 
            }
        }

        foreach(var key in toRemove)
        {
            activeTasks.Remove(key);
        }
    }

    public void ProcessInput(string playerName, int slotIndex, float inputValue)
    {
        if (!isRunning) return;

        if (activeTasks.ContainsKey(slotIndex))
        {
            TaskData task = activeTasks[slotIndex];
            bool success = false;
            int type = slotIndex % 3;

            if (type == 1) 
            {
                if (Mathf.Abs(inputValue - task.targetValue) < 0.1f) success = true;
            }
            else if (type == 2) 
            {
                if (Mathf.Abs(inputValue - task.targetValue) < 0.1f) success = true;
            }
            else 
            {
                success = true;
            }

            if (success)
            {
                activeTasks.Remove(slotIndex);
                if (playerScores.ContainsKey(playerName)) playerScores[playerName] += 100;
                SendAnimTrigger(playerName, "happy");
                BroadcastState();
            }
        }
        else
        {
            currentMistakes++;
            if (playerScores.ContainsKey(playerName)) playerScores[playerName] -= 50;
            SendAnimTrigger(playerName, "sad");
            BroadcastState();
        }
    }

    void SendAnimTrigger(string pName, string type)
    {
        if (serverManager == null) return;
        
        AnimEventData animData = new AnimEventData();
        animData.playerName = pName;
        animData.animType = type;
        
        string json = JsonUtility.ToJson(animData);
        serverManager.BroadcastMessage(new NetMessage("ANIM_TRIGGER", json));
    }

    void BroadcastState()
    {
        if (serverManager == null) return;

        GameStateData state = new GameStateData();
        state.timeRemaining = currentTime;
        state.currentMistakes = currentMistakes;
        state.maxMistakes = maxMistakes;
        state.tasks = activeTasks.Values.ToList();

        string json = JsonUtility.ToJson(state);
        serverManager.BroadcastMessage(new NetMessage("GAME_SYNC", json));
    }

    void EndGame(bool win)
    {
        isRunning = false;
        GameOverData data = new GameOverData();
        data.isWin = win;
        
        var sorted = playerScores.OrderByDescending(x => x.Value).ToList();
        data.mvpName = sorted.Count > 0 ? sorted[0].Key : "Nadie";
        data.scores = "";
        foreach(var p in sorted) data.scores += $"{p.Key}: {p.Value}\n";

        string json = JsonUtility.ToJson(data);
        if(serverManager != null)
            serverManager.BroadcastMessage(new NetMessage("GAME_OVER", json));
    }
}