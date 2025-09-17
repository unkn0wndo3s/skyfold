using UnityEngine;
using UnityEngine.UI;

public class FlightZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public float radius = 5f; // Rayon de la zone d'action
    public float height = 10f; // Hauteur de la zone d'action
    public float strength = 120f; // Force de base pour le calcul d'envol
    
    [Header("Visual Settings")]
    public GameObject overlayPrefab; // Prefab pour l'overlay (optionnel)
    public Material zoneMaterial; // Matériau pour visualiser la zone
    public Color zoneColor = Color.cyan;
    public float overlayHeight = 2f; // Hauteur de l'overlay au-dessus du cube
    
    [Header("Force Calculation")]
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Courbe de force basée sur la distance
    public float forceMultiplier = 1f; // Multiplicateur global de force
    public float minDistance = 0.5f; // Distance minimale pour la force maximale
    public bool useHeightFalloff = true; // Si la force diminue avec la hauteur
    public float heightFalloffStrength = 1f; // Intensité de la diminution avec la hauteur
    
    [Header("Stabilization")]
    public float stabilizationForce = 5f; // Force de stabilisation pour éviter les oscillations
    public float targetHeight = 3f; // Hauteur cible où le joueur doit flotter
    public float floatZone = 0.3f; // Zone de flottement autour de la hauteur cible
    public float dampingFactor = 0.95f; // Facteur d'amortissement pour la vitesse verticale (plus proche de 1 = plus de rebonds)
    public float initialBounceRetention = 0.78f; // Pourcentage de hauteur conservé au premier rebond
    public float retentionDecrease = 0.02f; // Diminution du pourcentage à chaque rebond
    public float minBounceRetention = 0.1f; // Pourcentage minimum de conservation
    public float minBounceVelocity = 2f; // Vitesse minimale pour avoir des rebonds
    
    private GameObject overlay;
    private Canvas overlayCanvas;
    private Image overlayImage;
    private Transform player;
    private bool playerInZone = false;
    private Rigidbody playerRb;
    private float lastBounceHeight = 0f; // Hauteur du dernier rebond
    private bool wasFalling = false; // Si le joueur était en train de tomber
    private int bounceCount = 0; // Nombre de rebonds effectués
    
    void Start()
    {
        // Créer l'overlay visuel
        CreateOverlay();
        
        // Créer la visualisation de la zone
        CreateZoneVisualization();
        
        // Trouver le joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody>();
            Debug.Log("FlightZone: Joueur trouvé - " + playerObj.name);
        }
        else
        {
            Debug.LogWarning("FlightZone: Aucun objet avec le tag 'Player' trouvé !");
            
            // Essayer de trouver par nom
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerRb = playerObj.GetComponent<Rigidbody>();
                Debug.Log("FlightZone: Joueur trouvé par nom - " + playerObj.name);
            }
            else
            {
                Debug.LogError("FlightZone: Impossible de trouver le joueur ! Vérifiez que votre cube a le tag 'Player' ou s'appelle 'Player'");
            }
        }
    }
    
    void CreateOverlay()
    {
        // Créer un Canvas pour l'overlay
        GameObject canvasObj = new GameObject("FlightZoneOverlay");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * overlayHeight;
        
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.WorldSpace;
        overlayCanvas.worldCamera = Camera.main;
        
        // Ajouter un CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        
        // Créer l'image de l'overlay (flèche pointant vers le haut)
        GameObject imageObj = new GameObject("OverlayImage");
        imageObj.transform.SetParent(canvasObj.transform);
        imageObj.transform.localPosition = Vector3.zero;
        imageObj.transform.localScale = Vector3.one * 0.5f;
        
        overlayImage = imageObj.AddComponent<Image>();
        overlayImage.color = zoneColor;
        
        // Créer une texture simple pour la flèche (carré pour l'instant)
        Texture2D arrowTexture = CreateArrowTexture();
        Sprite arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        overlayImage.sprite = arrowSprite;
        
        overlay = canvasObj;
    }
    
    Texture2D CreateArrowTexture()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        // Créer une forme de flèche simple
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int index = y * 64 + x;
                
                // Dessiner une flèche pointant vers le haut
                bool isArrow = false;
                
                // Tige de la flèche (verticale)
                if (x >= 28 && x <= 35 && y >= 10 && y <= 50)
                    isArrow = true;
                
                // Pointe de la flèche (triangle)
                if (y >= 50)
                {
                    int distanceFromCenter = Mathf.Abs(x - 32);
                    int maxWidth = 32 - (y - 50) * 2;
                    if (distanceFromCenter <= maxWidth)
                        isArrow = true;
                }
                
                pixels[index] = isArrow ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    void CreateZoneVisualization()
    {
        // Créer un cylindre pour visualiser la zone
        GameObject zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zoneObj.name = "FlightZoneVisualization";
        zoneObj.transform.SetParent(transform);
        zoneObj.transform.localPosition = Vector3.zero;
        zoneObj.transform.localScale = new Vector3(radius * 2, height, radius * 2);
        
        // Supprimer le collider du cylindre de visualisation
        Destroy(zoneObj.GetComponent<Collider>());
        
        // Appliquer le matériau
        Renderer renderer = zoneObj.GetComponent<Renderer>();
        if (zoneMaterial != null)
        {
            renderer.material = zoneMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
            mat.SetFloat("_Mode", 3); // Mode Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Vérifier si le joueur est dans la zone
        CheckPlayerInZone();
        
        // Appliquer la force d'envol si le joueur est dans la zone
        if (playerInZone && playerRb != null)
        {
            ApplyFlightForce();
        }
        
        // Faire tourner l'overlay pour qu'il regarde toujours la caméra
        if (overlay != null && Camera.main != null)
        {
            overlay.transform.LookAt(Camera.main.transform);
            overlay.transform.Rotate(0, 180, 0); // Inverser pour qu'il regarde la caméra
        }
    }
    
    void CheckPlayerInZone()
    {
        Vector3 playerPos = player.position;
        Vector3 zonePos = transform.position;
        
        // Calculer la distance horizontale
        float horizontalDistance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.z),
            new Vector2(zonePos.x, zonePos.z)
        );
        
        // Calculer la hauteur relative (peut être négative si en dessous)
        float heightAboveZone = playerPos.y - zonePos.y;
        
        // Vérifier si le joueur est dans la zone HORIZONTALEMENT
        bool inHorizontalZone = horizontalDistance <= radius;
        
        // Vérifier si le joueur est dans la zone VERTICALEMENT (peut être au-dessus ou en dessous)
        bool inVerticalZone = heightAboveZone >= -height && heightAboveZone <= height;
        
        // Le joueur est dans la zone s'il est dans les deux dimensions
        bool wasInZone = playerInZone;
        playerInZone = inHorizontalZone && inVerticalZone;
        
        // Debug pour voir si le joueur entre/sort de la zone
        if (playerInZone && !wasInZone)
        {
            Debug.Log("FlightZone: Joueur entre dans la zone ! Distance: " + horizontalDistance + " / " + radius + ", Hauteur: " + heightAboveZone);
        }
        else if (!playerInZone && wasInZone)
        {
            Debug.Log("FlightZone: Joueur sort de la zone ! Distance: " + horizontalDistance + " / " + radius + ", Hauteur: " + heightAboveZone);
        }
    }
    
    void ApplyFlightForce()
    {
        Vector3 playerPos = player.position;
        Vector3 zonePos = transform.position;
        
        // Calculer la distance horizontale
        float horizontalDistance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.z),
            new Vector2(zonePos.x, zonePos.z)
        );
        
        // Calculer la hauteur relative (0 = au niveau du cube, + = au-dessus)
        float heightAboveZone = playerPos.y - zonePos.y;
        
        // Calculer la force basée sur la distance ET la hauteur
        float baseForce = CalculateForceByDistanceAndHeight(horizontalDistance, heightAboveZone);
        
        // Appliquer l'amortissement de la vitesse verticale
        ApplyDamping();
        
        // Appliquer la force vers le haut
        Vector3 forceDirection = Vector3.up;
        playerRb.AddForce(forceDirection * baseForce, ForceMode.Force);
        
        // Ajouter la force de stabilisation SEULEMENT si le joueur est proche de la hauteur cible
        float stabilizationForce = CalculateStabilizationForce(heightAboveZone);
        if (stabilizationForce != 0)
        {
            playerRb.AddForce(forceDirection * stabilizationForce, ForceMode.Force);
        }
        
        // Debug pour voir la force appliquée
        Debug.DrawRay(playerPos, Vector3.up * baseForce * 0.1f, Color.yellow);
        if (stabilizationForce != 0)
        {
            Debug.DrawRay(playerPos, Vector3.up * stabilizationForce * 0.05f, Color.green);
        }
        
        // Debug dans la console
        Debug.Log("FlightZone: Force totale = " + (baseForce + stabilizationForce) + " (Base: " + baseForce + ", Stabilisation: " + stabilizationForce + ")");
    }
    
    float CalculateStabilizationForce(float heightAboveZone)
    {
        // Calculer la différence avec la hauteur cible
        float heightDifference = targetHeight - heightAboveZone;
        
        // Ne pas appliquer de stabilisation si le joueur est en chute rapide
        if (playerRb.linearVelocity.y < -5f)
        {
            return 0f; // Pas de stabilisation pendant la chute rapide
        }
        
        // Ne pas appliquer de stabilisation si on est très loin de la hauteur cible
        if (Mathf.Abs(heightDifference) > 8f)
        {
            return 0f; // Pas de stabilisation quand on est très loin
        }
        
        // Si le joueur est dans la zone de flottement ET que les rebonds sont très petits, pas de stabilisation
        if (Mathf.Abs(heightDifference) <= floatZone && bounceCount > 10)
        {
            return 0f; // Pas de stabilisation dans la zone de flottement si on a déjà beaucoup rebondi
        }
        
        // Force de stabilisation proportionnelle à la distance depuis la zone de flottement
        float distanceFromFloatZone = Mathf.Abs(heightDifference) - floatZone;
        float stabilization = Mathf.Sign(heightDifference) * distanceFromFloatZone * stabilizationForce;
        
        // Limiter la force de stabilisation pour éviter les oscillations excessives
        return Mathf.Clamp(stabilization, -stabilizationForce, stabilizationForce);
    }
    
    void ApplyDamping()
    {
        // Appliquer un amortissement progressif basé sur la vitesse
        Vector3 velocity = playerRb.linearVelocity;
        float currentHeight = player.position.y;
        
        // Détecter si le joueur était en train de tomber et vient de rebondir
        bool isFalling = velocity.y < 0;
        bool justBounced = wasFalling && !isFalling && velocity.y > 0;
        
        if (justBounced)
        {
            // Incrémenter le compteur de rebonds
            bounceCount++;
            
            // Calculer le pourcentage de conservation pour ce rebond
            float currentRetention = initialBounceRetention - (bounceCount - 1) * retentionDecrease;
            currentRetention = Mathf.Max(currentRetention, minBounceRetention);
            
            // Calculer la hauteur du rebond basée sur la hauteur précédente
            float targetBounceHeight = lastBounceHeight * currentRetention;
            
            // Si la hauteur de rebond est très petite (proche de la zone de flottement), réduire encore plus
            if (targetBounceHeight < targetHeight + floatZone + 1f)
            {
                targetBounceHeight = Mathf.Min(targetBounceHeight, 0.3f); // Limiter à 0.3 unités
            }
            
            // Ajuster la vitesse pour atteindre cette hauteur
            float requiredVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * targetBounceHeight);
            velocity.y = requiredVelocity;
            
            // Mettre à jour la hauteur de référence pour le prochain rebond
            lastBounceHeight = targetBounceHeight;
            
            Debug.Log("Rebond " + bounceCount + " détecté ! Conservation: " + (currentRetention * 100) + "%, Hauteur cible: " + targetBounceHeight + ", Vitesse: " + velocity.y);
        }
        else if (isFalling && !wasFalling)
        {
            // Le joueur commence à tomber, enregistrer la hauteur actuelle
            lastBounceHeight = currentHeight;
        }
        
        // Si la vitesse verticale est importante (chute), appliquer très peu d'amortissement
        if (Mathf.Abs(velocity.y) > minBounceVelocity)
        {
            // Amortissement très léger pour permettre les rebonds naturels
            velocity.y *= 0.99f; // Très peu d'amortissement
        }
        else
        {
            // Amortissement normal quand la vitesse est faible (stabilisation)
            velocity.y *= dampingFactor;
        }
        
        // Mettre à jour l'état de chute
        wasFalling = isFalling;
        
        playerRb.linearVelocity = velocity;
    }
    
    float CalculateForceByDistanceAndHeight(float horizontalDistance, float heightAboveZone)
    {
        // Calculer le facteur de distance horizontale (0 = centre, 1 = bord)
        float horizontalFactor = Mathf.Clamp01(horizontalDistance / radius);
        
        // Calculer le facteur de hauteur (0 = au niveau du cube, 1 = au sommet de la zone)
        // Utiliser la valeur absolue pour gérer les hauteurs négatives
        float heightFactor = Mathf.Clamp01(Mathf.Abs(heightAboveZone) / height);
        
        // Calculer la distance 3D totale depuis le centre du cube
        float totalDistance3D = Mathf.Sqrt(horizontalDistance * horizontalDistance + heightAboveZone * heightAboveZone);
        float maxDistance3D = Mathf.Sqrt(radius * radius + height * height);
        float distance3DFactor = Mathf.Clamp01(totalDistance3D / maxDistance3D);
        
        // Utiliser la courbe d'animation pour calculer le facteur de force
        float curveValue = forceCurve.Evaluate(distance3DFactor);
        
        // Appliquer un facteur de hauteur si activé
        float heightFalloff = 1f;
        if (useHeightFalloff)
        {
            if (heightAboveZone > 0)
            {
                // Au-dessus : plus on monte, plus la force diminue
                heightFalloff = Mathf.Pow(1f - heightFactor, heightFalloffStrength);
            }
            else if (heightAboveZone < 0)
            {
                // En dessous : force maximale (comme si on était au niveau du cube)
                heightFalloff = 1f;
            }
            else
            {
                // Exactement au niveau : force maximale
                heightFalloff = 1f;
            }
        }
        
        // Calculer la force finale : plus on s'éloigne (horizontalement ET verticalement), moins c'est fort
        float finalForce = strength * curveValue * heightFalloff * forceMultiplier;
        
        // S'assurer que la force ne soit jamais négative
        return Mathf.Max(0f, finalForce);
    }
    
    void OnDrawGizmosSelected()
    {
        // Dessiner la zone dans l'éditeur
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        
        // Dessiner le cylindre avec des cercles
        Vector3 center = transform.position;
        Vector3 top = center + Vector3.up * height;
        
        // Cercle du bas
        DrawWireCircle(center, radius, Vector3.up);
        // Cercle du haut
        DrawWireCircle(top, radius, Vector3.up);
        
        // Lignes verticales pour connecter les cercles
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(center + offset, top + offset);
        }
        
        // Dessiner la zone de force maximale
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius * 0.2f);
    }
    
    void DrawWireCircle(Vector3 center, float radius, Vector3 normal)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        Vector3 right = Vector3.Cross(normal, Vector3.forward).normalized;
        if (right == Vector3.zero)
            right = Vector3.Cross(normal, Vector3.right).normalized;
        
        Vector3 forward = Vector3.Cross(right, normal).normalized;
        
        Vector3 previousPoint = center + right * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
    
    // Méthodes publiques pour ajuster les paramètres en cours de jeu
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        if (overlay != null)
        {
            overlay.transform.localScale = Vector3.one * (newRadius / 5f);
        }
    }
    
    public void SetStrength(float newStrength)
    {
        strength = newStrength;
    }
    
    public void SetForceMultiplier(float newMultiplier)
    {
        forceMultiplier = newMultiplier;
    }
}
