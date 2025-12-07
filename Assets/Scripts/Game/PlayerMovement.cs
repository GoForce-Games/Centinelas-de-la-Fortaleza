using UnityEngine;
using UnityEngine.UI;

public enum MovementState
{
    Idle,
    MovingLeft,
    Waiting,
    MovingRight
}

public class PlayerMovement : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;

    [Header("Settings")]
    public float movementSpeed = 2f;
    public float rotationSpeed = 10f;
    public float smoothFactor = 10f;
    public float leftDistance = 6f;
    public float waitTime = 1f;

    [Header("Animations")]
    public string walkAnim = "Walking";
    public string idleAnim = "Idle";

    private Animator anim;
    private MovementState currentState;
    private Vector3 startPos;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float waitTimer;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim != null) anim.applyRootMotion = false;

        startPos = transform.position;
        
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        SetState(MovementState.Idle);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);
    }

    void Update()
    {
        ProcessStateLogic();
        ApplyTransformInterpolation();
    }

    void OnStartButtonPressed()
    {
        if (currentState == MovementState.Idle)
        {
            SetState(MovementState.MovingLeft);
        }
    }

    public void SetState(MovementState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case MovementState.Idle:
                PlayAnim(idleAnim);
                targetRotation = Quaternion.LookRotation(Vector3.left);
                break;

            case MovementState.MovingLeft:
                PlayAnim(walkAnim);
                targetRotation = Quaternion.LookRotation(Vector3.left);
                break;

            case MovementState.Waiting:
                PlayAnim(idleAnim);
                waitTimer = waitTime;
                break;

            case MovementState.MovingRight:
                PlayAnim(walkAnim);
                targetRotation = Quaternion.LookRotation(Vector3.right);
                break;
        }
    }

    void ProcessStateLogic()
    {
        switch (currentState)
        {
            case MovementState.MovingLeft:
                Vector3 destLeft = startPos - Vector3.right * leftDistance;
                targetPosition = Vector3.MoveTowards(targetPosition, destLeft, movementSpeed * Time.deltaTime);

                if (Vector3.Distance(targetPosition, destLeft) < 0.01f)
                {
                    SetState(MovementState.Waiting);
                }
                break;

            case MovementState.Waiting:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    SetState(MovementState.MovingRight);
                }
                break;

            case MovementState.MovingRight:
                targetPosition = Vector3.MoveTowards(targetPosition, startPos, movementSpeed * Time.deltaTime);

                if (Vector3.Distance(targetPosition, startPos) < 0.01f)
                {
                    SetState(MovementState.Idle);
                }
                break;
        }
    }

    void ApplyTransformInterpolation()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void PlayAnim(string name)
    {
        if (anim != null) anim.Play(name);
    }
}