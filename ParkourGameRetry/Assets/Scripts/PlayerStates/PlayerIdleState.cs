using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        
    }
    public override void Tick()
    {
        Debug.Log("Estoy en idle");
    }
    public override void Exit()
    {
        
    }


}
