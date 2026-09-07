using UnityEngine;

/// <summary>
/// Agarre y subida de bordes. Mantener espacio enlaza salto, trepada y mantle.
/// Espacio con dirección lateral/atrás salta desde el borde; crouch permite soltarse.
/// </summary>
public class PlayerLedgeGrabState : PlayerBaseState
{
    bool isClimbing;
    float climbTimer;
    Vector3 climbStartPos;
    Vector3 climbLiftPos;
    Vector3 entryHorizontalVelocity;

    public PlayerLedgeGrabState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.RegisterParkourAction(ParkourActionType.LedgeGrab);
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();
        stateMachine.ConsumeWallClimb();
        entryHorizontalVelocity = stateMachine.CurrentHorizontalVelocity;
        stateMachine.CurrentHorizontalVelocity = Vector3.zero;
        stateMachine.SetVerticalVelocity(0f);
        isClimbing = false;
        climbTimer = 0f;

        stateMachine.CC.Move(stateMachine.LedgeGrabPosition - stateMachine.transform.position);
        if (stateMachine.JumpHeld && WantsToClimb()) BeginClimb();
    }

    bool WantsToClimb()
    {
        Vector2 input = stateMachine.GetInput();
        return input.sqrMagnitude < 0.1f || input.y > 0.1f;
    }

    void BeginClimb()
    {
        if (!stateMachine.CanStartLedgeClimb()) return;
        stateMachine.ClearJumpInputFrame();
        stateMachine.JumpBufferCounter = 0f;
        isClimbing = true;
        climbTimer = 0f;
        climbStartPos = stateMachine.transform.position;
        climbLiftPos = new Vector3(climbStartPos.x, stateMachine.LedgeClimbTarget.y, climbStartPos.z);
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();
        if (stateMachine.Controls != null && stateMachine.Controls.CrouchHeld)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        if (isClimbing)
        {
            climbTimer += deltaTime;
            float progress = Mathf.Clamp01(climbTimer / Mathf.Max(0.05f, stateMachine.LedgeClimbDuration));
            const float liftPhaseEnd = 0.62f;
            Vector3 nextPosition = progress < liftPhaseEnd
                ? Vector3.Lerp(climbStartPos, climbLiftPos, Mathf.SmoothStep(0f, 1f, progress / liftPhaseEnd))
                : Vector3.Lerp(climbLiftPos, stateMachine.LedgeClimbTarget,
                    Mathf.SmoothStep(0f, 1f, (progress - liftPhaseEnd) / (1f - liftPhaseEnd)));

            // Mantener las colisiones también durante el mantle y ante obstáculos móviles.
            if (!stateMachine.CanMoveClimbCapsule(stateMachine.transform.position, nextPosition))
            {
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }
            stateMachine.CC.Move(nextPosition - stateMachine.transform.position);
            if (Vector3.Distance(stateMachine.transform.position, nextPosition) > 0.1f)
            {
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }
            if (progress >= 1f)
            {
                Vector3 exitDirection = Vector3.ProjectOnPlane(-stateMachine.LedgeWallNormal, Vector3.up).normalized;
                float exitSpeed = stateMachine.GetInput().y > 0.1f
                    ? stateMachine.ResolveActionSpeed(entryHorizontalVelocity.magnitude, 3.5f, 0.65f, 0f)
                    : 0f;
                stateMachine.CurrentHorizontalVelocity = exitDirection * exitSpeed;
                stateMachine.SwitchState(typeof(PlayerIdleState));
            }
            return;
        }

        if ((stateMachine.JumpHeld || stateMachine.JumpPressed) && WantsToClimb())
        {
            BeginClimb();
        }
        else if (stateMachine.JumpPressed)
        {
            Vector3 jumpDir = stateMachine.GetWorldMovementDirection(stateMachine.GetInput());
            jumpDir = Vector3.ProjectOnPlane(jumpDir, Vector3.up).normalized;
            if (entryHorizontalVelocity.magnitude > 0.1f
                && Vector3.Dot(entryHorizontalVelocity.normalized, jumpDir) > 0f)
                jumpDir = Vector3.Slerp(entryHorizontalVelocity.normalized, jumpDir, 0.6f).normalized;

            float exitSpeed = stateMachine.ResolveActionSpeed(entryHorizontalVelocity.magnitude,
                stateMachine.LedgeJumpHorizontalForce, 0.5f, 0f);
            stateMachine.CurrentHorizontalVelocity = jumpDir * exitSpeed;
            stateMachine.ExecuteJump(stateMachine.LedgeJumpForce);
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }
        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
        stateMachine.NotifyLedgeReleased();
    }
}