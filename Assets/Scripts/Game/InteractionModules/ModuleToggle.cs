using Game;
using Game.InteractionModules;
using UnityEngine;
using UnityEngine.UI;

public class ModuleToggle : InteractableModule
{
    [SerializeField] private Toggle toggle;

    private void Start()
    {
        _moduleData.moduleName = "ModuleToggle";
        _moduleData.ValueTypes = SentDataTypes.Bool;

        toggle.onValueChanged.AddListener(OnToggleChange);
    }

    private void OnToggleChange(bool value)
    {
        _moduleData.boolValue = value;
        SendMessageToManager();
    }

    public override void UpdateState(ModuleData data)
    {
        throw new System.NotImplementedException();
    }
}
