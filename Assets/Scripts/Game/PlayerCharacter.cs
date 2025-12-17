using UnityEngine;
using TMPro;

public class PlayerCharacter : MonoBehaviour
{
    public Animator animator;
    public TMP_Text nameLabel; // Opcional: para ver el nombre encima

    public void Setup(string playerName)
    {
        if (nameLabel != null) nameLabel.text = playerName;
    }

    public void PlayHappy()
    {
        if(animator != null) animator.SetTrigger("Happy");
    }

    public void PlaySad()
    {
        if(animator != null) animator.SetTrigger("Sad");
    }
}