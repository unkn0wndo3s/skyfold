using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    [Header("Input Settings")]
    public string horizontalInput = "Horizontal";
    public string verticalInput = "Vertical";
    
    private Rigidbody rb;
    private Vector3 movement;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Empêcher la rotation automatique du rigidbody
        rb.freezeRotation = true;
        
        // S'assurer que le joueur a le tag "Player"
        if (gameObject.tag != "Player")
        {
            gameObject.tag = "Player";
        }
    }
    
    void Update()
    {
        // Récupérer les inputs
        float horizontal = Input.GetAxis(horizontalInput);
        float vertical = Input.GetAxis(verticalInput);
        
        // Calculer le mouvement
        movement = new Vector3(horizontal, 0f, vertical).normalized;
    }
    
    void FixedUpdate()
    {
        // Appliquer le mouvement
        if (movement.magnitude > 0.1f)
        {
            // Déplacer le joueur
            Vector3 moveDirection = transform.TransformDirection(movement);
            rb.MovePosition(transform.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
            
            // Faire tourner le joueur dans la direction du mouvement
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
