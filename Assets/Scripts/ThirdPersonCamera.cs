using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Le cube player à suivre
    
    [Header("Camera Settings")]
    public float distance = 5f; // Distance entre la caméra et le target
    public float height = 2f; // Hauteur de la caméra par rapport au target
    public float rotationSpeed = 2f; // Vitesse de rotation de la caméra
    public float followSpeed = 5f; // Vitesse de suivi de la caméra
    
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
        // Verrouiller le curseur au centre de l'écran
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Calculer l'offset initial
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Désactiver le contrôle de la caméra par la souris
        // Les angles restent fixes
        // currentX et currentY ne sont plus modifiés par la souris
        
        // Calculer la rotation de la caméra (angles fixes)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calculer la position de la caméra
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition - rotation * Vector3.forward * distance;
        
        // Appliquer la position avec un smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Faire regarder la caméra vers le target
        Vector3 lookDirection = targetPosition - transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Méthode pour changer le target en cours de jeu
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    
    // Méthode pour ajuster la distance dynamiquement
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Max(1f, newDistance);
    }
    
    // Méthode pour ajuster la hauteur dynamiquement
    public void SetHeight(float newHeight)
    {
        height = newHeight;
    }
}
