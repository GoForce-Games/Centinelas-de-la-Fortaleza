using UnityEngine;
using UnityEngine.UI;

public class UIManagerEnviador : MonoBehaviour
{
    public Slider puertaSlider;
    public Toggle bloqueoToggle;
    public Button accion1Button;
    public Button accion2Button;

    public void EnviarEstadoUI()
    {
        UIStateMessage uiState = new UIStateMessage();

        uiState.sliderPuertaValue = puertaSlider.value;
        uiState.toggleBloqueoValue = bloqueoToggle.isOn;
        uiState.accion1Interactable = accion1Button.interactable;
        uiState.accion2Interactable = accion2Button.interactable;

        string dataJson = JsonUtility.ToJson(uiState);

        NetMessage msgParaEnviar = new NetMessage("UI_Update", dataJson);

        string jsonFinal = JsonUtility.ToJson(msgParaEnviar);

        Debug.Log("Enviando estado: " + jsonFinal);
    }


    void Start()
    {
        puertaSlider.onValueChanged.AddListener((valor) => EnviarEstadoUI());
        bloqueoToggle.onValueChanged.AddListener((activo) => EnviarEstadoUI());
    }
}