using UnityEngine;

/// <summary>
/// Estado de Ledge Grab: el jugador se agarra a un borde demasiado alto para vault.
/// 
/// ENTRADA: Se activa cuando el jugador está en el aire y detecta un borde
///          a una altura superior a la del vault pero dentro del rango de grab.
///
/// DURANTE: El jugador queda suspendido del borde. Puede:
///          - Pulsar salto sin dirección → subir al borde (climb)
///          - Pulsar salto con dirección → saltar en esa dirección desde el borde
///          - Pulsar crouch → soltarse y caer
///
/// SALIDA:  Por climb, salto direccional, o soltar (crouch).
/// </summary>
public class PlayerLedgeGrabState : PlayerBaseState
{
    private bool isClimbing;
    private float climbTimer;
    private Vector3 climbStartPos;
    private Vector3 climbLiftPos;
    private Vector3 entryHorizontalVelocity;
    private bool controllerDisabledForClimb;

    public PlayerLedgeGrabState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction(ParkourActionType.LedgeGrab);

        // Suscribirse a inputs
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();

        entryHorizontalVelocity = stateMachine.CurrentHorizontalVelocity;

        // Anular velocidad durante el agarre, conservando una reserva para
        // el salto de salida.
        stateMachine.CurrentHorizontalVelocity = Vector3.zero;
        stateMachine.ResetVerticalVelocity();

        isClimbing = false;
        climbTimer = 0f;
        controllerDisabledForClimb = false;
        if (!stateMachine.CC.enabled)
            stateMachine.CC.enabled = true;

        // Mover al jugador a la posición de agarre
        Vector3 displacement = stateMachine.LedgeGrabPosition - stateMachine.transform.position;
        stateMachine.CC.Move(displacement);

        climbStartPos = stateMachine.transform.position;
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // ---- Si estamos en proceso de subir (climb) ----
        if (isClimbing)
        {
            climbTimer += deltaTime;
            float progress = Mathf.Clamp01(climbTimer / stateMachine.LedgeClimbDuration);

            // Primero subir por fuera de la pared y después avanzar sobre la
            // superficie. El movimiento diagonal anterior chocaba con la esquina.
            Vector3 targetPos = stateMachine.LedgeClimbTarget;
            Vector3 newPos;
            const float liftPhaseEnd = 0.62f;
            if (progress < liftPhaseEnd)
            {
                float liftProgress = Mathf.SmoothStep(0f, 1f, progress / liftPhaseEnd);
                newPos = Vector3.Lerp(climbStartPos, climbLiftPos, liftProgress);
            }
            else
            {
                float forwardProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - liftPhaseEnd) / (1f - liftPhaseEnd)
                );
                newPos = Vector3.Lerp(climbLiftPos, targetPos, forwardProgress);
            }

            // Durante el mantle el CharacterController no debe resolver la
            // esquina como una pared; la trayectoria ya termina encima del suelo.
            stateMachine.transform.position = newPos;

            if (progress >= 1f)
            {
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }
            return;
        }

        // ---- SALTO DESDE EL BORDE ----
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();

            Vector2 input = stateMachine.GetInput();
            Vector3 jumpDir = input.sqrMagnitude > 0.1f
                ? stateMachine.GetWorldMovementDirection(input)
                : Vector3.zero;
            jumpDir.y = 0f;
            if (jumpDir.sqrMagnitude > 0.01f)
                jumpDir.Normalize();

            // La intención de climb se lee directamente del stick/WASD. Usar
            // la dirección mundial de cámara hacía que W pudiera convertirse
            // en salto lateral al mirar un poco de lado y chocara con el borde.
            bool hasDirectionalInput = input.sqrMagnitude > 0.1f;
            bool wantsToClimb = !hasDirectionalInput || input.y > 0.1f;

            if (!wantsToClimb)
            {
                // Salto direccional: impulso en la dirección del input + arriba
                if (entryHorizontalVelocity.magnitude > 0.1f
                    && Vector3.Dot(entryHorizontalVelocity.normalized, jumpDir) > 0f)
                {
                    jumpDir = Vector3.Slerp(
                        entryHorizontalVelocity.normalized,
                        jumpDir,
                        0.6f
                    ).normalized;
                }

                float exitSpeed = stateMachine.ResolveActionSpeed(
                    entryHorizontalVelocity.magnitude,
                    stateMachine.LedgeJumpHorizontalForce,
                    0.5f,
                    0f
                );
                stateMachine.CurrentHorizontalVelocity = jumpDir * exitSpeed;
                stateMachine.ExecuteJump(stateMachine.LedgeJumpForce);
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }
            else
            {
                // Espacio, o W + Espacio: subir al borde.
                isClimbing = true;
                climbTimer = 0f;
                climbStartPos = stateMachine.transform.position;
                climbLiftPos = new Vector3(
                    climbStartPos.x,
                    stateMachine.LedgeClimbTarget.y,
                    climbStartPos.z
                );
                stateMachine.CC.enabled = false;
                controllerDisabledForClimb = true;
                return;
            }
        }

        // ---- SOLTAR (CROUCH) ----
        if (stateMachine.CrouchPressed)
        {
            // Soltarse del borde y caer
            stateMachine.CurrentHorizontalVelocity = Vector3.zero;
            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        if (controllerDisabledForClimb)
        {
            Physics.SyncTransforms();
            stateMachine.CC.enabled = true;
            controllerDisabledForClimb = false;
        }

        // Desuscribirse de inputs
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
