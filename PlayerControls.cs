
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class NewBehaviourScript : MonoBehaviour
{
    private Rigidbody rb;
    private float defaultLinearDampening;
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
    private float coyoteTime = 0.2f;
    private bool isOnWall = false;
    private bool isExitingWall = false;
    public float maxWallTime;
    private float wallTimeCounter;
    private float pitch = 0; // we keep our own pitch 
    public float mouseSens;

    private AudioSource audioPlayer;
    private AudioClip currentClip;
    private float timeSinceLastPlayed = 0f;
    public AudioClip metalWalk;
    public AudioClip metalWalkTwo;

    public AudioClip sandWalk;
    public AudioClip sandWalkTwo;

    public AudioClip metalLand;
    public AudioClip sandLand;

    private RaycastHit wallhit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //* set state in class definition
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currFallMultiplier = fallMultiplier;

        rb = GetComponent<Rigidbody>();
        audioPlayer = GetComponent<AudioSource>();
        cam = GetComponentInChildren<Camera>();

        defaultLinearDampening = rb.linearDamping;
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLastPlayed += Time.deltaTime;
        
        HandleCameraPitch();
        // because camera is attached to player and its rotation is relative to it, we rotate the player left/right
        UpdateCameraFov();
        rb.transform.Rotate(Vector3.up * (lookInput.x * mouseSens));
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
        pitch -= lookInput.y * mouseSens;
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

    void UpdateCameraFov()
    {
        if (!doCameraTransition && cam.fieldOfView <= 60) return;
        float fovIncAmount = 80f * Time.deltaTime;
        //* does a bouncy zoom effect

        if (!doCameraTransition) fovIncAmount *= -1;
        else if (cam.fieldOfView >= 70) doCameraTransition = false;

        cam.fieldOfView += fovIncAmount;
    }

    // shows where we are casting overlapsphere
    // void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position + (Vector3.down * 0.7f), 0.5f); // Start position

    //     Debug.Log(isGrounded);
    // }
    void UpdateGroundedState()
    {
        Vector3 spherePosition = transform.position + (Vector3.down * 0.7f);
        Collider[] hits = Physics.OverlapSphere(spherePosition, 0.5f, 1 << 3);

        if (hits.Length == 0)
        {
            isGrounded = false;
            coyoteTime -= Time.deltaTime;
            return;
        }
        if (hits[0].CompareTag("sand")) currentClip = Random.value > 0.5f ? sandWalk : sandWalkTwo;
        else if (hits[0].CompareTag("metal")) currentClip = Random.value > 0.5f ? metalWalk : metalWalkTwo;

        coyoteTime = 0.2f;
        isGrounded = true;
        canJump = true;
        wallTimeCounter = maxWallTime;
        isExitingWall = false;
        rb.linearDamping = defaultLinearDampening;
    }

    void HandleWallContact()
    {
        Ray rayLeft = new Ray(transform.position, transform.right * -1);
        Ray rayRight = new Ray(transform.position, transform.right);

        isOnWall = Physics.Raycast(rayLeft, out wallhit, 1, 1 << 6) || Physics.Raycast(rayRight, out wallhit, 1, 1 << 6);

        if (!isOnWall || isGrounded || isExitingWall)
        {
            //rb.useGravity = true;
            return;
        }

        //rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y > 1 ? rb.linearVelocity.y : 0, rb.linearVelocity.z);
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
            if (timeSinceLastPlayed > 0.25f && move.magnitude > 5f)
            {
                timeSinceLastPlayed = 0;
                audioPlayer.PlayOneShot(currentClip);
            }

            
            rb.AddForce(move - currentVelocityNoY, ForceMode.VelocityChange);
            return;
        }

        if (!isOnWall || isExitingWall)
        {
            rb.AddForce(move - currentVelocityNoY, ForceMode.Acceleration);
            return;
        }

        // reflects the normal direction so that we are pushing against the
        //rb.AddForce((-wallhit.normal * 5) + (move - currentVelocityNoY), ForceMode.VelocityChange);
        rb.AddForce(-wallhit.normal + (move - currentVelocityNoY), ForceMode.Acceleration);
        wallTimeCounter -= Time.deltaTime;

        if (wallTimeCounter <= 0 || currentVelocityNoY.magnitude <= 5) isExitingWall = true;
    }

    void HandleJump()
    {
        if (!attemptingJump || !canJump) return;

        attemptingJump = false;
        canJump = false;

        rb.linearDamping = defaultLinearDampening / 2;
        if (isGrounded || (!isGrounded && coyoteTime > 0))
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

        rb.linearVelocity = cam.transform.forward * (jumpForce * 2);
        doCameraTransition = true;
    }

    void HandleFall()
    {
        if (isGrounded) return;

        // basically what this does is that like, we add extra gravity. if we want gravity to be normal like default unity physics,
        // then we set fall multiplier to 1, because 1 - 1 = 0 => physics.gravity.y * 0 = 0. so no additional gravity will be added because
        // engine already applying gravity
        float extraDownVel = Physics.gravity.y * (currFallMultiplier - 1);
        //rb.linearVelocity += Vector3.up * (extraDownVel * Time.fixedDeltaTime);
        rb.AddForce(Vector3.up * extraDownVel, ForceMode.Acceleration);
        //rb.linearVelocity = new Vector3(rb.linearVelocity.x, fallRate , rb.linearVelocity.z);
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
