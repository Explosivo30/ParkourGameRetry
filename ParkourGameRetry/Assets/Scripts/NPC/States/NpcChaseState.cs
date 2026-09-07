using UnityEngine;
using UnityEngine.AI;

public class NpcChaseState : State
{
    private NpcStateMachine stateMachine;
    private float recomputePathTimer = 0f;
    private float recomputePathInterval = 0.5f;

    public NpcChaseState(NpcStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public override void Enter()
    {
        if (stateMachine.Agent != null)
        {
            stateMachine.Agent.speed = stateMachine.ChaseSpeed;
        }

        if (stateMachine.Anim != null)
        {
            stateMachine.Anim.SetFloat("Speed", 1.0f); // 1.0 puede ser correr
        }
    }

    public override void Tick(float deltaTime)
    {
        // Si hay link, hacemos parkour
        if (stateMachine.Agent.isOnOffMeshLink)
        {
            stateMachine.SwitchState(typeof(NpcParkourState));
            return;
        }

        bool canSee = stateMachine.CanSeePlayer();

        if (canSee)
        {
            stateMachine.TargetLastKnownPosition = stateMachine.PlayerTarget.position;
        }
        else
        {
            // Si llegamos a la ultima posicion y ya no lo vemos, volver a patrullar
            if (!stateMachine.Agent.pathPending && stateMachine.Agent.remainingDistance < 1f)
            {
                stateMachine.SwitchState(typeof(NpcPatrolState));
                return;
            }
        }

        // Recalcular ruta hacia el jugador periodicamente
        recomputePathTimer += deltaTime;
        if (recomputePathTimer >= recomputePathInterval)
        {
            stateMachine.Agent.destination = stateMachine.TargetLastKnownPosition;
            recomputePathTimer = 0f;
        }
    }

    // El movimiento de este estado se actualiza en Tick y mediante el NavMeshAgent.
    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
    }
}
