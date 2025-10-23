using UnityEngine;
using UnityEngine.UI;

public class UIManagerReceptor : MonoBehaviour
{
    public Slider puertaSlider;
    public Toggle bloqueoToggle;
    public Button accion1Button;
    public Button accion2Button;

    public void RecibirMensaje(string jsonFinal)
    {
        NetMessage netMsg = JsonUtility.FromJson<NetMessage>(jsonFinal);

        if (netMsg.msgType == "UI_Update")
        {
            UIStateMessage uiState = JsonUtility.FromJson<UIStateMessage>(netMsg.msgData);

            puertaSlider.value = uiState.sliderPuertaValue;
            bloqueoToggle.isOn = uiState.toggleBloqueoValue;
            accion1Button.interactable = uiState.accion1Interactable;
            accion2Button.interactable = uiState.accion2Interactable;
            
            Debug.Log("UI actualizada desde la red.");
        }
        else if (netMsg.msgType == "Accion1_Clicked")
        {
            Debug.Log("¡El otro cliente ha pulsado Accion 1!");
        }
    }
}