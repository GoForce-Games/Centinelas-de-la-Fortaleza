using System;
using System.Collections.Generic;

[Serializable]
public class GameStateData
{
    public float timeRemaining;
    public int currentMistakes;
    public int maxMistakes;
    public List<TaskData> tasks;
}

[Serializable]
public class TaskData
{
    public int slotIndex;
    public string description;
    public float timestamp;
}

[Serializable]
public class PlayerInputData
{
    public int slotIndex;
    public string playerName;
}

[Serializable]
public class GameOverData
{
    public bool isWin;
    public string mvpName;
    public string scores;
}