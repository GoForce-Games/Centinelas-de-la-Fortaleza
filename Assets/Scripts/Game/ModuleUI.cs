using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ModuleUI : MonoBehaviour
{
    public Button button;
    public Slider slider;
    public Toggle toggle;
    public TMP_Text label;
    public Image background;
    public CanvasGroup canvasGroup;

    private int index;
    private Action<int, float> onClickAction;
    private bool isActive;
    private float fadeSpeed = 0.1f;
    private Coroutine debounceCoroutine;

    public void Setup(int slotIndex, Action<int, float> callback)
    {
        index = slotIndex;
        onClickAction = callback;

        if(button != null) 
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnInteract(1f));
        }
        
        if(slider != null) 
        {
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = 4;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(OnSliderChanged);
        }
        
        if(toggle != null) 
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((val) => OnInteract(val ? 1f : 0f));
        }
    }

    private void Update()
    {
        if (isActive && canvasGroup != null)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            if (canvasGroup.alpha < 0.2f) canvasGroup.alpha = 0.2f;
        }
    }

    private void OnSliderChanged(float val)
    {
        if (debounceCoroutine != null) StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(SendSliderValue(val));
    }

    private IEnumerator SendSliderValue(float val)
    {
        yield return new WaitForSeconds(0.25f);
        OnInteract(val);
    }

    private void OnInteract(float value)
    {
        onClickAction?.Invoke(index, value);
    }

    public void Refresh(string text, bool active)
    {
        isActive = active;
        
        if (active)
        {
            if(canvasGroup != null) canvasGroup.alpha = 1f;
            if(label != null) label.text = index.ToString(); 
            if(background != null) background.color = Color.white;
        }
        else
        {
            if(canvasGroup != null) canvasGroup.alpha = 0.5f;
            if(label != null) label.text = "";
            if(background != null) background.color = Color.gray;
        }

        if(button != null) button.interactable = true;
        if(slider != null) slider.interactable = true;
        if(toggle != null) toggle.interactable = true;
    }
}