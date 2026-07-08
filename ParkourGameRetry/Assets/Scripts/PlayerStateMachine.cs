using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerStateMachine : StateMachine
{
    //Control Start
    InputReader controls;
    public InputReader Controls => controls;
    //Control End

    //----- START GROUND DETECTION
    [SerializeField] float _height;
    [SerializeField] float _castRadius = 0.49f;
    [SerializeField] float _castLength = 0.1f;
    [SerializeField] float _maxAngle = 45f;
    Vector3 _downDir = Vector3.down;

    [SerializeField] LayerMask _groundMask = int.MinValue;
    public LayerMask GroundMask => _groundMask;

    public Vector3 GroundNormal => _groundNormal;
    Vector3 _groundNormal;
    public bool Grounded => _grounded;
    bool _grounded;

    public Vector3 SlideNormal => _slideNormal;
    Vector3 _slideNormal;
    public bool Sliding => _sliding;
    bool _sliding;
    //----- END GROUND DETECTION


    //----START Character Controller
    [SerializeField] CharacterController cc;

    Vector2 worldInputDir;

    //---- END Character Controller

    //---- START PLAYER OWN MOVEMENT

    //Variables 
    [ExcelPort]
    [SerializeField] float _acceleration = 8f;         // Fuerza de aceleración al mantener input
    [ExcelPort]
    [SerializeField] float _targetVelocity = 10f;      // Velocidad máxima horizontal

    [Header("Desaceleración")]
    [SerializeField] float _deceleration = 1.5f;       // Fricción leve (conserva el momentum extra)
    [SerializeField] float _brakingDeceleration = 20f; // Fuerza de frenado fuerte al soltar el input
    
    [Header("Air Control")]
    [SerializeField] float _airAcceleration = 3f;       // Aceleración/maniobrabilidad en el aire
    [SerializeField] float _airDeceleration = 0f;       // Freno en el aire (0 = conserva toda la inercia)

    [SerializeField, Range(0f, 1f)] float _turnaroundStrength;
    [SerializeField] float rotationSpeed = 50f;
    public CinemachineInputAxisController cineMachineCamera;

    // Estado interno de velocidad horizontal para conservar inercia real sin depender de cc.velocity
    public Vector3 CurrentHorizontalVelocity { get { return _currentHorizontalVelocity; } set { _currentHorizontalVelocity = value; } }
    private Vector3 _currentHorizontalVelocity;
    //---- END PLAYER OWN MOVEMENT

    // =====================================================================
    //  SLIDE — Deslizamiento en suelo estilo Mirror's Edge / Apex
    // =====================================================================
    [Header("Slide")]
    [SerializeField] float _slideSpeed = 12f;              // Velocidad inicial del slide
    [SerializeField] float _slideDuration = 0.8f;           // Duración máxima del slide
    [SerializeField] float _slideMinSpeed = 5f;              // Velocidad mínima para poder iniciar slide
    [SerializeField] float _slideFriction = 6f;              // Fricción durante el slide
    [SerializeField] float _slideSlopeBoost = 15f;           // Velocidad extra al deslizar cuesta abajo
    [SerializeField] float _slideHeightReduction = 0.5f;     // Cuánto se reduce la altura del CC al deslizar
    [SerializeField] float _slideCameraDropSpeed = 10f;      // Velocidad a la que la cámara baja al deslizar

    public float SlideSpeed => _slideSpeed;
    public float SlideDuration => _slideDuration;
    public float SlideMinSpeed => _slideMinSpeed;
    public float SlideFriction => _slideFriction;
    public float SlideSlopeBoost => _slideSlopeBoost;
    public float SlideHeightReduction => _slideHeightReduction;
    public float SlideCameraDropSpeed => _slideCameraDropSpeed;

    private float _originalCCHeight;
    private Vector3 _originalCCCenter;
    public float OriginalCCHeight => _originalCCHeight;
    public Vector3 OriginalCCCenter => _originalCCCenter;

    // =====================================================================
    //  DASH — Impulso rápido con cooldown
    // =====================================================================
    [Header("Dash")]
    [SerializeField] float _dashForce = 20f;              // Fuerza del dash
    [SerializeField] float _dashDuration = 0.15f;          // Duración del dash
    [SerializeField] float _dashCooldown = 1.5f;           // Cooldown entre dashes

    public float DashForce => _dashForce;
    public float DashDuration => _dashDuration;
    public float DashCooldown => _dashCooldown;
    public float LastDashTime { get; set; } = -999f;

    // =====================================================================
    //  LEDGE GRAB — Colgarse de bordes altos
    // =====================================================================
    [Header("Ledge Grab")]
    [SerializeField] float _ledgeCheckDistance = 1.0f;     // Distancia del raycast hacia adelante
    [SerializeField] float _ledgeGrabMaxHeight = 2.5f;     // Altura máxima para agarrarse
    [SerializeField] float _ledgeGrabMinHeight = 1.6f;     // Altura mínima (por debajo = vault)
    [SerializeField] float _ledgeClimbDuration = 0.4f;     // Duración de la animación de subir
    [SerializeField] float _ledgeJumpForce = 7f;           // Fuerza de salto desde ledge

    public float LedgeCheckDistance => _ledgeCheckDistance;
    public float LedgeGrabMaxHeight => _ledgeGrabMaxHeight;
    public float LedgeGrabMinHeight => _ledgeGrabMinHeight;
    public float LedgeClimbDuration => _ledgeClimbDuration;
    public float LedgeJumpForce => _ledgeJumpForce;
    public Vector3 LedgeGrabPosition { get; set; }
    public Vector3 LedgeClimbTarget { get; set; }

    // =====================================================================
    //  LANDING ROLL — Voltereta al aterrizar de caída alta
    // =====================================================================
    [Header("Landing Roll")]
    [SerializeField] float _rollMinFallSpeed = 8f;         // Velocidad mínima de caída para activar roll
    [SerializeField] float _rollDuration = 0.5f;            // Duración del roll
    [SerializeField] float _rollSpeedBoost = 1.2f;          // Multiplicador de velocidad durante roll
    [SerializeField] float _hardLandingPenalty = 0.3f;       // % de velocidad que pierdes si NO haces roll
    [SerializeField] float _rollCameraAngle = 15f;           // Inclinación de cámara durante roll

    public float RollMinFallSpeed => _rollMinFallSpeed;
    public float RollDuration => _rollDuration;
    public float RollSpeedBoost => _rollSpeedBoost;
    public float HardLandingPenalty => _hardLandingPenalty;
    public float RollCameraAngle => _rollCameraAngle;

    // =====================================================================
    //  GRAPPLE — Gancho que impulsa al jugador hacia un punto
    // =====================================================================
    [Header("Grapple")]
    [SerializeField] float _grappleMaxDistance = 30f;      // Distancia máxima del gancho
    [SerializeField] float _grappleSpeed = 20f;             // Velocidad de viaje
    [SerializeField] float _grappleExitBoost = 1.5f;        // Multiplicador de velocidad al llegar
    [SerializeField] LayerMask _grappleMask;                // Capas donde se puede enganchar

    public float GrappleMaxDistance => _grappleMaxDistance;
    public float GrappleSpeed => _grappleSpeed;
    public float GrappleExitBoost => _grappleExitBoost;
    public LayerMask GrappleMask => _grappleMask;
    public Vector3 GrappleTarget { get; set; }

    // =====================================================================
    //  ZIPLINE — Rails/cables deslizantes
    // =====================================================================
    [Header("Zipline")]
    [SerializeField] float _ziplineSpeed = 15f;            // Velocidad base en zipline
    [SerializeField] float _ziplineSpeedBoost = 1.3f;       // Multiplicador si vienes con impulso
    [SerializeField] float _ziplineExitUpForce = 4f;        // Fuerza vertical al saltar desde zipline

    public float ZiplineSpeed => _ziplineSpeed;
    public float ZiplineSpeedBoost => _ziplineSpeedBoost;
    public float ZiplineExitUpForce => _ziplineExitUpForce;

    // =====================================================================
    //  MOMENTUM SYSTEM — Multiplicador por encadenar parkour
    // =====================================================================
    [Header("Momentum System")]
    [SerializeField] float _momentumMultiplierPerChain = 0.1f;  // Bonus de velocidad por cada acción encadenada
    [SerializeField] float _momentumMaxMultiplier = 1.5f;        // Multiplicador máximo
    [SerializeField] float _momentumDecayTime = 2f;              // Tiempo hasta perder el combo
    [SerializeField] float _microSlowMoDuration = 0.05f;         // Duración del slow-mo al encadenar

    public float MomentumMultiplierPerChain => _momentumMultiplierPerChain;
    public float MomentumMaxMultiplier => _momentumMaxMultiplier;
    public float MomentumDecayTime => _momentumDecayTime;
    public float MicroSlowMoDuration => _microSlowMoDuration;

    // Runtime momentum tracking
    public int MomentumChainCount { get; set; }
    public float MomentumMultiplier => 1f + Mathf.Min(MomentumChainCount * _momentumMultiplierPerChain, _momentumMaxMultiplier - 1f);
    public float LastParkourActionTime { get; set; } = -999f;

    // ---- Crouch input ----
    public bool CrouchPressed { get; private set; }


    // =====================================================================
    //  WALL-RUN — Parámetros configurables desde el Inspector
    // =====================================================================
    [Header("Wall-Run")]
    [SerializeField] float _wallRunSpeed = 8f;              // Velocidad horizontal durante wall-run
    [SerializeField] float _wallRunMaxDuration = 1.5f;      // Tiempo máximo que puede durar el wall-run
    [SerializeField] float _wallRunGravityScale = 0.3f;     // Multiplicador de gravedad durante wall-run (0 = flotar, 1 = normal)
    [SerializeField] float _wallCheckDistance = 0.8f;       // Distancia del raycast lateral para detectar paredes
    [SerializeField] float _wallRunMinSpeed = 3f;           // Velocidad mínima para poder iniciar wall-run
    [SerializeField] float _wallRunExitForce = 6f;          // Fuerza de impulso lateral al salir del wall-run
    [SerializeField] float _wallRunExitUpForce = 5f;        // Fuerza de impulso vertical al salir del wall-run
    [SerializeField] LayerMask _wallRunMask;                // Capas que se consideran paredes válidas para wall-run

    // Propiedades públicas para que PlayerWallRunState acceda a los parámetros
    public float WallRunSpeed => _wallRunSpeed;
    public float WallRunMaxDuration => _wallRunMaxDuration;
    public float WallRunGravityScale => _wallRunGravityScale;
    public float WallRunExitForce => _wallRunExitForce;
    public float WallRunExitUpForce => _wallRunExitUpForce;
    public float GravityForce => _gravityForce;
    public CharacterController CC => cc;

    // =====================================================================
    //  CAMERA TILT — Rotación Z de la cámara durante wall-run
    // =====================================================================
    [Header("Camera Tilt")]
    [SerializeField] Transform _cameraTiltTarget;           // Transform de la cámara (o pivote hijo) donde se aplica el tilt Z
    [SerializeField] float _wallRunCameraTiltAngle = 15f;   // Ángulo máximo de inclinación en Z
    [SerializeField] float _wallRunCameraTiltSpeed = 5f;    // Velocidad del lerp para suavizar la transición

    public float WallRunCameraTiltAngle => _wallRunCameraTiltAngle;
    public float WallRunCameraTiltSpeed => _wallRunCameraTiltSpeed;

    // Estado interno del tilt: se interpola de _currentCameraTilt hacia _targetCameraTilt
    private float _currentCameraTilt = 0f;
    private float _targetCameraTilt = 0f;


    //----- START PLAYER GRAVITY

    [ExcelPort]
    [SerializeField] float _gravityForce = 9.81f;
    public Vector3 GravityDir { get { return _gravityDir; } set { _gravityDir = value.normalized; } }
    Vector3 _gravityDir = Vector3.down;


    //----- END PLAYER GRAVITY

    //  --- VAULT
    
    [Header("Vault")]
    [SerializeField] LayerMask _vaultMask;
    [SerializeField] float _vaultCheckDistance = 1.2f;   // how far forward to check
    [SerializeField] float _vaultMaxHeight = 1.5f;        // max obstacle height to vault
    [SerializeField] float _vaultMinHeight = 0.3f;        // min obstacle height
    [SerializeField] float _vaultDuration = 0.35f;
    public float VaultDuration => _vaultDuration;
    [SerializeField] float _vaultArcHeight = 0.4f;
    public float VaultArcHeight => _vaultArcHeight;
    [SerializeField] float _vaultExitSpeed = 4f;
    public float VaultExitSpeed => _vaultExitSpeed;
    [SerializeField] float _vaultExitUpForce = 1f;
    public float VaultExitUpForce => _vaultExitUpForce;

    public Vector3 VaultTargetPosition { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    [Header("Jump")]
    [ExcelPort]
    [SerializeField] float _jumpForce = 8f;
    public float JumpForce => _jumpForce;

    // Coyote Time: allows jumping for a brief window after walking off a ledge
    [ExcelPort]
    [SerializeField] float _coyoteTime = 0.15f;
    public float CoyoteTime => _coyoteTime;
    public float CoyoteTimeCounter;   // counts down from CoyoteTime

    // Jump Buffer: remembers jump input slightly before landing
    [ExcelPort(" remembers jump input slightly before landing")]
    [SerializeField] float _jumpBufferTime = 0.15f;
    public float JumpBufferTime => _jumpBufferTime;
    public float JumpBufferCounter;   // counts down from JumpBufferTime
   

    //  --- END VAULT

    private void OnValidate()
    {
        _downDir = _downDir.normalized;
        controls = GetComponent<InputReader>();
        
    }

    private void Start()
    {
        // Guardar dimensiones originales del CharacterController para restaurar tras slide
        _originalCCHeight = cc.height;
        _originalCCCenter = cc.center;

        // Registrar todos los estados disponibles en la máquina de estados
        AddState(new PlayerIdleState(this));
        AddState(new PlayerVaultState(this));
        AddState(new PlayerWallRunState(this));
        AddState(new PlayerSlideState(this));
        AddState(new PlayerDashState(this));
        AddState(new PlayerLedgeGrabState(this));
        AddState(new PlayerLandingRollState(this));
        AddState(new PlayerGrappleState(this));
        AddState(new PlayerZiplineState(this));
        
        SwitchState(typeof(PlayerIdleState));
    }

    // =====================================================================
    //  LATE UPDATE — Aplica el tilt de cámara cada frame después del render
    // =====================================================================
    private void LateUpdate()
    {
        UpdateCameraTilt();
        TickMomentum();
    }


    public void GroundDetection()
    {
        // Prevent snapping back to the ground if the player is moving up (e.g. jumping)
        if (Vector3.Dot(currentForceGravity, -_downDir) > 0.1f)
        {
            _grounded = false;
            _sliding = false;
            return;
        }

        if (Physics.SphereCast(transform.position + transform.up * _height, _castRadius, _downDir, out RaycastHit hitInfo, _castLength, _groundMask))
        {
            Debug.Log(hitInfo.transform.up);
            Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, 50f);
            
            if (Vector3.Dot(hitInfo.normal, -_downDir) > Mathf.Sin((90f - _maxAngle) * Mathf.PI / 180f))
            {
                _groundNormal = hitInfo.normal;
                _grounded = true;
                _sliding = false;
                Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.blue, Time.fixedDeltaTime);
                _slideNormal = hitInfo.normal;
            }
            else
            {
                _grounded = false;
                _sliding = true;
                _slideNormal = hitInfo.normal;
            }
        }
        else
        {
            _grounded = false;
            _groundNormal = Vector3.up;
            _sliding = false;
        }

        Debug.Log($"Grounded: {Grounded}, Sliding: {Sliding}");
    }

    public Vector2 GetInput()// TODO Replace with new input system
    {
        Vector2 moveInput;
        moveInput = controls.MovementValue;
        moveInput.Normalize();
        return moveInput;
    }

    public void PlayerHorizontalMovement(Vector3 inputDir)
    {
        Vector3 currentVelocity = _currentHorizontalVelocity;

        // Override velocity si es requerido (ej: salir de un vault para parar el impulso brusco)
        if (_forceZeroVelocityNextFrame)
        {
            currentVelocity = Vector3.zero;
            _forceZeroVelocityNextFrame = false;
        }

        // Alinear los vectores al plano del suelo (para no volar ni hundirnos al correr)
        inputDir = Vector3.ProjectOnPlane(inputDir, GroundNormal).normalized;
        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, GroundNormal);

        float accel = Grounded ? _acceleration : _airAcceleration;
        
        // Decidimos la fuerza de deceleración (fricción leve vs freno fuerte)
        float currentDecelValue;
        if (Grounded)
        {
            if (inputDir.sqrMagnitude < 0.01f)
            {
                currentDecelValue = _brakingDeceleration; // Frenar al soltar
            }
            else
            {
                currentDecelValue = _deceleration; // Fricción leve mientras te mueves
            }
        }
        else
        {
            currentDecelValue = _airDeceleration;
        }

        // 1. APLICAR FRICCIÓN/FRENADO SIEMPRE
        float speed = currentVelocity.magnitude;
        if (speed > 0)
        {
            float drop = currentDecelValue * Time.deltaTime;
            
            // Freno extra si estamos girando drásticamente en el suelo (turnaround)
            if (inputDir.sqrMagnitude > 0.01f && Grounded)
            {
                float dot = Vector3.Dot(currentVelocity.normalized, inputDir);
                if (dot < 0) 
                {
                    // Al cambiar de dirección bruscamente, usamos el freno fuerte para ser ágiles
                    drop += (_brakingDeceleration * -dot * _turnaroundStrength * Time.deltaTime);
                }
            }

            float newSpeed = Mathf.Max(speed - drop, 0);
            currentVelocity *= (newSpeed / speed);
        }

        // 2. APLICAR ACELERACIÓN (Fuerza Aditiva)
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Calculamos cuánta de nuestra velocidad actual ya va en la dirección del input
            float currentSpeedInInputDirection = Vector3.Dot(currentVelocity, inputDir);
            
            // Calculamos cuánta velocidad nos falta para llegar a la velocidad máxima en esa dirección
            float addSpeed = _targetVelocity - currentSpeedInInputDirection;

            if (addSpeed > 0)
            {
                // Aceleramos, pero sin pasarnos de 'addSpeed'
                float accelSpeed = accel * Time.deltaTime;
                if (accelSpeed > addSpeed) accelSpeed = addSpeed;
                
                currentVelocity += inputDir * accelSpeed;
            }
            // Si addSpeed <= 0, significa que ya vamos más rápido que _targetVelocity en esa dirección
            // (por ejemplo, saliendo de un wall-run). No frenamos forzosamente, la fricción se encargará gradualmente.
        }

        // 3. DESLIZAMIENTO EN PENDIENTES
        if (Sliding && Grounded)
        {
            Vector3 slideVelocity = Vector3.ProjectOnPlane(currentVelocity, SlideNormal);
            currentVelocity = slideVelocity;
        }

        // Guardar inercia real para el siguiente frame
        _currentHorizontalVelocity = currentVelocity;

        // Mover el CharacterController (horizontalmente, la gravedad va por separado)
        Vector3 posBeforeMove = transform.position;
        cc.Move(currentVelocity * Time.deltaTime);

        // ---- DETECCIÓN DE COLISIÓN ----
        // Comparar el desplazamiento real con el deseado.
        // Si el CC chocó con algo, el desplazamiento real será menor.
        // Ajustamos _currentHorizontalVelocity para que no conserve momentum fantasma.
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 actualDisplacement = transform.position - posBeforeMove;
            Vector3 intendedDisplacement = currentVelocity * Time.deltaTime;

            // Ignorar Y (la gravedad va por otro lado)
            actualDisplacement.y = 0f;
            intendedDisplacement.y = 0f;

            if (intendedDisplacement.sqrMagnitude > 0.0001f)
            {
                float movementRatio = actualDisplacement.magnitude / intendedDisplacement.magnitude;

                // Si nos movimos menos del 80% de lo que queríamos, hay colisión
                if (movementRatio < 0.8f)
                {
                    // Reducir la velocidad al ratio real del movimiento
                    // Esto hace que si chocas contra una pared, pierdas el momentum
                    _currentHorizontalVelocity *= movementRatio;
                }
            }
        }
        }

    /// <summary>
    /// Convierte el input 2D en una dirección 3D basada en la rotación de la cámara.
    /// </summary>
    public Vector3 GetWorldMovementDirection(Vector2 input)
    {
        Transform cameraTransform = Camera.main.transform;
        
        // Obtener forward y right de la cámara, ignorando la inclinación arriba/abajo
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        return (camRight * input.x + camForward * input.y).normalized;
    }


    // Nota: Las antiguas funciones 2D (CameraOritentedMovement, Get2dOrientation, MultiplyByPlane y Movement)
    // fueron reemplazadas por la lógica matemática en 3D de PlayerHorizontalMovement y GetWorldMovementDirection.

    public void SubscribeInputJump()
    {
        controls.OnJumpStarted += HandleJumpStarted;
        controls.OnJumpCanceled += HandleJumpCanceled;
    }

    public void UnsubscribeInputJump()
    {
        controls.OnJumpStarted -= HandleJumpStarted;
        controls.OnJumpCanceled -= HandleJumpCanceled;
    }

    private void HandleJumpStarted()
    {
        JumpPressed = true;
        JumpHeld = true;
        JumpBufferCounter = _jumpBufferTime; // Inicializar buffer de salto
    }

    private void HandleJumpCanceled()
    {
        JumpHeld = false;
        JumpPressed = false;
    }

    public void ClearJumpInputFrame()
    {
        JumpPressed = false;
    }

    public void DecrementJumpBuffer(float dt)
    {
        if (JumpBufferCounter > 0f)
            JumpBufferCounter -= dt;
    }

    public void ExecuteJump()
    {
        currentForceGravity = -_gravityDir * _jumpForce;
        cc.Move(currentForceGravity * Time.deltaTime);
        JumpBufferCounter = 0f;
        CoyoteTimeCounter = 0f;
        JumpPressed = false; // Consume the initial jump press to prevent infinite jumps
        _grounded = false;   // Force unground to ensure apply gravity doesn't reset velocity immediately
    }

    private bool _forceZeroVelocityNextFrame;

    /// <summary>
    /// Limpia inputs de salto y resetea la fisica interna al salir del vault.
    /// Evita el salto automatico y el disparo de velocidad post-vault.
    /// </summary>
    public void ResetVaultPhysics()
    {
        JumpPressed = false;
        JumpHeld = false;
        JumpBufferCounter = 0f;

        currentForceGravity = Vector3.zero;
        _currentHorizontalVelocity = Vector3.zero;

        // Ignora el spike de velocidad del CharacterController generado durante el vault
        _forceZeroVelocityNextFrame = true;
    }

    // =====================================================================
    //  UPDATE — Coyote Time counter
    // =====================================================================
    public void TickCoyoteTime(float dt)
    {
        if (Grounded)
            CoyoteTimeCounter = _coyoteTime;
        else if (CoyoteTimeCounter > 0f)
            CoyoteTimeCounter -= dt;
    }

    public float VelocityRemaining(Vector3 currentVelocity, Vector3 input)
    {
        float dot = Vector2.Dot(currentVelocity.normalized, input.normalized);

        dot += 1f + _turnaroundStrength;
        dot /= 2f + _turnaroundStrength;
        dot = Mathf.Pow(dot, 1f - _turnaroundStrength);
        return dot;
    }


    private Vector3 currentForceGravity = Vector3.zero;

    public void ApplyGravity()
    {
        if (Grounded)
        {
            currentForceGravity = (_gravityDir * _gravityForce) * Time.deltaTime;
            Vector3 groundedGravity = Vector3.ProjectOnPlane(_gravityDir * _gravityForce, GroundNormal.normalized);
            cc.Move((GravityDir * _gravityForce) * Time.deltaTime);
        }
        else
        {
            if (Sliding)
            {
                currentForceGravity = Vector3.zero;
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, SlideNormal).normalized;
                cc.Move(slideDirection * _gravityForce * Time.deltaTime);
            }
            else
            {
                currentForceGravity += (_gravityDir * _gravityForce) * Time.deltaTime;
                cc.Move(currentForceGravity * Time.deltaTime);
            }
        }
    }


    public void PlayerLook()
    {
        Vector2 rotateVector = controls.LookValue;
        float horizontalInput = rotateVector.x;

        if (horizontalInput != 0)
        {
            float rotationAngle = horizontalInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, rotationAngle, 0f);
        }
    }

    // =====================================================================
    //  WALL-RUN — Detección de paredes laterales con Raycast
    // =====================================================================

    /// <summary>
    /// Lanza un raycast hacia la DERECHA del jugador para detectar una pared.
    /// Origen: centro del jugador (a media altura), dirección: transform.right
    /// </summary>
    /// <param name="hit">Información del impacto si se encuentra pared</param>
    /// <returns>true si hay pared a la derecha dentro de _wallCheckDistance</returns>
    public bool CheckWallRight(out RaycastHit hit)
    {
        // Origen al nivel del pecho (1.0f) para NO detectar paredes cuando estamos en el suelo.
        // A 0.5f detectaba paredes incluso al estar de pie junto a ellas.
        return Physics.Raycast(
            transform.position + transform.up * 1.0f,    // Origen al nivel del pecho
            transform.right,                              // Dirección: derecha del jugador
            out hit,
            _wallCheckDistance,                           // Distancia máxima de detección
            _wallRunMask                                  // Solo capas marcadas como pared
        );
    }

    /// <summary>
    /// Lanza un raycast hacia la IZQUIERDA del jugador para detectar una pared.
    /// Origen: centro del jugador (a media altura), dirección: -transform.right
    /// </summary>
    /// <param name="hit">Información del impacto si se encuentra pared</param>
    /// <returns>true si hay pared a la izquierda dentro de _wallCheckDistance</returns>
    public bool CheckWallLeft(out RaycastHit hit)
    {
        // Origen al nivel del pecho (1.0f) — misma lógica que CheckWallRight
        return Physics.Raycast(
            transform.position + transform.up * 1.0f,    // Origen al nivel del pecho
            -transform.right,                             // Dirección: izquierda del jugador
            out hit,
            _wallCheckDistance,                           // Distancia máxima de detección
            _wallRunMask                                  // Solo capas marcadas como pared
        );
    }

    /// <summary>
    /// Evalúa si el jugador cumple TODAS las condiciones para iniciar un wall-run:
    /// 1. Debe estar en el aire (no grounded)
    /// 2. Debe tener suficiente velocidad horizontal (>= _wallRunMinSpeed)
    /// 3. Debe haber una pared a la izquierda O a la derecha
    /// </summary>
    /// <returns>true si se puede iniciar wall-run</returns>
    public bool CanWallRun()
    {
        // No se puede iniciar wall-run desde el suelo
        if (Grounded) return false;

        // Comprobar que el jugador lleva suficiente velocidad horizontal (usando la inercia real)
        Vector3 horizontalVel = new Vector3(_currentHorizontalVelocity.x, 0f, _currentHorizontalVelocity.z);
        if (horizontalVel.magnitude < _wallRunMinSpeed) return false;

        // Detectar pared a cualquier lado
        return CheckWallRight(out _) || CheckWallLeft(out _);
    }

    // =====================================================================
    //  CAMERA TILT — Rotación suave del eje Z de la cámara
    // =====================================================================

    /// <summary>
    /// Establece el ángulo objetivo del tilt de cámara.
    /// Valor positivo = pared a la izquierda (cámara inclina a la derecha del jugador).
    /// Valor negativo = pared a la derecha (cámara inclina a la izquierda del jugador).
    /// Valor 0 = sin tilt (posición neutral).
    /// </summary>
    /// <param name="targetAngle">Ángulo Z objetivo en grados</param>
    public void SetCameraTilt(float targetAngle)
    {
        _targetCameraTilt = targetAngle;
    }

    /// <summary>
    /// Interpola suavemente el tilt actual hacia el tilt objetivo.
    /// Se llama desde LateUpdate para que se aplique después de toda la lógica de cámara.
    /// Usa Mathf.Lerp con _wallRunCameraTiltSpeed para una transición suave.
    /// </summary>
    private void UpdateCameraTilt()
    {
        // Lerp suave del ángulo actual hacia el objetivo
        _currentCameraTilt = Mathf.Lerp(
            _currentCameraTilt,
            _targetCameraTilt,
            _wallRunCameraTiltSpeed * Time.deltaTime
        );

        // Aplicar la rotación Z al transform de la cámara (si está asignado)
        if (_cameraTiltTarget != null)
        {
            Vector3 euler = _cameraTiltTarget.localEulerAngles;
            euler.z = _currentCameraTilt;
            _cameraTiltTarget.localEulerAngles = euler;
        }
    }

    /// <summary>
    /// Aplica un impulso de salida del wall-run al CharacterController.
    /// Combina fuerza lateral (alejándose de la pared) + fuerza vertical (hacia arriba).
    /// Se usa desde PlayerWallRunState al salir por salto.
    /// </summary>
    /// <param name="wallNormal">Normal de la pared (apunta hacia fuera de la pared)</param>
    public void ApplyWallRunExitImpulse(Vector3 wallNormal)
    {
        // Resetear la gravedad acumulada para que el impulso vertical sea limpio
        currentForceGravity = Vector3.zero;

        // Impulso combinado: lateral (normal de pared) + vertical (arriba)
        Vector3 exitVelocity = wallNormal * _wallRunExitForce     // Fuerza que aleja de la pared
                             + Vector3.up * _wallRunExitUpForce;   // Fuerza hacia arriba

        // Aplicar la parte vertical a la gravedad
        currentForceGravity = Vector3.up * _wallRunExitUpForce;
        
        // Inyectar el impulso lateral directo en nuestra inercia horizontal para no perderlo
        _currentHorizontalVelocity = wallNormal * _wallRunExitForce;

        cc.Move(exitVelocity * Time.deltaTime);
    }

    //  VAULT DETECTION
    // =====================================================================
    /// <summary>
    /// Returns true if there's a vaultable obstacle in front of the player.
    /// Works both grounded (vault over) and airborne (ledge mantle/grab).
    /// Sets VaultTargetPosition to the landing spot.
    /// </summary>
    public bool CanVault()
    {
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        // Si el jugador se mueve significativamente ALEJANDOSE del obstaculo, no hacer vault.
        // Esto evita el vault hacia atras cuando caes de una plataforma mirando hacia ella.
        Vector3 horizontalVel = new Vector3(_currentHorizontalVelocity.x, 0f, _currentHorizontalVelocity.z);
        if (horizontalVel.magnitude > 1.0f && Vector3.Dot(horizontalVel.normalized, forward) < -0.3f)
            return false;

        // Busca una pared enfrente a la altura actual del jugador
        // Funciona tanto en suelo como en el aire (ledge mantle)
        if (!Physics.Raycast(transform.position + Vector3.up * (_vaultMinHeight + 0.1f),
            forward, out RaycastHit frontHit, _vaultCheckDistance, _vaultMask))
            return false;

        // Lanza un rayo hacia abajo desde arriba del punto de impacto
        // usando la Y del jugador como base, no la del suelo
        float castY = transform.position.y + _vaultMaxHeight;
        Vector3 overObstacle = new Vector3(frontHit.point.x, castY, frontHit.point.z) + forward * 0.1f;

        if (!Physics.Raycast(overObstacle, Vector3.down, out RaycastHit topHit, _vaultMaxHeight * 2f, _vaultMask))
            return false;

        float ledgeTopY = topHit.point.y;
        float playerY   = transform.position.y;

        // El techo del ledge debe estar ENCIMA del jugador (al menos _vaultMinHeight)
        // y no demasiado arriba (_vaultMaxHeight).
        //
        // El bound inferior es playerY + _vaultMinHeight, NO playerY - 0.3f.
        // Antes, esa tolerancia negativa permitia detectar la plataforma en la que
        // el jugador ya esta parado como un ledge valido → vault hacia atras.
        // Ahora, el ledge debe estar genuinamente por encima del jugador.
        if (ledgeTopY < playerY + _vaultMinHeight || ledgeTopY > playerY + _vaultMaxHeight)
            return false;

        // Posicion de aterrizaje: encima y al otro lado del ledge
        VaultTargetPosition = topHit.point + forward * (_vaultCheckDistance * 0.5f);
        return true;
    }

    // =====================================================================
    //  CROUCH INPUT — Suscripción/desuscripción para slide y landing roll
    // =====================================================================
    public void SubscribeInputCrouch()
    {
        controls.OnCrouchStarted += HandleCrouchStarted;
        controls.OnCrouchCanceled += HandleCrouchCanceled;
    }

    public void UnsubscribeInputCrouch()
    {
        controls.OnCrouchStarted -= HandleCrouchStarted;
        controls.OnCrouchCanceled -= HandleCrouchCanceled;
    }

    private void HandleCrouchStarted() { CrouchPressed = true; }
    private void HandleCrouchCanceled() { CrouchPressed = false; }

    // =====================================================================
    //  COLLIDER HELPERS — Para reducir/restaurar la altura del CC al deslizar
    // =====================================================================

    /// <summary>
    /// Reduce la altura del CharacterController para el slide.
    /// </summary>
    public void ShrinkCollider()
    {
        cc.height = _originalCCHeight - _slideHeightReduction;
        cc.center = _originalCCCenter + Vector3.down * (_slideHeightReduction * 0.5f);
    }

    /// <summary>
    /// Restaura el CharacterController a sus dimensiones originales.
    /// </summary>
    public void RestoreCollider()
    {
        cc.height = _originalCCHeight;
        cc.center = _originalCCCenter;
    }

    // =====================================================================
    //  VELOCITY HELPERS
    // =====================================================================

    /// <summary>
    /// Devuelve la velocidad vertical actual de caída (positivo = cayendo).
    /// Usado por PlayerCameraEffects y LandingRoll para medir la intensidad del impacto.
    /// </summary>
    public float GetVerticalSpeed()
    {
        return Vector3.Dot(currentForceGravity, Vector3.down);
    }

    // =====================================================================
    //  MOMENTUM SYSTEM — Registro y tick del sistema de encadenamiento
    // =====================================================================

    /// <summary>
    /// Registra una acción de parkour exitosa para el sistema de momentum.
    /// Si se encadena dentro de MomentumDecayTime, el combo sube.
    /// En combos > 1, se aplica un micro slow-mo cinematográfico.
    /// </summary>
    public void RegisterParkourAction()
    {
        if (Time.time - LastParkourActionTime < _momentumDecayTime)
        {
            MomentumChainCount++;
        }
        else
        {
            MomentumChainCount = 1;
        }
        LastParkourActionTime = Time.time;
        
        // Micro slow-mo al encadenar (solo si es combo de 2+)
        if (MomentumChainCount > 1)
        {
            Time.timeScale = 0.1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    /// <summary>
    /// Actualiza el sistema de momentum cada frame.
    /// Resetea el combo si ha pasado demasiado tiempo.
    /// Restaura el timeScale si fue ralentizado por el micro slow-mo.
    /// </summary>
    public void TickMomentum()
    {
        if (Time.time - LastParkourActionTime > _momentumDecayTime)
        {
            MomentumChainCount = 0;
        }
        // Restaurar time scale si fue ralentizado
        if (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, Time.unscaledDeltaTime / _microSlowMoDuration);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    // =====================================================================
    //  LEDGE GRAB DETECTION
    // =====================================================================

    /// <summary>
    /// Detecta si hay un borde al que agarrarse (demasiado alto para vault, alcanzable para grab).
    /// Usa dos raycasts: uno horizontal para encontrar la pared y uno vertical para encontrar el borde.
    /// </summary>
    public bool CanLedgeGrab()
    {
        if (Grounded) return false;
        
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        
        // Comprobar si hay una pared enfrente
        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, forward, out RaycastHit wallHit, _ledgeCheckDistance, _vaultMask))
            return false;
        
        // Comprobar el borde superior — lanzar raycast hacia abajo desde arriba
        Vector3 abovePoint = wallHit.point + Vector3.up * _ledgeGrabMaxHeight + forward * 0.1f;
        if (!Physics.Raycast(abovePoint, Vector3.down, out RaycastHit topHit, _ledgeGrabMaxHeight, _vaultMask))
            return false;
        
        float ledgeHeight = topHit.point.y - transform.position.y;
        
        // Debe estar por encima de la altura de vault pero por debajo de la máxima de grab
        if (ledgeHeight < _ledgeGrabMinHeight || ledgeHeight > _ledgeGrabMaxHeight)
            return false;
        
        LedgeGrabPosition = new Vector3(wallHit.point.x, topHit.point.y - cc.height, wallHit.point.z) + wallHit.normal * 0.1f;
        LedgeClimbTarget = topHit.point + forward * 0.5f + Vector3.up * 0.1f;
        return true;
    }
}
