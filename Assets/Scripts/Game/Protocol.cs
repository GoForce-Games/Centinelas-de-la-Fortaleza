using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInputData
{
    public string playerName;
    public int slotIndex;
    public float inputValue;
}

[Serializable]
public class TaskData
{
    public int slotIndex;
    public string description;
    public float timestamp;
    public float targetValue;
}

[Serializable]
public class GameStateData
{
    public float timeRemaining;
    public int currentMistakes;
    public int maxMistakes;
    public List<TaskData> tasks;
}

[Serializable]
public class GameOverData
{
    public bool isWin;
    public string mvpName;
    public string scores;
}