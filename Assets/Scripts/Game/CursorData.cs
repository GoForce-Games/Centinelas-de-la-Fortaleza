using System;

[Serializable]
public class CursorData
{
    public string playerName;
    public float x;
    public float y;

    public CursorData(string name, float x, float y)
    {
        this.playerName = name;
        this.x = x;
        this.y = y;
    }
}