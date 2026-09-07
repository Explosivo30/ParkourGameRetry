using UnityEngine;

/// <summary>
/// Zipline rail and trigger system for the parkour scene.
/// Connects start and end anchor transforms, renders a cable visual,
/// and engages PlayerZiplineState upon player trigger entry.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParkourZipline : MonoBehaviour
{
    [Header("Anchor Endpoints")]
    [Tooltip("Starting anchor point of the zipline cable.")]
    [SerializeField] private Transform startAnchor;

    [Tooltip("Ending anchor point of the zipline cable.")]
    [SerializeField] private Transform endAnchor;

    [Header("Speed Configuration")]
    [Tooltip("Movement speed along the zipline cable in m/s.")]
    [SerializeField] private float zipSpeed = 15f;

    [Header("Visual Cable")]
    [Tooltip("Optional LineRenderer for drawing the cable in the scene.")]
    [SerializeField] private LineRenderer cableRenderer;

    [Tooltip("Cable width in meters.")]
    [SerializeField] private float cableWidth = 0.04f;

    [Tooltip("Optional material for the zipline cable.")]
    [SerializeField] private Material cableMaterial;

    // Interface contract property aliases
    public Transform startAnchorPoint { get => startAnchor; set => startAnchor = value; }
    public Transform endAnchorPoint { get => endAnchor; set => endAnchor = value; }
    public Transform startPoint { get => startAnchor; set => startAnchor = value; }
    public Transform endPoint { get => endAnchor; set => endAnchor = value; }
    public float zipSpeedValue { get => zipSpeed; set => zipSpeed = value; }
    public float speed { get => zipSpeed; set => zipSpeed = value; }

    private void Awake()
    {
        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Setup visual cable if enabled
        SetupCableRenderer();
    }

    private void Start()
    {
        UpdateCablePositions();
    }

    private void OnValidate()
    {
        UpdateCablePositions();
    }

    /// <summary>
    /// Configures or creates a LineRenderer component for the cable visual.
    /// </summary>
    private void SetupCableRenderer()
    {
        if (cableRenderer == null)
        {
            cableRenderer = GetComponent<LineRenderer>();
        }

        if (cableRenderer == null && (startAnchor != null || endAnchor != null))
        {
            cableRenderer = gameObject.AddComponent<LineRenderer>();
            cableRenderer.useWorldSpace = true;
            cableRenderer.startWidth = cableWidth;
            cableRenderer.endWidth = cableWidth;
            cableRenderer.positionCount = 2;

            if (cableMaterial != null)
            {
                cableRenderer.material = cableMaterial;
            }
            else
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader != null)
                {
                    Material mat = new Material(unlitShader);
                    mat.color = new Color(1f, 0.7f, 0.1f, 1f); // Amber / Industrial Yellow
                    cableRenderer.material = mat;
                }
            }
        }

        UpdateCablePositions();
    }

    /// <summary>
    /// Updates LineRenderer start and end vertex positions to match anchors.
    /// </summary>
    public void UpdateCablePositions()
    {
        if (cableRenderer == null) return;

        Vector3 start = GetStartPosition();
        Vector3 end = GetEndPosition();

        cableRenderer.useWorldSpace = true;
        cableRenderer.positionCount = 2;
        cableRenderer.SetPosition(0, start);
        cableRenderer.SetPosition(1, end);
    }

    /// <summary>
    /// Returns the world position of the starting anchor point.
    /// </summary>
    public Vector3 GetStartPosition()
    {
        return startAnchor != null ? startAnchor.position : transform.position;
    }

    /// <summary>
    /// Returns the world position of the ending anchor point.
    /// </summary>
    public Vector3 GetEndPosition()
    {
        return endAnchor != null ? endAnchor.position : (transform.position + transform.forward * 25f);
    }

    /// <summary>
    /// Returns normalized direction vector pointing from start anchor to end anchor.
    /// </summary>
    public Vector3 GetZiplineDirection()
    {
        Vector3 delta = GetEndPosition() - GetStartPosition();
        return delta.sqrMagnitude > 0.001f ? delta.normalized : transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
        if (player == null) return;

        // Calculate travel direction along the zipline cable
        Vector3 direction = GetZiplineDirection();
        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);

        if (horizontalDir.sqrMagnitude > 0.001f)
        {
            // Align player forward orientation with zipline descent vector
            player.transform.forward = horizontalDir.normalized;
        }

        // Switch to zipline movement state
        player.SwitchState(typeof(PlayerZiplineState));
        Debug.Log($"[ParkourZipline] Player engaged zipline towards {GetEndPosition()} at speed {zipSpeed} m/s.");
    }

    private void OnDrawGizmos()
    {
        Vector3 start = GetStartPosition();
        Vector3 end = GetEndPosition();

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f); // Amber wire
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.4f);
        Gizmos.DrawWireSphere(end, 0.4f);

        // Direction indicator arrow
        Vector3 mid = Vector3.Lerp(start, end, 0.5f);
        Vector3 dir = GetZiplineDirection();
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(mid, dir * 2f);
    }
}
