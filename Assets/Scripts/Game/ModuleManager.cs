using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.InteractionModules;
using UnityEngine;

namespace Game
{
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
        public static object clientSide = new object();
        public static object serverSide = new object();
        
        private static int _idCounter = 0;
        private static ModuleManager _instance;
        
        [SerializeField] private List<InteractableModule> _modules = new List<InteractableModule>();
        
        private Dictionary<int, ModuleData> messagesToSend = new Dictionary<int, ModuleData>();

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
            if (!_instance) _instance = this;
            else if (_instance != this) Destroy(this);
            
            StartCoroutine(ClientProcessSend());
        }

        public static void ServerProcessReceive(NetMessage message)
        {
                ModuleNetPacket messagesReceived = JsonUtility.FromJson<ModuleNetPacket>(message.msgData);
                if(ServerManager.instance != null)
                    ServerManager.instance.BroadcastMessage(message);
        }

        public static void QueueMessage(ModuleData moduleData)
        {
            lock (clientSide)
            {
                Instance.messagesToSend[moduleData.moduleId] = moduleData;
            }
        }

        public IEnumerator ClientProcessSend()
        {
            NetMessage msg = new NetMessage("ModuleAction", "");
            while (isActiveAndEnabled)
            {
                if (messagesToSend.Count == 0 || !ClientManager.instance)
                {
                    yield return null;
                }
                else lock (clientSide)
                {
                    var auxList = new ModuleNetPacket(ClientManager.instance.playerName, messagesToSend.Values.ToArray());
                    msg.msgData = JsonUtility.ToJson(auxList);

                    if(ClientManager.instance != null)
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
                ModuleNetPacket messagesReceived = JsonUtility.FromJson<ModuleNetPacket>(message.msgData);
                messagesReceived.moduleList.ForEach(Instance.ClientProcessModule);
        }

        private void ClientProcessModule(ModuleData moduleData)
        {
            var monigote = _modules.OfType<MonigoteModule>().FirstOrDefault();
            monigote?.UpdateState(moduleData);
        }
    }
}   