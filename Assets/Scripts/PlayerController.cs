using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;
    
    [Header("Arrow Key Rotation")]
    public float upDownAngle = 45f; // Angle pour haut/bas
    public float leftRightAngle = 23f; // Angle pour gauche/droite
    
    [Header("Smooth Transition")]
    public float smoothSpeed = 5f; // Vitesse de transition fluide
    
    [Header("Player Rotation")]
    public float playerRotationSpeed = 30f; // Vitesse de rotation du joueur sur lui-même
    
    [Header("Gliding Physics")]
    public float glidingFallSpeed = 0.5f; // Vitesse de chute très lente (planage)
    public float gravity = 9.81f; // Gravité normale
    
    private Rigidbody rb;
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private float playerYRotation = 0f; // Rotation Y du joueur sur lui-même
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Permettre la rotation contrôlée du rigidbody
        rb.freezeRotation = false;
        rb.useGravity = false; // Désactiver la gravité Unity pour contrôler manuellement
        
        // S'assurer que le joueur a le tag "Player"
        if (gameObject.tag != "Player")
        {
            gameObject.tag = "Player";
        }
        
        // Initialiser la rotation actuelle et cible
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;
        playerYRotation = transform.eulerAngles.y;
    }
    
    void Update()
    {
        // Désactiver le mouvement WASD - plus de mouvement horizontal/vertical
        
        // Calculer les angles cibles basés sur les touches actuellement pressées
        float targetX = 0f;
        float targetZ = 0f;
        
        // Gestion des flèches directionnelles pour la rotation
        // Chaque touche ajoute son angle fixe quand pressée
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Rotation vers le haut (45°)
            targetX -= upDownAngle;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            // Rotation vers le bas (45°)
            targetX += upDownAngle;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // Rotation inclinée droite (23°)
            targetZ -= leftRightAngle;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // Rotation inclinée gauche (23°)
            targetZ += leftRightAngle;
        }
        
        // Mettre à jour la rotation cible
        targetRotation.x = targetX;
        targetRotation.z = targetZ;
        
        // Transition fluide vers la rotation cible
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, smoothSpeed * Time.deltaTime);
        
        // Rotation du joueur sur lui-même (Y axis)
        if (Input.GetKey(KeyCode.RightArrow))
        {
            playerYRotation += playerRotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            playerYRotation -= playerRotationSpeed * Time.deltaTime;
        }
        
        // Appliquer la rotation combinée
        Vector3 finalRotation = new Vector3(currentRotation.x, playerYRotation, currentRotation.z);
        transform.rotation = Quaternion.Euler(finalRotation);
    }
    
    void FixedUpdate()
    {
        // Physique de planage - chute très lente
        Vector3 gravityForce = Vector3.down * glidingFallSpeed;
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }
}
