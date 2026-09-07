using UnityEngine;
using UnityEngine.AI;

public class NpcStateMachine : StateMachine
{
    [Header("Componentes")]
    public NavMeshAgent Agent;
    public Animator Anim;
    public Transform PlayerTarget;

    [Header("Patrulla")]
    public Transform[] PatrolWaypoints;
    public float PatrolSpeed = 3f;

    [Header("Persecución")]
    public float ChaseSpeed = 6f;
    public float VisionRange = 15f;
    public float VisionAngle = 90f;
    public LayerMask VisionObstacleMask;

    [Header("Parkour / Cinemático")]
    public float ParkourTransitionSpeed = 10f; // Velocidad de rotación al hacer snap

    public Vector3 TargetLastKnownPosition { get; set; }

    private void Awake()
    {
        if (Agent == null) Agent = GetComponent<NavMeshAgent>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
        
        // El agente puede usar off-mesh links automáticamente
        Agent.autoTraverseOffMeshLink = false; // Queremos control manual para hacer la cinemática
    }

    private void Start()
    {
        // Añadimos estados
        AddState(new NpcPatrolState(this));
        AddState(new NpcChaseState(this));
        AddState(new NpcParkourState(this));

        SwitchState(typeof(NpcPatrolState));
    }

    public bool CanSeePlayer()
    {
        if (PlayerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) PlayerTarget = playerObj.transform;
        }
        
        if (PlayerTarget == null) return false;

        Vector3 directionToPlayer = PlayerTarget.position - transform.position;
        float distance = directionToPlayer.magnitude;

        if (distance > VisionRange) return false;

        if (Vector3.Angle(transform.forward, directionToPlayer) > VisionAngle / 2f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, distance, VisionObstacleMask))
        {
            return false;
        }

        return true;
    }
}
