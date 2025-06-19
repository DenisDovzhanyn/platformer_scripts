using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    private Camera cam;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;

    public float moveSpeed;
    public float jumpForce;
    private Vector2 moveDir;
    private Vector2 lookInput;
   
    public float fallMultiplier;
    private float currFallMultiplier;

    private bool jump;
    private bool canJump;
    private bool hasDoubleJumped;

    private bool isGrounded;
    private bool isOnWall;
    public float maxWallTime;
    private float wallTimeCounter;

    private float pitch; // we keep our own pitch 

    private RaycastHit wallhit;
    private State state;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    enum State
    {
        ExitingWall,
        Something
    }

    void Start()
    {
        state = State.Something;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        pitch = 0f;

        isOnWall = false;
        currFallMultiplier = fallMultiplier;
        
        canJump = true;
        jump = false;
        isGrounded = true;
        hasDoubleJumped = false;

        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();

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
            jump = true;
        }
    }

    void FixedUpdate()
    {
        // var move = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.y);
        CheckGround();
        CheckWall();
        if (isOnWall && !isGrounded && state != State.ExitingWall)
        {
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
        else
        {
            rb.useGravity = true;
        }
        HandleMovement();
        if (jump) HandleJump();
        HandleFall();
    }

    void LateUpdate()
    {
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void HandleCameraPitch()
    {
        // even though it hurts my head, we subtract because in 'eular angles' a positive pitch makes us point down on the x axis
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, -90, 90);
    }

    void GatherInput()
    {
        moveDir = moveAction.ReadValue<Vector2>() * moveSpeed;
        lookInput = lookAction.ReadValue<Vector2>();
    }

    void CheckGround()
    {
        Ray rayDown = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(rayDown, 1.5f, 1 << 3))
        {
            isGrounded = true;
            hasDoubleJumped = false;
            canJump = true;
            wallTimeCounter = maxWallTime;
            state = State.Something;
        }
        else
        {
            isGrounded = false;
        }
    }

    void CheckWall()
    {
        Ray rayLeft = new Ray(transform.position, transform.right * -1);
        Ray rayRight = new Ray(transform.position, transform.right);
        if (Physics.Raycast(rayLeft, out wallhit, 1, 1 << 6) || Physics.Raycast(rayRight, out wallhit, 1, 1 << 6))
        {
            isOnWall = true;
        }
        else
        {
            isOnWall = false;
        }
    }

    void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;


        //* if we want to keep speed when in air, instead of multiplying forward by the input, we multiply forward by the magnitude/vel of x and z
        //* this way we can like strafe/ do those surf movements by directing our velocity in the direction we are facing mid air
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
        else if (!isGrounded && isOnWall && state != State.ExitingWall)
        {
            // reflects the normal direction so that we are pushing against the
            rb.AddForce((-wallhit.normal * 5) + (move - currentVelocityNoY), ForceMode.VelocityChange);
            wallTimeCounter -= Time.deltaTime;
            if (wallTimeCounter <= 0) state = State.ExitingWall;
        }
        else
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.Acceleration);
        }
    }

    void HandleJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) + Vector3.up * jumpForce;
            jump = false;

            canJump = false;
            StartCoroutine(WaitToSetCanJump());
        }
        else if (!hasDoubleJumped && canJump)
        {
            if (isOnWall)
            {
                state = State.ExitingWall;
                wallTimeCounter = 0;
                StartCoroutine(ExitingWall());
            }
            rb.linearVelocity = cam.transform.forward * (jumpForce * 3);
            jump = false;
            hasDoubleJumped = true;
            canJump = false;
        }
    }

    void HandleFall()
    {
        if (rb.linearVelocity.y < 0)
        {

            // basically what this does is that like, we add extra gravity. if we want gravity to be normal like default unity physics,
            // then we set fall multiplier to 1, because 1 - 1 = 0 => physics.gravity.y * 0 = 0. so no additional gravity will be added because
            // engine already applying gravity
            float extraDownVel = Physics.gravity.y * (currFallMultiplier - 1);
            //rb.linearVelocity += Vector3.up * (extraDownVel * Time.fixedDeltaTime);
            rb.AddForce(Vector3.up * extraDownVel, ForceMode.Acceleration);
            //rb.linearVelocity = new Vector3(rb.linearVelocity.x, fallRate , rb.linearVelocity.z);
        }
    }

    
    IEnumerator WaitToSetCanJump()
    {
        yield return new WaitForSeconds(0.5f);

        if (!hasDoubleJumped) canJump = true;
    }

    IEnumerator ExitingWall()
    {
        yield return new WaitForSeconds(1f);

        state = State.Something;
    }
}
