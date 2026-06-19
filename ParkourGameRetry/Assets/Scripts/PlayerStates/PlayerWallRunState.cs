using UnityEngine;

/// <summary>
/// Estado de Wall-Run: el jugador corre a lo largo de una pared lateral.
/// 
/// ENTRADA: Se activa cuando el jugador está en el aire, tiene suficiente velocidad
///          horizontal, y hay una pared a su izquierda o derecha (detectada por raycast).
///
/// DURANTE: El jugador se mueve a lo largo de la pared con gravedad reducida.
///          La cámara se inclina en el eje Z (tilt) hacia el lado de la pared.
///
/// SALIDA:  Por salto (con impulso), fin de pared (raycast falla), duración máxima,
///          o soltar input de movimiento. La cámara vuelve suavemente a Z=0.
/// </summary>
public class PlayerWallRunState : PlayerBaseState
{
    // ---- Estado interno del wall-run ----
    private float wallRunTimer;           // Temporizador de duración del wall-run
    private bool isWallRight;             // true = pared a la derecha, false = pared a la izquierda
    private Vector3 wallNormal;           // Normal de la pared (apunta hacia fuera)
    private Vector3 wallRunDirection;     // Dirección de movimiento a lo largo de la pared
    private float verticalVelocity;       // Velocidad vertical (gravedad reducida)
    private bool exitedByJump;            // Flag para saber si se salió pulsando salto

    public PlayerWallRunState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    // =====================================================================
    //  ENTER — Se ejecuta al iniciar el wall-run
    // =====================================================================
    public override void Enter()
    {
        // Resetear temporizador y velocidad vertical al entrar
        wallRunTimer = 0f;
        verticalVelocity = 0f;
        exitedByJump = false;

        // Suscribirse al input de salto para poder saltar desde la pared
        stateMachine.SubscribeInputJump();

        // ---- Detectar en qué lado está la pared ----
        // Se prioriza la derecha; si no hay pared a la derecha, se busca a la izquierda
        if (stateMachine.CheckWallRight(out RaycastHit rightHit))
        {
            isWallRight = true;
            wallNormal = rightHit.normal;  // Normal apuntando HACIA el jugador (fuera de la pared)
        }
        else if (stateMachine.CheckWallLeft(out RaycastHit leftHit))
        {
            isWallRight = false;
            wallNormal = leftHit.normal;   // Normal apuntando HACIA el jugador (fuera de la pared)
        }

        // ---- Calcular dirección de movimiento a lo largo de la pared ----
        // Cross(wallNormal, Vector3.up) da un vector perpendicular a la pared y al suelo,
        // es decir, la dirección "adelante" a lo largo de la pared.
        wallRunDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Asegurar que la dirección de wall-run va en el mismo sentido que el jugador.
        // Si el jugador corre en sentido opuesto, invertimos la dirección.
        if (Vector3.Dot(wallRunDirection, stateMachine.transform.forward) < 0f)
        {
            wallRunDirection = -wallRunDirection;
        }

        // ---- Activar tilt de cámara ----
        // Pared a la derecha → tilt negativo (cámara inclina hacia la izquierda)
        // Pared a la izquierda → tilt positivo (cámara inclina hacia la derecha)
        float tiltAngle = isWallRight
            ? -stateMachine.WallRunCameraTiltAngle
            :  stateMachine.WallRunCameraTiltAngle;
        stateMachine.SetCameraTilt(tiltAngle);
    }

    // =====================================================================
    //  TICK — Se ejecuta cada frame mientras dure el wall-run
    // =====================================================================
    public override void Tick(float deltaTime)
    {
        // ---- Mantener el control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // ---- Incrementar temporizador ----
        wallRunTimer += deltaTime;

        // ---- CONDICIÓN DE SALIDA 1: Duración máxima agotada ----
        // El wall-run no puede durar más de WallRunMaxDuration segundos
        if (wallRunTimer >= stateMachine.WallRunMaxDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- CONDICIÓN DE SALIDA 2: No hay input de movimiento ----
        // Si el jugador suelta el stick/WASD, se despega de la pared
        Vector2 input = stateMachine.GetInput();
        if (input.sqrMagnitude < 0.1f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- CONDICIÓN DE SALIDA 3: Ya no hay pared ----
        // Si el raycast lateral ya no detecta pared, el jugador llegó al final
        bool wallStillExists;
        RaycastHit currentHit;
        if (isWallRight)
        {
            wallStillExists = stateMachine.CheckWallRight(out currentHit);
        }
        else
        {
            wallStillExists = stateMachine.CheckWallLeft(out currentHit);
        }

        if (!wallStillExists)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // Actualizar la normal de la pared (la pared puede curvarse)
        wallNormal = currentHit.normal;

        // Recalcular dirección de wall-run por si la pared curva
        wallRunDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallRunDirection, stateMachine.transform.forward) < 0f)
        {
            wallRunDirection = -wallRunDirection;
        }

        // ---- CONDICIÓN DE SALIDA 4: Salto desde la pared ----
        // Al pulsar salto, se aplica un impulso de salida (lateral + vertical)
        if (stateMachine.JumpPressed)
        {
            exitedByJump = true;
            stateMachine.ClearJumpInputFrame();
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- MOVIMIENTO A LO LARGO DE LA PARED ----
        // Se mueve en wallRunDirection a WallRunSpeed
        Vector3 moveVelocity = wallRunDirection * stateMachine.WallRunSpeed;

        // ---- GRAVEDAD REDUCIDA ----
        // La gravedad se aplica multiplicada por WallRunGravityScale para que
        // el jugador "flote" parcialmente mientras corre por la pared
        verticalVelocity -= stateMachine.GravityForce * stateMachine.WallRunGravityScale * deltaTime;

        // Combinar movimiento horizontal (a lo largo de la pared) + vertical (gravedad reducida)
        Vector3 finalVelocity = moveVelocity + Vector3.up * verticalVelocity;

        // Aplicar movimiento al CharacterController
        stateMachine.CC.Move(finalVelocity * deltaTime);

        // Limpiar el flag de salto para que no se acumule
        stateMachine.ClearJumpInputFrame();
    }

    // =====================================================================
    //  FIXED TICK — No se usa en este estado
    // =====================================================================
    public override void FixedTick(float fixedDeltaTime)
    {
        // El wall-run no necesita lógica en FixedUpdate
    }

    // =====================================================================
    //  EXIT — Se ejecuta al salir del wall-run (por cualquier motivo)
    // =====================================================================
    public override void Exit()
    {
        // ---- Resetear el tilt de cámara ----
        // La cámara vuelve suavemente a Z=0 gracias al lerp en UpdateCameraTilt()
        stateMachine.SetCameraTilt(0f);

        // ---- Desuscribirse del input de salto ----
        stateMachine.UnsubscribeInputJump();

        // ---- Aplicar impulso de salida si se salió por salto ----
        // El impulso combina fuerza lateral (alejándose de la pared) + fuerza vertical
        if (exitedByJump)
        {
            stateMachine.ApplyWallRunExitImpulse(wallNormal);
        }
    }
}
