using UnityEngine;

public partial class PlayerStateMachine
{
    /// <summary>Scene-local race recovery: exit the current action before clearing its impulses.</summary>
    public void ResetForCircuit(Transform spawn)
    {
        if (controls != null) controls.enabled = false;
        SwitchState(typeof(PlayerIdleState));
        RestoreCollider();
        SetGrappleActive(false);
        CurrentHorizontalVelocity = Vector3.zero;
        ResetVerticalVelocity();
        ResetVaultPhysics();
        CoyoteTimeCounter = 0f;
        CrouchPressed = false;
        LastDashTime = -999f;
        _lastGrappleFireTime = -999f;
        _airDashConsumed = false;
        _wallClimbConsumed = false;
        _ledgeReattachTime = 0f;
        _lastWallRunExitTime = -999f;
        _styleScore = 0f;
        MomentumChainCount = 0;
        LastParkourActionTime = -999f;
        _lastStyleAction = _previousStyleAction = ParkourActionType.None;
        _sameActionStreak = 0;
        _lastVarietyMultiplier = 1f;
        _currentCameraTilt = _targetCameraTilt = _cameraRollPitch = 0f;
        _currentCameraHeightOffset = _targetCameraHeightOffset = 0f;
        if (_cameraPanTilt != null) { _cameraPanTilt.PanAxis.Value = 0f; _cameraPanTilt.TiltAxis.Value = 0f; }
        if (motor != null) { motor.HorizontalVelocity = Vector3.zero; motor.VerticalVelocity = 0f; }
        cc.enabled = false;
        transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        cc.enabled = true;
        Physics.SyncTransforms();
        GroundDetection();
        UpdateCameraTilt();
        enabled = true;
        if (controls != null) controls.enabled = true;
    }
}