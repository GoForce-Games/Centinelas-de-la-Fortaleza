using Game;
using Game.InteractionModules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModuleButton : InteractableModule
{
    [SerializeField] private Button button;

    private new void Start()
    {
        base.Start();
        _moduleData.moduleName = "ModuleButton";
        _moduleData.ValueTypes = SentDataTypes.Integer | SentDataTypes.Bool; 
        _moduleData.intValue = 0;
        _moduleData.boolValue = true;

        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        _moduleData.intValue++;  
        SendMessageToManager();
    }

    public override void UpdateState(ModuleData data)
    {
    }
}