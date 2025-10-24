using System;
using System.Collections;
using Game;
using Game.InteractionModules;
using TMPro;
using UnityEngine;

public class ModuleAutoTimer : InteractableModule
{
    // Visuals
    [SerializeField] private TMP_Text timerText;
    
    // Variables for this module's behaviour
    [SerializeField] private float timeToWait;
    private float _timeLeft;
    
    void Start()
    {
        _moduleData.moduleName = "ModuleAutoTimer";
        _timeLeft = timeToWait;

        // Set what value types this module sends (accepts multiple flags via bit-wise OR)
        _moduleData.ValueTypes = SentDataTypes.Integer;

    }

    private void FixedUpdate()
    {
        if (!(_timeLeft <= 0)) return;
        
        _timeLeft += timeToWait;
        _moduleData.intValue++;
        SendMessageToManager();
    }

    private void Update()
    {
        // Update UI
        _timeLeft -= Time.deltaTime;
        timerText.text = _timeLeft.ToString();
    }
}
