using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Ordered gates, timing, recovery and results for the Distrito Libre scene.</summary>
public class DistritoLibreCourse : MonoBehaviour
{
    [Serializable]
    public class Gate
    {
        public string title;
        public string hint;
        public BoxCollider volume;
        public Transform spawn;
    }

    public PlayerStateMachine player;
    public Gate[] gates;
    public float fallHeight = -10f;
    public bool saveBestTime = true;
    public const string RecordKey = "DistritoLibre.Circuit.v1.Best";
    public bool Started { get; private set; }
    public bool Finished { get; private set; }
    public int NextGate { get; private set; }
    public int Recoveries { get; private set; }
    public float BestTime { get; private set; }
    public float LastSplit { get; private set; }
    public bool NewRecord { get; private set; }
    public ParkourStyleRank PeakRank { get; private set; }
    public float Elapsed => Started ? (Finished ? finishTime : (float)(Time.timeAsDouble - startTime)) : 0f;
    public string Sector => gates[Mathf.Clamp(NextGate - 1, 0, gates.Length - 1)].title;
    double startTime;
    float finishTime;
    float messageUntil;
    string message;
    CursorLockMode previousCursor;
    bool previousVisible;
    GUIStyle small, body, title;
    Canvas resultCanvas;
    readonly Color mint = new Color(.36f, 1f, .77f);

    void Start()
    {
        BestTime = PlayerPrefs.GetFloat(RecordKey, 0f);
        previousCursor = Cursor.lockState;
        previousVisible = Cursor.visible;
        LockCursor();
    }

    void Update()
    {
        if (player == null || gates == null || gates.Length < 2) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.tKey.wasPressedThisFrame || (Finished && keyboard.rKey.wasPressedThisFrame)) RestartLap();
            else if (keyboard.rKey.wasPressedThisFrame) Recover();
            if (keyboard.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        if (!Finished && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
        if (Finished) return;
        if (player.transform.position.y < fallHeight) { Recover(); return; }
        if (player.StyleRank > PeakRank) PeakRank = player.StyleRank;
        if (NextGate < gates.Length && Contains(gates[NextGate].volume, player.transform.position)) PassGate();
    }

    static bool Contains(BoxCollider volume, Vector3 position)
    {
        Vector3 p = volume.transform.InverseTransformPoint(position) - volume.center;
        Vector3 half = volume.size * .5f;
        return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
    }

    void PassGate()
    {
        int crossed = NextGate++;
        if (crossed == 0) { Started = true; startTime = Time.timeAsDouble; }
        LastSplit = Elapsed;
        message = gates[crossed].title + "  /  " + FormatTime(LastSplit);
        messageUntil = Time.unscaledTime + 2.5f;
        if (NextGate != gates.Length) return;
        finishTime = Elapsed;
        Finished = true;
        NewRecord = BestTime <= 0f || finishTime < BestTime;
        if (NewRecord)
        {
            BestTime = finishTime;
            if (saveBestTime) { PlayerPrefs.SetFloat(RecordKey, BestTime); PlayerPrefs.Save(); }
        }
        // Freeze only this player; the global clock and other scenes are unaffected.
        player.Controls.enabled = false;
        player.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowResults();
    }

    public void Recover()
    {
        if (Finished) { RestartLap(); return; }
        if (Started) Recoveries++;
        player.ResetForCircuit(gates[Mathf.Clamp(NextGate - 1, 0, gates.Length - 2)].spawn);
        message = Started ? "CHECKPOINT  /  El cronometro sigue corriendo" : "CRUZA LA SALIDA PARA EMPEZAR";
        messageUntil = Time.unscaledTime + 2.5f;
        LockCursor();
    }

    public void RestartLap()
    {
        if (resultCanvas != null) Destroy(resultCanvas.gameObject);
        Started = false; Finished = false; NextGate = 0; Recoveries = 0;
        startTime = 0; finishTime = 0; LastSplit = 0; NewRecord = false; PeakRank = ParkourStyleRank.D;
        messageUntil = 0;
        player.ResetForCircuit(gates[0].spawn);
        LockCursor();
    }

    void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void OnDisable() { Cursor.lockState = previousCursor; Cursor.visible = previousVisible; }
    public static string FormatTime(float value) => $"{Mathf.FloorToInt(value / 60f):00}:{value % 60f:00.00}";

    void ShowResults()
    {
        var root = new GameObject("Distrito Libre / Results", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        resultCanvas = root.GetComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        resultCanvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null)
        {
            var events = new GameObject("Circuit UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var panel = new GameObject("Finish card", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(600, 444);
        panel.GetComponent<Image>().color = new Color(.025f, .04f, .055f, .97f);
        var stripe = UiRect("Mint stripe", panelRect, new Rect(0, 0, 600, 5));
        stripe.gameObject.AddComponent<Image>().color = mint;
        Label(panelRect, "YA ERES PARTE DE LAS AZOTEAS", new Rect(36, 28, 528, 40), 25, mint, true);
        Label(panelRect, "Circuito completado / La terraza comun", new Rect(36, 74, 528, 32), 18, Color.white);
        Label(panelRect, FormatTime(finishTime), new Rect(20, 108, 560, 96), 68, Color.white, true, TextAnchor.MiddleCenter);
        Label(panelRect, NewRecord ? "NUEVA MEJOR MARCA" : "MEJOR MARCA  " + FormatTime(BestTime), new Rect(36, 220, 528, 34), 25, mint, true);
        Label(panelRect, $"Recuperaciones: {Recoveries}       Estilo maximo: {PeakRank}", new Rect(36, 269, 528, 32), 18, Color.white);
        Label(panelRect, "Abajo nos separan. Arriba encontramos nuestro camino.", new Rect(36, 309, 528, 42), 15, new Color(.68f, .76f, .78f));
        var buttonRect = UiRect("Otra vuelta", panelRect, new Rect(36, 366, 528, 48));
        var fill = buttonRect.gameObject.AddComponent<Image>(); fill.color = mint;
        var button = buttonRect.gameObject.AddComponent<Button>(); button.targetGraphic = fill;
        button.onClick.AddListener(RestartLap);
        Label(buttonRect, "OTRA VUELTA  [R]", new Rect(0, 0, 528, 48), 18, new Color(.03f,.08f,.07f), true, TextAnchor.MiddleCenter);
    }

    static RectTransform UiRect(string name, Transform parent, Rect rect)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(rect.x, -rect.y);
        rt.sizeDelta = rect.size;
        return rt;
    }

    static void Label(Transform parent, string text, Rect rect, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var rt = UiRect(text, parent, rect);
        var label = rt.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.raycastTarget = false;
    }
    void Styles()
    {
        if (small != null) return;
        small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        small.normal.textColor = new Color(.68f, .76f, .78f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        title.normal.textColor = mint;
    }

    static void Panel(Rect rect, Color color)
    {
        GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (gates == null || gates.Length < 2) return;
        Styles();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale, height = Screen.height / scale;
        Panel(new Rect(24, 24, 320, 108), new Color(.025f, .045f, .065f, .92f));
        Panel(new Rect(24, 24, 4, 108), mint);
        GUI.Label(new Rect(42, 32, 294, 35), "DISTRITO LIBRE", title);
        GUI.Label(new Rect(42, 68, 145, 35), FormatTime(Elapsed), title);
        GUI.Label(new Rect(191, 75, 146, 28), $"{Mathf.Clamp(NextGate - 1, 0, 5)} / 5 CHECKPOINTS", small);
        GUI.Label(new Rect(42, 105, 290, 22), BestTime > 0 ? "MEJOR  " + FormatTime(BestTime) : "PRIMERA VUELTA", small);
        Panel(new Rect(24, height - 120, 570, 96), new Color(.025f, .045f, .065f, .87f));
        GUI.Label(new Rect(40, height - 114, 538, 28), Sector, body);
        string hint = gates[Mathf.Clamp(NextGate - 1, 0, gates.Length - 1)].hint;
        GUI.Label(new Rect(40, height - 82, 538, 34), hint, small);
        GUI.Label(new Rect(40, height - 46, 538, 24), "R  checkpoint     T  nueva vuelta     Esc  cursor", small);
        if (!Started)
        {
            GUI.Label(new Rect(width * .5f - 265, height * .7f, 550, 36), "Cruza el arco verde para iniciar el cronometro.", body);
            GUI.Label(new Rect(width * .5f - 265, height * .7f + 32, 550, 36), "Menta: recorrido principal. Amarillo: atajos opcionales.", small);
        }
        else if (!Finished && Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(width * .5f - 240, 42, 480, 40), message, body);
        GUI.matrix = oldMatrix; GUI.color = oldColor;
    }
}