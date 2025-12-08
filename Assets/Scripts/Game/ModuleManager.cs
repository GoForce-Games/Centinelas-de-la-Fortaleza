using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.InteractionModules;
using UnityEngine;

namespace Game
{
    // ModuleData wrapper for serialization purposes
    [Serializable]
    public class ModuleNetPacket
    {
        public string sourcePlayer;
        public List<ModuleData> moduleList = new List<ModuleData>();
        public bool broadcast = false;
        
        
        public ModuleNetPacket(string player, ModuleData[] list)
        {
            sourcePlayer = player;
            moduleList = new List<ModuleData>(list);
        }
    }
    
    public class ModuleManager : MonoBehaviour
    {
        
        // Locks to prevent errors related to asynchronous access
        public static object clientSide = new ();
        public static object serverSide = new ();
        
        private static int _idCounter = 0;
        private static ModuleManager _instance;
        
        [SerializeField] private List<InteractableModule> _modules = new List<InteractableModule>();
        
        private Dictionary<int, ModuleData> messagesToSend = new ();

        public static ModuleManager Instance
        {
            get => (_instance != null ? _instance :  _instance = FindObjectOfType<ModuleManager>());
            private set => _instance = value;
        }

        public static void AddModule(InteractableModule module) => Instance._modules.Add(module);
        public static void RemoveModule(InteractableModule module) => Instance?._modules.Remove(module);

        public static int GetNextID() => _idCounter++;

        private void Start()
        {
            // Single instance active at any time
            if (!_instance) _instance = this;
            else if (_instance != this) Destroy(this);
            
            StartCoroutine(ClientProcessSend());
        }

        #region Server Code

        // Processes messages received from clients (WIP)
        public static void ServerProcessReceive(NetMessage message)
        {
                ModuleNetPacket messagesReceived = JsonUtility.FromJson<ModuleNetPacket>(message.msgData);
                messagesReceived.moduleList.ForEach(m => Debug.Log($"Received data from {messagesReceived.sourcePlayer}: {m.ToString()}"));
                ServerManager.instance?.BroadcastMessage(message);
        }

        #endregion

        #region Client code

        public static void QueueMessage(ModuleData moduleData)
        {
            lock (clientSide)
            {
                Instance.messagesToSend[moduleData.moduleId] = moduleData;
            }
        }

        // Processes messages to send to server
        public IEnumerator ClientProcessSend()
        {
            NetMessage msg = new NetMessage(NetworkGlobals.MODULE_MANAGER_EVENT_KEY, "");
            // Every frame send pending messages to server
            while (isActiveAndEnabled)
            {
                // Don't send a message if there's no data to send
                if (messagesToSend.Count == 0 || !ClientManager.instance)
                {
                    yield return null;
                }
                else lock (clientSide)
                {
                    // Pack all queued messages along with the player's alias
                    var auxList = new ModuleNetPacket(ClientManager.instance.playerName, messagesToSend.Values.ToArray());
                    msg.msgData = JsonUtility.ToJson(auxList);

                    lock (ClientManager.instance)
                    {
                        ClientManager.instance.SendMessageToServer(msg);
                    }
                    
                    Instance.messagesToSend.Clear();
                    
                    yield return null;
                }
            }

        }

        public static void ClientProcessReceive(NetMessage message)
        {
            lock (clientSide)
            {
                ModuleNetPacket messagesReceived = JsonUtility.FromJson<ModuleNetPacket>(message.msgData);
                messagesReceived.moduleList.ForEach(Instance.ClientProcessModule);
            }
        }

        private void ClientProcessModule(ModuleData moduleData)
        {
            // Temporary code, part of proper game logic will be implemented here
            var monigote = _modules.OfType<MonigoteModule>().FirstOrDefault();
            
            monigote?.UpdateState(moduleData);
            
        }

        #endregion
        
    }
}