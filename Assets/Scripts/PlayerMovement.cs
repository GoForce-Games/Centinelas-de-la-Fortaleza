using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;

    [Header("Settings")]
    public float speed = 2f;
    public float leftDistance = 6f;
    public float waitTime = 1f;

    [Header("Animations")]
    public string walkAnim = "Walking";
    public string idleAnim = "Idle";

    private Vector3 startPos;
    private Animator anim;
    private bool isBusy = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        startPos = transform.position;

        if (anim != null) anim.applyRootMotion = false;

        if (startButton != null)
            startButton.onClick.AddListener(StartRoutine);
    }

    void StartRoutine()
    {
        if (isBusy) return;
        StartCoroutine(PatrolPath());
    }

    IEnumerator PatrolPath()
    {
        isBusy = true;

        // 1. IDA (Izquierda)
        Vector3 targetLeft = startPos - new Vector3(leftDistance, 0, 0);
        
        FaceDirection(Vector3.left); 
        PlayAnim(walkAnim);

        while (Vector3.Distance(transform.position, targetLeft) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetLeft, speed * Time.deltaTime);
            yield return null;
        }

        // 2. ESPERA
        transform.position = targetLeft;
        PlayAnim(idleAnim);
        yield return new WaitForSeconds(waitTime);

        // 3. VUELTA (Derecha)
        FaceDirection(Vector3.right); // Aquí rota 180 grados
        PlayAnim(walkAnim);

        while (Vector3.Distance(transform.position, startPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, speed * Time.deltaTime);
            yield return null;
        }

        // 4. FINAL
        transform.position = startPos;
        PlayAnim(idleAnim);
        
        // Opcional: volver a mirar a la izquierda al terminar
        FaceDirection(Vector3.left); 

        isBusy = false;
    }

    void PlayAnim(string name)
    {
        if (anim != null) anim.Play(name);
    }

    void FaceDirection(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction);
    }
}