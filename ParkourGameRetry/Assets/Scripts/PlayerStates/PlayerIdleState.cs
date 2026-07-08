using UnityEngine;

/// <summary>
/// Estado principal del jugador: gestiona movimiento en suelo, aire, salto y transiciones.
/// A pesar del nombre "Idle", este estado maneja TODO el gameplay básico:
/// correr, caer, saltar, detectar vault, wall-run, slide, dash, ledge grab y landing roll.
/// </summary>
public class PlayerIdleState : PlayerBaseState
{
    // Track para detectar aterrizajes (landing roll / hard landing)
    private bool _wasGroundedLastFrame = true;
    private float _lastVerticalSpeed;

    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    // =====================================================================
    //  ENTER — Se suscribe a los eventos de input al entrar al estado
    // =====================================================================
    public override void Enter()
    {
        // Suscribirse al input de salto y crouch
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();
    }

    // =====================================================================
    //  TICK — Bucle principal: se ejecuta cada frame
    // =====================================================================
    public override void Tick(float deltaTime)
    {
        // ---- ROTACIÓN DEL JUGADOR ----
        stateMachine.PlayerLook();

        // ---- DETECCIÓN DE SUELO ----
        stateMachine.GroundDetection();

        // ---- COYOTE TIME ----
        stateMachine.TickCoyoteTime(Time.deltaTime);

        // ---- MOMENTUM SYSTEM ----
        // (Movido a PlayerStateMachine.LateUpdate para que no se atasque)

        // ---- GRAVEDAD ----
        stateMachine.ApplyGravity();

        // ---- MOVIMIENTO HORIZONTAL ----
        stateMachine.PlayerHorizontalMovement(stateMachine.GetWorldMovementDirection(stateMachine.GetInput()));

        // Leer el estado de crouch directamente del InputReader para fiabilidad
        bool crouchHeld = stateMachine.Controls != null && stateMachine.Controls.CrouchHeld;

        // ---- DETECCIÓN DE ATERRIZAJE ----
        // Comprobar si acabamos de aterrizar de una caída alta
        if (stateMachine.Grounded && !_wasGroundedLastFrame)
        {
            if (_lastVerticalSpeed > stateMachine.RollMinFallSpeed)
            {
                // Caída alta detectada
                if (crouchHeld)
                {
                    // ---- LANDING ROLL ----
                    // El jugador pulsó crouch al aterrizar → recompensa con roll
                    stateMachine.SwitchState(typeof(PlayerLandingRollState));
                    _wasGroundedLastFrame = stateMachine.Grounded;
                    _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
                    return;
                }
                else
                {
                    // ---- HARD LANDING (CASTIGO) ----
                    // No hizo roll → pierde velocidad como penalización
                    stateMachine.CurrentHorizontalVelocity *= stateMachine.HardLandingPenalty;
                }
            }
        }

        // ---- INPUT DE SALTO ----
        stateMachine.DecrementJumpBuffer(deltaTime);
        bool wantsToJump = stateMachine.JumpPressed || stateMachine.JumpBufferCounter > 0f;

        // =====================================================================
        //  PRIORIDADES DE TRANSICIÓN (ordenadas por importancia)
        // =====================================================================

        // ---- PRIORIDAD 1: VAULT ----
        if (wantsToJump && stateMachine.CanVault())
        {
            stateMachine.JumpBufferCounter = 0f;
            stateMachine.SwitchState(typeof(PlayerVaultState));
            _wasGroundedLastFrame = stateMachine.Grounded;
            _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 2: WALL-RUN ----
        if (!stateMachine.Grounded && stateMachine.CanWallRun())
        {
            stateMachine.SwitchState(typeof(PlayerWallRunState));
            _wasGroundedLastFrame = stateMachine.Grounded;
            _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 3: LEDGE GRAB ----
        // En el aire, cerca de un borde demasiado alto para vault
        if (!stateMachine.Grounded && stateMachine.CanLedgeGrab())
        {
            stateMachine.SwitchState(typeof(PlayerLedgeGrabState));
            _wasGroundedLastFrame = stateMachine.Grounded;
            _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 4: SLIDE ----
        // En suelo, crouch pulsado, y con suficiente velocidad
        if (stateMachine.Grounded && crouchHeld)
        {
            float currentSpeed = stateMachine.CurrentHorizontalVelocity.magnitude;
            if (currentSpeed >= stateMachine.SlideMinSpeed)
            {
                stateMachine.SwitchState(typeof(PlayerSlideState));
                _wasGroundedLastFrame = stateMachine.Grounded;
                _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
                return;
            }
        }

        // ---- PRIORIDAD 5: DASH ----
        // Sprint pulsado y cooldown cumplido (funciona en suelo y aire)
        if (!crouchHeld && stateMachine.Controls != null && stateMachine.Controls.SprintHeld)
        {
            float timeSinceLastDash = Time.time - stateMachine.LastDashTime;
            if (timeSinceLastDash >= stateMachine.DashCooldown)
            {
                stateMachine.SwitchState(typeof(PlayerDashState));
                _wasGroundedLastFrame = stateMachine.Grounded;
                _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
                return;
            }
        }

        // ---- PRIORIDAD 6: SALTO NORMAL ----
        bool canJump = stateMachine.Grounded || stateMachine.CoyoteTimeCounter > 0f;

        if (wantsToJump && canJump)
        {
            stateMachine.ExecuteJump();
        }

        // ---- LIMPIEZA DE INPUT ----
        stateMachine.ClearJumpInputFrame();

        // ---- Track estado del frame para detección de aterrizaje ----
        _wasGroundedLastFrame = stateMachine.Grounded;
        _lastVerticalSpeed = stateMachine.GetVerticalSpeed();
    }

    // =====================================================================
    //  FIXED TICK — No se usa en este estado
    // =====================================================================
    public override void FixedTick(float fixedDeltaTime)
    {
    }

    // =====================================================================
    //  EXIT — Se desuscribe de los eventos al salir del estado
    // =====================================================================
    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
