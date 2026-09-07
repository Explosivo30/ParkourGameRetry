using UnityEngine;

/// <summary>
/// Estado de Dash: impulso rápido y corto en la dirección del input (o forward si no hay input).
/// </summary>
public class PlayerDashState : PlayerBaseState
{
    private float dashTimer;
    private Vector3 dashDirection;
    private float dashSpeed;

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.RegisterParkourAction(ParkourActionType.Dash);
        stateMachine.NotifyDashStarted();

        dashTimer = 0f;

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

        Vector3 incomingVelocity = stateMachine.CurrentHorizontalVelocity;
        float incomingSpeed = incomingVelocity.magnitude;
        if (incomingSpeed > 0.1f && Vector3.Dot(incomingVelocity.normalized, dashDirection) > 0.15f)
            dashDirection = Vector3.Slerp(incomingVelocity.normalized, dashDirection, 0.35f).normalized;

        dashSpeed = stateMachine.ResolveActionSpeed(
            incomingSpeed,
            stateMachine.DashForce,
            1f,
            stateMachine.DashSpeedReward
        );
        stateMachine.CurrentHorizontalVelocity = dashDirection * dashSpeed;
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();
        dashTimer += deltaTime;
        stateMachine.GroundDetection();
        stateMachine.ApplyGravity();

        Vector3 dashVelocity = dashDirection * dashSpeed;
        stateMachine.CurrentHorizontalVelocity = dashVelocity;
        stateMachine.CC.Move(dashVelocity * deltaTime);

        if (dashTimer >= stateMachine.DashDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
    }
}
