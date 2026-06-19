using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerStateMachine : StateMachine
{
    //Control Start
    InputReader controls;
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
    [SerializeField] float _acceleration = 12f;        // Fuerza de aceleración al mantener input
    [ExcelPort]
    [SerializeField] float _targetVelocity = 10f;      // Velocidad máxima horizontal

    [Header("Desaceleración")]
    [SerializeField] float _deceleration = 8f;          // Fuerza de frenado cuando se suelta el input
    
    [Header("Air Control")]
    [SerializeField] float _airAcceleration = 5f;       // Aceleración/maniobrabilidad en el aire
    [SerializeField] float _airDeceleration = 0f;       // Freno en el aire (0 = conserva toda la inercia)

    [SerializeField, Range(0f, 1f)] float _turnaroundStrength;
    [SerializeField] float rotationSpeed = 50f;
    public CinemachineInputAxisController cineMachineCamera;

    // Estado interno de velocidad horizontal para conservar inercia real sin depender de cc.velocity
    public Vector3 CurrentHorizontalVelocity { get { return _currentHorizontalVelocity; } set { _currentHorizontalVelocity = value; } }
    private Vector3 _currentHorizontalVelocity;
    //---- END PLAYER OWN MOVEMENT


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
        // Registrar todos los estados disponibles en la máquina de estados
        AddState(new PlayerIdleState(this));
        AddState(new PlayerVaultState(this));
        AddState(new PlayerWallRunState(this));  // Nuevo estado de wall-run
        
        SwitchState(typeof(PlayerIdleState));
    }

    // =====================================================================
    //  LATE UPDATE — Aplica el tilt de cámara cada frame después del render
    // =====================================================================
    private void LateUpdate()
    {
        UpdateCameraTilt();
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
        float decel = Grounded ? _deceleration : _airDeceleration;

        // 1. APLICAR FRICCIÓN SIEMPRE (Como en la vida real)
        float speed = currentVelocity.magnitude;
        if (speed > 0)
        {
            float drop = decel * Time.deltaTime;
            
            // Freno extra si estamos girando drásticamente en el suelo (turnaround)
            if (inputDir.sqrMagnitude > 0.01f && Grounded)
            {
                float dot = Vector3.Dot(currentVelocity.normalized, inputDir);
                if (dot < 0) 
                {
                    drop += (decel * -dot * _turnaroundStrength * Time.deltaTime);
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
        cc.Move(currentVelocity * Time.deltaTime);
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
        return Physics.Raycast(
            transform.position + transform.up * 0.5f,   // Origen ligeramente elevado
            transform.right,                             // Dirección: derecha del jugador
            out hit,
            _wallCheckDistance,                          // Distancia máxima de detección
            _wallRunMask                                 // Solo capas marcadas como pared
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
        return Physics.Raycast(
            transform.position + transform.up * 0.5f,   // Origen ligeramente elevado
            -transform.right,                            // Dirección: izquierda del jugador
            out hit,
            _wallCheckDistance,                          // Distancia máxima de detección
            _wallRunMask                                 // Solo capas marcadas como pared
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

}
