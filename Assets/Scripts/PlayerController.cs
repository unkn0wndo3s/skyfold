using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Paper Plane Physics")]
    public float mass = 1f; // Aircraft mass
    public float gravity = 9.81f; // Gravity
    public float airDensity = 1.225f; // Air density
    
    [Header("Aerodynamic Properties")]
    public float liftCoefficient = 1.2f; // Lift coefficient (increased)
    public float dragCoefficient = 0.8f; // Drag coefficient (increased to slow down)
    public float wingArea = 0.8f; // Wing area (increased)
    public float stabilityFactor = 2f; // Stability factor
    public float maxFallSpeed = 1.2f; // Maximum fall speed while gliding
    public float maxClimbSpeed = 8f; // Maximum climb speed
    
    [Header("Control Surfaces")]
    public float elevatorSensitivity = 15f; // Elevator sensitivity (UP/DOWN)
    public float aileronSensitivity = 12f; // Aileron sensitivity (LEFT/RIGHT)
    public float rudderSensitivity = 8f; // Rudder sensitivity
    public float targetPitchAngle = 45f; // Target pitch angle (degrees)
    public float maxRollAngle = 60f; // Maximum roll angle (degrees)
    public float turnRate = 30f; // Turn rate (degrees/second)
    
    [Header("Barrel Roll")]
    public float barrelRollSpeed = 2f; // Barrel roll speed (rotations/second)
    public float barrelRollTurnBoost = 2f; // Turn multiplier during barrel roll
    
    [Header("Flight Dynamics")]
    public float maxSpeed = 25f; // Maximum speed
    public float minSpeed = 1f; // Minimum speed for control
    public float stallSpeed = 2f; // Stall speed
    public float glideRatio = 8f; // Glide ratio (horizontal distance / altitude loss)
    public float initialVelocity = 8f; // Initial velocity to enable control
    public float momentumDecay = 0.98f; // Momentum decay (0.98 = 2% lost per second)
    public float momentumThreshold = 3f; // Momentum threshold to climb (reduced)
    
    [Header("Input Response")]
    public float controlSmoothness = 12f; // Control smoothing
    public float maxControlDeflection = 45f; // Maximum control deflection (degrees)
    public float controlMultiplier = 3f; // Global control multiplier
    public float angleResponseSpeed = 8f; // Angle response speed
    
    private Rigidbody rb;
    
    // Flight variables
    private Vector3 velocity; // Aircraft velocity
    private Vector3 angularVelocity; // Angular velocity
    private float airspeed; // Airspeed
    private Vector3 windDirection; // Wind direction
    
    // Flight controls
    private float elevatorInput = 0f; // Elevator input (UP/DOWN)
    private float aileronInput = 0f; // Aileron input (LEFT/RIGHT)
    private float rudderInput = 0f; // Rudder input
    
    // Target angles
    private float targetPitch = 0f; // Target pitch angle
    private float currentPitch = 0f; // Current pitch angle
    private float targetRoll = 0f; // Target roll angle
    private float currentRoll = 0f; // Current roll angle
    private float targetYaw = 0f; // Target yaw angle
    private float currentYaw = 0f; // Current yaw angle
    
    // Momentum system
    private float currentMomentum = 0f; // Current momentum
    private float maxMomentum = 25f; // Maximum momentum
    private bool canClimb = false; // Can climb (has enough momentum)
    
    // Aerodynamic forces
    private Vector3 liftForce; // Lift force
    private Vector3 dragForce; // Drag force
    private Vector3 sideForce; // Side force
    
    // Flight state
    private bool isStalling = false; // If aircraft is stalling
    private bool isGliding = false; // If aircraft is gliding
    private float glideStartTime = 0f; // Glide start time
    private Vector3 glideStartPosition; // Glide start position
    
    // Barrel roll system
    private bool isBarrelRolling = false; // If aircraft is barrel rolling
    private float barrelRollProgress = 0f; // Barrel roll progress (0-1)
    private float barrelRollDirection = 0f; // Barrel roll direction (-1 or 1)
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for aircraft physics
        rb.mass = mass;
        rb.linearDamping = 0.1f; // Base drag
        rb.angularDamping = 2f; // Angular drag
        rb.useGravity = true; // Use Unity gravity
        
        // Ensure player has "Player" tag
        if (gameObject.tag != "Player")
        {
            gameObject.tag = "Player";
        }
        
        // Initialize flight variables
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        airspeed = 0f;
        windDirection = Vector3.zero;
        
        // Give initial velocity to enable control
        rb.linearVelocity = transform.forward * initialVelocity;
        
        // Initial gliding position
        glideStartPosition = transform.position;
    }
    
    void Update()
    {
        // Handle flight control inputs
        HandleFlightInputs();
        
        // Update flight state
        UpdateFlightState();
    }
    
    void HandleFlightInputs()
    {
        float targetElevator = 0f;
        float targetAileron = 0f;
        
        // Elevator inputs (UP/DOWN or W/S) - forward/backward tilt
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.S)) // S to tilt backward and climb
        {
            targetElevator = 1f;
            targetPitch = -targetPitchAngle; // -45° to climb (backward tilt)
            isGliding = false;
            
            // Use momentum to climb if available
            if (canClimb)
            {
                // Consume momentum while climbing (2x faster)
                currentMomentum -= Time.deltaTime * 6.5f;
                currentMomentum = Mathf.Max(currentMomentum, 0f);
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.W)) // W to tilt forward and gain momentum
        {
            targetElevator = -1f;
            targetPitch = targetPitchAngle; // +45° to descend (forward tilt)
            isGliding = false;
            
            // Accumulate momentum while descending (1/4 of previous speed)
            currentMomentum = Mathf.Min(currentMomentum + Time.deltaTime * 3.2f, maxMomentum);
        }
        
        // Gliding mode - no input
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
        {
            targetPitch = 0f; // Return to horizontal
            isGliding = true;
        }
        
        // Aileron inputs (LEFT/RIGHT or A/D) - lateral tilt to turn
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) // A to tilt left
        {
            targetAileron = 1f;
            targetRoll = maxRollAngle; // Tilt left
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) // D to tilt right
        {
            targetAileron = -1f;
            targetRoll = -maxRollAngle; // Tilt right
        }
        else
        {
            targetRoll = 0f; // Return to horizontal
        }
        
        // Barrel roll with Shift + A/D
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                StartBarrelRoll(1f); // Barrel roll left
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                StartBarrelRoll(-1f); // Barrel roll right
            }
        }
        
        // Input smoothing
        elevatorInput = Mathf.Lerp(elevatorInput, targetElevator, controlSmoothness * Time.deltaTime);
        aileronInput = Mathf.Lerp(aileronInput, targetAileron, controlSmoothness * Time.deltaTime);
        
        // Angle smoothing (except for roll during barrel roll)
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, angleResponseSpeed * Time.deltaTime);
        
        // For roll, don't smooth during barrel roll, otherwise smooth normally
        if (!isBarrelRolling)
        {
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, angleResponseSpeed * Time.deltaTime);
        }
        
        // Calculate turn based on banking (banking turn)
        if (Mathf.Abs(currentRoll) > 5f) // Minimum banking threshold to turn
        {
            float turnDirection = -Mathf.Sign(currentRoll); // Reverse turn direction
            float turnMultiplier = isBarrelRolling ? barrelRollTurnBoost : 1f; // Boost during barrel roll
            targetYaw += turnDirection * turnRate * Time.deltaTime * (Mathf.Abs(currentRoll) / maxRollAngle) * turnMultiplier;
        }
        
        // Update barrel roll
        UpdateBarrelRoll();
        
        // Yaw angle smoothing
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, angleResponseSpeed * Time.deltaTime);
        
        // Limit inputs to maximum values
        elevatorInput = Mathf.Clamp(elevatorInput, -1f, 1f);
        aileronInput = Mathf.Clamp(aileronInput, -1f, 1f);
        
        // Update climb capability
        canClimb = currentMomentum >= momentumThreshold;
    }
    
    void StartBarrelRoll(float direction)
    {
        if (!isBarrelRolling) // Don't start new barrel roll if already doing one
        {
            isBarrelRolling = true;
            barrelRollProgress = 0f;
            barrelRollDirection = direction;
        }
    }
    
    void UpdateBarrelRoll()
    {
        if (isBarrelRolling)
        {
            // Advance barrel roll
            barrelRollProgress += barrelRollSpeed * Time.deltaTime;
            
            // Apply barrel roll rotation (360°) - only if no W/S input
            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
            {
                float rollAngle = barrelRollProgress * 360f * barrelRollDirection;
                currentRoll = rollAngle;
            }
            
            // Fast turn during barrel roll
            targetYaw += barrelRollDirection * turnRate * barrelRollTurnBoost * Time.deltaTime;
            
            // Finish barrel roll after complete turn
            if (barrelRollProgress >= 1f)
            {
                isBarrelRolling = false;
                barrelRollProgress = 0f;
                if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
                {
                    currentRoll = 0f; // Return to horizontal only if no input
                }
            }
        }
    }
    
    void UpdateFlightState()
    {
        // Calculate airspeed
        velocity = rb.linearVelocity;
        airspeed = velocity.magnitude;
        
        // Check if aircraft is stalling
        isStalling = airspeed < stallSpeed;
        
        // Update momentum (natural decay only while gliding)
        if (isGliding)
        {
            currentMomentum *= momentumDecay;
        }
        
        // Check if aircraft is gliding (no input and sufficient speed)
        if (isGliding && !isStalling)
        {
            if (glideStartTime == 0f)
            {
                glideStartTime = Time.time;
                glideStartPosition = transform.position;
            }
        }
        else
        {
            glideStartTime = 0f;
        }
    }
    
    void FixedUpdate()
    {
        // Calculate aerodynamic forces
        CalculateAerodynamicForces();
        
        // Apply forces to rigidbody
        ApplyAerodynamicForces();
        
        // Apply flight controls
        ApplyFlightControls();
        
        // Limit maximum speed
        LimitMaxSpeed();
    }
    
    void CalculateAerodynamicForces()
    {
        // Calculate air-relative velocity
        Vector3 relativeVelocity = velocity - windDirection;
        float relativeSpeed = relativeVelocity.magnitude;
        
        if (relativeSpeed < 0.1f) return; // Avoid division by zero
        
        // Airflow direction
        Vector3 airflowDirection = relativeVelocity.normalized;
        
        // Calculate angle of attack (angle between aircraft direction and airflow)
        float angleOfAttack = Vector3.Angle(transform.forward, airflowDirection);
        
        // Lift force (perpendicular to airflow)
        float liftMagnitude = 0.5f * airDensity * relativeSpeed * relativeSpeed * wingArea * liftCoefficient;
        liftMagnitude *= Mathf.Cos(angleOfAttack * Mathf.Deg2Rad); // Reduce lift with angle of attack
        
        // Increase lift when aircraft descends to slow down fall
        if (velocity.y < 0)
        {
            liftMagnitude *= 1.5f; // 50% more lift in descent
        }
        
        Vector3 liftDirection = Vector3.Cross(airflowDirection, transform.right).normalized;
        liftForce = liftDirection * liftMagnitude;
        
        // Drag force (opposite to airflow)
        float dragMagnitude = 0.5f * airDensity * relativeSpeed * relativeSpeed * wingArea * dragCoefficient;
        
        // Increase drag in descent to slow down
        if (velocity.y < 0)
        {
            dragMagnitude *= 1.3f; // 30% more drag in descent
        }
        
        dragForce = -airflowDirection * dragMagnitude;
        
        // Side force (drift effect)
        Vector3 sideDirection = Vector3.Cross(airflowDirection, Vector3.up).normalized;
        sideForce = sideDirection * dragMagnitude * 0.1f; // 10% of drag
    }
    
    void ApplyAerodynamicForces()
    {
        // Apply lift
        rb.AddForce(liftForce, ForceMode.Force);
        
        // Apply drag
        rb.AddForce(dragForce, ForceMode.Force);
        
        // Apply side force
        rb.AddForce(sideForce, ForceMode.Force);
        
        // Apply gravity
        Vector3 gravityForce = Vector3.down * gravity * mass;
        rb.AddForce(gravityForce, ForceMode.Force);
    }
    
    void ApplyFlightControls()
    {
        // Multiply sensitivity based on speed (more control at high speed)
        float speedMultiplier = Mathf.Clamp(airspeed / 10f, 0.5f, 2f);
        
        // Apply target angles (X=pitch, Y=yaw, Z=roll)
        Vector3 targetRotation = new Vector3(currentPitch, currentYaw, currentRoll);
        
        // Rotate towards target angle
        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternion, angleResponseSpeed * Time.deltaTime);
        
        // Aileron controls (LEFT/RIGHT) - rotation around Z axis (roll)
        float aileronDeflection = aileronInput * maxControlDeflection;
        Vector3 aileronTorque = transform.forward * aileronDeflection * aileronSensitivity * controlMultiplier * speedMultiplier;
        rb.AddTorque(aileronTorque, ForceMode.Force);
        
        // Turn force based on banking (banking turn)
        if (Mathf.Abs(currentRoll) > 5f)
        {
            float turnForce = Mathf.Sin(currentRoll * Mathf.Deg2Rad) * airspeed * 0.5f;
            Vector3 turnDirection = transform.right * -Mathf.Sign(currentRoll); // Reverse force direction
            rb.AddForce(turnDirection * turnForce, ForceMode.Force);
        }
        
        // Reduced automatic stability (tendency to return to horizontal)
        Vector3 stabilityTorque = Vector3.Cross(transform.up, Vector3.up) * stabilityFactor * 0.3f;
        rb.AddTorque(stabilityTorque, ForceMode.Force);
        
        // Climb force based on momentum
        if (canClimb && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.S)))
        {
            float climbForce = currentMomentum * 3f; // Force proportional to momentum (increased)
            Vector3 climbVector = Vector3.up * climbForce;
            rb.AddForce(climbVector, ForceMode.Force);
        }
        
        // Add basic propulsion force to maintain speed
        if (airspeed < minSpeed)
        {
            Vector3 propulsionForce = transform.forward * 8f;
            rb.AddForce(propulsionForce, ForceMode.Force);
        }
    }
    
    void LimitMaxSpeed()
    {
        // Limit horizontal speed
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedHorizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedHorizontalVelocity.x, velocity.y, limitedHorizontalVelocity.z);
        }
        
        // Limit vertical fall speed while gliding
        if (isGliding && velocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector3(velocity.x, -maxFallSpeed, velocity.z);
        }
        
        // Limit climb speed
        if (velocity.y > maxClimbSpeed)
        {
            rb.linearVelocity = new Vector3(velocity.x, maxClimbSpeed, velocity.z);
        }
    }
    
    void OnGUI()
    {
        // Debug UI for paper plane physics
        GUI.Label(new Rect(10, 10, 300, 20), $"Speed: {airspeed:F2} m/s");
        GUI.Label(new Rect(10, 30, 300, 20), $"Altitude: {transform.position.y:F2} m");
        GUI.Label(new Rect(10, 50, 300, 20), $"Stalling: {isStalling}");
        GUI.Label(new Rect(10, 70, 300, 20), $"Gliding: {isGliding}");
        GUI.Label(new Rect(10, 90, 300, 20), $"Elevator: {elevatorInput:F2}");
        GUI.Label(new Rect(10, 110, 300, 20), $"Ailerons: {aileronInput:F2}");
        GUI.Label(new Rect(10, 130, 300, 20), $"Lift: {liftForce.magnitude:F2} N");
        GUI.Label(new Rect(10, 150, 300, 20), $"Drag: {dragForce.magnitude:F2} N");
        GUI.Label(new Rect(10, 170, 300, 20), $"Velocity X: {velocity.x:F2}");
        GUI.Label(new Rect(10, 190, 300, 20), $"Velocity Y: {velocity.y:F2}");
        GUI.Label(new Rect(10, 210, 300, 20), $"Velocity Z: {velocity.z:F2}");
        GUI.Label(new Rect(10, 230, 300, 20), $"Controls: W=forward+momentum, S=backward+climb, A=left, D=right");
        GUI.Label(new Rect(10, 250, 300, 20), $"Barrel Roll: Shift+A/D (360° roll + fast turn)");
        GUI.Label(new Rect(10, 270, 300, 20), $"Max fall speed: {maxFallSpeed:F1} m/s");
        GUI.Label(new Rect(10, 290, 300, 20), $"Pitch angle: {currentPitch:F1}°");
        GUI.Label(new Rect(10, 310, 300, 20), $"Roll angle: {currentRoll:F1}°");
        GUI.Label(new Rect(10, 330, 300, 20), $"Yaw angle: {currentYaw:F1}°");
        GUI.Label(new Rect(10, 350, 300, 20), $"Momentum: {currentMomentum:F2}");
        GUI.Label(new Rect(10, 370, 300, 20), $"Can climb: {canClimb}");
        GUI.Label(new Rect(10, 390, 300, 20), $"Gliding mode: {isGliding}");
        GUI.Label(new Rect(10, 410, 300, 20), $"Barrel roll: {isBarrelRolling} ({barrelRollProgress:F2})");
        GUI.Label(new Rect(10, 430, 300, 20), $"Target Pitch: {targetPitch:F1}°");
        GUI.Label(new Rect(10, 450, 300, 20), $"Current Pitch: {currentPitch:F1}°");
    }
}
