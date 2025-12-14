using System.Collections.Generic;

[System.Serializable]
public class UIStateMessage
{
    public Dictionary<string, float> sliders = new();
    public Dictionary<string, bool> toggles = new();
    public Dictionary<string, bool> buttons = new();
}