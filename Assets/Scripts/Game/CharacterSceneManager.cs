using UnityEngine;
using System.Collections.Generic;

public class CharacterSceneManager : MonoBehaviour
{
    [Header("Posiciones de Spawn")]
    public Transform[] spawnPoints;

    [Header("Prefabs de Personajes (Ordenados)")]
    public GameObject[] charPrefabs;

    private Dictionary<string, PlayerCharacter> spawnedCharacters = new Dictionary<string, PlayerCharacter>();

    void Start()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnAnimReceived += HandleAnimEvent;
            
            ClientManager.instance.OnPlayerListUpdated += SpawnCharacters;
            
            if(ServerManager.instance != null)
            {
               SpawnCharacters(ServerManager.instance.GetConnectedPlayers());
            }
        }
    }

    void OnDestroy()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnAnimReceived -= HandleAnimEvent;
            ClientManager.instance.OnPlayerListUpdated -= SpawnCharacters;
        }
    }

    private bool hasSpawned = false;

    void SpawnCharacters(List<string> players)
    {
        if (hasSpawned || players.Count == 0) return;
        hasSpawned = true;

        for (int i = 0; i < players.Count; i++)
        {
            GameObject prefabToUse = charPrefabs[i % charPrefabs.Length];
            
            Transform spot = (i < spawnPoints.Length) ? spawnPoints[i] : spawnPoints[0];
    
            GameObject obj = Instantiate(prefabToUse, spot.position, spot.rotation);
            PlayerCharacter pc = obj.GetComponent<PlayerCharacter>();
            
            if (pc != null)
            {
                pc.Setup(players[i]);
                spawnedCharacters.Add(players[i], pc);
            }
        }
    }

    void HandleAnimEvent(string json)
    {
        AnimEventData data = JsonUtility.FromJson<AnimEventData>(json);
        if (spawnedCharacters.ContainsKey(data.playerName))
        {
            PlayerCharacter pc = spawnedCharacters[data.playerName];
            if (data.animType == "happy") pc.PlayHappy();
            else if (data.animType == "sad") pc.PlaySad();
        }
    }
}