using UnityEngine;
using System;

[Serializable]
public class NetMessage
{
    public string opCode;
    public string msgType; 
    public string msgData;
    public string messageId; 

    public NetMessage(string op, string data)
    {
        this.opCode = op;
        this.msgData = data;
        this.msgType = op;
        this.messageId = Guid.NewGuid().ToString(); 
    }
}