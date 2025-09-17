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
    
    private Rigidbody rb;
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    
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
        
        // Initialiser la rotation actuelle et cible
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;
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
        
        // Appliquer la rotation
        transform.rotation = Quaternion.Euler(currentRotation);
    }
    
    void FixedUpdate()
    {
        // Plus de mouvement - le joueur reste statique
        // Seule la rotation est gérée dans Update()
    }
}
