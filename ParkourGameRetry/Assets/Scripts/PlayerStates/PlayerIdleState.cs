using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.SubscribeInputJump();


    }
    public override void Tick(float deltaTime)
    {
        Debug.Log("Estoy en idle");
        stateMachine.PlayerLook();
        stateMachine.GroundDetection();
        stateMachine.TickCoyoteTime(Time.deltaTime);

      

        stateMachine.ApplyGravity();
        stateMachine.PlayerHorizontalMovement(stateMachine.CameraOritentedMovement(stateMachine.GetInput()));

        // --- Jump input ---
        stateMachine.DecrementJumpBuffer(deltaTime);
        bool wantsToJump = stateMachine.JumpPressed || stateMachine.JumpBufferCounter > 0f;

        if (wantsToJump && stateMachine.CanVault())
        {
            stateMachine.JumpBufferCounter = 0f;
            stateMachine.SwitchState(typeof(PlayerVaultState));
            return;
        }

        bool canJump = stateMachine.Grounded || stateMachine.CoyoteTimeCounter > 0f;

        if (wantsToJump && canJump)
        {
            stateMachine.ExecuteJump();
        }

        // Limpiar el input de salto para que no se quede guardado para saltos futuros infinitos
        stateMachine.ClearJumpInputFrame();
    }


    public override void FixedTick(float fixedDeltaTime)
    {

    }

    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
    }

   
}
