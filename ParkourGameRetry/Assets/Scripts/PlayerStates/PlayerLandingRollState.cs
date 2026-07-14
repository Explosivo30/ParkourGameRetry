using UnityEngine;

/// <summary>
/// Estado de Landing Roll: voltereta al aterrizar de una caída alta.
/// 
/// ENTRADA: Se activa cuando el jugador aterriza de una caída alta (> RollMinFallSpeed)
///          y pulsa crouch justo al tocar el suelo.
///
/// DURANTE: El jugador rueda hacia adelante conservando el momentum ganado.
///          La cámara completa una vuelta frontal de 360 grados.
///
/// SALIDA:  Cuando el timer expira. El jugador sale con más velocidad de la que tendría
///          si hubiera aterrizado sin roll (castigo por no rodar = HardLandingPenalty).
/// </summary>
public class PlayerLandingRollState : PlayerBaseState
{
    private float rollTimer;
    private Vector3 rollDirection;
    private float rollSpeed;

    public PlayerLandingRollState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction(ParkourActionType.LandingRoll);

        rollTimer = 0f;

        // Dirección del roll: la dirección en la que se movía, o forward si no hay velocidad
        rollDirection = stateMachine.CurrentHorizontalVelocity.normalized;
        if (rollDirection.sqrMagnitude < 0.01f)
            rollDirection = stateMachine.transform.forward;

        rollSpeed = stateMachine.ResolveActionSpeed(
            stateMachine.CurrentHorizontalVelocity.magnitude,
            stateMachine.RollMinimumSpeed,
            1f,
            stateMachine.RollSpeedBoost
        );
        stateMachine.CurrentHorizontalVelocity = rollDirection * rollSpeed;

        stateMachine.SetCameraTilt(0f);
        stateMachine.SetCameraRollPitch(0f);
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
        float progress = Mathf.Clamp01(rollTimer / stateMachine.RollDuration);

        // Vuelta frontal completa. SmoothStep evita un comienzo o final seco,
        // y la curva senoidal baja la cabeza al pasar cerca del suelo.
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        stateMachine.SetCameraRollPitch(stateMachine.RollCameraAngle * easedProgress);
        stateMachine.SetCameraHeightOffset(
            -stateMachine.RollCameraDropAmount * Mathf.Sin(progress * Mathf.PI),
            20f
        );

        // ---- Movimiento en la dirección del roll ----
        Vector3 moveVelocity = rollDirection * rollSpeed;
        stateMachine.CurrentHorizontalVelocity = moveVelocity;
        stateMachine.CC.Move(moveVelocity * deltaTime);

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
        // 360 y 0 representan la misma orientación, así que no hay salto visual.
        stateMachine.SetCameraRollPitch(0f);
        stateMachine.SetCameraTilt(0f);
        stateMachine.SetCameraHeightOffset(0f, 12f);
    }
}
