using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CursorSyncManager : MonoBehaviour
{
    public GameObject cursorPrefab;
    public Canvas canvas;
    private Dictionary<string, RectTransform> remoteCursors = new Dictionary<string, RectTransform>();

    void Start()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnCursorMoveReceived += UpdateCursorPosition;
        }
    }

    void OnDestroy()
    {
        if (ClientManager.instance != null)
        {
            ClientManager.instance.OnCursorMoveReceived -= UpdateCursorPosition;
        }
    }

    private void UpdateCursorPosition(string json)
    {
        CursorData data = JsonUtility.FromJson<CursorData>(json);

        if (data.playerName == ClientManager.instance.playerName) return;

        if (!remoteCursors.ContainsKey(data.playerName))
        {
            GameObject newCursor = Instantiate(cursorPrefab, canvas.transform);
            newCursor.name = "Cursor_" + data.playerName;
            remoteCursors.Add(data.playerName, newCursor.GetComponent<RectTransform>());
            
            Text label = newCursor.GetComponentInChildren<Text>();
            if (label != null) label.text = data.playerName;
        }

        Vector2 screenPos = new Vector2(data.x * Screen.width, data.y * Screen.height);
        remoteCursors[data.playerName].position = screenPos;
    }
}