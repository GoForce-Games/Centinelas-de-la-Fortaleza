using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManagerEnviador : MonoBehaviour
{
    private ClientManager clientManager;
    private ServerManager serverManager;

    private List<Slider> sliders = new();
    private List<Toggle> toggles = new();
    private List<Button> buttons = new();

    void Start()
    {
        clientManager = FindObjectOfType<ClientManager>();
        serverManager = FindObjectOfType<ServerManager>();

        sliders.AddRange(FindObjectsOfType<Slider>());
        toggles.AddRange(FindObjectsOfType<Toggle>());
        buttons.AddRange(FindObjectsOfType<Button>());

        foreach (var s in sliders)
            s.onValueChanged.AddListener((_) => EnviarEstadoUI());

        foreach (var t in toggles)
            t.onValueChanged.AddListener((bool activo) =>
            {
                EnviarEstadoUI();
                Debug.Log($"Toggle {t.name} está ahora {(activo ? "ON" : "OFF")}");
            });

        foreach (var b in buttons)
            b.onClick.AddListener(() => EnviarAccion(b));
    }

    public void EnviarEstadoUI()
    {
        UIStateMessage state = new UIStateMessage();

        foreach (var s in sliders)
            state.sliders[s.name] = s.value;

        foreach (var t in toggles)
            state.toggles[t.name] = t.isOn;

        foreach (var b in buttons)
            state.buttons[b.name] = b.interactable;

        string json = JsonUtility.ToJson(state);
        EnviarMensaje(new NetMessage("UI_Update", json));

        Debug.Log("Estado UI enviado.");
    }

    public void EnviarAccion(Button b)
    {
        EnviarMensaje(new NetMessage("Button_Clicked", b.name));
        Debug.Log($"Enviando acción: {b.name}");
    }


    private void EnviarMensaje(NetMessage msg)
    {
        if (clientManager != null)
            clientManager.SendMessageToServer(msg);
        else
            serverManager.BroadcastMessage(msg);
    }
}
