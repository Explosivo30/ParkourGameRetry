using UnityEngine;

/// <summary>
/// Estado de Grapple: el jugador viaja hacia un punto de enganche con el gancho.
/// 
/// ENTRADA: Se activa cuando el jugador dispara el gancho hacia un punto válido.
///
/// DURANTE: El jugador se mueve en línea recta hacia GrappleTarget a GrappleSpeed.
///          La gravedad se desactiva durante el viaje.
///
/// SALIDA:  Cuando llega al punto de destino (con boost de velocidad) o cancela con salto.
///          La inercia al llegar se conserva multiplicada por GrappleExitBoost.
/// </summary>
public class PlayerGrappleState : PlayerBaseState
{
    private Vector3 travelDirection;
    private float entrySpeed;
    private float travelSpeed;
    private float previousDistance;
    private float blockedTimer;
    private bool exitedByJump;
    private bool reachedAnchor;

    public PlayerGrappleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction(ParkourActionType.Grapple);

        // Suscribirse al input de salto para poder cancelar el grapple
        stateMachine.SubscribeInputJump();

        // Calcular dirección de viaje hacia el punto de enganche
        travelDirection = (stateMachine.GrappleTarget - stateMachine.transform.position).normalized;
        entrySpeed = stateMachine.CurrentHorizontalVelocity.magnitude;
        travelSpeed = Mathf.Max(entrySpeed, stateMachine.GrappleSpeed);
        previousDistance = Vector3.Distance(stateMachine.transform.position, stateMachine.GrappleTarget);
        blockedTimer = 0f;
        exitedByJump = false;
        reachedAnchor = false;
        stateMachine.ResetVerticalVelocity();
        stateMachine.SetGrappleActive(true);
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // Recalcular dirección cada frame (por si el jugador gira ligeramente)
        travelDirection = (stateMachine.GrappleTarget - stateMachine.transform.position).normalized;

        // ---- Movimiento hacia el punto de enganche ----
        Vector3 moveVelocity = travelDirection * travelSpeed;
        stateMachine.CurrentHorizontalVelocity = Vector3.ProjectOnPlane(moveVelocity, Vector3.up);
        stateMachine.CC.Move(moveVelocity * deltaTime);

        // ---- CONDICIÓN DE SALIDA 1: Llegamos al destino ----
        float distanceToTarget = Vector3.Distance(stateMachine.transform.position, stateMachine.GrappleTarget);
        if (distanceToTarget < stateMachine.GrappleStopDistance)
        {
            Vector3 exitDirection = Vector3.ProjectOnPlane(travelDirection, Vector3.up).normalized;
            if (exitDirection.sqrMagnitude < 0.01f)
                exitDirection = stateMachine.transform.forward;
            float exitSpeed = stateMachine.ResolveActionSpeed(
                travelSpeed,
                stateMachine.GrappleSpeed,
                1f,
                stateMachine.GrappleSpeed * (stateMachine.GrappleExitBoost - 1f)
            );
            stateMachine.CurrentHorizontalVelocity = exitDirection * exitSpeed;
            stateMachine.SetVerticalVelocity(Mathf.Clamp(travelDirection.y * travelSpeed * 0.65f, 0f, 8f));

            reachedAnchor = true;
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // Si una pared bloquea el CharacterController, soltar el cable en vez
        // de dejar al jugador vibrando indefinidamente contra la geometría.
        if (distanceToTarget >= previousDistance - 0.01f)
            blockedTimer += deltaTime;
        else
            blockedTimer = 0f;
        previousDistance = distanceToTarget;

        if (blockedTimer > 0.25f)
        {
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        // ---- CONDICIÓN DE SALIDA 2: Cancelar con salto ----
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();

            // Conservar la velocidad actual como inercia + un salto vertical
            Vector3 exitDirection = Vector3.ProjectOnPlane(travelDirection, Vector3.up).normalized;
            if (exitDirection.sqrMagnitude < 0.01f)
                exitDirection = stateMachine.transform.forward;
            float exitSpeed = stateMachine.ResolveActionSpeed(
                travelSpeed,
                stateMachine.GrappleSpeed,
                1f,
                0f
            );
            stateMachine.CurrentHorizontalVelocity = exitDirection * exitSpeed;
            stateMachine.ExecuteJump();

            exitedByJump = true;
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        if (!exitedByJump && !reachedAnchor)
            stateMachine.SetVerticalVelocity(travelDirection.y * travelSpeed * 0.5f);

        stateMachine.SetGrappleActive(false);

        // Desuscribirse del input de salto
        stateMachine.UnsubscribeInputJump();
    }
}
