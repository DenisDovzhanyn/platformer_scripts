
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
    private bool doCameraTransition = false;

    private bool isGrounded = true;
    private float timeSinceLastJump = 0;
    private float coyoteTime = 0.2f;
    private bool isOnWall = false;
    private bool canWallJump = true;
    private bool isExitingWall = false;
    public float maxWallTime;
    private float wallTimeCounter;

    private bool canClimbLedge;
    private bool hasClimbedLedge = false;
    private float ledgeY = 0;

    private float pitch = 0; // we keep our own pitch 
    public float mouseSens;
    private float rollLerp;

    private AudioSource audioPlayer;
    private AudioClip[] currentClip = new AudioClip[2];
    private float timeSinceLastPlayed = 0f;
    private bool playLandSound = false;
    private bool isLandSoundLoud;
    public AudioClip metalWalk;
    public AudioClip metalWalkTwo;

    public AudioClip sandWalk;
    public AudioClip sandWalkTwo;

    public AudioClip stoneWalk;
    public AudioClip stoneWalkTwo;

    public AudioClip metalLand;
    public AudioClip sandLand;
    public AudioClip stoneLand;

    private RaycastHit wallhit;
    // i do this because even if raycast hits nothing, wall hit will be given default values which i do not want for my cam roll
    private Vector3 wallNormal;
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
        timeSinceLastJump += Time.deltaTime;
        HandleCameraPitch();
        // because camera is attached to player and its rotation is relative to it, we rotate the player left/right
        UpdateCameraFov();
        rb.transform.Rotate(Vector3.up * (lookInput.x * mouseSens));
    }

    void FixedUpdate()
    {
        UpdateGroundedState();
        HandleWallContact();
        CheckIfCanClimbLedge();
        HandleMovement();
        HandleJump();
        HandleClimbLedge();
        HandleFall();
    }

    void LateUpdate()
    {
        cam.transform.localRotation = Quaternion.Euler(pitch, cam.transform.localRotation.y, cam.transform.localRotation.z);
        UpdateCameraOrientationOnWall();
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

        if (isGrounded || coyoteTime > 0 || (isOnWall && canWallJump))
        {
            attemptingJump = true;
        }
        else
        {
            attemptingJump = false;
        }
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

    void UpdateCameraOrientationOnWall()
    {
        float dot = Vector3.Dot(wallNormal, transform.right);
        //why -mathf.sign()?  a negative z value = rolling cam right and vice versa
        //if i have a wall normal facing (1,0,0), and my transform.right is (1,0,0)
        //my dot product is 1, meaning that the wall is to my left, if i multiply that by 10 or multiply mathf.sign(dot) (which will equal 1)
        //by 10, then that mean i will rotate my z positively by 10, which in turn will roll the camera to the left, making me turn into the wall
        // so instead we take the opposite sign, letting us tilt in the direction the wall is facing
        Quaternion roll = Quaternion.Euler(pitch, cam.transform.localRotation.y, 10 * -Mathf.Sign(dot));
        
        cam.transform.localRotation = Quaternion.Lerp(cam.transform.localRotation, roll, rollLerp);
        
    }

    //shows where we are casting overlapsphere
    // void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position + (transform.forward * 0.35f) + (Vector3.up * 0.6f), 0.7f); // Start position

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
            playLandSound = coyoteTime < 0;
            isLandSoundLoud = coyoteTime < -1;
            return;
        }

        if (hits[0].CompareTag("sand"))
        {
            currentClip[0] = Random.value > 0.5f ? sandWalk : sandWalkTwo;
            currentClip[1] = sandLand;
        }
        else if (hits[0].CompareTag("metal"))
        {
            currentClip[0] = Random.value > 0.5f ? metalWalk : metalWalkTwo;
            currentClip[1] = metalLand;
        }
        else if (hits[0].CompareTag("stone"))
        {
            currentClip[0] = Random.value > 0.5f ? stoneWalk : stoneWalkTwo;
            currentClip[1] = stoneLand;
        }

        coyoteTime = 0.2f;
        isGrounded = true;
        wallTimeCounter = maxWallTime;
        isExitingWall = false;
        rb.linearDamping = defaultLinearDampening;
        canWallJump = true;
        hasClimbedLedge = false;
    }

    void HandleWallContact()
    {
        Ray rayLeft = new Ray(transform.position, transform.right * -1);
        Ray rayRight = new Ray(transform.position, transform.right);

        // we dont wanna say we are on a wall if we are grounded AND touching a wall yk
        isOnWall = (Physics.Raycast(rayLeft, out wallhit, 1, 1 << 6) || Physics.Raycast(rayRight, out wallhit, 1, 1 << 6)) && !isGrounded;

        if (!isOnWall || isGrounded || isExitingWall)
        {
            rollLerp -= Time.deltaTime * 5;
            rollLerp = Mathf.Clamp(rollLerp, 0, 1);
            return;
        }

        wallNormal = wallhit.normal;
        rollLerp += Time.deltaTime * 5;
        rollLerp = Mathf.Clamp(rollLerp, 0, 1);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y > 1 ? rb.linearVelocity.y : 0, rb.linearVelocity.z);
    }

    void CheckIfCanClimbLedge()
    {
        Vector3 spherePosition = transform.position + (transform.forward * 0.35f)+ (Vector3.up * 0.6f);
        // only take in floor and wall collisions, basically want to make sure we are touching a floor AND wall bc thats prob a ledge right
        Collider[] hits = Physics.OverlapSphere(spherePosition, 0.7f, 1 << 3 | 1 << 6);
        bool isTouchingFloor = false;
        bool isTouchingWall = false;
        
        foreach (Collider col in hits)
        {
            // dont wanna keep looping if both conditions are met
            if (isTouchingFloor && isTouchingWall) break;

            if (col.gameObject.layer == 3)
            {
                isTouchingFloor = true;
                ledgeY = col.gameObject.transform.position.y;
            }
            else if (col.gameObject.layer == 6) isTouchingWall = true;
        }

        // check for grounded too because i dont want to apply jump force later on just bc we are standing near a ledge
        if (!isTouchingFloor || !isTouchingWall || isGrounded)
        {
            canClimbLedge = false;
            return;
        }

        canClimbLedge = true;

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
            if (playLandSound)
            {
                timeSinceLastPlayed = 0.1f;
                audioPlayer.PlayOneShot(currentClip[1], isLandSoundLoud ? 0.8f : 0.5f);
                playLandSound = false;
                isLandSoundLoud = false;
            }
            else if (timeSinceLastPlayed > 0.25f && move.magnitude > 5f)
            {
                timeSinceLastPlayed = 0;
                audioPlayer.PlayOneShot(currentClip[0], 0.5f);
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
        if (!attemptingJump) return;
        attemptingJump = false;

        rb.linearDamping = defaultLinearDampening / 2;
        if (isGrounded || coyoteTime > 0)
        {
            if (timeSinceLastJump < 0.5f) return;
            timeSinceLastJump = 0;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) + Vector3.up * jumpForce;
            audioPlayer.PlayOneShot(currentClip[0], 0.8f);
            return;
        }

        isExitingWall = true;
        canWallJump = false;
        wallTimeCounter = 0;
        rb.linearVelocity = (cam.transform.forward * jumpForce) + (wallhit.normal * jumpForce / 2) + (Vector3.up * jumpForce);
    }

    void HandleClimbLedge()
    {
        if (!canClimbLedge || hasClimbedLedge) return;

        Debug.Log("WE ARE CLIMBING");
        rb.linearVelocity = Vector3.up * jumpForce;
        isExitingWall = true;
        canClimbLedge = false;
        hasClimbedLedge = true;
    }

    void HandleFall()
    {
        if (isGrounded) return;

        // basically what this does is that like, we add extra gravity. if we want gravity to be normal like default unity physics,
        // then we set fall multiplier to 1, because 1 - 1 = 0 => physics.gravity.y * 0 = 0. so no additional gravity will be added because
        // engine already applying gravity
        float extraDownVel = Physics.gravity.y * (currFallMultiplier - 1);
        rb.AddForce(Vector3.up * extraDownVel, ForceMode.Acceleration);

    }

    IEnumerator ExitingWall()
    {
        yield return new WaitForSeconds(1f);

        isExitingWall = false;
    }

}
