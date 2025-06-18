using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class NewBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;
    private Camera camera;
    public float moveSpeed;
    private Vector2 moveDir;
    private Vector2 lookInput;
    public float jumpForce;
    public float fallMultiplier;
    private bool jump;
    private bool isGrounded;
    private bool hasDoubleJumped;
    private bool canJump;
    private float pitch; // we keep our own pitch 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pitch = 0f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        canJump = true;
        jump = false;
        isGrounded = true;
        hasDoubleJumped = false;

        camera = GetComponentInChildren<Camera>();
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        lookAction = InputSystem.actions.FindAction("Look");
        
    }

    // Update is called once per frame
    void Update()
    {
        GatherInput();
        HandleCameraPitch();

        // because camera is attached to player and its rotation is relative to it, we rotate the player left/right
        rb.transform.Rotate(Vector3.up * lookInput.x);
        
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
        // var move = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.y);
        HandleMovement();
        HandleJump();
        HandleFall();
    }

    void LateUpdate()
    {
        camera.transform.localRotation = Quaternion.Euler(pitch, 0,0);
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

    void HandleCameraPitch()
    {
        // even though it hurts my head, we subtract because in 'eular angles' a positive pitch makes us point down on the x axis
        pitch -= lookInput.y ;
        pitch = Mathf.Clamp(pitch, -90, 90);
    }

    void GatherInput()
    {
        moveDir = moveAction.ReadValue<Vector2>() * moveSpeed;
        lookInput = lookAction.ReadValue<Vector2>();
    }

    void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // makes player move either forward or backward depending on y input (W or S);
        forward *= moveDir.y;
        // makes player move side to side relative to way they are facing depending on x input (A or D);
        right *= moveDir.x;
        // combines all these values together while keeping their vertical velocity;
        //var move = forward + right + new Vector3(0,rb.linearVelocity.y,0);
        var move = forward + right;
        var currentVelocityNoY = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (isGrounded)
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.VelocityChange);
        }
        else
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.Acceleration);
        }
        Debug.Log(rb.linearVelocity.magnitude);
    }

    void HandleJump()
    {
        if (jump)
        {
            Debug.Log("adding jump force");
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) + Vector3.up * jumpForce ; 
            //rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jump = false;
        }
    }

    void HandleFall()
    {
        if (rb.linearVelocity.y < 0)
        {

            // basically what this does is that like, we add extra gravity. if we want gravity to be normal like default unity physics,
            // then we set fall multiplier to 1, because 1 - 1 = 0 => physics.gravity.y * 0 = 0. so no additional gravity will be added because
            // engine already applying gravity
            float extraDownVel = Physics.gravity.y * (fallMultiplier - 1);
            //rb.linearVelocity += Vector3.up * (extraDownVel * Time.fixedDeltaTime);
            rb.AddForce(Vector3.up * extraDownVel, ForceMode.Acceleration);
            //rb.linearVelocity = new Vector3(rb.linearVelocity.x, fallRate , rb.linearVelocity.z);
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
