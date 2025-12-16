

using UnityEngine;

[System.Serializable]
public class NetMessage
{
    public string msgType;
    public string msgData;
    public string opCode;
    // Aquí podemos añadir más campos fácilmente, ej:
    // public string senderName;
    // public int value;
    
    public NetMessage(string op, string data)
    {
        this.opCode = op;
        this.msgData = data;
        
        this.msgType = op;
    }

    public byte[] ToBytes()
    {
        string jsonMessage = JsonUtility.ToJson(this);
        return NetworkGlobals.ENCODING.GetBytes(jsonMessage);
    }

    public static NetMessage FromBytes(byte[] bytes)
    {
        string jsonMessage = NetworkGlobals.ENCODING.GetString(bytes);
        return JsonUtility.FromJson<NetMessage>(jsonMessage);
    }
}