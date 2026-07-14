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
    [SerializeField] float _groundProbeDistance = 0.18f;
    [SerializeField, Range(0.5f, 1f)] float _groundProbeRadiusScale = 0.9f;
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
    [SerializeField] float _mouseLookSensitivity = 0.09f;
    [SerializeField] float _gamepadLookSpeed = 180f;
    public CinemachineInputAxisController cineMachineCamera;
    [SerializeField] CinemachinePanTilt _cameraPanTilt;

    [Header("Speed Feedback")]
    [SerializeField] bool _dynamicFov = true;
    [SerializeField] float _baseFov = 90f;
    [SerializeField] float _maxFov = 105f;
    [SerializeField] float _fovSpeedReference = 23f;
    [SerializeField] float _fovLerpSpeed = 6f;
    [SerializeField] bool _enableParkourAudioFeedback = true;
    CinemachineCamera _activeCinemachineCamera;

    // Estado interno de velocidad horizontal para conservar inercia real sin depender de cc.velocity
    public Vector3 CurrentHorizontalVelocity
    {
        get { return _currentHorizontalVelocity; }
        set { _currentHorizontalVelocity = new Vector3(value.x, 0f, value.z); }
    }
    private Vector3 _currentHorizontalVelocity;
    //---- END PLAYER OWN MOVEMENT

    // =====================================================================
    //  SLIDE — Deslizamiento en suelo estilo Mirror's Edge / Apex
    // =====================================================================
    [Header("Slide")]
    [SerializeField] float _slideSpeed = 12f;              // Velocidad inicial del slide
    [SerializeField] float _slideDuration = 0.8f;           // Duración máxima del slide
    [SerializeField] float _slideMinSpeed = 5f;              // Velocidad mínima para poder iniciar slide
    [SerializeField] float _slideFriction = 3f;              // Fricción durante el slide
    [SerializeField] float _slideSlopeBoost = 15f;           // Velocidad extra al deslizar cuesta abajo
    [SerializeField] float _slideSteering = 3f;
    [SerializeField, Range(0f, 1f)] float _slideMomentumRetention = 0.99f;
    [SerializeField] float _slideSpeedReward = 0.4f;
    [SerializeField] float _slideHeightReduction = 0.5f;     // Cuánto se reduce la altura del CC al deslizar
    [SerializeField] float _slideCameraDropAmount = 0.55f;   // Bajada visible del punto de vista
    [SerializeField] float _slideCameraDropSpeed = 10f;      // Velocidad a la que la cámara baja al deslizar

    public float SlideSpeed => _slideSpeed;
    public float SlideDuration => _slideDuration;
    public float SlideMinSpeed => _slideMinSpeed;
    public float SlideFriction => _slideFriction;
    public float SlideSlopeBoost => _slideSlopeBoost;
    public float SlideSteering => _slideSteering;
    public float SlideMomentumRetention => _slideMomentumRetention;
    public float SlideSpeedReward => _slideSpeedReward;
    public float SlideHeightReduction => _slideHeightReduction;
    public float SlideCameraDropAmount => _slideCameraDropAmount;
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
    [SerializeField] float _dashSpeedReward = 0.75f;

    public float DashForce => _dashForce;
    public float DashDuration => _dashDuration;
    public float DashCooldown => _dashCooldown;
    public float DashSpeedReward => _dashSpeedReward;
    public float LastDashTime { get; set; } = -999f;
    bool _airDashConsumed;

    // =====================================================================
    //  LEDGE GRAB — Colgarse de bordes altos
    // =====================================================================
    [Header("Ledge Grab")]
    [SerializeField] float _ledgeCheckDistance = 1.0f;     // Distancia del raycast hacia adelante
    [SerializeField] float _ledgeGrabMaxHeight = 2.5f;     // Altura máxima para agarrarse
    [SerializeField] float _ledgeGrabMinHeight = 1.6f;     // Altura mínima (por debajo = vault)
    [SerializeField] float _ledgeClimbDuration = 0.4f;     // Duración de la animación de subir
    [SerializeField] float _ledgeJumpForce = 10f;          // Fuerza vertical propia del salto desde ledge
    [SerializeField] float _ledgeJumpHorizontalForce = 7f; // Impulso horizontal al separarse del borde
    [SerializeField] float _ledgeHangClearance = 0.12f;
    [SerializeField] float _ledgeClimbForwardOffset = 0.3f;
    [SerializeField] float _ledgeClimbLandingClearance = 0.12f;

    public float LedgeCheckDistance => _ledgeCheckDistance;
    public float LedgeGrabMaxHeight => _ledgeGrabMaxHeight;
    public float LedgeGrabMinHeight => _ledgeGrabMinHeight;
    public float LedgeClimbDuration => _ledgeClimbDuration;
    public float LedgeJumpForce => _ledgeJumpForce;
    public float LedgeJumpHorizontalForce => _ledgeJumpHorizontalForce;
    public Vector3 LedgeGrabPosition { get; set; }
    public Vector3 LedgeClimbTarget { get; set; }
    public Vector3 LedgeWallNormal { get; private set; }

    // =====================================================================
    //  LANDING ROLL — Voltereta al aterrizar de caída alta
    // =====================================================================
    [Header("Landing Roll")]
    [SerializeField] float _rollMinFallSpeed = 8f;         // Velocidad mínima de caída para activar roll
    [SerializeField] float _rollDuration = 0.5f;            // Duración del roll
    [SerializeField] float _rollInputBufferTime = 0.35f;    // Permite pulsar crouch poco antes de aterrizar
    [SerializeField] float _rollMinimumSpeed = 7.5f;        // Un roll desde caída vertical debe avanzar de verdad
    [SerializeField] float _rollSpeedBoost = 0.75f;         // Bonus aditivo, no multiplicador exponencial
    [SerializeField] float _hardLandingPenalty = 0.65f;     // Velocidad que conservas si NO haces roll
    [SerializeField] float _rollCameraAngle = 360f;          // Vuelta frontal completa durante el roll
    [SerializeField] float _rollCameraDropAmount = 0.35f;    // Acerca la cámara al suelo en mitad del roll

    public float RollMinFallSpeed => _rollMinFallSpeed;
    public float RollDuration => _rollDuration;
    public float RollMinimumSpeed => _rollMinimumSpeed;
    public float RollSpeedBoost => _rollSpeedBoost;
    public float HardLandingPenalty => _hardLandingPenalty;
    public float RollCameraAngle => _rollCameraAngle;
    public float RollCameraDropAmount => _rollCameraDropAmount;

    public bool LandingRollInputBuffered => controls != null
        && (controls.CrouchHeld
            || Time.unscaledTime - controls.LastCrouchPressedTime <= _rollInputBufferTime);

    // =====================================================================
    //  GRAPPLE — Gancho que impulsa al jugador hacia un punto
    // =====================================================================
    [Header("Grapple")]
    [SerializeField] float _grappleMaxDistance = 30f;      // Distancia máxima del gancho
    [SerializeField] float _grappleSpeed = 20f;             // Velocidad de viaje
    [SerializeField] float _grappleExitBoost = 1.5f;        // Multiplicador de velocidad al llegar
    [SerializeField] float _grappleStopDistance = 1.75f;
    [SerializeField] float _grappleAimAssistRadius = 0.35f;
    [SerializeField] float _grappleRefireDelay = 0.2f;
    [SerializeField] LayerMask _grappleMask;                // Capas donde se puede enganchar

    public float GrappleMaxDistance => _grappleMaxDistance;
    public float GrappleSpeed => _grappleSpeed;
    public float GrappleExitBoost => _grappleExitBoost;
    public float GrappleStopDistance => _grappleStopDistance;
    public LayerMask GrappleMask => _grappleMask;
    public Vector3 GrappleTarget { get; set; }
    public bool GrappleActive { get; private set; }
    public bool GrappleTargetAvailable { get; private set; }
    float _lastGrappleFireTime = -999f;
    LineRenderer _grappleRope;
    Material _grappleRopeMaterial;

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
    //  FLOW STYLE — Rango D/C/B/A/S/SS y límite de velocidad suave
    // =====================================================================
    [Header("Flow Style")]
    [SerializeField] bool _showStyleHud = true;
    [SerializeField] float _styleGraceTime = 1.6f;
    [SerializeField] float _styleDecayPerSecond = 95f;
    [SerializeField] float _styleMaxScore = 1000f;
    [SerializeField, Range(0f, 1f)] float _firstRepeatMultiplier = 0.35f;
    [SerializeField, Range(0f, 1f)] float _continuedRepeatMultiplier = 0.1f;

    [Header("Flow Speed Caps")]
    [SerializeField] float _speedCapD = 14f;
    [SerializeField] float _speedCapC = 15.5f;
    [SerializeField] float _speedCapB = 17f;
    [SerializeField] float _speedCapA = 19f;
    [SerializeField] float _speedCapS = 21f;
    [SerializeField] float _speedCapSS = 23f;

    public int MomentumChainCount { get; private set; }
    public float MomentumMultiplier => 1f;
    public float LastParkourActionTime { get; private set; } = -999f;
    public float StyleScore => _styleScore;
    public ParkourStyleRank StyleRank => GetStyleRank(_styleScore);
    public float StyleRankProgress => GetStyleRankProgress(_styleScore);
    public float StyleSpeedCap => GetStyleSpeedCap(StyleRank);

    float _styleScore;
    ParkourActionType _lastStyleAction = ParkourActionType.None;
    ParkourActionType _previousStyleAction = ParkourActionType.None;
    int _sameActionStreak;

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
    [SerializeField] float _wallRunReattachCooldown = 0.3f;
    [SerializeField, Range(0f, 1f)] float _wallRunMomentumRetention = 0.99f;
    [SerializeField] float _wallRunSpeedReward = 0.5f;
    [SerializeField] LayerMask _wallRunMask;                // Capas que se consideran paredes válidas para wall-run

    // Propiedades públicas para que PlayerWallRunState acceda a los parámetros
    public float WallRunSpeed => _wallRunSpeed;
    public float WallRunMaxDuration => _wallRunMaxDuration;
    public float WallRunGravityScale => _wallRunGravityScale;
    public float WallRunExitForce => _wallRunExitForce;
    public float WallRunExitUpForce => _wallRunExitUpForce;
    public float WallRunMomentumRetention => _wallRunMomentumRetention;
    public float WallRunSpeedReward => _wallRunSpeedReward;
    public float GravityForce => _gravityForce;
    public CharacterController CC => cc;
    float _lastWallRunExitTime = -999f;

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
    private float _cameraRollPitch;
    private float _currentCameraHeightOffset;
    private float _targetCameraHeightOffset;
    private float _cameraHeightTransitionSpeed = 10f;
    private Quaternion _cameraBaseLocalRotation = Quaternion.identity;
    private Vector3 _cameraBaseLocalPosition;
    private bool _cameraPoseInitialized;


    //----- START PLAYER GRAVITY

    [ExcelPort]
    [SerializeField] float _gravityForce = 9.81f;
    [SerializeField] float _groundStickSpeed = 2f;
    [SerializeField] float _terminalFallSpeed = 45f;
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
    [SerializeField, Range(0f, 1f)] float _vaultMomentumRetention = 0.98f;
    [SerializeField] float _vaultSpeedReward = 0.6f;
    public float VaultMomentumRetention => _vaultMomentumRetention;
    public float VaultSpeedReward => _vaultSpeedReward;

    public Vector3 VaultTargetPosition { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    [Header("Jump")]
    [ExcelPort]
    [SerializeField] float _jumpForce = 8f;
    [SerializeField, Range(0.1f, 1f)] float _jumpCutMultiplier = 0.5f;
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
        _groundProbeDistance = Mathf.Max(0.05f, _groundProbeDistance);
        _terminalFallSpeed = Mathf.Max(_gravityForce, _terminalFallSpeed);
        controls = GetComponent<InputReader>();
        if (cc == null) cc = GetComponent<CharacterController>();
        ConfigureDefaultLayers();
    }

    private void Awake()
    {
        controls = GetComponent<InputReader>();
        if (cc == null) cc = GetComponent<CharacterController>();
        ConfigureDefaultLayers();
    }

    private void Start()
    {
        if (controls == null)
            Debug.LogError("PlayerStateMachine necesita un InputReader en el mismo GameObject.");
        if (cc == null)
        {
            Debug.LogError("PlayerStateMachine necesita un CharacterController.");
            enabled = false;
            return;
        }

        // Guardar dimensiones originales del CharacterController para restaurar tras slide
        _originalCCHeight = cc.height;
        _originalCCCenter = cc.center;

        if (_cameraPanTilt == null)
            _cameraPanTilt = GetComponentInChildren<CinemachinePanTilt>(true);
        if (_cameraTiltTarget == null && _cameraPanTilt != null)
            _cameraTiltTarget = _cameraPanTilt.transform.parent;
        InitializeCameraPose();
        _activeCinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        InitializeGrappleRope();

        if (_showStyleHud)
        {
            ParkourStyleHUD hud = GetComponent<ParkourStyleHUD>();
            if (hud == null) hud = gameObject.AddComponent<ParkourStyleHUD>();
            hud.Bind(this);
        }

        if (_enableParkourAudioFeedback)
        {
            ParkourAudioManager audioFeedback = GetComponent<ParkourAudioManager>();
            if (audioFeedback == null)
                audioFeedback = gameObject.AddComponent<ParkourAudioManager>();
            audioFeedback.Bind(this);
        }

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

        if (controls != null)
            controls.OnGrapplePressed += HandleGrapplePressed;
        
        SwitchState(typeof(PlayerIdleState));
    }

    private void OnDestroy()
    {
        if (controls != null)
            controls.OnGrapplePressed -= HandleGrapplePressed;
        if (_grappleRopeMaterial != null)
            Destroy(_grappleRopeMaterial);
    }

    // =====================================================================
    //  LATE UPDATE — Aplica el tilt de cámara cada frame después del render
    // =====================================================================
    private void LateUpdate()
    {
        UpdateCameraTilt();
        TickMomentum();
        UpdateDynamicFov();
        UpdateGrappleAim();
        UpdateGrappleRope();
    }

    private void ConfigureDefaultLayers()
    {
        if (_grappleMask.value != 0) return;

        int grappleLayer = LayerMask.NameToLayer("Grapple");
        if (grappleLayer >= 0)
            _grappleMask = 1 << grappleLayer;
    }

    private bool TryGetGrappleHit(out RaycastHit hit)
    {
        Transform aimTransform = Camera.main != null ? Camera.main.transform : _cameraTiltTarget;
        if (aimTransform == null)
        {
            hit = default;
            return false;
        }

        Ray aimRay = new Ray(aimTransform.position, aimTransform.forward);
        return Physics.SphereCast(
            aimRay,
            _grappleAimAssistRadius,
            out hit,
            _grappleMaxDistance,
            _grappleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleGrapplePressed()
    {
        if (GetCurrentState() is PlayerGrappleState)
        {
            SwitchState(typeof(PlayerIdleState));
            return;
        }

        if (Time.unscaledTime - _lastGrappleFireTime < _grappleRefireDelay)
            return;

        if (!TryGetGrappleHit(out RaycastHit hit))
            return;

        GrappleTarget = hit.point;
        _lastGrappleFireTime = Time.unscaledTime;
        SwitchState(typeof(PlayerGrappleState));
    }

    public void SetGrappleActive(bool active)
    {
        GrappleActive = active;
        if (_grappleRope != null)
            _grappleRope.enabled = active;
    }

    private void InitializeGrappleRope()
    {
        if (_grappleRope != null) return;

        GameObject ropeObject = new GameObject("Grapple Rope");
        ropeObject.transform.SetParent(transform, false);
        _grappleRope = ropeObject.AddComponent<LineRenderer>();
        _grappleRope.useWorldSpace = true;
        _grappleRope.positionCount = 3;
        _grappleRope.startWidth = 0.045f;
        _grappleRope.endWidth = 0.018f;
        _grappleRope.numCornerVertices = 5;
        _grappleRope.numCapVertices = 4;

        Shader ropeShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (ropeShader == null) ropeShader = Shader.Find("Sprites/Default");
        if (ropeShader != null)
        {
            _grappleRopeMaterial = new Material(ropeShader);
            Color ropeColor = new Color(0.15f, 1f, 0.85f, 1f);
            _grappleRopeMaterial.color = ropeColor;
            if (_grappleRopeMaterial.HasProperty("_BaseColor"))
                _grappleRopeMaterial.SetColor("_BaseColor", ropeColor);
            _grappleRope.material = _grappleRopeMaterial;
        }

        _grappleRope.enabled = false;
    }

    private void UpdateGrappleAim()
    {
        GrappleTargetAvailable = GrappleActive || TryGetGrappleHit(out _);
    }

    private void UpdateGrappleRope()
    {
        if (!GrappleActive || _grappleRope == null) return;

        Vector3 start = _cameraTiltTarget != null
            ? _cameraTiltTarget.position
            : transform.position + Vector3.up * 0.7f;
        Vector3 end = GrappleTarget;
        float distance = Vector3.Distance(start, end);
        Vector3 middle = Vector3.Lerp(start, end, 0.5f)
            + Vector3.down * Mathf.Min(0.45f, distance * 0.025f);

        _grappleRope.SetPosition(0, start);
        _grappleRope.SetPosition(1, middle);
        _grappleRope.SetPosition(2, end);
    }

    private void UpdateDynamicFov()
    {
        if (!_dynamicFov || _activeCinemachineCamera == null) return;

        float speedRatio = Mathf.Clamp01(CurrentHorizontalVelocity.magnitude / _fovSpeedReference);
        float targetFov = Mathf.Lerp(_baseFov, _maxFov, speedRatio);
        var lens = _activeCinemachineCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, _fovLerpSpeed * Time.unscaledDeltaTime);
        _activeCinemachineCamera.Lens = lens;
    }


    public void GroundDetection()
    {
        // No permitir que una esfera grande vuelva a pegarnos al suelo durante el ascenso.
        if (Vector3.Dot(currentForceGravity, -_downDir) > 0.1f)
        {
            _grounded = false;
            _groundNormal = -_downDir;
            _sliding = false;
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(cc.center);
        float scaledHeight = cc.height * Mathf.Abs(transform.lossyScale.y);
        float scaledRadius = cc.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.z)
        );
        float radius = scaledRadius * _groundProbeRadiusScale;
        float bottomOffset = Mathf.Max(0f, scaledHeight * 0.5f - scaledRadius);
        Vector3 bottomSphereCenter = worldCenter + _downDir * bottomOffset;
        Vector3 probeOrigin = bottomSphereCenter - _downDir * 0.03f;

        bool hasGround = Physics.SphereCast(
            probeOrigin,
            radius,
            _downDir,
            out RaycastHit hitInfo,
            _groundProbeDistance,
            _groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hasGround)
        {
            float groundAngle = Vector3.Angle(hitInfo.normal, -_downDir);
            if (groundAngle <= _maxAngle)
            {
                _groundNormal = hitInfo.normal;
                _grounded = true;
                _sliding = false;
                _slideNormal = hitInfo.normal;
                _airDashConsumed = false;
            }
            else
            {
                _grounded = false;
                _sliding = true;
                _slideNormal = hitInfo.normal;
                _groundNormal = -_downDir;
            }
        }
        else
        {
            _grounded = false;
            _groundNormal = -_downDir;
            _sliding = false;
        }
    }

    public Vector2 GetInput()// TODO Replace with new input system
    {
        return controls != null ? Vector2.ClampMagnitude(controls.MovementValue, 1f) : Vector2.zero;
    }

    public void PlayerHorizontalMovement(Vector3 inputDir)
    {
        Vector3 currentVelocity = new Vector3(
            _currentHorizontalVelocity.x,
            0f,
            _currentHorizontalVelocity.z
        );
        float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);
        Vector3 wishDirection = inputMagnitude > 0.01f
            ? new Vector3(inputDir.x, 0f, inputDir.z).normalized
            : Vector3.zero;

        float accel = Grounded ? _acceleration : _airAcceleration;
        
        // Decidimos la fuerza de deceleración (fricción leve vs freno fuerte)
        float currentDecelValue;
        if (Grounded)
        {
            if (wishDirection.sqrMagnitude < 0.01f)
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
            if (wishDirection.sqrMagnitude > 0.01f && Grounded)
            {
                float dot = Vector3.Dot(currentVelocity.normalized, wishDirection);
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
        if (wishDirection.sqrMagnitude > 0.01f)
        {
            float currentSpeedInInputDirection = Vector3.Dot(currentVelocity, wishDirection);
            float desiredSpeed = _targetVelocity * inputMagnitude;
            float maximumLocomotionSpeed = Mathf.Max(currentVelocity.magnitude, desiredSpeed);
            
            // La carrera normal acelera hasta targetVelocity. El exceso ganado
            // mediante parkour se conserva y solo se desgasta con la fricción.
            float addSpeed = desiredSpeed - currentSpeedInInputDirection;

            if (addSpeed > 0)
            {
                // Aceleramos, pero sin pasarnos de 'addSpeed'
                float accelSpeed = accel * Time.deltaTime;
                if (accelSpeed > addSpeed) accelSpeed = addSpeed;

                currentVelocity += wishDirection * accelSpeed;

                // El input puede redirigir el momentum, pero no fabricarlo al
                // hacer strafe. Solo las acciones de parkour aumentan el techo.
                if (currentVelocity.magnitude > maximumLocomotionSpeed)
                    currentVelocity = currentVelocity.normalized * maximumLocomotionSpeed;
            }
        }

        currentVelocity.y = 0f;
        _currentHorizontalVelocity = currentVelocity;

        // El momentum se guarda siempre en el plano XZ. Solo el desplazamiento
        // del frame se adapta a la pendiente para no mezclar gravedad y velocidad.
        Vector3 movementVelocity = currentVelocity;
        if (Grounded && currentVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(currentVelocity.normalized, GroundNormal);
            if (slopeDirection.sqrMagnitude > 0.001f)
                movementVelocity = slopeDirection.normalized * currentVelocity.magnitude;
        }

        Vector3 posBeforeMove = transform.position;
        cc.Move(movementVelocity * Time.deltaTime);

        if (movementVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 actualDisplacement = transform.position - posBeforeMove;
            actualDisplacement.y = 0f;
            Vector3 intendedDisplacement = movementVelocity * Time.deltaTime;
            intendedDisplacement.y = 0f;

            if (intendedDisplacement.sqrMagnitude > 0.0001f)
            {
                float movementRatio = actualDisplacement.magnitude / intendedDisplacement.magnitude;
                if (movementRatio < 0.8f)
                {
                    // Conserva la componente tangencial al rozar una pared,
                    // pero elimina el momentum fantasma que empuja contra ella.
                    Vector3 actualVelocity = actualDisplacement / Mathf.Max(Time.deltaTime, 0.0001f);
                    if (actualVelocity.magnitude < _currentHorizontalVelocity.magnitude)
                        _currentHorizontalVelocity = actualVelocity;
                }
            }
        }
    }

    /// <summary>
    /// Convierte el input 2D en una dirección 3D basada en la rotación de la cámara.
    /// </summary>
    public Vector3 GetWorldMovementDirection(Vector2 input)
    {
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        
        // Obtener forward y right de la cámara, ignorando la inclinación arriba/abajo
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        Vector3 worldDirection = camRight * input.x + camForward * input.y;
        return worldDirection.sqrMagnitude > 1f ? worldDirection.normalized : worldDirection;
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

        // Soltar pronto produce un salto corto; mantener conserva el arco completo.
        float upwardSpeed = Vector3.Dot(currentForceGravity, -_gravityDir);
        if (upwardSpeed > 0f)
            currentForceGravity *= _jumpCutMultiplier;
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
        ExecuteJump(_jumpForce);
    }

    public void ExecuteJump(float upwardForce)
    {
        SetVerticalVelocity(upwardForce);
        cc.Move(currentForceGravity * Time.deltaTime);
        JumpBufferCounter = 0f;
        CoyoteTimeCounter = 0f;
        JumpPressed = false; // Consume the initial jump press to prevent infinite jumps
    }

    /// <summary>
    /// Limpia el input al salir del vault sin borrar el momentum ganado.
    /// </summary>
    public void ResetVaultPhysics()
    {
        JumpPressed = false;
        JumpHeld = false;
        JumpBufferCounter = 0f;
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
        float dt = Time.deltaTime;

        if (Grounded)
        {
            // Una velocidad pequeña mantiene contacto sin intentar penetrar
            // el suelo a 21 m/s como hacía la implementación anterior.
            if (Vector3.Dot(currentForceGravity, -_gravityDir) <= 0f)
                currentForceGravity = _gravityDir * _groundStickSpeed;
        }
        else
        {
            currentForceGravity += _gravityDir * _gravityForce * dt;
            float fallSpeed = Vector3.Dot(currentForceGravity, _gravityDir);
            if (fallSpeed > _terminalFallSpeed)
                currentForceGravity += -_gravityDir * (fallSpeed - _terminalFallSpeed);
        }

        cc.Move(currentForceGravity * dt);
    }

    public void SetVerticalVelocity(float upwardSpeed)
    {
        currentForceGravity = -_gravityDir * upwardSpeed;
        _grounded = false;
    }

    public void ResetVerticalVelocity()
    {
        currentForceGravity = Vector3.zero;
    }

    public Vector3 GetVerticalVelocity()
    {
        return currentForceGravity;
    }


    public void PlayerLook()
    {
        if (controls == null) return;

        Vector2 look = controls.LookValue;
        float yawDelta;
        float pitchDelta;

        if (controls.LookComesFromPointer)
        {
            yawDelta = look.x * _mouseLookSensitivity;
            pitchDelta = -look.y * _mouseLookSensitivity;
        }
        else
        {
            yawDelta = look.x * _gamepadLookSpeed * Time.unscaledDeltaTime;
            pitchDelta = -look.y * _gamepadLookSpeed * Time.unscaledDeltaTime;
        }

        if (Mathf.Abs(yawDelta) > 0.0001f)
            transform.Rotate(0f, yawDelta, 0f);

        if (_cameraPanTilt != null && Mathf.Abs(pitchDelta) > 0.0001f)
        {
            _cameraPanTilt.TiltAxis.Value = Mathf.Clamp(
                _cameraPanTilt.TiltAxis.Value + pitchDelta,
                _cameraPanTilt.TiltAxis.Range.x,
                _cameraPanTilt.TiltAxis.Range.y
            );
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
        if (Grounded) return false;
        if (Time.unscaledTime - _lastWallRunExitTime < _wallRunReattachCooldown) return false;

        Vector3 horizontalVel = new Vector3(_currentHorizontalVelocity.x, 0f, _currentHorizontalVelocity.z);
        if (horizontalVel.magnitude < _wallRunMinSpeed) return false;

        Vector2 input = GetInput();
        if (input.sqrMagnitude < 0.1f) return false;

        bool hasWall = CheckWallRight(out RaycastHit hit) || CheckWallLeft(out hit);
        if (!hasWall) return false;

        Vector3 wallTangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
        float velocityAlignment = Mathf.Abs(Vector3.Dot(horizontalVel.normalized, wallTangent));
        Vector3 inputDirection = GetWorldMovementDirection(input).normalized;
        float inputAlignment = Mathf.Abs(Vector3.Dot(inputDirection, wallTangent));

        return velocityAlignment >= 0.3f && inputAlignment >= 0.2f;
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

    public void SetCameraRollPitch(float pitchAngle)
    {
        _cameraRollPitch = pitchAngle;
    }

    public void SetCameraHeightOffset(float heightOffset, float transitionSpeed)
    {
        _targetCameraHeightOffset = heightOffset;
        _cameraHeightTransitionSpeed = Mathf.Max(0.01f, transitionSpeed);
    }

    private void InitializeCameraPose()
    {
        if (_cameraTiltTarget == null) return;

        _cameraBaseLocalRotation = _cameraTiltTarget.localRotation;
        _cameraBaseLocalPosition = _cameraTiltTarget.localPosition;
        _cameraPoseInitialized = true;
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

        if (_cameraTiltTarget != null)
        {
            if (!_cameraPoseInitialized)
                InitializeCameraPose();

            _currentCameraHeightOffset = Mathf.MoveTowards(
                _currentCameraHeightOffset,
                _targetCameraHeightOffset,
                _cameraHeightTransitionSpeed * Time.deltaTime
            );

            _cameraTiltTarget.localPosition = _cameraBaseLocalPosition
                + Vector3.up * _currentCameraHeightOffset;
            _cameraTiltTarget.localRotation = _cameraBaseLocalRotation
                * Quaternion.Euler(_cameraRollPitch, 0f, _currentCameraTilt);
        }
    }

    /// <summary>
    /// Aplica un impulso de salida del wall-run al CharacterController.
    /// Combina fuerza lateral (alejándose de la pared) + fuerza vertical (hacia arriba).
    /// Se usa desde PlayerWallRunState al salir por salto.
    /// </summary>
    /// <param name="wallNormal">Normal de la pared (apunta hacia fuera de la pared)</param>
    public void ApplyWallRunExitImpulse(Vector3 wallNormal, Vector3 wallTangent, float alongWallSpeed)
    {
        Vector3 flatNormal = Vector3.ProjectOnPlane(wallNormal, Vector3.up).normalized;
        Vector3 flatTangent = Vector3.ProjectOnPlane(wallTangent, Vector3.up).normalized;
        Vector3 exitHorizontalVelocity = flatTangent * alongWallSpeed
                                       + flatNormal * _wallRunExitForce;

        CurrentHorizontalVelocity = exitHorizontalVelocity;
        SetVerticalVelocity(_wallRunExitUpForce);
        cc.Move((exitHorizontalVelocity + currentForceGravity) * Time.deltaTime);
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
        float minimumHeight = cc.radius * 2f + 0.05f;
        float reducedHeight = Mathf.Max(minimumHeight, _originalCCHeight - _slideHeightReduction);
        float appliedReduction = _originalCCHeight - reducedHeight;

        cc.height = reducedHeight;
        cc.center = _originalCCCenter + Vector3.down * (appliedReduction * 0.5f);
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
    /// Registra una acción real. Variar movimientos puntúa mucho más que repetir
    /// el mismo; el estilo nunca modifica Time.timeScale.
    /// </summary>
    public void RegisterParkourAction(ParkourActionType action, float quality = 1f)
    {
        quality = Mathf.Clamp(quality, 0.25f, 1.5f);
        float now = Time.unscaledTime;

        if (now - LastParkourActionTime <= _styleGraceTime)
            MomentumChainCount++;
        else
            MomentumChainCount = 1;

        float varietyMultiplier = 1f;
        if (action == _lastStyleAction)
        {
            _sameActionStreak++;
            varietyMultiplier = _sameActionStreak == 1
                ? _firstRepeatMultiplier
                : _continuedRepeatMultiplier;
        }
        else
        {
            _sameActionStreak = 0;
            if (action == _previousStyleAction)
                varietyMultiplier = 0.65f;
        }

        float speedBonus = Mathf.InverseLerp(_targetVelocity, _speedCapSS, CurrentHorizontalVelocity.magnitude);
        float points = GetActionStylePoints(action) * varietyMultiplier * quality;
        points *= 1f + speedBonus * 0.25f;

        _styleScore = Mathf.Clamp(_styleScore + points, 0f, _styleMaxScore);
        _lastVarietyMultiplier = varietyMultiplier;
        _previousStyleAction = _lastStyleAction;
        _lastStyleAction = action;
        LastParkourActionTime = now;
    }

    public void TickMomentum()
    {
        if (Time.unscaledTime - LastParkourActionTime <= _styleGraceTime)
            return;

        _styleScore = Mathf.MoveTowards(
            _styleScore,
            0f,
            _styleDecayPerSecond * Time.unscaledDeltaTime
        );

        if (_styleScore <= 0.01f)
        {
            _styleScore = 0f;
            MomentumChainCount = 0;
            _lastStyleAction = ParkourActionType.None;
            _previousStyleAction = ParkourActionType.None;
            _sameActionStreak = 0;
        }
    }

    public float ResolveActionSpeed(
        float incomingSpeed,
        float minimumSpeed,
        float momentumRetention,
        float earnedBoost
    )
    {
        float retainedSpeed = Mathf.Max(incomingSpeed * momentumRetention, minimumSpeed);
        float rankBoost = 1f + (int)StyleRank * 0.08f;
        float boost = earnedBoost * rankBoost * _lastVarietyMultiplier;

        // El límite es suave: si ya veníamos por encima no se recorta de golpe.
        float maximumAfterBoost = Mathf.Max(retainedSpeed, StyleSpeedCap);
        return Mathf.Min(retainedSpeed + boost, maximumAfterBoost);
    }

    public bool CanDash()
    {
        if (Time.unscaledTime - LastDashTime < _dashCooldown) return false;
        return Grounded || !_airDashConsumed;
    }

    public void NotifyDashStarted()
    {
        LastDashTime = Time.unscaledTime;
        if (!Grounded) _airDashConsumed = true;
    }

    public void NotifyWallRunEnded()
    {
        _lastWallRunExitTime = Time.unscaledTime;
    }

    private float _lastVarietyMultiplier = 1f;

    private static float GetActionStylePoints(ParkourActionType action)
    {
        return action switch
        {
            ParkourActionType.Dash => 65f,
            ParkourActionType.Slide => 75f,
            ParkourActionType.Vault => 115f,
            ParkourActionType.WallRun => 135f,
            ParkourActionType.LandingRoll => 150f,
            ParkourActionType.LedgeGrab => 80f,
            ParkourActionType.Grapple => 175f,
            ParkourActionType.Zipline => 125f,
            _ => 0f
        };
    }

    private static ParkourStyleRank GetStyleRank(float score)
    {
        if (score >= 900f) return ParkourStyleRank.SS;
        if (score >= 700f) return ParkourStyleRank.S;
        if (score >= 450f) return ParkourStyleRank.A;
        if (score >= 250f) return ParkourStyleRank.B;
        if (score >= 100f) return ParkourStyleRank.C;
        return ParkourStyleRank.D;
    }

    private static float GetStyleRankProgress(float score)
    {
        if (score >= 900f) return Mathf.InverseLerp(900f, 1000f, score);
        if (score >= 700f) return Mathf.InverseLerp(700f, 900f, score);
        if (score >= 450f) return Mathf.InverseLerp(450f, 700f, score);
        if (score >= 250f) return Mathf.InverseLerp(250f, 450f, score);
        if (score >= 100f) return Mathf.InverseLerp(100f, 250f, score);
        return Mathf.InverseLerp(0f, 100f, score);
    }

    private float GetStyleSpeedCap(ParkourStyleRank rank)
    {
        return rank switch
        {
            ParkourStyleRank.C => _speedCapC,
            ParkourStyleRank.B => _speedCapB,
            ParkourStyleRank.A => _speedCapA,
            ParkourStyleRank.S => _speedCapS,
            ParkourStyleRank.SS => _speedCapSS,
            _ => _speedCapD
        };
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
        Vector3 horizontalVelocity = CurrentHorizontalVelocity;
        Vector3 inputDirection = GetWorldMovementDirection(GetInput());

        // No agarrarse automáticamente mientras nos alejamos o pasamos de lado.
        bool movingTowardLedge = horizontalVelocity.magnitude > 0.5f
            && Vector3.Dot(horizontalVelocity.normalized, forward) > 0.15f;
        bool pressingTowardLedge = inputDirection.magnitude > 0.1f
            && Vector3.Dot(inputDirection.normalized, forward) > 0.25f;
        if (!movingTowardLedge && !pressingTowardLedge) return false;

        float upwardSpeed = Vector3.Dot(currentForceGravity, -_gravityDir);
        if (upwardSpeed > 3f) return false;
        
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
        
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float scaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float halfHeight = cc.height * scaleY * 0.5f;
        float centerHeight = cc.center.y * scaleY;
        float radius = cc.radius * scaleXZ;
        float skin = cc.skinWidth * scaleXZ;

        LedgeWallNormal = wallHit.normal.normalized;

        // Posición de cuelgue: la parte superior de la cápsula queda justo
        // debajo del borde y todo su radio permanece fuera de la pared.
        LedgeGrabPosition = wallHit.point
            + LedgeWallNormal * (radius + skin + 0.03f);
        LedgeGrabPosition = new Vector3(
            LedgeGrabPosition.x,
            topHit.point.y - halfHeight - centerHeight - _ledgeHangClearance,
            LedgeGrabPosition.z
        );

        // Posición final: la base de la cápsula queda encima de la superficie,
        // no dentro de ella como ocurría con topHit.point + 0.1f.
        LedgeClimbTarget = topHit.point
            - LedgeWallNormal * (radius + _ledgeClimbForwardOffset);
        LedgeClimbTarget = new Vector3(
            LedgeClimbTarget.x,
            topHit.point.y + halfHeight - centerHeight + skin + _ledgeClimbLandingClearance,
            LedgeClimbTarget.z
        );
        return true;
    }
}
