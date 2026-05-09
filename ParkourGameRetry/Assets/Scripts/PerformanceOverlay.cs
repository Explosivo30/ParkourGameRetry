using UnityEngine;
using TMPro; // Asegúrate de tener el paquete TextMeshPro instalado

public class PerformanceOverlay : MonoBehaviour
{
    [Header("Configuración UI (Opcional)")]
    [Tooltip("Si dejas esto vacío, se dibujará una UI automática en la pantalla.")]
    public TextMeshProUGUI performanceText;

    [Header("Ajustes")]
    public float refreshRate = 0.5f;
    public Color goodColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color badColor = Color.red;
    public int warningFPS = 50;
    public int badFPS = 30;

    private float timer;
    private int frameCount;
    private float dt;

    // Métricas
    private int currentFPS;
    private float frameTime;
    private int lowestFPS = int.MaxValue;
    private int highestFPS = 0;
    
    // Memoria
    private float totalMemoryMB;
    private float allocatedMemoryMB;

    void Start()
    {
        timer = Time.unscaledTime + refreshRate;
    }

    void Update()
    {
        // Tiempo delta suavizado para un frame time más estable de ver
        dt += (Time.unscaledDeltaTime - dt) * 0.1f;
        frameTime = dt * 1000.0f;

        frameCount++;

        if (Time.unscaledTime >= timer)
        {
            int fps = Mathf.RoundToInt(frameCount / refreshRate);
            currentFPS = fps;

            if (Time.unscaledTime > 3f) // Ignorar los primeros 3 segundos de tirones de carga
            {
                if (fps < lowestFPS) lowestFPS = fps;
                if (fps > highestFPS) highestFPS = fps;
            }

            // Calcular uso de memoria profilada
            totalMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576f;
            allocatedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f;

            UpdateUI();

            frameCount = 0;
            timer = Time.unscaledTime + refreshRate;
        }
    }

    private void UpdateUI()
    {
        if (performanceText != null)
        {
            performanceText.text = GetPerformanceString();
            performanceText.color = GetPerformanceColor(currentFPS);
        }
    }

    private string GetPerformanceString()
    {
        return $"FPS: {currentFPS}\n" +
               $"Frame Time: {frameTime:0.0} ms\n" +
               $"Min FPS: {(lowestFPS == int.MaxValue ? '-' : lowestFPS)}\n" +
               $"Max FPS: {highestFPS}\n" +
               $"Memory: {allocatedMemoryMB:0.0} / {totalMemoryMB:0.0} MB";
    }

    private Color GetPerformanceColor(int fps)
    {
        if (fps >= warningFPS) return goodColor;
        if (fps >= badFPS) return warningColor;
        return badColor;
    }

    // ====== UI ALTERNATIVA (Automática si no asignas un TextMeshPro) ======
    private void OnGUI()
    {
        if (performanceText == null)
        {
            int anchura = 220;
            int altura = 130;
            Rect rect = new Rect(10, 10, anchura, altura);
            
            // Fondo oscuro para que se vea siempre
            GUI.Box(rect, "");
            
            GUIStyle style = new GUIStyle();
            
            // Ajustar posición del texto dentro del cuadro
            Rect textRect = new Rect(15, 15, anchura, altura);

            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            
            // Sombra para mejor legibilidad
            Rect shadowRect = textRect;
            shadowRect.x += 2;
            shadowRect.y += 2;
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;
            GUI.Label(shadowRect, GetPerformanceString(), shadowStyle);
            
            // Texto principal
            style.normal.textColor = GetPerformanceColor(currentFPS);
            GUI.Label(textRect, GetPerformanceString(), style);
        }
    }
}
