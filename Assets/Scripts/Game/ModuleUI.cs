using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ModuleUI : MonoBehaviour
{
    public Button button;
    public TMP_Text label;
    public Image background;
    
    private int index;
    private Action<int> onClickAction;

    public void Setup(int slotIndex, Action<int> callback)
    {
        index = slotIndex;
        onClickAction = callback;
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        onClickAction?.Invoke(index);
    }

    public void Refresh(string text, bool active)
    {
        label.text = active ? text : "";
        button.interactable = active;
        background.color = active ? Color.white : Color.gray;
    }
}