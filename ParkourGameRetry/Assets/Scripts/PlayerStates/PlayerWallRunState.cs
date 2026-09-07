using UnityEngine;

/// <summary>
/// Estado de Wall-Run: el jugador corre a lo largo de una pared lateral.
/// </summary>
public class PlayerWallRunState : PlayerBaseState
{
    private float wallRunTimer;
    private bool isWallRight;
    private Vector3 wallNormal;
    private Vector3 wallRunDirection;
    private float verticalVelocity;
    private float currentWallRunSpeed;
    private bool exitedByJump;

    public PlayerWallRunState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.RegisterParkourAction(ParkourActionType.WallRun);

        wallRunTimer = 0f;
        verticalVelocity = 0f;
        exitedByJump = false;
        stateMachine.ResetVerticalVelocity();

        stateMachine.SubscribeInputJump();

        if (stateMachine.CheckWallRight(out RaycastHit rightHit))
        {
            isWallRight = true;
            wallNormal = rightHit.normal;
        }
        else if (stateMachine.CheckWallLeft(out RaycastHit leftHit))
        {
            isWallRight = false;
            wallNormal = leftHit.normal;
        }

        wallRunDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;

        Vector3 incomingVelocity = stateMachine.CurrentHorizontalVelocity;
        if (Vector3.Dot(wallRunDirection, incomingVelocity) < 0f)
        {
            wallRunDirection = -wallRunDirection;
        }

        currentWallRunSpeed = stateMachine.ResolveActionSpeed(
            incomingVelocity.magnitude,
            stateMachine.WallRunSpeed,
            stateMachine.WallRunMomentumRetention,
            stateMachine.WallRunSpeedReward
        );
        stateMachine.CurrentHorizontalVelocity = wallRunDirection * currentWallRunSpeed;

        float tiltAngle = isWallRight
            ?  stateMachine.WallRunCameraTiltAngle
            : -stateMachine.WallRunCameraTiltAngle;
        stateMachine.SetCameraTilt(tiltAngle);
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();
        stateMachine.GroundDetection();
        if (stateMachine.Grounded)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        wallRunTimer += deltaTime;

        if (wallRunTimer >= stateMachine.WallRunMaxDuration)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        Vector2 input = stateMachine.GetInput();
        if (input.sqrMagnitude < 0.1f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

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

        wallNormal = currentHit.normal;

        wallRunDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallRunDirection, stateMachine.CurrentHorizontalVelocity) < 0f)
        {
            wallRunDirection = -wallRunDirection;
        }

        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();
            stateMachine.JumpBufferCounter = 0f;

            // Capturar la dirección de input del jugador para Air Control
            Vector3 playerInputDir = stateMachine.GetWorldMovementDirection(input);

            stateMachine.ApplyWallRunExitImpulse(
                wallNormal,
                wallRunDirection,
                currentWallRunSpeed,
                playerInputDir
            );

            exitedByJump = true;
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        Vector3 moveVelocity = wallRunDirection * currentWallRunSpeed;
        stateMachine.CurrentHorizontalVelocity = moveVelocity;

        verticalVelocity -= stateMachine.GravityForce * stateMachine.WallRunGravityScale * deltaTime;
        Vector3 finalVelocity = moveVelocity + Vector3.up * verticalVelocity;
        stateMachine.CC.Move(finalVelocity * deltaTime);
        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        stateMachine.SetCameraTilt(0f);
        stateMachine.UnsubscribeInputJump();
        if (!exitedByJump)
            stateMachine.SetVerticalVelocity(verticalVelocity);
        stateMachine.NotifyWallRunEnded();
    }
}
