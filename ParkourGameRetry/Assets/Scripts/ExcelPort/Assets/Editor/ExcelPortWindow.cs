using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;

public class ExcelPortWindow : EditorWindow
{
    // ── Config ───────────────────────────────────────────────────────────────
    private string _webAppURL  = "https://script.google.com/macros/s/TU_WEB_APP_URL/exec";
    private string _exportPath = "Assets/Data/ExcelPort";
    private string _status     = "Listo.";
    private bool   _working    = false;
    private Vector2 _scroll;

    private const string PREF_URL  = "ExcelPort_WebAppURL";
    private const string PREF_PATH = "ExcelPort_ExportPath";

    void OnEnable()
    {
        _webAppURL  = EditorPrefs.GetString(PREF_URL,  _webAppURL);
        _exportPath = EditorPrefs.GetString(PREF_PATH, _exportPath);
    }

    void OnDisable()
    {
        EditorPrefs.SetString(PREF_URL,  _webAppURL);
        EditorPrefs.SetString(PREF_PATH, _exportPath);
    }

    [MenuItem("Tools/ExcelPort")]
    public static void Open() => GetWindow<ExcelPortWindow>("ExcelPort").minSize = new Vector2(380, 320);

    // ── GUI ──────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        GUILayout.Space(8);
        GUILayout.Label("ExcelPort", EditorStyles.boldLabel);
        GUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        _webAppURL  = EditorGUILayout.TextField("Web App URL", _webAppURL);
        _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREF_URL,  _webAppURL);
            EditorPrefs.SetString(PREF_PATH, _exportPath);
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "EXPORT → Lee [ExcelPort] de tus scripts y sube cada uno como pestaña al Google Sheet.\n" +
            "IMPORT → Lee los CSVs locales (descárgalos del sheet primero) y aplica los valores a la escena.",
            MessageType.None);

        GUILayout.Space(8);
        GUI.enabled = !_working;

        if (GUILayout.Button("📤  Export → Google Sheets", GUILayout.Height(34)))
            EditorCoroutineUtility.StartCoroutine(ExportAndUpload(), this);

        GUILayout.Space(4);

        if (GUILayout.Button("📥  Import ← Google Sheets", GUILayout.Height(34)))
            EditorCoroutineUtility.StartCoroutine(ImportAll(), this);

        GUILayout.Space(4);

        if (GUILayout.Button("🔍  Preview"))
            LogPreview();

        GUI.enabled = true;
        GUILayout.Space(8);
        EditorGUILayout.HelpBox(_status, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    // ── EXPORT + UPLOAD ──────────────────────────────────────────────────────

    IEnumerator ExportAndUpload()
    {
        _working = true;
        var discovered = DiscoverExcelPortFields();

        if (discovered.Count == 0)
        {
            _status = "No se encontraron campos con [ExcelPort].";
            _working = false;
            Repaint();
            yield break;
        }

        if (!Directory.Exists(_exportPath))
            Directory.CreateDirectory(_exportPath);

        int done = 0;
        foreach (var (typeName, fields) in discovered)
        {
            _status = $"Subiendo {typeName}... ({done + 1}/{discovered.Count})";
            Repaint();

            var rows = BuildRows(typeName, fields);

            // Backup local
            SaveCSVLocal(Path.Combine(_exportPath, $"{typeName}.csv"), rows);

            // Subir al sheet
            yield return UploadSheet(typeName, rows);
            done++;
        }

        AssetDatabase.Refresh();
        _status = $"✓ {done} scripts exportados y subidos a Google Sheets.";
        _working = false;
        Repaint();
    }

    IEnumerator UploadSheet(string sheetName, List<List<string>> rows)
    {
        // Serializar a JSON manualmente para evitar dependencias
        var sb = new StringBuilder();
        sb.Append("{\"sheetName\":\"").Append(EscapeJSON(sheetName)).Append("\",\"rows\":[");

        for (int i = 0; i < rows.Count; i++)
        {
            sb.Append("[");
            for (int j = 0; j < rows[i].Count; j++)
            {
                sb.Append("\"").Append(EscapeJSON(rows[i][j])).Append("\"");
                if (j < rows[i].Count - 1) sb.Append(",");
            }
            sb.Append("]");
            if (i < rows.Count - 1) sb.Append(",");
        }
        sb.Append("]}");

        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        var www = new UnityWebRequest(_webAppURL, "POST");
        www.uploadHandler   = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[ExcelPort] Error subiendo {sheetName}: {www.error}");
        else
            Debug.Log($"[ExcelPort] ✓ {sheetName} subido → {www.downloadHandler.text}");
    }

    // ── IMPORT ───────────────────────────────────────────────────────────────

    IEnumerator ImportAll()
    {
        _working = true;
        var discovered = DiscoverExcelPortFields();

        if (discovered.Count == 0)
        {
            _status = "No se encontraron campos con [ExcelPort].";
            _working = false;
            Repaint();
            yield break;
        }

        int imported = 0;
        foreach (var (typeName, _) in discovered)
        {
            _status = $"Descargando {typeName}... ({imported + 1}/{discovered.Count})";
            Repaint();

            string url = $"{_webAppURL}?sheet={UnityWebRequest.EscapeURL(typeName)}";
            var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ExcelPort] Error descargando {typeName}: {www.error}");
                continue;
            }

            // Parsear respuesta JSON: { status: "ok", rows: [[...],[...],...] }
            string csv = ParseRowsJSONToCSV(www.downloadHandler.text, typeName);
            if (csv == null) continue;

            // Guardar CSV local como backup
            string localPath = Path.Combine(_exportPath, $"{typeName}.csv");
            if (!Directory.Exists(_exportPath)) Directory.CreateDirectory(_exportPath);
            File.WriteAllText(localPath, csv, Encoding.UTF8);

            ApplyCSVToScene(typeName, csv);
            imported++;
        }

        AssetDatabase.Refresh();
        _status = $"✓ {imported} scripts importados desde Google Sheets.";
        _working = false;
        Repaint();
    }

    // Convierte el JSON de rows que devuelve el Apps Script a CSV plano
    string ParseRowsJSONToCSV(string json, string typeName)
    {
        // Respuesta esperada: {"status":"ok","rows":[["field","type",...],[...],...]}
        if (json.Contains("\"status\":\"error\""))
        {
            Debug.LogError($"[ExcelPort] El sheet '{typeName}' no existe o hay un error. Haz Export primero.");
            return null;
        }

        // Extraer el array de rows del JSON manualmente (sin dependencia de JsonUtility)
        int rowsStart = json.IndexOf("\"rows\":");
        if (rowsStart < 0) { Debug.LogError($"[ExcelPort] Respuesta inesperada para {typeName}: {json}"); return null; }

        // Usar JsonUtility con wrapper
        string rowsJSON = json.Substring(rowsStart + 7).TrimEnd('}').Trim();

        // Parsear con mini-parser: convertir array de arrays a CSV
        var sb = new StringBuilder();
        var rows = ParseJSONArrayOfArrays(rowsJSON);
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(SanitizeCSV)));

        return sb.ToString();
    }

    // Mini-parser para [[a,b,c],[d,e,f],...] sin dependencias externas
    List<List<string>> ParseJSONArrayOfArrays(string json)
    {
        var result = new List<List<string>>();
        int i = 0;

        while (i < json.Length)
        {
            if (json[i] == '[' && i > 0) // inicio de fila (no el array exterior)
            {
                i++;
                var row = new List<string>();
                while (i < json.Length && json[i] != ']')
                {
                    if (json[i] == '"')
                    {
                        i++;
                        var val = new StringBuilder();
                        while (i < json.Length && json[i] != '"')
                        {
                            if (json[i] == '\\' && i + 1 < json.Length) { i++; val.Append(json[i]); }
                            else val.Append(json[i]);
                            i++;
                        }
                        row.Add(val.ToString());
                    }
                    i++;
                }
                if (row.Count > 0) result.Add(row);
            }
            i++;
        }

        return result;
    }

    void ApplyCSVToScene(string typeName, string csv)
    {
        var type = GetTypeByName(typeName);
        if (type == null) return;

        var instances = FindObjectsByType(type, FindObjectsSortMode.None);
        if (instances.Length == 0)
        {
            Debug.LogWarning($"[ExcelPort] No hay instancias de {typeName} en la escena.");
            return;
        }

        var lines = csv.Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 2) return;

        var headers = lines[0].Split(',');

        var fieldRows = new Dictionary<string, List<string>>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var parts = line.Split(',');
            var vals = new List<string>();
            for (int i = 3; i < parts.Length; i++) vals.Add(parts[i].Trim());
            fieldRows[parts[0].Trim()] = vals;
        }

        var goColOffset = new Dictionary<string, int>();
        for (int i = 3; i < headers.Length; i++)
            goColOffset[headers[i].Trim()] = i - 3;

        var fields = type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.GetCustomAttribute<ExcelPortAttribute>() != null);

        foreach (var inst in instances)
        {
            var comp = (Component)inst;
            int colOffset = goColOffset.TryGetValue(comp.gameObject.name, out int idx) ? idx : 0;

            foreach (var field in fields)
            {
                if (!ExcelPortHandlerRegistry.TryGet(field.FieldType, out var handler)) continue;

                if (handler.ColumnSuffixes.Length == 1)
                {
                    if (!fieldRows.TryGetValue(field.Name, out var vals)) continue;
                    string raw = colOffset < vals.Count ? vals[colOffset] : "";
                    field.SetValue(inst, handler.FromColumns(new[] { raw }));
                }
                else
                {
                    var subVals = new string[handler.ColumnSuffixes.Length];
                    for (int si = 0; si < handler.ColumnSuffixes.Length; si++)
                    {
                        string key = field.Name + handler.ColumnSuffixes[si];
                        subVals[si] = fieldRows.TryGetValue(key, out var vals) && colOffset < vals.Count
                            ? vals[colOffset] : "0";
                    }
                    field.SetValue(inst, handler.FromColumns(subVals));
                }

                EditorUtility.SetDirty(comp);
            }
        }
    }

    // ── BUILD ROWS ───────────────────────────────────────────────────────────

    List<List<string>> BuildRows(string typeName, List<FieldInfo> fields)
    {
        var rows = new List<List<string>>();
        var type = GetTypeByName(typeName);
        var instances = type != null
            ? FindObjectsByType(type, FindObjectsSortMode.None)
            : Array.Empty<UnityEngine.Object>();

        var header = new List<string> { "field", "type", "description" };
        if (instances.Length == 0) header.Add("default_value");
        else foreach (var inst in instances) header.Add(((Component)inst).gameObject.name);
        rows.Add(header);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ExcelPortAttribute>();
            if (!ExcelPortHandlerRegistry.TryGet(field.FieldType, out var handler)) continue;

            if (handler.ColumnSuffixes.Length == 1)
            {
                var row = new List<string> { field.Name, field.FieldType.Name, attr.Description };
                if (instances.Length == 0) row.Add("");
                else foreach (var inst in instances) row.Add(handler.ToColumns(field.GetValue(inst))[0]);
                rows.Add(row);
            }
            else
            {
                for (int si = 0; si < handler.ColumnSuffixes.Length; si++)
                {
                    var row = new List<string>
                    {
                        field.Name + handler.ColumnSuffixes[si],
                        field.FieldType.Name + handler.ColumnSuffixes[si],
                        si == 0 ? attr.Description : ""
                    };
                    if (instances.Length == 0) row.Add("");
                    else foreach (var inst in instances)
                        row.Add(handler.ToColumns(field.GetValue(inst))[si]);
                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    void SaveCSVLocal(string path, List<List<string>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(SanitizeCSV)));
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    // ── DISCOVERY ────────────────────────────────────────────────────────────

    Dictionary<string, List<FieldInfo>> DiscoverExcelPortFields()
    {
        var result = new Dictionary<string, List<FieldInfo>>();
        var mbType = typeof(MonoBehaviour);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName.StartsWith("Unity") ||
                assembly.FullName.StartsWith("System") ||
                assembly.FullName.StartsWith("mscorlib")) continue;

            foreach (var type in assembly.GetTypes())
            {
                if (!mbType.IsAssignableFrom(type) || type.IsAbstract) continue;

                var fields = type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<ExcelPortAttribute>() != null)
                    .Where(f => ExcelPortHandlerRegistry.TryGet(f.FieldType, out _))
                    .ToList();

                if (fields.Count > 0)
                    result[type.Name] = fields;
            }
        }

        return result;
    }

    // ── PREVIEW ──────────────────────────────────────────────────────────────

    void LogPreview()
    {
        var discovered = DiscoverExcelPortFields();
        if (discovered.Count == 0) { _status = "No se encontraron campos con [ExcelPort]."; Repaint(); return; }

        Debug.Log($"[ExcelPort] {discovered.Count} scripts encontrados:");
        foreach (var (typeName, fields) in discovered)
        {
            Debug.Log($"  📄 {typeName} ({fields.Count} campos):");
            foreach (var f in fields)
            {
                var attr = f.GetCustomAttribute<ExcelPortAttribute>();
                Debug.Log($"      • {f.Name} ({f.FieldType.Name}) — {(string.IsNullOrEmpty(attr.Description) ? "(sin descripción)" : attr.Description)}");
            }
        }
        _status = $"Preview: {discovered.Count} scripts. Ver Console.";
        Repaint();
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    Type GetTypeByName(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = assembly.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    string SanitizeCSV(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    string EscapeJSON(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }
}
