using UnityEngine;

/// <summary>
/// Estado de Slide: el jugador se desliza por el suelo con las piernas por delante.
/// </summary>
public class PlayerSlideState : PlayerBaseState
{
    private float slideTimer;
    private Vector3 slideDirection;
    private float currentSlideSpeed;

    public PlayerSlideState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.RegisterParkourAction(ParkourActionType.Slide);
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();
        stateMachine.ShrinkCollider();

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

        stateMachine.SetCameraHeightOffset(
            -stateMachine.SlideCameraDropAmount,
            stateMachine.SlideCameraDropSpeed
        );
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();
        stateMachine.GroundDetection();

        if (!stateMachine.Grounded)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ApplyGravity();

        slideTimer += deltaTime;
        currentSlideSpeed -= stateMachine.SlideFriction * deltaTime;

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

        float groundDot = Vector3.Dot(stateMachine.GroundNormal, Vector3.up);
        if (groundDot < 0.95f && stateMachine.Grounded)
        {
            Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, stateMachine.GroundNormal).normalized;
            float slopeAlignment = Vector3.Dot(slideDirection, slopeDown);

            if (slopeAlignment > 0.1f)
            {
                currentSlideSpeed += stateMachine.SlideSlopeBoost * slopeAlignment * deltaTime;
            }
        }

        Vector3 moveVelocity = slideDirection * currentSlideSpeed;
        stateMachine.CurrentHorizontalVelocity = moveVelocity;
        stateMachine.CC.Move(moveVelocity * deltaTime);

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

        bool crouchStillHeld = stateMachine.Controls != null && stateMachine.Controls.CrouchHeld;
        if (!crouchStillHeld)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        if (slideTimer >= stateMachine.SlideDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

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
        stateMachine.RestoreCollider();
        stateMachine.SetCameraHeightOffset(0f, stateMachine.SlideCameraDropSpeed);
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
