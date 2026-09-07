using UnityEngine;

/// <summary>
/// Estado principal del jugador: gestiona movimiento en suelo, aire, salto y transiciones.
/// A pesar del nombre "Idle", este estado maneja TODO el gameplay básico:
/// correr, caer, saltar, detectar vault, wall-run, slide, dash, ledge grab y landing roll.
/// </summary>
public class PlayerIdleState : PlayerBaseState
{
    // Track para detectar aterrizajes (landing roll / hard landing)
    private bool wasGroundedLastFrame = true;
    private float lastVerticalSpeed;

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

        // ---- GRAVEDAD ----
        stateMachine.ApplyGravity();

        // ---- MOVIMIENTO HORIZONTAL ----
        stateMachine.PlayerHorizontalMovement(stateMachine.GetWorldMovementDirection(stateMachine.GetInput()), deltaTime);

        // Leer el estado de crouch directamente del InputReader para fiabilidad
        bool crouchHeld = stateMachine.Controls != null && stateMachine.Controls.CrouchHeld;
        bool crouchPressed = stateMachine.Controls != null
            && stateMachine.Controls.CrouchPressedThisFrame;
        bool sprintPressed = stateMachine.Controls != null
            && stateMachine.Controls.SprintPressedThisFrame;

        // ---- DETECCIÓN DE ATERRIZAJE ----
        // Comprobar si acabamos de aterrizar de una caída alta
        if (stateMachine.Grounded && !wasGroundedLastFrame)
        {
            if (lastVerticalSpeed > stateMachine.RollMinFallSpeed)
            {
                // Caída alta detectada
                if (stateMachine.LandingRollInputBuffered)
                {
                    // ---- LANDING ROLL ----
                    // El jugador pulsó crouch al aterrizar → recompensa con roll
                    stateMachine.SwitchState(typeof(PlayerLandingRollState));
                    wasGroundedLastFrame = stateMachine.Grounded;
                    lastVerticalSpeed = stateMachine.GetVerticalSpeed();
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

        // Mantener espacio prioriza la subida frontal sobre el wall-run lateral.
        if (!stateMachine.Grounded && stateMachine.JumpHeld && stateMachine.CanLedgeGrab())
        {
            stateMachine.SwitchState(typeof(PlayerLedgeGrabState));
            wasGroundedLastFrame = false;
            lastVerticalSpeed = 0f;
            return;
        }
        if (stateMachine.CanWallClimb())
        {
            stateMachine.SwitchState(typeof(PlayerWallClimbState));
            wasGroundedLastFrame = false;
            lastVerticalSpeed = 0f;
            return;
        }

        // ---- PRIORIDAD 1: VAULT ----
        if (wantsToJump && stateMachine.CanVault())
        {
            stateMachine.JumpBufferCounter = 0f;
            stateMachine.SwitchState(typeof(PlayerVaultState));
            wasGroundedLastFrame = stateMachine.Grounded;
            lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 2: WALL-RUN ----
        if (!stateMachine.Grounded && stateMachine.CanWallRun())
        {
            stateMachine.SwitchState(typeof(PlayerWallRunState));
            wasGroundedLastFrame = stateMachine.Grounded;
            lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 3: LEDGE GRAB ----
        // En el aire, cerca de un borde demasiado alto para vault
        if (!stateMachine.Grounded && stateMachine.CanLedgeGrab())
        {
            stateMachine.SwitchState(typeof(PlayerLedgeGrabState));
            wasGroundedLastFrame = stateMachine.Grounded;
            lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
        }

        // ---- PRIORIDAD 4: SLIDE ----
        if (stateMachine.Grounded && crouchPressed)
        {
            float currentSpeed = stateMachine.CurrentHorizontalVelocity.magnitude;
            if (currentSpeed >= stateMachine.SlideMinSpeed)
            {
                stateMachine.SwitchState(typeof(PlayerSlideState));
                wasGroundedLastFrame = stateMachine.Grounded;
                lastVerticalSpeed = stateMachine.GetVerticalSpeed();
                return;
            }
        }

        // ---- PRIORIDAD 5: DASH ----
        if (!crouchHeld && sprintPressed && stateMachine.CanDash())
        {
            stateMachine.SwitchState(typeof(PlayerDashState));
            wasGroundedLastFrame = stateMachine.Grounded;
            lastVerticalSpeed = stateMachine.GetVerticalSpeed();
            return;
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
        wasGroundedLastFrame = stateMachine.Grounded;
        lastVerticalSpeed = stateMachine.GetVerticalSpeed();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
    }

    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
