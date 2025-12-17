

using UnityEngine;

[System.Serializable]
public class NetMessage
{
    public string msgType;
    public string msgData;
    // Aquí podemos añadir más campos fácilmente, ej:
    // public string senderName;
    // public int value;
    
    public NetMessage(string type, string data)
    {
        this.msgType = type;
        this.msgData = data;
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