using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ServerGameController : MonoBehaviour
{
    public static ServerGameController instance;
    private ServerManager serverManager;

    public float totalTime = 120f;
    public float taskDuration = 10f; 
    public int baseMistakesLimit = 5;

    private float currentTime;
    private int currentMistakes;
    private int maxMistakes;
    private bool isRunning;

    private Dictionary<int, TaskData> activeTasks = new Dictionary<int, TaskData>();
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    
    private string[] actions = { 
    "Izar", "Arriar", "Tensar", "Destensar", "Cargar", 
    "Disparar", "Prender", "Extinguir", "Verter", "Drenar", 
    "Reforzar", "Apuntalar", "Bloquear", "Desbloquear", "Engrasar", 
    "Limpiar", "Bendecir", "Vigilar", "Convocar", "Evacuar" 
};

    private string[] systems = { 
        "Rastrillo Norte", "Rastrillo Sur", "Puente Levadizo", "Portón de Hierro", "Puerta Trasera", 
        "Catapulta Mayor", "Balista de Torre", "Trebuchet", "Mangonel", "Escorpión", 
        "Caldero de Aceite", "Caldero de Brea", "Foso Exterior", "Foso Interior", "Trampa de Pinchos", 
        "Muro Cortina", "Torre del Homenaje", "Barbacana", "Almena Oeste", "Almena Este", 
        "Matacán Central", "Saetera", "Almenara", "Campana de Alerta", "Cuerno de Guerra", 
        "Tambor de Batalla", "Armería Real", "Herrería", "Establos", "Pozo de Agua", 
        "Despensa", "Túnel Secreto", "Cadena del Puerto", "Compuerta del Río", "Grúa de Carga", 
        "Horno de Pan", "Mazmorras", "Sala del Trono", "Capilla", "Puesto de Guardia" 
    };

    void Awake()
    {
        instance = this;
    }

    public void Initialize(ServerManager manager, List<string> players)
    {
        serverManager = manager;
        playerScores.Clear();
        foreach (var p in players) 
        {
            if(!playerScores.ContainsKey(p)) playerScores.Add(p, 0);
        }

        currentTime = totalTime;
        currentMistakes = 0;
        maxMistakes = Mathf.Max(baseMistakesLimit, players.Count * 2);
        activeTasks.Clear();
        
        int initialTasks = Mathf.Clamp(players.Count * 4, 4, 16);
        for (int i = 0; i < initialTasks; i++)
        {
            CreateTask();
        }

        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        List<int> expired = new List<int>();
        foreach(var kvp in activeTasks)
        {
            if (Time.time > kvp.Value.timestamp + taskDuration)
            {
                expired.Add(kvp.Key);
            }
        }

        foreach(var key in expired)
        {
            activeTasks.Remove(key);
            currentMistakes++;
            CreateTask();
        }

        if (currentMistakes >= maxMistakes)
        {
            EndGame(false);
        }
        else if (currentTime <= 0)
        {
            EndGame(true);
        }
        else
        {
            BroadcastState();
        }
    }

    public void ProcessInput(string playerName, int slot)
    {
        if (!isRunning) return;

        if (activeTasks.ContainsKey(slot))
        {
            activeTasks.Remove(slot);
            if (playerScores.ContainsKey(playerName)) playerScores[playerName]++;
            CreateTask();
        }
    }

    void CreateTask()
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
            t.description = $"{actions[UnityEngine.Random.Range(0, actions.Length)]}\n{systems[UnityEngine.Random.Range(0, systems.Length)]}";
            t.timestamp = Time.time;
            activeTasks.Add(slot, t);
        }
    }

    void BroadcastState()
    {
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

        serverManager.BroadcastMessage(new NetMessage("GAME_OVER", JsonUtility.ToJson(data)));
    }
}