

using UnityEngine;

[System.Serializable]
public class NetMessage
{
    public static int packageIDs = 0;
    public int ID = packageIDs++;
    public string msgType;
    public string msgData;
    
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

public class PendingMessage
{
    public readonly NetMessage msg;
    public float timeStamp;
    public short retryCount = 0;

    public PendingMessage(NetMessage msg, float timeStamp)
    {
        this.msg = msg;
        this.timeStamp = timeStamp;
    }
}