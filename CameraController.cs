using UnityEngine;

public class CameraController : MonoBehaviour
{
    private PlayerController playerController;
    private float rollLerp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (playerController.isOnWall && !playerController.isExitingWall) rollLerp += Time.deltaTime * 5;
        else rollLerp -= Time.deltaTime * 5;

        rollLerp = Mathf.Clamp(rollLerp, 0, 1);

        transform.localRotation = Quaternion.Euler(playerController.pitch, transform.localRotation.y, transform.localRotation.z);
        UpdateCameraOrientationOnWall();
    }
    

    void UpdateCameraOrientationOnWall()
    {
        float dot = Vector3.Dot(playerController.wallNormal, transform.right);
        //why -mathf.sign()?  a negative z value = rolling cam right and vice versa
        //if i have a wall normal facing (1,0,0), and my transform.right is (1,0,0)
        //my dot product is 1, meaning that the wall is to my left, if i multiply that by 10 or multiply mathf.sign(dot) (which will equal 1)
        //by 10, then that mean i will rotate my z positively by 10, which in turn will roll the camera to the left, making me turn into the wall
        // so instead we take the opposite sign, letting us tilt in the direction the wall is facing
        Quaternion roll = Quaternion.Euler(playerController.pitch, transform.localRotation.y, 10 * -Mathf.Sign(dot));
        
        transform.localRotation = Quaternion.Lerp(transform.localRotation, roll, rollLerp);
    }
}
