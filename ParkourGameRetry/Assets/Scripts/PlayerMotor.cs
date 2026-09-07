using UnityEngine;

/// <summary>
/// Componente Motor de Movimiento Físico para el Jugador.
/// Separa la física y las colisiones del CharacterController de la máquina de estados.
/// Ejecuta el movimiento en una ÚNICA llamada a cc.Move() por frame (Single-Pass Integration).
/// Utiliza AnimationCurves para ajustar aceleración y frenado con precisión AAA.
/// </summary>
public class PlayerMotor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] CharacterController cc;
    public CharacterController CC => cc;

    [Header("Aceleración y Velocidad")]
    [SerializeField] float targetVelocity = 10f;
    [SerializeField] float acceleration = 12f;
    [SerializeField] float airAcceleration = 4f;
    [SerializeField] AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1f);

    [Header("Desaceleración y Frenado")]
    [SerializeField] float deceleration = 1.5f;
    [SerializeField] float brakingDeceleration = 22f;
    [SerializeField] float airDeceleration = 0f;
    [SerializeField] float turnaroundStrength = 0.85f;
    [SerializeField] AnimationCurve decelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

    [Header("Gravedad y Suelo")]
    [SerializeField] float gravityForce = 20f;
    [SerializeField] Vector3 gravityDir = Vector3.down;
    [SerializeField] float maxSlopeAngle = 45f;
    [SerializeField] float groundProbeDistance = 0.22f;
    [SerializeField, Range(0.5f, 1f)] float groundProbeRadiusScale = 0.9f;
    [SerializeField] LayerMask groundMask = int.MinValue;

    // Estado de velocidad interna (XZ horizontal, Y vertical)
    public Vector3 HorizontalVelocity { get; set; }
    public float VerticalVelocity { get; set; }
    public Vector3 CombinedVelocity => HorizontalVelocity + Vector3.up * VerticalVelocity;

    // Estado del suelo
    public bool Grounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public bool SlidingOnSlope { get; private set; }
    public Vector3 SlopeNormal { get; private set; } = Vector3.up;

    // Propiedades de configuración expuestas
    public float TargetVelocity { get => targetVelocity; set => targetVelocity = value; }
    public float Acceleration { get => acceleration; set => acceleration = value; }
    public float AirAcceleration { get => airAcceleration; set => airAcceleration = value; }
    public float Deceleration { get => deceleration; set => deceleration = value; }
    public float BrakingDeceleration { get => brakingDeceleration; set => brakingDeceleration = value; }
    public float AirDeceleration { get => airDeceleration; set => airDeceleration = value; }
    public float TurnaroundStrength { get => turnaroundStrength; set => turnaroundStrength = value; }
    public float GravityForce { get => gravityForce; set => gravityForce = value; }
    public LayerMask GroundMask => groundMask;
    public AnimationCurve AccelerationCurve => accelerationCurve;
    public AnimationCurve DecelerationCurve => decelerationCurve;

    [Header("Estabilización de Colisiones")]
    [SerializeField] float minSkinWidth = 0.15f;

    private float originalCCHeight;
    private Vector3 originalCCCenter;
    public float OriginalCCHeight => originalCCHeight;
    public Vector3 OriginalCCCenter => originalCCCenter;

    private bool forceZeroVelocityNextFrame;

    private void Awake()
    {
        if (cc == null)
            cc = GetComponent<CharacterController>();

        if (cc != null)
        {
            // Aumentar skinWidth para evitar atravesar paredes a altas velocidades
            if (cc.skinWidth < minSkinWidth)
                cc.skinWidth = minSkinWidth;

            originalCCHeight = cc.height;
            originalCCCenter = cc.center;
        }
    }

    /// <summary>
    /// Ejecuta la detección de suelo mediante SphereCast.
    /// </summary>
    public void DetectGround()
    {
        if (cc == null) return;

        // Si estamos ascendiendo rápido (ej. salto), no pegar forzadamente al suelo
        if (VerticalVelocity > 0.1f)
        {
            Grounded = false;
            GroundNormal = Vector3.up;
            SlidingOnSlope = false;
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(cc.center);
        float scaledHeight = cc.height * Mathf.Abs(transform.lossyScale.y);
        float scaledRadius = cc.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float radius = scaledRadius * groundProbeRadiusScale;
        float bottomOffset = Mathf.Max(0f, scaledHeight * 0.5f - scaledRadius);
        Vector3 bottomSphereCenter = worldCenter + Vector3.down * bottomOffset;
        Vector3 probeOrigin = bottomSphereCenter - Vector3.down * 0.03f;

        bool hasGround = Physics.SphereCast(
            probeOrigin,
            radius,
            Vector3.down,
            out RaycastHit hitInfo,
            groundProbeDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hasGround)
        {
            float groundAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
            if (groundAngle <= maxSlopeAngle)
            {
                GroundNormal = hitInfo.normal;
                Grounded = true;
                SlidingOnSlope = false;
                SlopeNormal = hitInfo.normal;
            }
            else
            {
                Grounded = false;
                SlidingOnSlope = true;
                SlopeNormal = hitInfo.normal;
                GroundNormal = Vector3.up;
            }
        }
        else
        {
            Grounded = false;
            GroundNormal = Vector3.up;
            SlidingOnSlope = false;
        }
    }

    /// <summary>
    /// Procesa el movimiento horizontal del jugador usando fuerza aditiva y AnimationCurves.
    /// No llama a cc.Move() directamente; en su lugar actualiza HorizontalVelocity.
    /// </summary>
    public void CalculateHorizontalMovement(Vector3 wishDirection, float overrideTargetSpeed = -1f, float deltaTime = 0f)
    {
        if (deltaTime <= 0f) deltaTime = Time.deltaTime;

        if (forceZeroVelocityNextFrame)
        {
            HorizontalVelocity = Vector3.zero;
            forceZeroVelocityNextFrame = false;
        }

        Vector3 currentVel = new Vector3(HorizontalVelocity.x, 0f, HorizontalVelocity.z);
        float inputMagnitude = Mathf.Clamp01(wishDirection.magnitude);
        Vector3 wishDir = inputMagnitude > 0.01f ? wishDirection.normalized : Vector3.zero;

        float currentSpeed = currentVel.magnitude;
        float maxSpeed = overrideTargetSpeed > 0f ? overrideTargetSpeed : targetVelocity;
        float targetLocomotionSpeed = maxSpeed * inputMagnitude;

        float accelRate = Grounded ? acceleration : airAcceleration;

        // Decidir ritmo de fricción/frenado
        float currentDecelRate;
        if (Grounded)
        {
            currentDecelRate = wishDir.sqrMagnitude < 0.01f ? brakingDeceleration : deceleration;
        }
        else
        {
            currentDecelRate = airDeceleration;
        }

        // 1. Fricción / Frenado utilizando DecelerationCurve
        if (currentSpeed > 0f)
        {
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
            float curveMult = decelerationCurve != null ? decelerationCurve.Evaluate(speedRatio) : 1f;
            float drop = currentDecelRate * curveMult * deltaTime;

            // Freno extra al girar de golpe (turnaround)
            if (wishDir.sqrMagnitude > 0.01f && Grounded)
            {
                float dot = Vector3.Dot(currentVel.normalized, wishDir);
                if (dot < 0f)
                {
                    drop += (brakingDeceleration * -dot * turnaroundStrength * deltaTime);
                }
            }

            float newSpeed = Mathf.Max(currentSpeed - drop, 0f);
            currentVel = currentVel * (newSpeed / currentSpeed);
        }

        // 2. Aceleración Aditiva utilizando AccelerationCurve
        if (wishDir.sqrMagnitude > 0.01f)
        {
            float currentSpeedInWishDir = Vector3.Dot(currentVel, wishDir);
            float addSpeed = targetLocomotionSpeed - currentSpeedInWishDir;

            if (addSpeed > 0f)
            {
                float speedRatio = Mathf.Clamp01(currentSpeedInWishDir / maxSpeed);
                float curveMult = accelerationCurve != null ? accelerationCurve.Evaluate(speedRatio) : 1f;
                float accelSpeed = accelRate * curveMult * deltaTime;

                if (accelSpeed > addSpeed) accelSpeed = addSpeed;

                currentVel += wishDir * accelSpeed;

                float maxAllowed = Mathf.Max(currentSpeed, targetLocomotionSpeed);
                if (currentVel.magnitude > maxAllowed)
                    currentVel = currentVel.normalized * maxAllowed;
            }
        }

        currentVel.y = 0f;
        HorizontalVelocity = currentVel;
    }

    /// <summary>
    /// Acumula o aplica la gravedad en la velocidad vertical.
    /// </summary>
    public void CalculateGravity(float gravityScale = 1.0f, float deltaTime = 0f)
    {
        if (deltaTime <= 0f) deltaTime = Time.deltaTime;

        if (Grounded)
        {
            // Pequeña fuerza hacia abajo para mantener pegado al suelo al bajar pendientes
            VerticalVelocity = -2.0f;
        }
        else
        {
            if (SlidingOnSlope)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, SlopeNormal).normalized;
                HorizontalVelocity += slideDir * gravityForce * deltaTime;
                VerticalVelocity = Mathf.MoveTowards(VerticalVelocity, -gravityForce, gravityForce * deltaTime);
            }
            else
            {
                VerticalVelocity -= gravityForce * gravityScale * deltaTime;
            }
        }
    }

    /// <summary>
    /// UNIFICACIÓN SINGLE-PASS: Mueve el CharacterController en una ÚNICA llamada a cc.Move().
    /// Combina el desplazamiento horizontal adaptado a la pendiente con la gravedad vertical.
    /// </summary>
    public void MoveSinglePass(float deltaTime = 0f)
    {
        if (cc == null) return;
        if (deltaTime <= 0f) deltaTime = Time.deltaTime;

        Vector3 moveHorizontal = HorizontalVelocity;

        // Adaptar la inercia horizontal al plano de la pendiente cuando estamos en el suelo
        if (Grounded && HorizontalVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(HorizontalVelocity.normalized, GroundNormal);
            if (slopeDir.sqrMagnitude > 0.001f)
            {
                moveHorizontal = slopeDir.normalized * HorizontalVelocity.magnitude;
            }
        }

        // Vector final que combina Horizontal + Vertical en 1 sola llamada
        Vector3 finalMoveVector = moveHorizontal + Vector3.up * VerticalVelocity;

        Vector3 posBeforeMove = transform.position;
        cc.Move(finalMoveVector * deltaTime);

        // Detección de colisión frontal para no conservar momentum fantasma al chocar contra paredes
        if (moveHorizontal.sqrMagnitude > 0.01f)
        {
            Vector3 actualDisplacement = transform.position - posBeforeMove;
            actualDisplacement.y = 0f;
            Vector3 intendedDisplacement = moveHorizontal * deltaTime;
            intendedDisplacement.y = 0f;

            if (intendedDisplacement.sqrMagnitude > 0.0001f)
            {
                float ratio = actualDisplacement.magnitude / intendedDisplacement.magnitude;
                if (ratio < 0.8f)
                {
                    HorizontalVelocity *= ratio;
                }
            }
        }
    }

    /// <summary>
    /// Aplica un impulso de salto directo (resetea velocidad vertical y añade impulso).
    /// </summary>
    public void ExecuteJump(float jumpForce)
    {
        VerticalVelocity = jumpForce;
        Grounded = false;
    }

    /// <summary>
    /// Resetea la física del motor tras salir de un vault.
    /// </summary>
    public void ResetVaultPhysics()
    {
        VerticalVelocity = 0f;
        HorizontalVelocity = Vector3.zero;
        forceZeroVelocityNextFrame = true;
    }

    /// <summary>
    /// Reduce las dimensiones del CharacterController para el deslizado (slide).
    /// </summary>
    public void ShrinkCollider(float heightReduction)
    {
        if (cc == null) return;
        cc.height = originalCCHeight - heightReduction;
        cc.center = originalCCCenter + Vector3.down * (heightReduction * 0.5f);
    }

    /// <summary>
    /// Restaura el CharacterController a sus dimensiones originales.
    /// </summary>
    public void RestoreCollider()
    {
        if (cc == null) return;
        cc.height = originalCCHeight;
        cc.center = originalCCCenter;
    }
}
