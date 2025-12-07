using System;
using System.IO;
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

        public SentDataTypes ValueTypes
        {
            get => (SentDataTypes)_typesMask;
            set => _typesMask = (int)value;
        }

       
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(moduleId);
            writer.Write((int)ValueTypes);

            if (ValueTypes.HasFlag(SentDataTypes.Bool))
                writer.Write(boolValue);

            if (ValueTypes.HasFlag(SentDataTypes.Integer))
                writer.Write(intValue);

            if (ValueTypes.HasFlag(SentDataTypes.Float))
                writer.Write(floatValue);

            if (ValueTypes.HasFlag(SentDataTypes.String))
                writer.Write(stringValue ?? "");
        }

        
        public static ModuleData Deserialize(BinaryReader reader)
        {
            ModuleData data = new ModuleData();
            data.moduleId = reader.ReadInt32();
            data.ValueTypes = (SentDataTypes)reader.ReadInt32();

            if (data.ValueTypes.HasFlag(SentDataTypes.Bool))
                data.boolValue = reader.ReadBoolean();

            if (data.ValueTypes.HasFlag(SentDataTypes.Integer))
                data.intValue = reader.ReadInt32();

            if (data.ValueTypes.HasFlag(SentDataTypes.Float))
                data.floatValue = reader.ReadSingle();

            if (data.ValueTypes.HasFlag(SentDataTypes.String))
                data.stringValue = reader.ReadString();

            return data;
        }

        public override string ToString()
        {
            return $"[{moduleName}: ID={moduleId}, flags=({ValueTypes}), boolValue={boolValue}, intValue={intValue}, floatValue={floatValue}, stringValue={stringValue}]";
        }
    }
}