using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManagerReceptor : MonoBehaviour
{
    private Dictionary<string, Slider> sliders = new();
    private Dictionary<string, Toggle> toggles = new();
    private Dictionary<string, Button> buttons = new();

    void Start()
    {
        foreach (var s in FindObjectsOfType<Slider>())
            sliders[s.name] = s;

        foreach (var t in FindObjectsOfType<Toggle>())
            toggles[t.name] = t;

        foreach (var b in FindObjectsOfType<Button>())
            buttons[b.name] = b;
    }

    public void RecibirMensaje(string jsonFinal)
    {
        NetMessage netMsg = JsonUtility.FromJson<NetMessage>(jsonFinal);

        if (netMsg.msgType == "UI_Update")
        {
            UIStateMessage state = JsonUtility.FromJson<UIStateMessage>(netMsg.msgData);

            foreach (var p in state.sliders)
                if (sliders.ContainsKey(p.Key))
                    sliders[p.Key].value = p.Value;

            foreach (var p in state.toggles)
                if (toggles.ContainsKey(p.Key))
                    toggles[p.Key].isOn = p.Value;

            foreach (var p in state.buttons)
                if (buttons.ContainsKey(p.Key))
                    buttons[p.Key].interactable = p.Value;

            Debug.Log("UI actualizada desde la red.");
        }

        else if (netMsg.msgType == "Accion1_Clicked")
        {
            Debug.Log($"El otro jugador pulsó: {netMsg.msgData}");
        }
    }
}
