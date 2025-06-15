using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class NewBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private Camera camera;
    public float moveSpeed;
    private Vector3 moveDir;
    public float jumpForce;
    private bool jump;
    private bool isGrounded;
    private bool hasDoubleJumped;
    private bool canJump;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canJump = true;
        jump = false;
        isGrounded = true;
        hasDoubleJumped = false;
        camera = GetComponentInChildren<Camera>();
        rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = moveSpeed;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 movement = moveAction.ReadValue<Vector2>();

        moveDir = new Vector3(movement.x, 0, movement.y) * moveSpeed;
        if (sprintAction.IsPressed())
        {
            camera.fieldOfView = 90;
        }
        else if (camera.fieldOfView != 60)
        {
            camera.fieldOfView = 60;
        }


        if (jumpAction.IsPressed() && canJump)
        {
            Debug.Log("jump action started");
            hasDoubleJumped = !isGrounded;
            isGrounded = false;
            jump = true;
            canJump = false;
            // coroutine here waiting .5 to set it back to true ?
            StartCoroutine(WaitToSetCanJump());
        }
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.magnitude < moveSpeed) // this factors in y magnitude too btw jesus
        {
            rb.AddForce(moveDir, ForceMode.Impulse);
        }

        if (jump)
        {
            Debug.Log("adding jump force");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jump = false;
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 3)
        {
            Debug.Log("Touched a floor");
            isGrounded = true;
            hasDoubleJumped = false;
            canJump = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == 3)
        {
            Debug.Log("Left floor");
            isGrounded = false;
        }
    }

    IEnumerator WaitToSetCanJump()
    {
        yield return new WaitForSeconds(1);

        if (!hasDoubleJumped) canJump = true;
        Debug.Log("coroutine finished");
        Debug.Log($"canJump is {canJump}");
    }
}
