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
        stateMachine.RegisterParkourAction(ParkourActionType.Slide);

        // Suscribirse a inputs
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();

        // Reducir el collider para pasar por debajo de obstáculos
        stateMachine.ShrinkCollider();

        // Guardar dirección inicial del slide (la dirección en la que corríamos)
        slideDirection = stateMachine.CurrentHorizontalVelocity.normalized;
        if (slideDirection.sqrMagnitude < 0.01f)
            slideDirection = stateMachine.transform.forward;

        currentSlideSpeed = stateMachine.ResolveActionSpeed(
            stateMachine.CurrentHorizontalVelocity.magnitude,
            stateMachine.SlideSpeed,
            stateMachine.SlideMomentumRetention,
            stateMachine.SlideSpeedReward
        );

        slideTimer = 0f;

        // Bajar el punto de vista junto con el collider para que el slide
        // se lea físicamente y no como una carrera inclinada.
        stateMachine.SetCameraHeightOffset(
            -stateMachine.SlideCameraDropAmount,
            stateMachine.SlideCameraDropSpeed
        );
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // ---- Detección de suelo ----
        stateMachine.GroundDetection();
        if (!stateMachine.Grounded)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- Gravedad ----
        stateMachine.ApplyGravity();

        // ---- Timer ----
        slideTimer += deltaTime;

        // ---- FRICCIÓN DEL SLIDE ----
        currentSlideSpeed -= stateMachine.SlideFriction * deltaTime;

        // Dirección asistida suave: permite corregir el slide sin girarlo
        // instantáneamente ni destruir la inercia.
        Vector3 desiredDirection = stateMachine.GetWorldMovementDirection(stateMachine.GetInput());
        if (desiredDirection.sqrMagnitude > 0.05f)
        {
            desiredDirection.y = 0f;
            slideDirection = Vector3.Slerp(
                slideDirection,
                desiredDirection.normalized,
                stateMachine.SlideSteering * deltaTime
            ).normalized;
        }

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

        // 1. Salto o vault desde slide. Nunca se hace vault automático
        // por tocar un obstáculo sin intención del jugador.
        if (stateMachine.JumpPressed)
        {
            if (stateMachine.CanVault())
            {
                stateMachine.JumpBufferCounter = 0f;
                stateMachine.SwitchState(typeof(PlayerVaultState));
            }
            else
            {
                stateMachine.ExecuteJump();
                stateMachine.SwitchState(typeof(PlayerIdleState));
            }
            return;
        }

        // 2. Soltar crouch (leer directamente del InputReader)
        bool crouchStillHeld = stateMachine.Controls != null && stateMachine.Controls.CrouchHeld;
        if (!crouchStillHeld)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // 3. Timer agotado
        if (slideTimer >= stateMachine.SlideDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // 4. Velocidad demasiado baja
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

        // Recuperar la altura normal de la cámara de forma suave.
        stateMachine.SetCameraHeightOffset(0f, stateMachine.SlideCameraDropSpeed);

        // Desuscribirse de inputs
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
