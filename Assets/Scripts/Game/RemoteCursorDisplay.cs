using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game
{
    public class RemoteCursorDisplay : MonoBehaviour
    {
        [Header("Prefab & Container")]
        [SerializeField] private GameObject cursorPrefab;
        
        [Header("Cursor Appearance")]
        [SerializeField] private float cursorSize = 32f;
        [SerializeField] private float smoothSpeed = 15f;
        
        private Dictionary<string, RemoteCursor> activeCursors = new Dictionary<string, RemoteCursor>();
        private Canvas parentCanvas;
        private RectTransform canvasRectTransform;
        
        private class RemoteCursor
        {
            public RectTransform rectTransform;
            public Image cursorImage;
            public TMP_Text nameLabel;
            public Vector2 targetPosition;
            public float lastUpdateTime;
        }
        
        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            }
            
            if (cursorPrefab == null)
            {
                CreateDefaultCursorPrefab();
            }
        }
        
        private void Start()
        {
            if (CursorNetworkManager.Instance != null)
            {
                CursorNetworkManager.Instance.OnCursorsUpdated += UpdateCursors;
            }
        }
        
        private void OnDestroy()
        {
            if (CursorNetworkManager.Instance != null)
            {
                CursorNetworkManager.Instance.OnCursorsUpdated -= UpdateCursors;
            }
        }
        
        private void Update()
        {
            foreach (var cursor in activeCursors.Values)
            {
                if (cursor.rectTransform != null)
                {
                    cursor.rectTransform.anchoredPosition = Vector2.Lerp(
                        cursor.rectTransform.anchoredPosition,
                        cursor.targetPosition,
                        Time.deltaTime * smoothSpeed
                    );
                }
            }
            
            // Remove stale cursors
            List<string> toRemove = new List<string>();
            float currentTime = Time.time;
            foreach (var kvp in activeCursors)
            {
                if (currentTime - kvp.Value.lastUpdateTime > 2f)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (string name in toRemove)
            {
                if (activeCursors[name].rectTransform != null)
                    Destroy(activeCursors[name].rectTransform.gameObject);
                activeCursors.Remove(name);
            }
        }
        
        private void UpdateCursors(CursorPacket packet)
        {
            if (packet == null || packet.cursors == null) return;
            
            Debug.Log($"[RemoteCursorDisplay] Received {packet.cursors.Count} cursors");
            
            foreach (CursorData data in packet.cursors)
            {
                UpdateOrCreateCursor(data);
            }
        }
        
        private void UpdateOrCreateCursor(CursorData data)
        {
            if (!activeCursors.TryGetValue(data.playerName, out RemoteCursor cursor))
            {
                cursor = CreateCursor(data);
                activeCursors[data.playerName] = cursor;
                Debug.Log($"[RemoteCursorDisplay] Created cursor for {data.playerName}");
            }
            
            if (canvasRectTransform == null) 
            {
                Debug.LogWarning("[RemoteCursorDisplay] canvasRectTransform is null!");
                return;
            }
            
            // Get the canvas size (this already accounts for Canvas Scaler)
            Vector2 canvasSize = canvasRectTransform.rect.size;
            
            // With anchors at bottom-left (0,0), simply multiply normalized pos by canvas size
            float localX = data.posX * canvasSize.x;
            float localY = data.posY * canvasSize.y;
            
            cursor.targetPosition = new Vector2(localX, localY);
            
            Debug.Log($"[RemoteCursorDisplay] {data.playerName} normalized({data.posX:F2},{data.posY:F2}) -> canvas({canvasSize.x:F0}x{canvasSize.y:F0}) -> local({localX:F0},{localY:F0})");
            
            cursor.lastUpdateTime = Time.time;
            
            Color playerColor = CursorNetworkManager.GetPlayerColor(data.playerColorIndex);
            if (cursor.cursorImage != null)
                cursor.cursorImage.color = playerColor;
        }
        
        private RemoteCursor CreateCursor(CursorData data)
        {
            // Create cursor as child of canvas
            GameObject cursorObj = Instantiate(cursorPrefab, canvasRectTransform);
            cursorObj.SetActive(true);
            cursorObj.name = $"Cursor_{data.playerName}";
            
            RectTransform rectTransform = cursorObj.GetComponent<RectTransform>();
            
            // IMPORTANT: Set anchors to bottom-left corner for correct positioning
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1); // Pivot at top-left of cursor
            
            RemoteCursor cursor = new RemoteCursor
            {
                rectTransform = rectTransform,
                cursorImage = cursorObj.GetComponentInChildren<Image>(),
                nameLabel = cursorObj.GetComponentInChildren<TMP_Text>()
            };
            
            if (cursor.nameLabel != null)
            {
                cursor.nameLabel.text = data.playerName;
            }
            
            Color playerColor = CursorNetworkManager.GetPlayerColor(data.playerColorIndex);
            if (cursor.cursorImage != null)
            {
                cursor.cursorImage.color = playerColor;
            }
            
            return cursor;
        }
        
        private void CreateDefaultCursorPrefab()
        {
            cursorPrefab = new GameObject("DefaultCursor");
            RectTransform rect = cursorPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cursorSize, cursorSize + 20);
            
            // Cursor icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cursorPrefab.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(cursorSize, cursorSize);
            iconRect.anchorMin = new Vector2(0, 1);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = Vector2.zero;
            
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = CreateArrowSprite();
            iconImage.raycastTarget = false;
            
            // Player name label
            GameObject labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(cursorPrefab.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(cursorSize * 0.5f, -cursorSize);
            labelRect.sizeDelta = new Vector2(100, 20);
            
            TMP_Text label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            
            cursorPrefab.SetActive(false);
        }
        
        private Sprite CreateArrowSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inArrow = (x + y < size) && (x < size * 0.7f || y < size * 0.3f);
                    pixels[y * size + x] = inArrow ? Color.white : Color.clear;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0, 1));
        }
    }
}
