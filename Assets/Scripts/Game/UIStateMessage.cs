[System.Serializable]
public class UIStateMessage
{
    // Para el Slider (guardamos su valor)
    public float sliderPuertaValue;

    // Para el Toggle (guardamos si está activo)
    public bool toggleBloqueoValue;

    // Para los Botones (normalmente no se "serializa" un botón,
    // pero sí puedes querer sincronizar si está activo/inactivo)
    public bool accion1Interactable;
    public bool accion2Interactable;
}
