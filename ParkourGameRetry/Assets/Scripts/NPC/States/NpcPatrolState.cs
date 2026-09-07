using UnityEngine;
using UnityEngine.AI;

public class NpcPatrolState : State
{
    private NpcStateMachine stateMachine;
    private int currentWaypointIndex;

    public NpcPatrolState(NpcStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public override void Enter()
    {
        if (stateMachine.Agent != null)
        {
            stateMachine.Agent.speed = stateMachine.PatrolSpeed;
            SetNextWaypoint();
        }
        
        if (stateMachine.Anim != null)
        {
            stateMachine.Anim.SetFloat("Speed", 0.5f); // 0.5 puede ser caminar
        }
    }

    public override void Tick(float deltaTime)
    {
        // Detectar si vemos al jugador
        if (stateMachine.CanSeePlayer())
        {
            stateMachine.SwitchState(typeof(NpcChaseState));
            return;
        }

        // Detectar OffMeshLink (Parkour)
        if (stateMachine.Agent.isOnOffMeshLink)
        {
            stateMachine.SwitchState(typeof(NpcParkourState));
            return;
        }

        // Lógica de patrulla
        if (!stateMachine.Agent.pathPending && stateMachine.Agent.remainingDistance < 0.5f)
        {
            SetNextWaypoint();
        }
    }

    // El movimiento de este estado se actualiza en Tick y mediante el NavMeshAgent.
    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
    }

    private void SetNextWaypoint()
    {
        if (stateMachine.PatrolWaypoints == null || stateMachine.PatrolWaypoints.Length == 0)
            return;

        stateMachine.Agent.destination = stateMachine.PatrolWaypoints[currentWaypointIndex].position;
        currentWaypointIndex = (currentWaypointIndex + 1) % stateMachine.PatrolWaypoints.Length;
    }
}
