using UnityEngine;

/// <summary>
/// Estado de Zipline: el jugador se desliza por un cable/rail.
/// 
/// ENTRADA: Se activa al tocar un objeto de zipline (trigger externo).
///
/// DURANTE: El jugador se mueve a lo largo de la dirección del zipline.
///          La velocidad es el máximo entre ZiplineSpeed y la velocidad actual * ZiplineSpeedBoost.
///
/// SALIDA:  Por salto (con impulso vertical) o al llegar al final del rail.
/// </summary>
public class PlayerZiplineState : PlayerBaseState
{
    private Vector3 ziplineDirection;
    private float currentZiplineSpeed;

    public PlayerZiplineState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Registrar acción de parkour para el sistema de momentum
        stateMachine.RegisterParkourAction();

        // Suscribirse al input de salto para poder saltar desde el zipline
        stateMachine.SubscribeInputJump();

        // La dirección del zipline es la dirección forward del jugador al entrar
        // (en una implementación completa, vendría del objeto Zipline)
        ziplineDirection = stateMachine.transform.forward;
        ziplineDirection.y = 0f;
        ziplineDirection.Normalize();

        // La velocidad es el máximo entre la base y la actual con boost
        float currentSpeed = stateMachine.CurrentHorizontalVelocity.magnitude;
        currentZiplineSpeed = Mathf.Max(stateMachine.ZiplineSpeed, currentSpeed * stateMachine.ZiplineSpeedBoost);

        // Anular velocidad vertical (no caer mientras estás en el zipline)
        stateMachine.CurrentHorizontalVelocity = ziplineDirection * currentZiplineSpeed;
    }

    public override void Tick(float deltaTime)
    {
        // ---- Control de cámara (yaw) ----
        stateMachine.PlayerLook();

        // ---- Movimiento a lo largo del zipline ----
        Vector3 moveVelocity = ziplineDirection * currentZiplineSpeed;
        stateMachine.CC.Move(moveVelocity * deltaTime);

        // ---- SALTO DESDE EL ZIPLINE ----
        if (stateMachine.JumpPressed)
        {
            stateMachine.ClearJumpInputFrame();

            // Conservar velocidad horizontal + impulso vertical
            stateMachine.CurrentHorizontalVelocity = ziplineDirection * currentZiplineSpeed;
            stateMachine.ExecuteJump();

            stateMachine.SwitchState(typeof(PlayerIdleState));
            return;
        }

        stateMachine.ClearJumpInputFrame();
    }

    public override void FixedTick(float fixedDeltaTime) { }

    public override void Exit()
    {
        // Desuscribirse del input de salto
        stateMachine.UnsubscribeInputJump();
    }
}
