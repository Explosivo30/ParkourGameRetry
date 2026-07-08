using UnityEngine;

/// <summary>
/// Estado de Landing Roll: voltereta al aterrizar de una caída alta.
/// 
/// ENTRADA: Se activa cuando el jugador aterriza de una caída alta (> RollMinFallSpeed)
///          y pulsa crouch justo al tocar el suelo.
///
/// DURANTE: El jugador rueda hacia adelante. La velocidad se MULTIPLICA por RollSpeedBoost
///          (recompensa por hacer el roll). La cámara rota en Z simulando la voltereta.
///
/// SALIDA:  Cuando el timer expira. El jugador sale con más velocidad de la que tendría
///          si hubiera aterrizado sin roll (castigo por no rodar = HardLandingPenalty).
/// </summary>
public class PlayerLandingRollState : PlayerBaseState
{
    private float rollTimer;
    private Vector3 rollDirection;

    public PlayerLandingRollState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction();

        // Suscribirse al input de salto (por si quiere saltar durante el roll)
        stateMachine.SubscribeInputJump();

        rollTimer = 0f;

        // Dirección del roll: la dirección en la que se movía, o forward si no hay velocidad
        rollDirection = stateMachine.CurrentHorizontalVelocity.normalized;
        if (rollDirection.sqrMagnitude < 0.01f)
            rollDirection = stateMachine.transform.forward;

        // Multiplicar la velocidad por el boost de roll (recompensa)
        stateMachine.CurrentHorizontalVelocity *= stateMachine.RollSpeedBoost;

        // Iniciar la rotación de cámara simulando la voltereta
        stateMachine.SetCameraTilt(stateMachine.RollCameraAngle);
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
        rollTimer += deltaTime;
        float progress = rollTimer / stateMachine.RollDuration;

        // ---- Animación de cámara: el tilt vuelve a 0 gradualmente ----
        float tiltProgress = 1f - progress;
        stateMachine.SetCameraTilt(stateMachine.RollCameraAngle * tiltProgress);

        // ---- Movimiento en la dirección del roll ----
        Vector3 moveVelocity = rollDirection * stateMachine.CurrentHorizontalVelocity.magnitude;
        stateMachine.CC.Move(moveVelocity * deltaTime);

        // ---- Salto durante el roll (cancelar roll y saltar) ----
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();
            stateMachine.ExecuteJump();
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- Fin del roll ----
        if (progress >= 1f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        // Resetear tilt de cámara
        stateMachine.SetCameraTilt(0f);

        // Desuscribirse del input de salto
        stateMachine.UnsubscribeInputJump();
    }
}
