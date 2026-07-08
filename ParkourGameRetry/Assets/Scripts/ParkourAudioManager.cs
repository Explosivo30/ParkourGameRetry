using UnityEngine;

/// <summary>
/// Gestiona todos los efectos de audio del parkour:
/// Sonido de viento progresivo, pisadas dinámicas, grunts de esfuerzo,
/// y feedback sonoro al encadenar movimientos.
/// Colocar en el GameObject del jugador.
/// </summary>
public class ParkourAudioManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] PlayerStateMachine playerSM;

    // =====================================================================
    //  VIENTO — Sonido que sube con la velocidad
    // =====================================================================
    [Header("Viento")]
    [SerializeField] AudioSource windSource;              // AudioSource dedicado al loop de viento
    [SerializeField] AudioClip windLoop;                   // Clip de viento en loop
    [SerializeField] float _windMinSpeed = 5f;             // Velocidad a la que empieza a sonar
    [SerializeField] float _windMaxSpeed = 25f;            // Velocidad a la que suena al máximo
    [SerializeField] float _windMaxVolume = 0.6f;          // Volumen máximo del viento
    [SerializeField] float _windMaxPitch = 1.5f;           // Pitch máximo del viento
    [SerializeField] float _windLerpSpeed = 3f;            // Suavizado del cambio

    // =====================================================================
    //  PISADAS — Ritmo que se acelera con la velocidad
    // =====================================================================
    [Header("Pisadas")]
    [SerializeField] AudioSource footstepSource;           // AudioSource para pisadas
    [SerializeField] AudioClip[] footstepClips;            // Array de clips de pisadas (variación)
    [SerializeField] float _footstepBaseInterval = 0.5f;   // Intervalo base entre pisadas (velocidad baja)
    [SerializeField] float _footstepMinInterval = 0.2f;    // Intervalo mínimo (velocidad máxima)
    [SerializeField] float _footstepSpeedRef = 15f;        // Velocidad de referencia para intervalo mínimo
    [SerializeField] float _footstepMinSpeed = 1f;         // Velocidad mínima para que suenen pisadas

    // =====================================================================
    //  GRUNTS — Sonidos de esfuerzo al hacer parkour
    // =====================================================================
    [Header("Esfuerzo / Grunts")]
    [SerializeField] AudioSource gruntSource;              // AudioSource para grunts
    [SerializeField] AudioClip[] vaultGrunts;              // Grunts al hacer vault
    [SerializeField] AudioClip[] jumpGrunts;               // Grunts al saltar
    [SerializeField] AudioClip[] wallRunGrunts;            // Grunts al iniciar wall-run
    [SerializeField] AudioClip[] landingGrunts;            // Grunts al aterrizar fuerte
    [SerializeField] AudioClip[] slideGrunts;              // Grunts al deslizar

    // =====================================================================
    //  FEEDBACK DE COMBO — Sonido que sube de tono al encadenar parkour
    // =====================================================================
    [Header("Feedback de Combo")]
    [SerializeField] AudioSource comboSource;              // AudioSource para feedback de combo
    [SerializeField] AudioClip comboChainSound;            // Sonido al encadenar movimientos
    [SerializeField] float _comboPitchBase = 0.8f;         // Pitch base del combo
    [SerializeField] float _comboPitchPerChain = 0.1f;     // Cuánto sube el pitch por cada chain
    [SerializeField] float _comboMaxPitch = 1.5f;          // Pitch máximo

    // ---- Estado interno ----
    private float _footstepTimer;
    private float _targetWindVolume;
    private float _targetWindPitch;
    private bool _wasGroundedLastFrame;
    private int _lastMomentumCount;

    private void Start()
    {
        // Configurar viento como loop silencioso
        if (windSource != null && windLoop != null)
        {
            windSource.clip = windLoop;
            windSource.loop = true;
            windSource.volume = 0f;
            windSource.Play();
        }
        _wasGroundedLastFrame = true;
    }

    private void Update()
    {
        if (playerSM == null) return;

        float speed = playerSM.CurrentHorizontalVelocity.magnitude;
        float dt = Time.deltaTime;

        UpdateWind(speed, dt);
        UpdateFootsteps(speed, dt);
        CheckMomentumChain();

        _wasGroundedLastFrame = playerSM.Grounded;
    }

    // ==================== VIENTO ====================
    private void UpdateWind(float speed, float dt)
    {
        if (windSource == null) return;

        // Volumen y pitch aumentan proporcionalmente a la velocidad
        float speedRatio = Mathf.InverseLerp(_windMinSpeed, _windMaxSpeed, speed);
        _targetWindVolume = speedRatio * _windMaxVolume;
        _targetWindPitch = Mathf.Lerp(1f, _windMaxPitch, speedRatio);

        windSource.volume = Mathf.Lerp(windSource.volume, _targetWindVolume, _windLerpSpeed * dt);
        windSource.pitch = Mathf.Lerp(windSource.pitch, _targetWindPitch, _windLerpSpeed * dt);
    }

    // ==================== PISADAS ====================
    private void UpdateFootsteps(float speed, float dt)
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;

        // Solo pisar si estamos en el suelo y moviéndonos
        if (!playerSM.Grounded || speed < _footstepMinSpeed)
        {
            _footstepTimer = 0f;
            return;
        }

        // Calcular intervalo: más rápido = menos tiempo entre pisadas
        float speedRatio = Mathf.Clamp01(speed / _footstepSpeedRef);
        float interval = Mathf.Lerp(_footstepBaseInterval, _footstepMinInterval, speedRatio);

        _footstepTimer += dt;
        if (_footstepTimer >= interval)
        {
            _footstepTimer = 0f;
            PlayRandomClip(footstepSource, footstepClips);
        }
    }

    // ==================== MOMENTUM CHAIN ====================
    private void CheckMomentumChain()
    {
        if (playerSM.MomentumChainCount > _lastMomentumCount && playerSM.MomentumChainCount > 1)
        {
            // Se ha encadenado una nueva acción de parkour
            PlayComboSound();
        }
        _lastMomentumCount = playerSM.MomentumChainCount;
    }

    private void PlayComboSound()
    {
        if (comboSource == null || comboChainSound == null) return;

        // El pitch sube con cada acción encadenada (cada vez suena más agudo)
        float pitch = _comboPitchBase + (playerSM.MomentumChainCount * _comboPitchPerChain);
        pitch = Mathf.Min(pitch, _comboMaxPitch);

        comboSource.pitch = pitch;
        comboSource.PlayOneShot(comboChainSound);
    }

    // ==================== API PÚBLICA ====================

    /// <summary>Reproduce un grunt aleatorio del array dado.</summary>
    public void PlayGrunt(AudioClip[] clips)
    {
        if (gruntSource == null || clips == null || clips.Length == 0) return;
        PlayRandomClip(gruntSource, clips);
    }

    public void PlayVaultGrunt() => PlayGrunt(vaultGrunts);
    public void PlayJumpGrunt() => PlayGrunt(jumpGrunts);
    public void PlayWallRunGrunt() => PlayGrunt(wallRunGrunts);
    public void PlayLandingGrunt() => PlayGrunt(landingGrunts);
    public void PlaySlideGrunt() => PlayGrunt(slideGrunts);

    // ==================== UTILIDADES ====================
    private void PlayRandomClip(AudioSource source, AudioClip[] clips)
    {
        if (clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        source.pitch = Random.Range(0.9f, 1.1f); // Variación sutil de pitch para que no suene repetitivo
        source.PlayOneShot(clips[index]);
    }
}
