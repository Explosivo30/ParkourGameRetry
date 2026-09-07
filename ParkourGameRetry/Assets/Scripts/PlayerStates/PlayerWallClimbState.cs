using UnityEngine;

/// <summary>Trepada frontal limitada: mantener salto para subir y enlazar con el borde.</summary>
public class PlayerWallClimbState : PlayerBaseState
{
    float elapsed;
    float startHeight;
    float upwardSpeed;
    bool reachedLedge;

    public PlayerWallClimbState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        elapsed = 0f;
        startHeight = stateMachine.transform.position.y;
        upwardSpeed = stateMachine.WallClimbSpeed;
        reachedLedge = false;
        stateMachine.ConsumeWallClimb();
        stateMachine.CurrentHorizontalVelocity = Vector3.zero;
        stateMachine.SetVerticalVelocity(upwardSpeed);
        stateMachine.SubscribeInputJump();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();
        if (!stateMachine.WantsWallClimb)
        {
            StopClimbing();
            return;
        }

        if (stateMachine.CanLedgeGrab())
        {
            reachedLedge = true;
            stateMachine.SwitchState(typeof(PlayerLedgeGrabState));
            return;
        }

        float riseRemaining = stateMachine.WallClimbMaxRise - (stateMachine.transform.position.y - startHeight);
        float timeRemaining = stateMachine.WallClimbDuration - elapsed;
        if (timeRemaining <= 0f || riseRemaining <= 0.001f || !stateMachine.CheckClimbWall(out RaycastHit wall))
        {
            StopClimbing();
            return;
        }

        float step = Mathf.Min(Mathf.Max(0f, deltaTime), timeRemaining);
        float nextSpeed = Mathf.Max(0f, upwardSpeed - stateMachine.WallClimbDeceleration * step);
        float rise = Mathf.Min(riseRemaining, (upwardSpeed + nextSpeed) * 0.5f * step);
        elapsed += step;
        upwardSpeed = nextSpeed;
        Vector3 before = stateMachine.transform.position;
        CollisionFlags flags = stateMachine.CC.Move(Vector3.up * rise - wall.normal * (0.5f * step));
        stateMachine.SetVerticalVelocity(upwardSpeed);
        stateMachine.ClearJumpInputFrame();

        if ((flags & CollisionFlags.Above) != 0
            || (rise > 0.001f && stateMachine.transform.position.y - before.y < rise * 0.1f))
            StopClimbing();
    }

    void StopClimbing()
    {
        stateMachine.SetVerticalVelocity(0f);
        stateMachine.SwitchState(typeof(PlayerIdleState));
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
        if (!reachedLedge) stateMachine.NotifyLedgeReleased();
    }
}