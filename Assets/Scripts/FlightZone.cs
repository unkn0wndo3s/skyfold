using UnityEngine;
using UnityEngine.UI;

public class FlightZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public float radius = 5f; // Action zone radius
    public float height = 10f; // Action zone height
    public float strength = 120f; // Base force for flight calculation
    
    [Header("Visual Settings")]
    public GameObject overlayPrefab; // Prefab for overlay (optional)
    public Material zoneMaterial; // Material to visualize the zone
    public Color zoneColor = Color.cyan;
    public float overlayHeight = 2f; // Overlay height above the cube
    
    [Header("Force Calculation")]
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Force curve based on distance
    public float forceMultiplier = 1f; // Global force multiplier
    public float minDistance = 0.5f; // Minimum distance for maximum force
    public bool useHeightFalloff = true; // If force decreases with height
    public float heightFalloffStrength = 1f; // Intensity of decrease with height
    
    [Header("Stabilization")]
    public float stabilizationForce = 5f; // Stabilization force to avoid oscillations
    public float targetHeight = 3f; // Target height where player should float
    public float floatZone = 0.3f; // Floating zone around target height
    public float dampingFactor = 0.95f; // Damping factor for vertical velocity (closer to 1 = more bounces)
    public float initialBounceRetention = 0.78f; // Percentage of height retained on first bounce
    public float retentionDecrease = 0.02f; // Decrease in percentage at each bounce
    public float minBounceRetention = 0.1f; // Minimum retention percentage
    public float minBounceVelocity = 2f; // Minimum velocity to have bounces
    
    private GameObject overlay;
    private Canvas overlayCanvas;
    private Image overlayImage;
    private Transform player;
    private bool playerInZone = false;
    private Rigidbody playerRb;
    private float lastBounceHeight = 0f; // Height of last bounce
    private bool wasFalling = false; // If player was falling
    private int bounceCount = 0; // Number of bounces performed
    
    void Start()
    {
        // Create visual overlay
        CreateOverlay();
        
        // Create zone visualization
        CreateZoneVisualization();
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody>();
            Debug.Log("FlightZone: Player found - " + playerObj.name);
        }
        else
        {
            Debug.LogWarning("FlightZone: No object with 'Player' tag found!");
            
            // Try to find by name
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerRb = playerObj.GetComponent<Rigidbody>();
                Debug.Log("FlightZone: Player found by name - " + playerObj.name);
            }
            else
            {
                Debug.LogError("FlightZone: Unable to find player! Check that your cube has the 'Player' tag or is named 'Player'");
            }
        }
    }
    
    void CreateOverlay()
    {
        // Create Canvas for overlay
        GameObject canvasObj = new GameObject("FlightZoneOverlay");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * overlayHeight;
        
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.WorldSpace;
        overlayCanvas.worldCamera = Camera.main;
        
        // Add CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        
        // Create overlay image (arrow pointing upward)
        GameObject imageObj = new GameObject("OverlayImage");
        imageObj.transform.SetParent(canvasObj.transform);
        imageObj.transform.localPosition = Vector3.zero;
        imageObj.transform.localScale = Vector3.one * 0.5f;
        
        overlayImage = imageObj.AddComponent<Image>();
        overlayImage.color = zoneColor;
        
        // Create simple texture for arrow (square for now)
        Texture2D arrowTexture = CreateArrowTexture();
        Sprite arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        overlayImage.sprite = arrowSprite;
        
        overlay = canvasObj;
    }
    
    Texture2D CreateArrowTexture()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        // Create simple arrow shape
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int index = y * 64 + x;
                
                // Draw arrow pointing upward
                bool isArrow = false;
                
                // Arrow shaft (vertical)
                if (x >= 28 && x <= 35 && y >= 10 && y <= 50)
                    isArrow = true;
                
                // Arrow head (triangle)
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
        // Create cylinder to visualize zone
        GameObject zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zoneObj.name = "FlightZoneVisualization";
        zoneObj.transform.SetParent(transform);
        zoneObj.transform.localPosition = Vector3.zero;
        zoneObj.transform.localScale = new Vector3(radius * 2, height, radius * 2);
        
        // Remove collider from visualization cylinder
        Destroy(zoneObj.GetComponent<Collider>());
        
        // Apply material
        Renderer renderer = zoneObj.GetComponent<Renderer>();
        if (zoneMaterial != null)
        {
            renderer.material = zoneMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
            mat.SetFloat("_Mode", 3); // Transparent Mode
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
        
        // Check if player is in zone
        CheckPlayerInZone();
        
        // Apply flight force if player is in zone
        if (playerInZone && playerRb != null)
        {
            ApplyFlightForce();
        }
        
        // Rotate overlay so it always looks at camera
        if (overlay != null && Camera.main != null)
        {
            overlay.transform.LookAt(Camera.main.transform);
            overlay.transform.Rotate(0, 180, 0); // Reverse so it looks at camera
        }
    }
    
    void CheckPlayerInZone()
    {
        Vector3 playerPos = player.position;
        Vector3 zonePos = transform.position;
        
        // Calculate horizontal distance
        float horizontalDistance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.z),
            new Vector2(zonePos.x, zonePos.z)
        );
        
        // Calculate relative height (can be negative if below)
        float heightAboveZone = playerPos.y - zonePos.y;
        
        // Check if player is in zone HORIZONTALLY
        bool inHorizontalZone = horizontalDistance <= radius;
        
        // Check if player is in zone VERTICALLY (can be above or below)
        bool inVerticalZone = heightAboveZone >= -height && heightAboveZone <= height;
        
        // Player is in zone if in both dimensions
        bool wasInZone = playerInZone;
        playerInZone = inHorizontalZone && inVerticalZone;
        
        // Debug to see if player enters/exits zone
        if (playerInZone && !wasInZone)
        {
            Debug.Log("FlightZone: Player enters zone! Distance: " + horizontalDistance + " / " + radius + ", Height: " + heightAboveZone);
        }
        else if (!playerInZone && wasInZone)
        {
            Debug.Log("FlightZone: Player exits zone! Distance: " + horizontalDistance + " / " + radius + ", Height: " + heightAboveZone);
        }
    }
    
    void ApplyFlightForce()
    {
        Vector3 playerPos = player.position;
        Vector3 zonePos = transform.position;
        
        // Calculate horizontal distance
        float horizontalDistance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.z),
            new Vector2(zonePos.x, zonePos.z)
        );
        
        // Calculate relative height (0 = at cube level, + = above)
        float heightAboveZone = playerPos.y - zonePos.y;
        
        // Calculate force based on distance AND height
        float baseForce = CalculateForceByDistanceAndHeight(horizontalDistance, heightAboveZone);
        
        // Apply vertical velocity damping
        ApplyDamping();
        
        // Apply upward force
        Vector3 forceDirection = Vector3.up;
        playerRb.AddForce(forceDirection * baseForce, ForceMode.Force);
        
        // Add stabilization force ONLY if player is close to target height
        float stabilizationForce = CalculateStabilizationForce(heightAboveZone);
        if (stabilizationForce != 0)
        {
            playerRb.AddForce(forceDirection * stabilizationForce, ForceMode.Force);
        }
        
        // Debug to see applied force
        Debug.DrawRay(playerPos, Vector3.up * baseForce * 0.1f, Color.yellow);
        if (stabilizationForce != 0)
        {
            Debug.DrawRay(playerPos, Vector3.up * stabilizationForce * 0.05f, Color.green);
        }
        
        // Debug in console
        Debug.Log("FlightZone: Total force = " + (baseForce + stabilizationForce) + " (Base: " + baseForce + ", Stabilization: " + stabilizationForce + ")");
    }
    
    float CalculateStabilizationForce(float heightAboveZone)
    {
        // Calculate difference with target height
        float heightDifference = targetHeight - heightAboveZone;
        
        // Don't apply stabilization if player is falling fast
        if (playerRb.linearVelocity.y < -5f)
        {
            return 0f; // No stabilization during fast fall
        }
        
        // Don't apply stabilization if very far from target height
        if (Mathf.Abs(heightDifference) > 8f)
        {
            return 0f; // No stabilization when very far
        }
        
        // If player is in floating zone AND bounces are very small, no stabilization
        if (Mathf.Abs(heightDifference) <= floatZone && bounceCount > 10)
        {
            return 0f; // No stabilization in floating zone if already bounced a lot
        }
        
        // Stabilization force proportional to distance from floating zone
        float distanceFromFloatZone = Mathf.Abs(heightDifference) - floatZone;
        float stabilization = Mathf.Sign(heightDifference) * distanceFromFloatZone * stabilizationForce;
        
        // Limit stabilization force to avoid excessive oscillations
        return Mathf.Clamp(stabilization, -stabilizationForce, stabilizationForce);
    }
    
    void ApplyDamping()
    {
        // Apply progressive damping based on velocity
        Vector3 velocity = playerRb.linearVelocity;
        float currentHeight = player.position.y;
        
        // Detect if player was falling and just bounced
        bool isFalling = velocity.y < 0;
        bool justBounced = wasFalling && !isFalling && velocity.y > 0;
        
        if (justBounced)
        {
            // Increment bounce counter
            bounceCount++;
            
            // Calculate retention percentage for this bounce
            float currentRetention = initialBounceRetention - (bounceCount - 1) * retentionDecrease;
            currentRetention = Mathf.Max(currentRetention, minBounceRetention);
            
            // Calculate bounce height based on previous height
            float targetBounceHeight = lastBounceHeight * currentRetention;
            
            // If bounce height is very small (close to floating zone), reduce even more
            if (targetBounceHeight < targetHeight + floatZone + 1f)
            {
                targetBounceHeight = Mathf.Min(targetBounceHeight, 0.3f); // Limit to 0.3 units
            }
            
            // Adjust velocity to reach this height
            float requiredVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * targetBounceHeight);
            velocity.y = requiredVelocity;
            
            // Update reference height for next bounce
            lastBounceHeight = targetBounceHeight;
            
            Debug.Log("Bounce " + bounceCount + " detected! Retention: " + (currentRetention * 100) + "%, Target height: " + targetBounceHeight + ", Velocity: " + velocity.y);
        }
        else if (isFalling && !wasFalling)
        {
            // Player starts falling, record current height
            lastBounceHeight = currentHeight;
        }
        
        // If vertical velocity is significant (falling), apply very little damping
        if (Mathf.Abs(velocity.y) > minBounceVelocity)
        {
            // Very light damping to allow natural bounces
            velocity.y *= 0.99f; // Very little damping
        }
        else
        {
            // Normal damping when velocity is low (stabilization)
            velocity.y *= dampingFactor;
        }
        
        // Update falling state
        wasFalling = isFalling;
        
        playerRb.linearVelocity = velocity;
    }
    
    float CalculateForceByDistanceAndHeight(float horizontalDistance, float heightAboveZone)
    {
        // Calculate horizontal distance factor (0 = center, 1 = edge)
        float horizontalFactor = Mathf.Clamp01(horizontalDistance / radius);
        
        // Calculate height factor (0 = at cube level, 1 = at zone top)
        // Use absolute value to handle negative heights
        float heightFactor = Mathf.Clamp01(Mathf.Abs(heightAboveZone) / height);
        
        // Calculate total 3D distance from cube center
        float totalDistance3D = Mathf.Sqrt(horizontalDistance * horizontalDistance + heightAboveZone * heightAboveZone);
        float maxDistance3D = Mathf.Sqrt(radius * radius + height * height);
        float distance3DFactor = Mathf.Clamp01(totalDistance3D / maxDistance3D);
        
        // Use animation curve to calculate force factor
        float curveValue = forceCurve.Evaluate(distance3DFactor);
        
        // Apply height factor if enabled
        float heightFalloff = 1f;
        if (useHeightFalloff)
        {
            if (heightAboveZone > 0)
            {
                // Above: the higher you go, the more force decreases
                heightFalloff = Mathf.Pow(1f - heightFactor, heightFalloffStrength);
            }
            else if (heightAboveZone < 0)
            {
                // Below: maximum force (as if at cube level)
                heightFalloff = 1f;
            }
            else
            {
                // Exactly at level: maximum force
                heightFalloff = 1f;
            }
        }
        
        // Calculate final force: the further away (horizontally AND vertically), the weaker
        float finalForce = strength * curveValue * heightFalloff * forceMultiplier;
        
        // Ensure force is never negative
        return Mathf.Max(0f, finalForce);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw zone in editor
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        
        // Draw cylinder with circles
        Vector3 center = transform.position;
        Vector3 top = center + Vector3.up * height;
        
        // Bottom circle
        DrawWireCircle(center, radius, Vector3.up);
        // Top circle
        DrawWireCircle(top, radius, Vector3.up);
        
        // Vertical lines to connect circles
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(center + offset, top + offset);
        }
        
        // Draw maximum force zone
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
    
    // Public methods to adjust parameters during gameplay
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
