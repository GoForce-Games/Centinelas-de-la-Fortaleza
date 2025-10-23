
using UnityEngine.UI;

[System.Serializable]
public class NetMessage
{
    public string msgType;
    public string msgData;
    // Aquí podemos añadir más campos fácilmente, ej:
    // public string senderName;
    // public int value;

    public Button accion1;
    public Button accion2;
    public Slider puerta;
    public Toggle bloqueo;
    
    public NetMessage(string type, string data)
    {
        this.msgType = type;
        this.msgData = data;
    }
}