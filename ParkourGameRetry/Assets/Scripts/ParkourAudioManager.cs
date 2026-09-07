using UnityEngine;

/// <summary>
/// Gestiona todos los efectos de audio del parkour:
/// Sonido de viento progresivo, pisadas dinámicas, grunts de esfuerzo,
/// y feedback sonoro al encadenar movimientos.
/// </summary>
public class ParkourAudioManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] PlayerStateMachine playerSM;
    [SerializeField] bool createProceduralFallbacks = true;

    [Header("Viento")]
    [SerializeField] AudioSource windSource;
    [SerializeField] AudioClip windLoop;
    [SerializeField] float windMinSpeed = 5f;
    [SerializeField] float windMaxSpeed = 25f;
    [SerializeField] float windMaxVolume = 0.6f;
    [SerializeField] float windMaxPitch = 1.5f;
    [SerializeField] float windLerpSpeed = 3f;

    [Header("Pisadas")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClips;
    [SerializeField] float footstepBaseInterval = 0.5f;
    [SerializeField] float footstepMinInterval = 0.2f;
    [SerializeField] float footstepSpeedRef = 15f;
    [SerializeField] float footstepMinSpeed = 1f;

    [Header("Esfuerzo / Grunts")]
    [SerializeField] AudioSource gruntSource;
    [SerializeField] AudioClip[] vaultGrunts;
    [SerializeField] AudioClip[] jumpGrunts;
    [SerializeField] AudioClip[] wallRunGrunts;
    [SerializeField] AudioClip[] landingGrunts;
    [SerializeField] AudioClip[] slideGrunts;

    [Header("Feedback de Combo")]
    [SerializeField] AudioSource comboSource;
    [SerializeField] AudioClip comboChainSound;
    [SerializeField] float comboPitchBase = 0.8f;
    [SerializeField] float comboPitchPerChain = 0.1f;
    [SerializeField] float comboMaxPitch = 1.5f;

    private float footstepTimer;
    private float targetWindVolume;
    private float targetWindPitch;
    private bool wasGroundedLastFrame;
    private int lastMomentumCount;

    private void Awake()
    {
        if (playerSM == null)
            playerSM = GetComponent<PlayerStateMachine>();

        if (!createProceduralFallbacks) return;

        if (windSource == null)
            windSource = CreateAudioSource(0f);
        if (comboSource == null)
            comboSource = CreateAudioSource(0.55f);
        if (windLoop == null)
            windLoop = CreateProceduralWindLoop();
        if (comboChainSound == null)
            comboChainSound = CreateProceduralComboTone();
    }

    public void Bind(PlayerStateMachine stateMachine)
    {
        playerSM = stateMachine;
    }

    private void Start()
    {
        if (windSource != null && windLoop != null)
        {
            windSource.clip = windLoop;
            windSource.loop = true;
            windSource.volume = 0f;
            windSource.Play();
        }
        wasGroundedLastFrame = true;
    }

    private void Update()
    {
        if (playerSM == null) return;

        float speed = playerSM.CurrentHorizontalVelocity.magnitude;
        float dt = Time.deltaTime;

        UpdateWind(speed, dt);
        UpdateFootsteps(speed, dt);
        CheckMomentumChain();

        wasGroundedLastFrame = playerSM.Grounded;
    }

    private void UpdateWind(float speed, float dt)
    {
        if (windSource == null) return;

        float speedRatio = Mathf.InverseLerp(windMinSpeed, windMaxSpeed, speed);
        targetWindVolume = speedRatio * windMaxVolume;
        targetWindPitch = Mathf.Lerp(1f, windMaxPitch, speedRatio);

        windSource.volume = Mathf.Lerp(windSource.volume, targetWindVolume, windLerpSpeed * dt);
        windSource.pitch = Mathf.Lerp(windSource.pitch, targetWindPitch, windLerpSpeed * dt);
    }

    private void UpdateFootsteps(float speed, float dt)
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;

        if (!playerSM.Grounded || speed < footstepMinSpeed)
        {
            footstepTimer = 0f;
            return;
        }

        float speedRatio = Mathf.Clamp01(speed / footstepSpeedRef);
        float interval = Mathf.Lerp(footstepBaseInterval, footstepMinInterval, speedRatio);

        footstepTimer += dt;
        if (footstepTimer >= interval)
        {
            footstepTimer = 0f;
            PlayRandomClip(footstepSource, footstepClips);
        }
    }

    private void CheckMomentumChain()
    {
        if (playerSM.MomentumChainCount > lastMomentumCount && playerSM.MomentumChainCount > 1)
        {
            PlayComboSound();
        }
        lastMomentumCount = playerSM.MomentumChainCount;
    }

    private void PlayComboSound()
    {
        if (comboSource == null || comboChainSound == null) return;

        float pitch = comboPitchBase + (playerSM.MomentumChainCount * comboPitchPerChain);
        pitch = Mathf.Min(pitch, comboMaxPitch);

        comboSource.pitch = pitch;
        comboSource.PlayOneShot(comboChainSound);
    }

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

    private void PlayRandomClip(AudioSource source, AudioClip[] clips)
    {
        if (clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(clips[index]);
    }

    private AudioSource CreateAudioSource(float initialVolume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = initialVolume;
        return source;
    }

    private static AudioClip CreateProceduralWindLoop()
    {
        const int sampleRate = 44100;
        const int sampleCount = sampleRate * 2;
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(7319);
        float filteredNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            filteredNoise = Mathf.Lerp(filteredNoise, whiteNoise, 0.035f);
            samples[i] = filteredNoise * 0.28f;
        }

        AudioClip clip = AudioClip.Create("Procedural Parkour Wind", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateProceduralComboTone()
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.12f);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = 1f - i / (float)samples.Length;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 620f * time) * envelope * envelope * 0.22f;
        }

        AudioClip clip = AudioClip.Create("Procedural Flow Chain", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
