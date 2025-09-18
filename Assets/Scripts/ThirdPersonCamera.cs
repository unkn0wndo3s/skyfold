using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Player cube to follow
    
    [Header("Camera Settings")]
    public float distance = 5f; // Distance between camera and target
    public float height = 2f; // Camera height relative to target
    public float rotationSpeed = 2f; // Camera rotation speed
    public float followSpeed = 5f; // Camera follow speed
    
    [Header("Input Settings")]
    public string mouseXInput = "Mouse X";
    public string mouseYInput = "Mouse Y";
    public float mouseSensitivity = 2f;
    
    [Header("Camera Limits")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 offset;
    
    void Start()
    {
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Calculate initial offset
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Disable camera control by mouse
        // Angles remain fixed
        // currentX and currentY are no longer modified by mouse
        
        // Follow player rotation for camera Y rotation
        currentX = target.eulerAngles.y;
        
        // Calculate camera rotation (fixed angles)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calculate camera position
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition - rotation * Vector3.forward * distance;
        
        // Apply position with smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Make camera look at target
        Vector3 lookDirection = targetPosition - transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Method to change target during gameplay
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    
    // Method to adjust distance dynamically
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Max(1f, newDistance);
    }
    
    // Method to adjust height dynamically
    public void SetHeight(float newHeight)
    {
        height = newHeight;
    }
}
