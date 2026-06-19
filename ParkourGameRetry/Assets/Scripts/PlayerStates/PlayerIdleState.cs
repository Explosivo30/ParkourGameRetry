using UnityEngine;

/// <summary>
/// Estado principal del jugador: gestiona movimiento en suelo, aire, salto y transiciones.
/// A pesar del nombre "Idle", este estado maneja TODO el gameplay básico:
/// correr, caer, saltar, detectar vault y detectar wall-run.
/// </summary>
public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    // =====================================================================
    //  ENTER — Se suscribe a los eventos de salto al entrar al estado
    // =====================================================================
    public override void Enter()
    {
        // Suscribirse al input de salto para recibir JumpPressed/JumpHeld
        stateMachine.SubscribeInputJump();
    }

    // =====================================================================
    //  TICK — Bucle principal: se ejecuta cada frame
    // =====================================================================
    public override void Tick(float deltaTime)
    {
        // ---- ROTACIÓN DEL JUGADOR ----
        // Rota el transform del jugador en Y según el input horizontal del ratón/stick
        stateMachine.PlayerLook();

        // ---- DETECCIÓN DE SUELO ----
        // SphereCast hacia abajo para determinar si está en el suelo o en pendiente
        stateMachine.GroundDetection();

        // ---- COYOTE TIME ----
        // Permite saltar brevemente después de abandonar una plataforma
        stateMachine.TickCoyoteTime(Time.deltaTime);

        // ---- GRAVEDAD ----
        // Aplica gravedad acumulativa. En suelo: fuerza mínima. En aire: acumula.
        stateMachine.ApplyGravity();

        // ---- MOVIMIENTO HORIZONTAL ----
        // 1. GetInput() → Vector2 normalizado del WASD/stick
        // 2. GetWorldMovementDirection() → lo transforma a dirección relativa a la cámara en el mundo 3D
        // 3. PlayerHorizontalMovement() → aplica fricción y aceleración aditiva
        stateMachine.PlayerHorizontalMovement(stateMachine.GetWorldMovementDirection(stateMachine.GetInput()));

        // ---- INPUT DE SALTO ----
        // Decrementa el buffer de salto (permite saltar un poco antes de aterrizar)
        stateMachine.DecrementJumpBuffer(deltaTime);
        bool wantsToJump = stateMachine.JumpPressed || stateMachine.JumpBufferCounter > 0f;

        // ---- PRIORIDAD 1: VAULT ----
        // Si quiere saltar y hay un obstáculo vaultable enfrente, hacer vault
        if (wantsToJump && stateMachine.CanVault())
        {
            stateMachine.JumpBufferCounter = 0f;
            stateMachine.SwitchState(typeof(PlayerVaultState));
            return;
        }

        // ---- PRIORIDAD 2: WALL-RUN ----
        // Si está en el aire y hay una pared lateral con suficiente velocidad, wall-run
        // Se comprueba cada frame para que el jugador se adhiera automáticamente al
        // saltar cerca de una pared o al caer contra ella
        if (!stateMachine.Grounded && stateMachine.CanWallRun())
        {
            stateMachine.SwitchState(typeof(PlayerWallRunState));
            return;
        }

        // ---- PRIORIDAD 3: SALTO NORMAL ----
        // Solo si está en el suelo o dentro del coyote time
        bool canJump = stateMachine.Grounded || stateMachine.CoyoteTimeCounter > 0f;

        if (wantsToJump && canJump)
        {
            stateMachine.ExecuteJump();
        }

        // ---- LIMPIEZA DE INPUT ----
        // Evita que el flag JumpPressed se quede activo y cause saltos infinitos
        stateMachine.ClearJumpInputFrame();
    }

    // =====================================================================
    //  FIXED TICK — No se usa en este estado
    // =====================================================================
    public override void FixedTick(float fixedDeltaTime)
    {
        // No hay lógica de física en FixedUpdate para este estado
    }

    // =====================================================================
    //  EXIT — Se desuscribe de los eventos de salto al salir del estado
    // =====================================================================
    public override void Exit()
    {
        // Desuscribirse del input de salto para evitar leaks de eventos
        stateMachine.UnsubscribeInputJump();
    }
}
