using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Cronómetro, meta y reintento del circuito Skyline. Solo se usa en esta escena.</summary>
public class SkylineParkourCourse : MonoBehaviour
{
    public PlayerStateMachine player;
    public CheckpointManager checkpoints;
    public Transform finish;
    public float fallHeight = -12f;
    public bool Finished { get; private set; }
    public int Retries { get; private set; }
    public float Elapsed => started ? (Finished ? finishTime : Time.time - startTime) : 0f;
    bool started;
    float startTime;
    float finishTime;
    Vector3 startPosition;
    CursorLockMode previousCursor;
    bool previousCursorVisible;
    GUIStyle heading, body, timer;

    void Start()
    {
        startPosition = player.transform.position;
        previousCursor = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        LockCursor();
    }

    void Update()
    {
        if (player == null || checkpoints == null) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Retry();
        if (player.transform.position.y < fallHeight) Retry();
        if (!started && Vector3.ProjectOnPlane(player.transform.position - startPosition, Vector3.up).magnitude > 1.5f)
        {
            started = true;
            startTime = Time.time;
        }
        if (started && !Finished && finish != null
            && Vector3.ProjectOnPlane(player.transform.position - finish.position, Vector3.up).sqrMagnitude < 6.25f
            && Mathf.Abs(player.transform.position.y - finish.position.y) < 2f)
        {
            Finished = true;
            finishTime = Time.time - startTime;
        }
    }

    public void Retry()
    {
        if (Finished)
        {
            checkpoints.ActivateCheckpoint(checkpoints.allCheckpoints[0]);
            Finished = false;
            started = false;
            finishTime = 0f;
            Retries = 0;
        }
        else Retries++;
        checkpoints.RespawnPlayer(player);
        if (!started) startPosition = player.transform.position;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = previousCursor;
        Cursor.visible = previousCursorVisible;
    }

    void OnGUI()
    {
        if (heading == null)
        {
            heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(0.3f, 0.92f, 0.85f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            timer = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
        }
        float y = Screen.height - 132;
        GUI.Box(new Rect(18, y, 385, 114), GUIContent.none);
        GUI.Label(new Rect(32, y + 8, 355, 30), Finished ? "SKYLINE / COMPLETADO" : "SKYLINE / ROOFTOP RUN", heading);
        int minutes = Mathf.FloorToInt(Elapsed / 60f);
        GUI.Label(new Rect(32, y + 36, 170, 40), $"{minutes:00}:{Elapsed % 60f:00.00}", timer);
        string sector = checkpoints != null && checkpoints.currentCheckpoint != null
            ? checkpoints.currentCheckpoint.sectorName : "SALIDA";
        GUI.Label(new Rect(206, y + 43, 186, 25), sector, body);
        GUI.Label(new Rect(32, y + 83, 355, 24), $"R: {(Finished ? "nueva vuelta" : "checkpoint")}    Reintentos: {Retries}    Esc: cursor", body);
        if (!started)
            GUI.Label(new Rect(Screen.width * 0.5f - 185, Screen.height * 0.7f, 390, 30), "Avanza para iniciar el cronómetro. Sigue el turquesa.", body);
        Color previous = GUI.color;
        GUI.color = player != null && player.GrappleTargetAvailable ? Color.green : Color.white;
        GUI.DrawTexture(new Rect(Screen.width / 2f - 2, Screen.height / 2f - 2, 4, 4), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}