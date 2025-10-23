using UnityEngine;
using UnityEngine.UI;

public class UIManagerEnviador : MonoBehaviour
{
    public Slider puertaSlider;
    public Toggle bloqueoToggle;
    public Button accion1Button;
    public Button accion2Button;
    private ClientManager clientManager;
    private ServerManager serverManager;
    private UIManagerReceptor localReceptor;
    public void EnviarEstadoUI()
    {
        UIStateMessage uiState = new UIStateMessage();

        uiState.sliderPuertaValue = puertaSlider.value;
        uiState.toggleBloqueoValue = bloqueoToggle.isOn;
        uiState.accion1Interactable = accion1Button.interactable;
        uiState.accion2Interactable = accion2Button.interactable;

        string dataJson = JsonUtility.ToJson(uiState);

        NetMessage msgParaEnviar = new NetMessage("UI_Update", dataJson);

        EnviarMensaje(msgParaEnviar);

        string jsonFinal = JsonUtility.ToJson(msgParaEnviar);

        Debug.Log("Enviando estado: " + jsonFinal);
    }


    void Start()
    {
        puertaSlider.onValueChanged.AddListener((valor) => EnviarEstadoUI());
        bloqueoToggle.onValueChanged.AddListener((activo) => EnviarEstadoUI());

        accion1Button.onClick.AddListener(() => EnviarAccion1());

        clientManager = FindObjectOfType<ClientManager>();
        serverManager = FindObjectOfType<ServerManager>();

        if (serverManager != null)
        {
            localReceptor = FindObjectOfType<UIManagerReceptor>();
        }
    }

    public void EnviarAccion1()
    {
        NetMessage msgParaEnviar = new NetMessage("Accion1_Clicked", "");
        EnviarMensaje(msgParaEnviar);
        Debug.Log("Enviando Accion 1");
    }

    private void EnviarMensaje(NetMessage msg)
    {
        if (clientManager != null)
        {
            clientManager.SendMessageToServer(msg);
        }
        else if (serverManager != null)
        {
            serverManager.BroadcastMessage(msg);

            string jsonMsg = JsonUtility.ToJson(msg);
            if (localReceptor != null)
            {
                localReceptor.RecibirMensaje(jsonMsg);
            }
        }
    }
}