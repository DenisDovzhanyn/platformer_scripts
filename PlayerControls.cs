
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    private Camera cam;

    public float moveSpeed;
    public float jumpForce;
    private Vector2 moveDir;
    private Vector2 lookInput;

    public float fallMultiplier;
    private float currFallMultiplier;

    private bool attemptingJump = false;
    private bool canJump = true;
    private bool doCameraTransition = false;

    private bool isGrounded = true;
    private bool isOnWall = false;
    private bool isExitingWall = false;
    public float maxWallTime;
    private float wallTimeCounter;
    private float lerpCount = 0;
    private float pitch = 0; // we keep our own pitch 

    private RaycastHit wallhit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //* set state in class definition
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currFallMultiplier = fallMultiplier;

        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();

    }

    // Update is called once per frame
    void Update()
    {
        //GatherInput();
        HandleCameraPitch();
        // because camera is attached to player and its rotation is relative to it, we rotate the player left/right
        if (doCameraTransition)
        {
            cam.fieldOfView += 60f * Time.deltaTime;
            if (cam.fieldOfView >= 70) doCameraTransition = false;
        }
        else if (cam.fieldOfView >= 60)
        {
            cam.fieldOfView -= 60f * Time.deltaTime;
        }

        rb.transform.Rotate(Vector3.up * lookInput.x);
    }

    void FixedUpdate()
    {
        // var move = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.y);
        UpdateGroundedState();
        HandleWallContact();
        HandleMovement();
        HandleJump();
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

    public void OnMove(InputValue value)
    {
        moveDir = value.Get<Vector2>() * moveSpeed;
    }

    public void OnJump(InputValue value)
    {
        attemptingJump = value.isPressed && canJump;
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void UpdateGroundedState()
    {
        Ray rayDown = new Ray(transform.position, Vector3.down);

        if (!Physics.Raycast(rayDown, 1.1f, 1 << 3))
        {
            isGrounded = false;
            return;
        }

        isGrounded = true;
        canJump = true;
        wallTimeCounter = maxWallTime;
        isExitingWall = false;
    }

    void HandleWallContact()
    {
        Ray rayLeft = new Ray(transform.position, transform.right * -1);
        Ray rayRight = new Ray(transform.position, transform.right);

        isOnWall = Physics.Raycast(rayLeft, out wallhit, 1, 1 << 6) || Physics.Raycast(rayRight, out wallhit, 1, 1 << 6);

        if (!isOnWall || isGrounded || isExitingWall)
        {
            rb.useGravity = true;
            return;
        }

        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
    }

    void HandleMovement()
    {
        Vector3 longitudanal = transform.forward;
        Vector3 lateral = transform.right;

        // makes player move either forward or backward depending on y input (W or S);
        longitudanal *= moveDir.y;
        // makes player move side to side relative to way they are facing depending on x input (A or D);
        lateral *= moveDir.x;
        // combines all these values together while keeping their vertical velocity;
        var move = longitudanal + lateral;
        var currentVelocityNoY = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (isGrounded)
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.VelocityChange);
            return;
        }

        if (!isOnWall || isExitingWall)
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.Acceleration);
            return;
        }

        // reflects the normal direction so that we are pushing against the
        rb.AddForce((-wallhit.normal * 5) + (move - currentVelocityNoY), ForceMode.VelocityChange);
        wallTimeCounter -= Time.deltaTime;
        if (wallTimeCounter <= 0) isExitingWall = true;
    }

    void HandleJump()
    {
        if (!attemptingJump || !canJump) return;

        attemptingJump = false;
        canJump = false;

        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) + Vector3.up * jumpForce;
            StartCoroutine(WaitToSetCanJump());
            return;
        }

        //* either player has already jumped or they have fallen off a ledge
        if (isOnWall)
        {
            isExitingWall = true;
            wallTimeCounter = 0;
            StartCoroutine(ExitingWall());
        }

        rb.linearVelocity = cam.transform.forward * (jumpForce * 3);
        //doCameraTransition = true;
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

        canJump = true;
    }

    IEnumerator ExitingWall()
    {
        yield return new WaitForSeconds(1f);

        isExitingWall = false;
    }

}
