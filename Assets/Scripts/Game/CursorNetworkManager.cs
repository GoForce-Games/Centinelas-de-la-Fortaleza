using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    
    
    
    public class CursorNetworkManager : MonoBehaviour
    {
        public static CursorNetworkManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private float sendInterval = 0.05f; 
        [SerializeField] private Canvas targetCanvas;
        
        public event Action<CursorPacket> OnCursorsUpdated;
        
        private Coroutine sendCoroutine;
        private int localPlayerColorIndex = 0;
        
        
        public static readonly Color[] PlayerColors = new Color[]
        {
            new Color(1f, 0.267f, 0.267f),      
            new Color(0.267f, 0.533f, 1f),       
            new Color(0.267f, 1f, 0.533f),       
            new Color(1f, 0.8f, 0.267f)          
        };
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            
            if (targetCanvas == null)
                targetCanvas = FindObjectOfType<Canvas>();
            
            
            if (ClientManager.instance != null)
            {
                ClientManager.instance.OnCursorsReceived += HandleCursorsReceived;
                
                
                AssignPlayerColorIndex();
                
                
                sendCoroutine = StartCoroutine(SendCursorPositionRoutine());
            }
        }
        
        private void OnDestroy()
        {
            if (ClientManager.instance != null)
            {
                ClientManager.instance.OnCursorsReceived -= HandleCursorsReceived;
            }
            
            if (sendCoroutine != null)
                StopCoroutine(sendCoroutine);
        }
        
        private void AssignPlayerColorIndex()
        {
            
            if (ClientManager.instance != null)
            {
                string myName = ClientManager.instance.playerName;
                
                
                localPlayerColorIndex = Mathf.Abs(myName.GetHashCode()) % 4;
            }
        }
        
        private IEnumerator SendCursorPositionRoutine()
        {
            var wait = new WaitForSecondsRealtime(sendInterval);
            
            while (true)
            {
                yield return wait;
                
                if (ClientManager.instance != null && ClientManager.instance.IsConnected)
                {
                    SendCursorPosition();
                }
            }
        }
        
        private void SendCursorPosition()
        {
            if (targetCanvas == null) return;
            
            Vector2 mousePos = Input.mousePosition;
            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
            
            
            float normalizedX = mousePos.x / Screen.width;
            float normalizedY = mousePos.y / Screen.height;
            
            CursorData cursorData = new CursorData(
                ClientManager.instance.playerName,
                normalizedX,
                normalizedY,
                localPlayerColorIndex
            );
            
            
            ClientManager.instance.SendCursorPosition(cursorData);
        }
        
        private void HandleCursorsReceived(CursorPacket packet)
        {
            
            if (ClientManager.instance != null)
            {
                string myName = ClientManager.instance.playerName;
                packet.cursors.RemoveAll(c => c.playerName == myName);
            }
            
            OnCursorsUpdated?.Invoke(packet);
        }
        
        
        
        
        public static Color GetPlayerColor(int index)
        {
            return PlayerColors[Mathf.Clamp(index, 0, PlayerColors.Length - 1)];
        }
    }
}
