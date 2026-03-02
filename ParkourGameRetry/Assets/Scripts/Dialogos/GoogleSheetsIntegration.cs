using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dialogo
{
    /// <summary>
    /// Sistema de integración con Google Sheets para traducción colaborativa
    /// Permite exportar/importar traducciones directamente desde/hacia Google Sheets
    /// </summary>
    public class GoogleSheetsIntegration
    {
        // URL base de la API de Google Sheets
        private const string SHEETS_API_BASE = "https://sheets.googleapis.com/v4/spreadsheets";

        /// <summary>
        /// Configuración de conexión con Google Sheets
        /// </summary>
        [Serializable]
        public class GoogleSheetsConfig
        {
            [Tooltip("ID de la hoja de Google (extraído de la URL)")]
            public string spreadsheetId;

            [Tooltip("API Key de Google Cloud (ver documentación)")]
            public string apiKey;

            [Tooltip("Nombre de la pestaña/hoja dentro del documento")]
            public string sheetName = "Traducciones";

            [Tooltip("Actualizar automáticamente al iniciar Unity")]
            public bool autoFetchOnStartup = false;

            [Tooltip("Intervalo de actualización automática (minutos, 0 = desactivado)")]
            public int autoUpdateInterval = 0;
        }

        /// <summary>
        /// Resultado de una operación de sincronización
        /// </summary>
        public class SyncResult
        {
            public bool success;
            public string message;
            public int entriesProcessed;
            public List<string> errors = new List<string>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Exporta la base de datos a Google Sheets
        /// </summary>
        public static IEnumerator ExportToGoogleSheets(LocalizationDatabase database, GoogleSheetsConfig config, System.Action<SyncResult> callback)
        {
            var result = new SyncResult();

            if (string.IsNullOrEmpty(config.spreadsheetId) || string.IsNullOrEmpty(config.apiKey))
            {
                result.success = false;
                result.message = "Falta configurar spreadsheetId o apiKey";
                callback?.Invoke(result);
                yield break;
            }

            // Preparar datos para exportar
            var data = PrepareDataForExport(database);

            // Construir la solicitud
            string url = $"{SHEETS_API_BASE}/{config.spreadsheetId}/values/{config.sheetName}!A1:clear?key={config.apiKey}";
            
            // Primero limpiar la hoja
            using (UnityWebRequest clearRequest = UnityWebRequest.PostWwwForm(url, ""))
            {
                yield return clearRequest.SendWebRequest();

                if (clearRequest.result != UnityWebRequest.Result.Success)
                {
                    result.success = false;
                    result.message = "Error al limpiar la hoja: " + clearRequest.error;
                    callback?.Invoke(result);
                    yield break;
                }
            }

            // Ahora escribir los nuevos datos
            url = $"{SHEETS_API_BASE}/{config.spreadsheetId}/values/{config.sheetName}!A1?valueInputOption=RAW&key={config.apiKey}";
            
            string jsonData = JsonUtility.ToJson(new SheetData { values = data });
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    result.success = true;
                    result.message = "Exportación exitosa a Google Sheets";
                    result.entriesProcessed = database.GetAllEntries().Count;
                }
                else
                {
                    result.success = false;
                    result.message = "Error al exportar: " + request.error;
                }
            }

            callback?.Invoke(result);
        }

        /// <summary>
        /// Importa traducciones desde Google Sheets
        /// </summary>
        public static IEnumerator ImportFromGoogleSheets(LocalizationDatabase database, GoogleSheetsConfig config, System.Action<SyncResult> callback)
        {
            var result = new SyncResult();

            if (string.IsNullOrEmpty(config.spreadsheetId) || string.IsNullOrEmpty(config.apiKey))
            {
                result.success = false;
                result.message = "Falta configurar spreadsheetId o apiKey";
                callback?.Invoke(result);
                yield break;
            }

            // Construir URL para obtener datos
            string range = $"{config.sheetName}!A:Z"; // Obtener todas las columnas
            string url = $"{SHEETS_API_BASE}/{config.spreadsheetId}/values/{range}?key={config.apiKey}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // Parsear respuesta
                        SheetData sheetData = JsonUtility.FromJson<SheetData>(request.downloadHandler.text);
                        
                        // Procesar datos
                        ProcessImportedData(database, sheetData.values, result);
                        
                        result.success = true;
                        result.message = $"Importación exitosa: {result.entriesProcessed} entradas actualizadas";
                    }
                    catch (Exception e)
                    {
                        result.success = false;
                        result.message = "Error al procesar datos: " + e.Message;
                        result.errors.Add(e.ToString());
                    }
                }
                else
                {
                    result.success = false;
                    result.message = "Error al importar: " + request.error;
                }
            }

            callback?.Invoke(result);
        }

        /// <summary>
        /// Prepara los datos de la base de datos para exportar a Google Sheets
        /// </summary>
        private static List<List<string>> PrepareDataForExport(LocalizationDatabase database)
        {
            var data = new List<List<string>>();
            var languages = LocalizationManager.SupportedLanguages;

            // Crear encabezados
            var headers = new List<string> { "ID", "Categoría", "Contexto", "Max Caracteres", "Variables" };
            foreach (var lang in languages)
            {
                headers.Add(lang.ToString());
            }
            data.Add(headers);

            // Añadir cada entrada
            foreach (var entry in database.GetAllEntries())
            {
                var row = new List<string>
                {
                    entry.localizationID,
                    entry.category ?? "",
                    entry.context ?? "",
                    entry.maxCharacters.ToString(),
                    entry.supportsVariables ? "SÍ" : "NO"
                };

                // Añadir traducciones
                foreach (var lang in languages)
                {
                    string text = entry.localizedString.GetText(lang);
                    // Reemplazar saltos de línea para que se vean bien en Google Sheets
                    text = text.Replace("\n", "\\n");
                    row.Add(text);
                }

                data.Add(row);
            }

            return data;
        }

        /// <summary>
        /// Procesa datos importados desde Google Sheets
        /// </summary>
        private static void ProcessImportedData(LocalizationDatabase database, List<List<string>> data, SyncResult result)
        {
            if (data == null || data.Count < 2)
            {
                result.errors.Add("La hoja está vacía o mal formateada");
                return;
            }

            // Leer encabezados
            var headers = data[0];
            var languages = new List<SystemLanguage>();
            var languageColumns = new Dictionary<SystemLanguage, int>();

            for (int i = 5; i < headers.Count; i++) // Saltar ID, Categoría, Contexto, MaxChars, Variables
            {
                if (Enum.TryParse(headers[i], out SystemLanguage lang))
                {
                    languages.Add(lang);
                    languageColumns[lang] = i;
                }
            }

            // Procesar cada fila
            Undo.RecordObject(database, "Import from Google Sheets");

            for (int rowIndex = 1; rowIndex < data.Count; rowIndex++)
            {
                var row = data[rowIndex];
                if (row.Count < 5) continue;

                string localizationID = row[0];
                if (string.IsNullOrEmpty(localizationID)) continue;

                // Obtener o crear entrada
                var entry = database.GetEntry(localizationID);
                if (entry == null)
                {
                    // Crear nueva entrada
                    entry = database.CreateOrUpdateEntry(localizationID);
                }

                Undo.RecordObject(database, "Update Localization Entry");

                // Actualizar metadatos
                if (row.Count > 1) entry.category = row[1];
                if (row.Count > 2) entry.context = row[2];
                if (row.Count > 3 && int.TryParse(row[3], out int maxChars))
                {
                    entry.maxCharacters = maxChars;
                }
                if (row.Count > 4) entry.supportsVariables = row[4].ToUpper() == "SÍ" || row[4].ToUpper() == "YES";

                // Actualizar traducciones
                foreach (var lang in languages)
                {
                    int colIndex = languageColumns[lang];
                    if (row.Count > colIndex)
                    {
                        string text = row[colIndex];
                        // Restaurar saltos de línea
                        text = text.Replace("\\n", "\n");
                        entry.localizedString.SetText(lang, text);
                    }
                }

                result.entriesProcessed++;
            }

            EditorUtility.SetDirty(database);
        }

        /// <summary>
        /// Obtiene el ID del spreadsheet de una URL de Google Sheets
        /// </summary>
        public static string ExtractSpreadsheetIdFromUrl(string url)
        {
            // URL típica: https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit
            if (string.IsNullOrEmpty(url)) return "";

            int startIndex = url.IndexOf("/d/");
            if (startIndex == -1) return "";

            startIndex += 3;
            int endIndex = url.IndexOf("/", startIndex);
            
            if (endIndex == -1)
            {
                return url.Substring(startIndex);
            }

            return url.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Valida la configuración de Google Sheets
        /// </summary>
        public static bool ValidateConfig(GoogleSheetsConfig config, out string error)
        {
            if (string.IsNullOrEmpty(config.spreadsheetId))
            {
                error = "Falta el ID del Spreadsheet";
                return false;
            }

            if (string.IsNullOrEmpty(config.apiKey))
            {
                error = "Falta la API Key de Google Cloud";
                return false;
            }

            if (string.IsNullOrEmpty(config.sheetName))
            {
                error = "Falta el nombre de la pestaña";
                return false;
            }

            error = "";
            return true;
        }

#endif

        /// <summary>
        /// Clase auxiliar para serialización JSON de Google Sheets
        /// </summary>
        [Serializable]
        private class SheetData
        {
            public List<List<string>> values;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// ScriptableObject para guardar la configuración de Google Sheets
    /// </summary>
    [CreateAssetMenu(fileName = "GoogleSheetsConfig", menuName = "Torbellino Studio/Google Sheets Config", order = 2)]
    public class GoogleSheetsConfigAsset : ScriptableObject
    {
        public GoogleSheetsIntegration.GoogleSheetsConfig config = new GoogleSheetsIntegration.GoogleSheetsConfig();

        [Header("Referencias")]
        [Tooltip("Base de datos de localización a sincronizar")]
        public LocalizationDatabase database;
    }
#endif
}
