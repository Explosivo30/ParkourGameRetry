using UnityEngine;

public class PlayerVaultState : PlayerBaseState
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float vaultTimer;
    private CharacterController cc;
    private Vector3 exitDirection;
    private float entrySpeed;

    public PlayerVaultState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.RegisterParkourAction(ParkourActionType.Vault);

        startPos = stateMachine.transform.position;
        targetPos = stateMachine.VaultTargetPosition;
        vaultTimer = 0f;
        cc = stateMachine.GetComponent<CharacterController>();
        entrySpeed = stateMachine.CurrentHorizontalVelocity.magnitude;
        exitDirection = entrySpeed > 0.1f
            ? stateMachine.CurrentHorizontalVelocity.normalized
            : stateMachine.transform.forward;
        exitDirection.y = 0f;
        exitDirection.Normalize();

        stateMachine.ResetVerticalVelocity();
        stateMachine.ClearJumpInputFrame();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PlayerLook();

        vaultTimer += deltaTime;
        float progress = Mathf.Clamp01(vaultTimer / stateMachine.VaultDuration);

        // Apply horizontal interpolation
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(startPos.x, 0, startPos.z), 
            new Vector3(targetPos.x, 0, targetPos.z), 
            progress
        );
        
        // Apply vertical interpolation with an arc
        float verticalY = Mathf.Lerp(startPos.y, targetPos.y, progress);
        float arcProgress = progress * Mathf.PI;
        verticalY += Mathf.Sin(arcProgress) * stateMachine.VaultArcHeight;

        Vector3 newPos = new Vector3(horizontalPos.x, verticalY, horizontalPos.z);
        
        // Move the CharacterController to the new position
        Vector3 displacement = newPos - stateMachine.transform.position;
        cc.Move(displacement);

        if (progress >= 1f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }
    }

    public override void FixedTick(float fixedDeltaTime)
    {
    }

    public override void Exit()
    {
        float exitSpeed = stateMachine.ResolveActionSpeed(
            entrySpeed,
            stateMachine.VaultExitSpeed,
            stateMachine.VaultMomentumRetention,
            stateMachine.VaultSpeedReward
        );
        stateMachine.CurrentHorizontalVelocity = exitDirection * exitSpeed;
        stateMachine.SetVerticalVelocity(stateMachine.VaultExitUpForce);
        stateMachine.ResetVaultPhysics();
    }
}
