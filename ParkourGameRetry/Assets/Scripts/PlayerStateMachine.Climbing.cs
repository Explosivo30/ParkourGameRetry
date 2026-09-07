using UnityEngine;

public partial class PlayerStateMachine
{
    [Header("Wall Climb - mantener Espacio")]
    [SerializeField] bool _enableWallClimb = true;
    [Tooltip("0 usa las capas de suelo, vault y wall-run existentes.")]
    [SerializeField] LayerMask _wallClimbMask;
    [SerializeField, Min(0.05f)] float _wallClimbReach = 0.35f;
    [SerializeField, Min(0.1f)] float _wallClimbSpeed = 5f;
    [SerializeField, Min(0f)] float _wallClimbDeceleration = 5f;
    [SerializeField, Min(0.1f)] float _wallClimbDuration = 0.85f;
    [SerializeField, Min(0.1f)] float _wallClimbMaxRise = 2.4f;
    [SerializeField, Range(0f, 1f)] float _wallClimbFacingDot = 0.7f;
    [SerializeField, Min(0f)] float _ledgeReattachDelay = 0.3f;

    public float WallClimbSpeed => Mathf.Max(0.1f, _wallClimbSpeed);
    public float WallClimbDeceleration => Mathf.Max(0f, _wallClimbDeceleration);
    public float WallClimbDuration => Mathf.Max(0.1f, _wallClimbDuration);
    public float WallClimbMaxRise => Mathf.Max(0.1f, _wallClimbMaxRise);
    bool _wallClimbConsumed;
    float _ledgeReattachTime;
    readonly Collider[] _climbOverlapHits = new Collider[32];
    readonly RaycastHit[] _climbSweepHits = new RaycastHit[32];

    int ClimbMask
    {
        get
        {
            if (_wallClimbMask.value != 0) return _wallClimbMask.value;
            int mask = _vaultMask.value | _wallRunMask.value;
            if (_groundMask.value != int.MinValue) mask |= _groundMask.value;
            return mask != 0 ? mask : Physics.DefaultRaycastLayers;
        }
    }

    public bool WantsWallClimb => JumpHeld && GetInput().y >= -0.1f
        && (controls == null || !controls.CrouchHeld);

    public bool CanWallClimb()
    {
        return _enableWallClimb && !Grounded && !_wallClimbConsumed
            && Time.time >= _ledgeReattachTime && WantsWallClimb
            && Vector3.Dot(currentForceGravity, Vector3.up) >= -3f
            && CheckClimbWall(out _);
    }

    public void ConsumeWallClimb()
    {
        _wallClimbConsumed = true;
        JumpBufferCounter = 0f;
        CoyoteTimeCounter = 0f;
        ClearJumpInputFrame();
    }

    public void NotifyLedgeReleased()
    {
        _ledgeReattachTime = Time.time + _ledgeReattachDelay;
        JumpBufferCounter = 0f;
        CoyoteTimeCounter = 0f;
        ClearJumpInputFrame();
    }

    void GetClimbDimensions(out Vector3 centerOffset, out float halfHeight, out float radius)
    {
        Vector3 scale = transform.lossyScale;
        centerOffset = transform.TransformPoint(cc.center) - transform.position;
        radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        halfHeight = Mathf.Max(radius, cc.height * Mathf.Abs(scale.y) * 0.5f);
    }

    public bool CheckClimbWall(out RaycastHit hit)
    {
        GetClimbDimensions(out Vector3 centerOffset, out _, out float radius);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        return Physics.Raycast(transform.position + centerOffset, forward, out hit,
                radius + cc.skinWidth + _wallClimbReach, ClimbMask, QueryTriggerInteraction.Ignore)
            && Mathf.Abs(hit.normal.y) <= 0.2f
            && Vector3.Dot(forward, -hit.normal) >= _wallClimbFacingDot;
    }

    bool TryFindClimbLedge(float minHeight)
    {
        GetClimbDimensions(out Vector3 centerOffset, out float halfHeight, out float radius);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float feetY = transform.position.y + centerOffset.y - halfHeight;
        Vector3 wallOrigin = transform.position + centerOffset;
        wallOrigin.y = feetY + Mathf.Min(halfHeight, minHeight - 0.05f);
        float reach = Mathf.Max(_ledgeCheckDistance, radius + cc.skinWidth + _wallClimbReach);
        if (!Physics.Raycast(wallOrigin, forward, out RaycastHit wallHit, reach,
                ClimbMask, QueryTriggerInteraction.Ignore)
            || Mathf.Abs(wallHit.normal.y) > 0.2f
            || Vector3.Dot(forward, -wallHit.normal) < _wallClimbFacingDot)
            return false;

        Vector3 wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
        float skin = cc.skinWidth * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        Vector3 topOrigin = wallHit.point - wallNormal * (radius + skin + _ledgeClimbForwardOffset);
        topOrigin.y = feetY + _ledgeGrabMaxHeight + 0.05f;
        if (!Physics.Raycast(topOrigin, Vector3.down, out RaycastHit topHit,
                _ledgeGrabMaxHeight - minHeight + 0.1f, ClimbMask, QueryTriggerInteraction.Ignore)
            || Vector3.Angle(topHit.normal, Vector3.up) > _maxAngle
            || topHit.point.y < feetY + minHeight || topHit.point.y > feetY + _ledgeGrabMaxHeight)
            return false;

        Vector3 hang = wallHit.point + wallNormal * (radius + skin + 0.03f) - centerOffset;
        hang.y = topHit.point.y - halfHeight - centerOffset.y - _ledgeHangClearance;
        if (JumpHeld) hang.y = Mathf.Max(hang.y, transform.position.y);
        Vector3 landing = topHit.point - centerOffset;
        landing.y += halfHeight + skin + _ledgeClimbLandingClearance;
        Vector3 lift = new Vector3(hang.x, landing.y, hang.z);

        // Comprobar todo el recorrido, incluido el hueco para la cabeza y la llegada.
        if (!CanMoveClimbCapsule(transform.position, hang)
            || !CanMoveClimbCapsule(hang, lift) || !CanMoveClimbCapsule(lift, landing))
            return false;

        LedgeWallNormal = wallNormal;
        LedgeGrabPosition = hang;
        LedgeClimbTarget = landing;
        return true;
    }

    public bool CanStartLedgeClimb()
    {
        Vector3 start = transform.position;
        Vector3 lift = new Vector3(start.x, LedgeClimbTarget.y, start.z);
        return CanMoveClimbCapsule(start, lift) && CanMoveClimbCapsule(lift, LedgeClimbTarget);
    }

    public bool CanMoveClimbCapsule(Vector3 from, Vector3 to)
    {
        GetClimbDimensions(out Vector3 offset, out float halfHeight, out float radius);
        float segment = halfHeight - radius;
        // Una tolerancia pequeña permite el contacto con el suelo sin ignorar paredes.
        radius = Mathf.Max(0.01f, radius - 0.02f);
        Vector3 bottom = from + offset - Vector3.up * segment;
        Vector3 top = from + offset + Vector3.up * segment;
        Vector3 delta = to - from;
        int overlaps = Physics.OverlapCapsuleNonAlloc(bottom + delta, top + delta, radius,
            _climbOverlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (overlaps == _climbOverlapHits.Length) return false;
        for (int i = 0; i < overlaps; i++)
            if (!IsPlayerCollider(_climbOverlapHits[i])) return false;

        if (delta.sqrMagnitude < 0.000001f) return true;
        int hits = Physics.CapsuleCastNonAlloc(bottom, top, radius, delta.normalized, _climbSweepHits,
            delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == _climbSweepHits.Length) return false;
        for (int i = 0; i < hits; i++)
            if (!IsPlayerCollider(_climbSweepHits[i].collider)) return false;
        return true;
    }

    bool IsPlayerCollider(Collider candidate)
    {
        return candidate.transform == transform || candidate.transform.IsChildOf(transform);
    }
}