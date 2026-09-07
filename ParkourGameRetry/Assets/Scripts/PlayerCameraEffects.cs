using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestiona todos los efectos de cámara del parkour:
/// FOV dinámico, head bob, landing dip, sprint lean, camera shake.
/// Se coloca en la cámara o en un pivote hijo de la cámara.
/// </summary>
public class PlayerCameraEffects : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] PlayerStateMachine playerSM;
    [SerializeField] CinemachineCamera cinemachineCamera;
    
    [Header("FOV Dinámico")]
    [SerializeField] float baseFOV = 75f;
    [SerializeField] float maxFOV = 100f;
    [SerializeField] float fovSpeedReference = 20f;
    [SerializeField] float fovLerpSpeed = 5f;
    
    [Header("Head Bob")]
    [SerializeField] float bobFrequency = 8f;
    [SerializeField] float bobAmplitudeY = 0.03f;
    [SerializeField] float bobAmplitudeX = 0.02f;
    [SerializeField] float bobSpeedMultiplier = 1.5f;
    
    [Header("Landing Dip")]
    [SerializeField] float landingDipAmount = 0.15f;
    [SerializeField] float landingDipRecoverySpeed = 8f;
    [SerializeField] float landingDipMinFallSpeed = 3f;
    
    [Header("Sprint Lean")]
    [SerializeField] float sprintLeanAngle = 3f;
    [SerializeField] float sprintLeanSpeed = 4f;
    
    [Header("Camera Shake")]
    [SerializeField] float landingShakeIntensity = 0.1f;
    [SerializeField] float wallHitShakeIntensity = 0.05f;
    [SerializeField] float impulseShakeIntensity = 0.08f;
    [SerializeField] float shakeDuration = 0.2f;
    [SerializeField] float shakeFrequency = 25f;
    
    private float currentFOV;
    private float bobTimer;
    private Vector3 originalLocalPos;
    private float landingDipOffset;
    private float currentShakeIntensity;
    private float shakeTimer;
    private bool wasGroundedLastFrame;
    private float lastVerticalSpeed;
    private float currentLean;
    private float lastAppliedLean;
    private float lastYaw;

    private void Start()
    {
        originalLocalPos = transform.localPosition;
        currentFOV = baseFOV;
        if (cinemachineCamera != null)
            currentFOV = cinemachineCamera.Lens.FieldOfView;
        wasGroundedLastFrame = true;
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
        
        wasGroundedLastFrame = playerSM.Grounded;
        lastVerticalSpeed = playerSM.GetVerticalSpeed();
        lastYaw = playerSM.transform.eulerAngles.y;
    }

    private void UpdateFOV(float speed, float dt)
    {
        float speedRatio = Mathf.Clamp01(speed / fovSpeedReference);
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovLerpSpeed * dt);
        
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.FieldOfView = currentFOV;
        }
    }

    private void UpdateHeadBob(float speed, float dt)
    {
        if (playerSM.Grounded && speed > 0.5f)
        {
            float adjustedFrequency = bobFrequency + (speed * bobSpeedMultiplier);
            bobTimer += dt * adjustedFrequency;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, dt * 5f);
        }
    }

    private void UpdateLandingDip(float dt)
    {
        if (playerSM.Grounded && !wasGroundedLastFrame)
        {
            if (lastVerticalSpeed > landingDipMinFallSpeed)
            {
                float intensity = Mathf.Clamp01(lastVerticalSpeed / 20f);
                landingDipOffset = -landingDipAmount * intensity;
                TriggerShake(landingShakeIntensity * intensity);
            }
        }
        
        landingDipOffset = Mathf.Lerp(landingDipOffset, 0f, landingDipRecoverySpeed * dt);
    }

    private void UpdateSprintLean(float speed, float dt)
    {
        float currentYaw = playerSM.transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(lastYaw, currentYaw);
        
        float targetLean = 0f;
        if (speed > 3f)
        {
            targetLean = -yawDelta * sprintLeanAngle * Mathf.Clamp01(speed / 10f);
            targetLean = Mathf.Clamp(targetLean, -sprintLeanAngle * 2f, sprintLeanAngle * 2f);
        }
        
        currentLean = Mathf.Lerp(currentLean, targetLean, sprintLeanSpeed * dt);
    }

    private void UpdateShake(float dt)
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= dt;
            currentShakeIntensity = Mathf.Lerp(0f, currentShakeIntensity, shakeTimer / shakeDuration);
        }
        else
        {
            currentShakeIntensity = 0f;
        }
    }

    private void ApplyOffsets()
    {
        Vector3 offset = originalLocalPos;
        
        float bobY = Mathf.Sin(bobTimer) * bobAmplitudeY;
        float bobX = Mathf.Sin(bobTimer * 0.5f) * bobAmplitudeX;
        offset += new Vector3(bobX, bobY, 0f);
        
        offset.y += landingDipOffset;
        
        if (currentShakeIntensity > 0)
        {
            float shakeX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;
            offset += new Vector3(shakeX, shakeY, 0f) * currentShakeIntensity;
        }
        
        transform.localPosition = offset;
        
        Vector3 euler = transform.localEulerAngles;
        euler.z += currentLean - lastAppliedLean;
        transform.localEulerAngles = euler;
        lastAppliedLean = currentLean;
    }

    public void TriggerShake(float intensity)
    {
        currentShakeIntensity = intensity;
        shakeTimer = shakeDuration;
    }

    public void TriggerLandingShake(float fallSpeed)
    {
        float intensity = landingShakeIntensity * Mathf.Clamp01(fallSpeed / 15f);
        TriggerShake(intensity);
    }

    public void TriggerWallHitShake()
    {
        TriggerShake(wallHitShakeIntensity);
    }

    public void TriggerImpulseShake()
    {
        TriggerShake(impulseShakeIntensity);
    }
}
