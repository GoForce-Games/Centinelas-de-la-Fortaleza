using Game;
using Game.InteractionModules;
using UnityEngine;
using UnityEngine.UI;

public class ModuleSlider : InteractableModule
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        _moduleData.moduleName = "ModuleSlider";
        _moduleData.ValueTypes = SentDataTypes.Float;

        slider.onValueChanged.AddListener(OnSliderChange);
    }

    private void OnSliderChange(float value)
    {
        _moduleData.floatValue = value;
        SendMessageToManager();
    }
}
