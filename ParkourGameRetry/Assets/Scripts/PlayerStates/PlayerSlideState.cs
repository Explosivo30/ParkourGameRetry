using UnityEngine;

/// <summary>
/// Estado de Slide: el jugador se desliza por el suelo con las piernas por delante.
/// 
/// ENTRADA: Se activa cuando el jugador está en el suelo, corriendo a velocidad alta,
///          y pulsa agacharse (crouch).
///
/// DURANTE: Se mueve en la dirección inicial del slide con fricción progresiva.
///          En pendientes hacia abajo, GANA velocidad en vez de perderla.
///          El collider se reduce para pasar por debajo de obstáculos.
///
/// SALIDA:  Por soltar crouch, fin del timer, velocidad muy baja, salto,
///          o si detecta un obstáculo vaultable (encadena slide → vault).
/// </summary>
public class PlayerSlideState : PlayerBaseState
{
    private float slideTimer;
    private Vector3 slideDirection;
    private float currentSlideSpeed;

    public PlayerSlideState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction();

        // Suscribirse a inputs
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();

        // Reducir el collider para pasar por debajo de obstáculos
        stateMachine.ShrinkCollider();

        // Guardar dirección inicial del slide (la dirección en la que corríamos)
        slideDirection = stateMachine.CurrentHorizontalVelocity.normalized;
        if (slideDirection.sqrMagnitude < 0.01f)
            slideDirection = stateMachine.transform.forward;

        // La velocidad del slide es la mayor entre la velocidad actual y SlideSpeed
        currentSlideSpeed = Mathf.Max(stateMachine.CurrentHorizontalVelocity.magnitude, stateMachine.SlideSpeed);

        // Aplicar multiplicador de momentum si hay combo activo
        currentSlideSpeed *= stateMachine.MomentumMultiplier;

        slideTimer = 0f;

        // Inclinar la cámara ligeramente hacia abajo (el jugador se agacha)
        stateMachine.SetCameraTilt(-5f);
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // ---- Detección de suelo ----
        stateMachine.GroundDetection();

        // ---- Gravedad ----
        stateMachine.ApplyGravity();

        // ---- Timer ----
        slideTimer += deltaTime;

        // ---- FRICCIÓN DEL SLIDE ----
        currentSlideSpeed -= stateMachine.SlideFriction * deltaTime;

        // ---- BOOST EN PENDIENTES ----
        // Si el suelo está inclinado (dot < 0.95), ganar velocidad cuesta abajo
        float groundDot = Vector3.Dot(stateMachine.GroundNormal, Vector3.up);
        if (groundDot < 0.95f && stateMachine.Grounded)
        {
            // Calcular cuánto baja la pendiente en nuestra dirección de slide
            Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, stateMachine.GroundNormal).normalized;
            float slopeAlignment = Vector3.Dot(slideDirection, slopeDown);

            if (slopeAlignment > 0.1f)
            {
                // Estamos deslizando cuesta abajo → ganar velocidad
                currentSlideSpeed += stateMachine.SlideSlopeBoost * slopeAlignment * deltaTime;
            }
        }

        // ---- MOVIMIENTO ----
        Vector3 moveVelocity = slideDirection * currentSlideSpeed;
        stateMachine.CurrentHorizontalVelocity = moveVelocity;
        stateMachine.CC.Move(moveVelocity * deltaTime);

        // ---- CONDICIONES DE SALIDA ----

        // 1. Slide → Vault: si hay un obstáculo vaultable, encadenar automáticamente
        if (stateMachine.CanVault())
        {
            stateMachine.SwitchState(typeof(PlayerVaultState));
            return;
        }

        // 2. Salto desde el slide
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // 3. Soltar crouch (leer directamente del InputReader)
        bool crouchStillHeld = stateMachine.Controls != null && stateMachine.Controls.CrouchHeld;
        if (!crouchStillHeld)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // 4. Timer agotado
        if (slideTimer >= stateMachine.SlideDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // 5. Velocidad demasiado baja
        if (currentSlideSpeed < 2f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        // Restaurar collider a su tamaño original
        stateMachine.RestoreCollider();

        // Resetear tilt de cámara (vuelve suavemente a 0 por el lerp)
        stateMachine.SetCameraTilt(0f);

        // Desuscribirse de inputs
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
