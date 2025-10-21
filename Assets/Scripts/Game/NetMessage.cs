
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
}