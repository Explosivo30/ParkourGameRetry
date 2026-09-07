using UnityEngine;
using UnityEngine.AI;

public class NpcParkourState : State
{
    private NpcStateMachine stateMachine;
    private OffMeshLinkData linkData;
    private Vector3 startPos;
    private Vector3 endPos;
    
    private float t = 0f;
    private float duration = 1.0f; // Duración genérica si no hay ParkourLinkData

    private ParkourLinkData currentLinkComponent;

    public NpcParkourState(NpcStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public override void Enter()
    {
        if (!stateMachine.Agent.isOnOffMeshLink)
        {
            stateMachine.SwitchState(typeof(NpcChaseState));
            return;
        }

        linkData = stateMachine.Agent.currentOffMeshLinkData;
        startPos = stateMachine.Agent.transform.position;
        endPos = linkData.endPos + Vector3.up * stateMachine.Agent.baseOffset;

        // Desactivamos la actualización automática
        stateMachine.Agent.updatePosition = false;
        stateMachine.Agent.updateRotation = false;

        t = 0f;

        // Intentar leer el componente custom si existe (depende de cómo se cree el link, si es un NavMeshLink nuevo, requiere otra busqueda)
        // Por simplificar, simulamos que extraemos la info basada en el Area Type.
        int areaType = linkData.offMeshLink != null ? linkData.offMeshLink.area : NavMesh.GetAreaFromName("Walkable");
        // O con NavMesh.GetAreaFromName("NPC_WallRun") etc.

        // Determinar animacion y duracion basado en el tipo de área
        // Aquí asumimos duraciones hardcoded por ahora, luego se pueden afinar.
        duration = 1.2f;
        string animTrigger = "Vault";

        // Ejemplos (asumiendo que los enteros de Area coinciden con nombres):
        // if (areaType == NavMesh.GetAreaFromName("NPC_WallRun")) animTrigger = "WallRun";
        
        if (stateMachine.Anim != null)
        {
            stateMachine.Anim.SetTrigger(animTrigger);
        }
    }

    public override void Tick(float deltaTime)
    {
        t += deltaTime / duration;

        if (t < 1f)
        {
            // Lerp de posición
            Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
            
            // Añadir curva de altura genérica (parábola) para saltos
            float heightOffset = Mathf.Sin(t * Mathf.PI) * 1.5f; 
            newPos.y += heightOffset;

            stateMachine.Agent.transform.position = newPos;

            // Rotación hacia el final
            Vector3 dir = (endPos - startPos).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                stateMachine.Agent.transform.rotation = Quaternion.Slerp(stateMachine.Agent.transform.rotation, targetRot, deltaTime * stateMachine.ParkourTransitionSpeed);
            }
        }
        else
        {
            // Termina el link
            stateMachine.Agent.transform.position = endPos;
            stateMachine.Agent.CompleteOffMeshLink();
            
            stateMachine.Agent.updatePosition = true;
            stateMachine.Agent.updateRotation = true;

            // Volvemos a persecución
            stateMachine.SwitchState(typeof(NpcChaseState));
        }
    }

    // El movimiento de este estado se actualiza en Tick y mediante el NavMeshAgent.
    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        // Limpieza de triggers
        if (stateMachine.Anim != null)
        {
            // Reset triggers if needed
        }
    }
}
