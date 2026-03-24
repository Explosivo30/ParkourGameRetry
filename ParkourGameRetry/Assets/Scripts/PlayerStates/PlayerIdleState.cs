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
        // Either fresh press OR buffered jump that was saved in air
        bool wantsToJump = stateMachine.JumpPressed || stateMachine.JumpBufferCounter > 0f;
        bool canJump = stateMachine.Grounded || stateMachine.CoyoteTimeCounter > 0f;

        if (wantsToJump && canJump)
        {
            stateMachine.ExecuteJump();
        }
    }


    public override void FixedTick(float fixedDeltaTime)
    {

    }

    public override void Exit()
    {
        stateMachine.UnsubscribeInputJump();
    }

   
}
