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
    
    [Header("Fixed Camera Settings")]
    public Vector3 fixedAngles = new Vector3(35f, -90f, 0f); // Fixed camera angles (X, Y, Z)
    public Vector3 fixedOffset = new Vector3(-3f, 36f, -20f); // Fixed position offset from player
    
    private Vector3 offset;
    
    void Start()
    {
        // Calculate initial offset
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Apply fixed camera angles
        transform.rotation = Quaternion.Euler(fixedAngles);
        
        // Calculate camera position using fixed offset
        Vector3 desiredPosition = target.position + fixedOffset;
        
        // Apply position with smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
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
