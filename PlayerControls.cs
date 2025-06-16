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
    private Vector2 moveDir;
    public float jumpForce;
    public float fallRate;
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

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    // Update is called once per frame
    void Update()
    {
        
        moveDir = moveAction.ReadValue<Vector2>() * moveSpeed;

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

        rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.y);

        if (jump)
        {
            Debug.Log("adding jump force");
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            //rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jump = false;
        }

        if (rb.linearVelocity.y < 0)
        {
            //rb.linearVelocity = new Vector3(rb.linearVelocity.x, fallRate , rb.linearVelocity.z);
            rb.linearVelocity += Vector3.up * (fallRate * Time.fixedDeltaTime);
            // if this doesnt work we can set it back to (physics.gravity.y * fallrate * time.timeimttietiteimteit)
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
        yield return new WaitForSeconds(0.5f);

        if (!hasDoubleJumped) canJump = true;
        Debug.Log("coroutine finished");
        Debug.Log($"canJump is {canJump}");
    }
}
