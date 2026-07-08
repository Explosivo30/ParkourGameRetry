using UnityEngine;

/// <summary>
/// Estado de Dash: impulso rápido y corto en la dirección del input (o forward si no hay input).
/// 
/// ENTRADA: Se activa al pulsar sprint si el cooldown ha pasado.
///          Puede usarse en suelo o en el aire (Air Dash).
///
/// DURANTE: El jugador se mueve a DashForce de velocidad durante DashDuration segundos.
///
/// SALIDA:  Cuando el timer expira. La velocidad se conserva para mantener el momentum.
/// </summary>
public class PlayerDashState : PlayerBaseState
{
    private float dashTimer;
    private Vector3 dashDirection;

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction();

        // Registrar tiempo del dash para el cooldown
        stateMachine.LastDashTime = Time.time;

        dashTimer = 0f;

        // Dirección del dash: hacia donde mira el input, o forward si no hay input
        Vector2 input = stateMachine.GetInput();
        if (input.sqrMagnitude > 0.1f)
        {
            dashDirection = stateMachine.GetWorldMovementDirection(input);
        }
        else
        {
            dashDirection = stateMachine.transform.forward;
        }
        dashDirection = new Vector3(dashDirection.x, 0f, dashDirection.z).normalized;

        // Inyectar la velocidad del dash directamente en la inercia horizontal
        stateMachine.CurrentHorizontalVelocity = dashDirection * stateMachine.DashForce;
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara ----
        stateMachine.PlayerLook();

        // ---- Timer ----
        dashTimer += deltaTime;

        // ---- Gravedad (se sigue aplicando para que el dash no flote) ----
        stateMachine.ApplyGravity();

        // ---- Movimiento del dash ----
        Vector3 dashVelocity = dashDirection * stateMachine.DashForce;
        stateMachine.CC.Move(dashVelocity * deltaTime);

        // ---- Salida cuando se acaba la duración ----
        if (dashTimer >= stateMachine.DashDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        // La velocidad se conserva (no reseteamos _currentHorizontalVelocity)
        // para que el jugador salga del dash con todo el momentum
    }
}
