using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestiona todos los efectos de cámara del parkour:
/// FOV dinámico, head bob, landing dip, sprint lean, camera shake.
/// Se coloca en la cámara o en un pivote hijo de la cámara.
/// Lee la velocidad del PlayerStateMachine para calcular la intensidad de cada efecto.
/// </summary>
public class PlayerCameraEffects : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] PlayerStateMachine playerSM;           // Referencia al jugador
    [SerializeField] CinemachineCamera cinemachineCamera;   // Referencia a la cámara Cinemachine (para FOV)
    
    // =====================================================================
    //  FOV DINÁMICO — El campo de visión aumenta con la velocidad
    // =====================================================================
    [Header("FOV Dinámico")]
    [SerializeField] float _baseFOV = 75f;              // FOV base en reposo
    [SerializeField] float _maxFOV = 100f;              // FOV máximo a velocidad extrema
    [SerializeField] float _fovSpeedReference = 20f;    // Velocidad a la que se alcanza el FOV máximo
    [SerializeField] float _fovLerpSpeed = 5f;          // Suavizado del cambio de FOV
    
    // =====================================================================
    //  HEAD BOB — Oscilación de cámara al correr
    // =====================================================================
    [Header("Head Bob")]
    [SerializeField] float _bobFrequency = 8f;          // Frecuencia de oscilación (ciclos por segundo)
    [SerializeField] float _bobAmplitudeY = 0.03f;      // Amplitud vertical del bobbing
    [SerializeField] float _bobAmplitudeX = 0.02f;      // Amplitud horizontal del bobbing
    [SerializeField] float _bobSpeedMultiplier = 1.5f;   // Cuánto se acelera el bob con la velocidad
    
    // =====================================================================
    //  LANDING DIP — La cámara baja al aterrizar (absorción de rodillas)
    // =====================================================================
    [Header("Landing Dip")]
    [SerializeField] float _landingDipAmount = 0.15f;    // Cuánto baja la cámara al aterrizar
    [SerializeField] float _landingDipRecoverySpeed = 8f; // Velocidad de recuperación
    [SerializeField] float _landingDipMinFallSpeed = 3f;  // Velocidad mínima de caída para activar dip
    
    // =====================================================================
    //  SPRINT LEAN — Inclinación Z al girar mientras sprints
    // =====================================================================
    [Header("Sprint Lean")]
    [SerializeField] float _sprintLeanAngle = 3f;        // Ángulo Z máximo al girar sprintando
    [SerializeField] float _sprintLeanSpeed = 4f;        // Velocidad del lerp
    
    // =====================================================================
    //  CAMERA SHAKE — Temblor por impactos, aterrizajes e impulsos
    // =====================================================================
    [Header("Camera Shake")]
    [SerializeField] float _landingShakeIntensity = 0.1f;  // Intensidad del shake al aterrizar
    [SerializeField] float _wallHitShakeIntensity = 0.05f; // Shake al chocar con pared
    [SerializeField] float _impulseShakeIntensity = 0.08f; // Shake al recibir impulso (wall-run exit)
    [SerializeField] float _shakeDuration = 0.2f;          // Duración del shake
    [SerializeField] float _shakeFrequency = 25f;          // Frecuencia de vibración del shake
    
    // ---- Estado interno ----
    private float _currentFOV;
    private float _bobTimer;
    private Vector3 _originalLocalPos;      // Posición local original del transform
    private float _landingDipOffset;        // Offset Y actual del landing dip
    private float _currentShakeIntensity;
    private float _shakeTimer;
    private bool _wasGroundedLastFrame;
    private float _lastVerticalSpeed;       // Para detectar aterrizajes
    private float _currentLean;             // Sprint lean actual
    private float _lastYaw;                 // Para calcular velocidad angular

    private void Start()
    {
        _originalLocalPos = transform.localPosition;
        _currentFOV = _baseFOV;
        if (cinemachineCamera != null)
            _currentFOV = cinemachineCamera.Lens.FieldOfView;
        _wasGroundedLastFrame = true;
    }

    private void LateUpdate()
    {
        if (playerSM == null) return;
        
        float speed = playerSM.CurrentHorizontalVelocity.magnitude;
        float dt = Time.deltaTime;
        
        UpdateFOV(speed, dt);
        UpdateHeadBob(speed, dt);
        UpdateLandingDip(dt);
        UpdateSprintLean(speed, dt);
        UpdateShake(dt);
        ApplyOffsets();
        
        // Track estado del frame anterior
        _wasGroundedLastFrame = playerSM.Grounded;
        _lastVerticalSpeed = playerSM.GetVerticalSpeed();
        _lastYaw = playerSM.transform.eulerAngles.y;
    }

    // ==================== FOV DINÁMICO ====================
    private void UpdateFOV(float speed, float dt)
    {
        float speedRatio = Mathf.Clamp01(speed / _fovSpeedReference);
        float targetFOV = Mathf.Lerp(_baseFOV, _maxFOV, speedRatio);
        _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, _fovLerpSpeed * dt);
        
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.FieldOfView = _currentFOV;
        }
    }

    // ==================== HEAD BOB ====================
    private void UpdateHeadBob(float speed, float dt)
    {
        if (playerSM.Grounded && speed > 0.5f)
        {
            // La frecuencia del bob aumenta con la velocidad para simular pasos más rápidos
            float adjustedFrequency = _bobFrequency + (speed * _bobSpeedMultiplier);
            _bobTimer += dt * adjustedFrequency;
        }
        else
        {
            // Cuando paramos o estamos en el aire, el bob se desvanece suavemente
            _bobTimer = Mathf.Lerp(_bobTimer, 0f, dt * 5f);
        }
    }

    // ==================== LANDING DIP ====================
    private void UpdateLandingDip(float dt)
    {
        // Detectar aterrizaje: no estaba grounded y ahora sí
        if (playerSM.Grounded && !_wasGroundedLastFrame)
        {
            if (_lastVerticalSpeed > _landingDipMinFallSpeed)
            {
                float intensity = Mathf.Clamp01(_lastVerticalSpeed / 20f);
                _landingDipOffset = -_landingDipAmount * intensity;
                
                // Shake proporcional a la caída
                TriggerShake(_landingShakeIntensity * intensity);
            }
        }
        
        // Recuperar suavemente del dip (las rodillas se enderezan)
        _landingDipOffset = Mathf.Lerp(_landingDipOffset, 0f, _landingDipRecoverySpeed * dt);
    }

    // ==================== SPRINT LEAN ====================
    private void UpdateSprintLean(float speed, float dt)
    {
        float currentYaw = playerSM.transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(_lastYaw, currentYaw);
        
        float targetLean = 0f;
        if (speed > 3f)
        {
            // Girando a la derecha → lean negativo (interior de la curva, como una moto)
            targetLean = -yawDelta * _sprintLeanAngle * Mathf.Clamp01(speed / 10f);
            targetLean = Mathf.Clamp(targetLean, -_sprintLeanAngle * 2f, _sprintLeanAngle * 2f);
        }
        
        _currentLean = Mathf.Lerp(_currentLean, targetLean, _sprintLeanSpeed * dt);
    }

    // ==================== SHAKE ====================
    private void UpdateShake(float dt)
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            _currentShakeIntensity = Mathf.Lerp(0f, _currentShakeIntensity, _shakeTimer / _shakeDuration);
        }
        else
        {
            _currentShakeIntensity = 0f;
        }
    }

    // ==================== APLICAR TODOS LOS OFFSETS ====================
    private void ApplyOffsets()
    {
        Vector3 offset = _originalLocalPos;
        
        // Head bob
        float bobY = Mathf.Sin(_bobTimer) * _bobAmplitudeY;
        float bobX = Mathf.Sin(_bobTimer * 0.5f) * _bobAmplitudeX;
        offset += new Vector3(bobX, bobY, 0f);
        
        // Landing dip
        offset.y += _landingDipOffset;
        
        // Shake (ruido Perlin para vibración orgánica)
        if (_currentShakeIntensity > 0)
        {
            float shakeX = Mathf.PerlinNoise(Time.time * _shakeFrequency, 0f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0f, Time.time * _shakeFrequency) * 2f - 1f;
            offset += new Vector3(shakeX, shakeY, 0f) * _currentShakeIntensity;
        }
        
        transform.localPosition = offset;
        
        // Sprint lean (rotación Z, se suma al tilt de wall-run que gestiona PlayerStateMachine)
        Vector3 euler = transform.localEulerAngles;
        euler.z += _currentLean;
        transform.localEulerAngles = euler;
    }

    // ==================== API PÚBLICA ====================

    /// <summary>Dispara un camera shake con la intensidad especificada.</summary>
    public void TriggerShake(float intensity)
    {
        _currentShakeIntensity = intensity;
        _shakeTimer = _shakeDuration;
    }

    /// <summary>Shake de aterrizaje (intensidad proporcional a la velocidad de caída).</summary>
    public void TriggerLandingShake(float fallSpeed)
    {
        float intensity = _landingShakeIntensity * Mathf.Clamp01(fallSpeed / 15f);
        TriggerShake(intensity);
    }

    /// <summary>Shake al chocar contra una pared.</summary>
    public void TriggerWallHitShake()
    {
        TriggerShake(_wallHitShakeIntensity);
    }

    /// <summary>Shake al recibir un impulso (wall-run exit, grapple arrive).</summary>
    public void TriggerImpulseShake()
    {
        TriggerShake(_impulseShakeIntensity);
    }
}
