using UnityEngine;

public class PlayerVaultState : PlayerBaseState
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float vaultTimer;
    private CharacterController cc;

    public PlayerVaultState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        startPos = stateMachine.transform.position;
        targetPos = stateMachine.VaultTargetPosition;
        vaultTimer = 0f;
        cc = stateMachine.GetComponent<CharacterController>();
    }

    public override void Tick(float deltaTime)
    {
        vaultTimer += deltaTime;
        float progress = vaultTimer / stateMachine.VaultDuration;

        if (progress >= 1f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

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
    }

    public override void FixedTick(float fixedDeltaTime)
    {
    }

    public override void Exit()
    {
    }
}
