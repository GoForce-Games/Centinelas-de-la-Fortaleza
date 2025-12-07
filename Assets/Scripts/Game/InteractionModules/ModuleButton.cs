using Game;
using Game.InteractionModules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModuleButton : InteractableModule
{
    [SerializeField] private Button button;

    private void Start()
    {
        _moduleData.moduleName = "ModuleButton";
        _moduleData.ValueTypes = SentDataTypes.Integer; 
        _moduleData.intValue = 0;

        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        _moduleData.intValue++;  
        SendMessageToManager();
    }
}