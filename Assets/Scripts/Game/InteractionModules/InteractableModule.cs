using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.InteractionModules
{

    public abstract class InteractableModule : MonoBehaviour
    {
        protected ModuleData _moduleData = new ModuleData();
        
        protected NetMessage Msg;

        public void SendMessageToManager()
        {
            ModuleManager.QueueMessage(_moduleData);
        }
    }
    
    
    
    [Flags] public enum SentDataTypes
    {
        None      = 0,
        Bool      = 1 << 0,
        Integer   = 1 << 2,
        Float     = 1 << 3,
        String    = 1 << 4,
    }
    
    [Serializable]
    public class ModuleData
    {
        public int moduleId = ModuleManager.GetNextID();
        public string moduleName = "DEFAULT MODULE";
        [SerializeField] private int _typesMask;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        
        public SentDataTypes ValueTypes {get => (SentDataTypes) _typesMask; set => _typesMask = (int) value; }

        public override string ToString()
        {
            return $"[{moduleName}: ID={moduleId}, flags=({ValueTypes}), boolValue={boolValue}, intValue={intValue}, floatValue={floatValue}, stringValue={stringValue}]";
        }
    }
}