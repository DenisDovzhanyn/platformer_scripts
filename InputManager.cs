using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    [HideInInspector]
    public Vector2 moveInput { get; private set; }
    [HideInInspector]
    public Vector2 lookInput { get; private set; }
    [HideInInspector]
    public UnityEvent jumpAttempt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    // this feels so stupid, i listen for an event just to fire another one off?
    public void OnJump(InputValue value)
    {
        jumpAttempt.Invoke();
    }
}
