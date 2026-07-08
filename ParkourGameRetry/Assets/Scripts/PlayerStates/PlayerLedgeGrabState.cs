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

    public PlayerLedgeGrabState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction();

        // Suscribirse a inputs
        stateMachine.SubscribeInputJump();
        stateMachine.SubscribeInputCrouch();

        // Anular toda velocidad y gravedad — el jugador queda suspendido
        stateMachine.CurrentHorizontalVelocity = Vector3.zero;

        isClimbing = false;
        climbTimer = 0f;

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
            float progress = climbTimer / stateMachine.LedgeClimbDuration;

            if (progress >= 1f)
            {
                // Llegamos arriba, salir al estado normal
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }

            // Lerp suave desde la posición de agarre hasta arriba del borde
            Vector3 targetPos = stateMachine.LedgeClimbTarget;
            Vector3 newPos = Vector3.Lerp(climbStartPos, targetPos, progress);
            Vector3 displacement = newPos - stateMachine.transform.position;
            stateMachine.CC.Move(displacement);
            return;
        }

        // ---- SALTO DESDE EL BORDE ----
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();

            Vector2 input = stateMachine.GetInput();
            if (input.sqrMagnitude > 0.1f)
            {
                // Salto direccional: impulso en la dirección del input + arriba
                Vector3 jumpDir = stateMachine.GetWorldMovementDirection(input);
                Vector3 horizontalForce = jumpDir * stateMachine.LedgeJumpForce;
                stateMachine.CurrentHorizontalVelocity = horizontalForce;
                stateMachine.ExecuteJump();
                stateMachine.SwitchState(typeof(PlayerIdleState));
                return;
            }
            else
            {
                // Sin dirección: subir al borde (climb)
                isClimbing = true;
                climbTimer = 0f;
                climbStartPos = stateMachine.transform.position;
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
        // Desuscribirse de inputs
        stateMachine.UnsubscribeInputJump();
        stateMachine.UnsubscribeInputCrouch();
    }
}
