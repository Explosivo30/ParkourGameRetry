using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Dialogo.Editor
{
    /// <summary>
    /// Ventana del editor para gestionar la integración con Google Sheets
    /// </summary>
    public class GoogleSheetsWindow : EditorWindow
    {
        private GoogleSheetsConfigAsset configAsset;
        private Vector2 scrollPosition;
        private bool isSyncing = false;
        private string lastSyncMessage = "";
        private MessageType lastSyncMessageType = MessageType.Info;

        [MenuItem("Window/Google Sheets Integration")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleSheetsWindow>("Google Sheets");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawConfiguration();
            EditorGUILayout.Space(10);
            DrawActions();
            EditorGUILayout.Space(10);
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Integración con Google Sheets", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Sincroniza traducciones en tiempo real con traductores", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Archivo de Configuración:", GUILayout.Width(150));
            configAsset = (GoogleSheetsConfigAsset)EditorGUILayout.ObjectField(
                configAsset,
                typeof(GoogleSheetsConfigAsset),
                false
            );
            EditorGUILayout.EndHorizontal();

            if (configAsset == null)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Crear Nueva Configuración"))
                {
                    CreateNewConfig();
                }
            }
        }

        private void DrawConfiguration()
        {
            if (configAsset == null)
            {
                EditorGUILayout.HelpBox("Crea o selecciona una configuración para comenzar", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Configuración", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Base de datos
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base de Datos:", GUILayout.Width(150));
            configAsset.database = (LocalizationDatabase)EditorGUILayout.ObjectField(
                configAsset.database,
                typeof(LocalizationDatabase),
                false
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // URL de Google Sheets
            EditorGUILayout.LabelField("URL de Google Sheets:");
            EditorGUILayout.BeginHorizontal();
            string url = EditorGUILayout.TextField(configAsset.config.spreadsheetId);
            
            // Si pegaron una URL completa, extraer el ID
            if (url != configAsset.config.spreadsheetId && url.Contains("docs.google.com"))
            {
                configAsset.config.spreadsheetId = GoogleSheetsIntegration.ExtractSpreadsheetIdFromUrl(url);
                EditorUtility.SetDirty(configAsset);
            }
            else
            {
                configAsset.config.spreadsheetId = url;
            }

            if (GUILayout.Button("Pegar URL", GUILayout.Width(80)))
            {
                string clipboardUrl = GUIUtility.systemCopyBuffer;
                if (clipboardUrl.Contains("docs.google.com"))
                {
                    configAsset.config.spreadsheetId = GoogleSheetsIntegration.ExtractSpreadsheetIdFromUrl(clipboardUrl);
                    EditorUtility.SetDirty(configAsset);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // API Key
            EditorGUILayout.LabelField("API Key de Google Cloud:");
            configAsset.config.apiKey = EditorGUILayout.PasswordField(configAsset.config.apiKey);

            EditorGUILayout.Space(5);

            // Nombre de la pestaña
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Nombre de la Pestaña:", GUILayout.Width(150));
            configAsset.config.sheetName = EditorGUILayout.TextField(configAsset.config.sheetName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Opciones automáticas
            configAsset.config.autoFetchOnStartup = EditorGUILayout.Toggle(
                "Actualizar al iniciar Unity",
                configAsset.config.autoFetchOnStartup
            );

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Intervalo auto-actualización (min):", GUILayout.Width(200));
            configAsset.config.autoUpdateInterval = EditorGUILayout.IntField(
                configAsset.config.autoUpdateInterval,
                GUILayout.Width(50)
            );
            EditorGUILayout.LabelField("(0 = desactivado)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Guardar cambios
            if (GUI.changed)
            {
                EditorUtility.SetDirty(configAsset);
            }
        }

        private void DrawActions()
        {
            if (configAsset == null || configAsset.database == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.enabled = !isSyncing;

            // Validar configuración
            string validationError;
            bool isValid = GoogleSheetsIntegration.ValidateConfig(configAsset.config, out validationError);

            if (!isValid)
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();

            // Exportar
            if (GUILayout.Button("⬆️ Exportar a Google Sheets", GUILayout.Height(40)))
            {
                if (isValid)
                {
                    ExportToSheets();
                }
            }

            // Importar
            if (GUILayout.Button("⬇️ Importar desde Google Sheets", GUILayout.Height(40)))
            {
                if (isValid)
                {
                    ImportFromSheets();
                }
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            // Mostrar estado de sincronización
            if (isSyncing)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Sincronizando...", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Por favor espera mientras se completa la sincronización", MessageType.Info);
            }

            // Mostrar último mensaje
            if (!string.IsNullOrEmpty(lastSyncMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(lastSyncMessage, lastSyncMessageType);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("📖 Instrucciones de Configuración", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Paso 1: Crear una hoja de Google Sheets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "• Ve a Google Sheets y crea una nueva hoja\n" +
                "• Copia la URL completa de la hoja\n" +
                "• Pégala en el campo 'URL de Google Sheets' arriba",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Paso 2: Obtener una API Key de Google Cloud", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "• Ve a: https://console.cloud.google.com/apis/credentials\n" +
                "• Crea un nuevo proyecto (si no tienes uno)\n" +
                "• Haz clic en 'Crear Credenciales' → 'Clave de API'\n" +
                "• Copia la API Key y pégala arriba\n" +
                "• IMPORTANTE: Habilita 'Google Sheets API' en tu proyecto",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Paso 3: Configurar permisos de la hoja", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "• En tu hoja de Google Sheets, haz clic en 'Compartir'\n" +
                "• Cambia a 'Cualquier persona con el enlace puede ver'\n" +
                "• Esto permite que Unity lea/escriba la hoja",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Paso 4: Sincronizar", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "• Exportar: Sube tus traducciones actuales a Google Sheets\n" +
                "• Importar: Descarga las traducciones desde Google Sheets\n" +
                "• Los traductores pueden trabajar directamente en Google Sheets",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Botón de ayuda
            if (GUILayout.Button("🌐 Abrir Documentación de Google Cloud"))
            {
                Application.OpenURL("https://console.cloud.google.com/apis/credentials");
            }
        }

        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Guardar Configuración de Google Sheets",
                "GoogleSheetsConfig",
                "asset",
                "Selecciona dónde guardar la configuración"
            );

            if (!string.IsNullOrEmpty(path))
            {
                configAsset = CreateInstance<GoogleSheetsConfigAsset>();
                AssetDatabase.CreateAsset(configAsset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = configAsset;
            }
        }

        private void ExportToSheets()
        {
            if (!EditorUtility.DisplayDialog(
                "Exportar a Google Sheets",
                "Esto sobrescribirá el contenido actual de la hoja de Google Sheets.\n\n¿Continuar?",
                "Sí, Exportar",
                "Cancelar"))
            {
                return;
            }

            isSyncing = true;
            lastSyncMessage = "";
            Repaint();

            EditorCoroutineUtility.StartCoroutine(
                GoogleSheetsIntegration.ExportToGoogleSheets(
                    configAsset.database,
                    configAsset.config,
                    OnExportComplete
                ),
                this
            );
        }

        private void OnExportComplete(GoogleSheetsIntegration.SyncResult result)
        {
            isSyncing = false;
            lastSyncMessage = result.message;
            lastSyncMessageType = result.success ? MessageType.Info : MessageType.Error;

            if (result.success)
            {
                EditorUtility.DisplayDialog(
                    "Exportación Exitosa",
                    $"Se exportaron {result.entriesProcessed} entradas a Google Sheets.\n\n" +
                    "Los traductores ya pueden trabajar en la hoja.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Error al Exportar",
                    result.message,
                    "OK"
                );
            }

            Repaint();
        }

        private void ImportFromSheets()
        {
            if (!EditorUtility.DisplayDialog(
                "Importar desde Google Sheets",
                "Esto sobrescribirá las traducciones actuales en Unity.\n\n" +
                "Se recomienda hacer un backup primero.\n\n¿Continuar?",
                "Sí, Importar",
                "Cancelar"))
            {
                return;
            }

            isSyncing = true;
            lastSyncMessage = "";
            Repaint();

            EditorCoroutineUtility.StartCoroutine(
                GoogleSheetsIntegration.ImportFromGoogleSheets(
                    configAsset.database,
                    configAsset.config,
                    OnImportComplete
                ),
                this
            );
        }

        private void OnImportComplete(GoogleSheetsIntegration.SyncResult result)
        {
            isSyncing = false;
            lastSyncMessage = result.message;
            lastSyncMessageType = result.success ? MessageType.Info : MessageType.Error;

            if (result.success)
            {
                EditorUtility.DisplayDialog(
                    "Importación Exitosa",
                    $"Se importaron {result.entriesProcessed} entradas desde Google Sheets.\n\n" +
                    "Las traducciones han sido actualizadas.",
                    "OK"
                );

                // Refrescar el proyecto
                AssetDatabase.Refresh();
            }
            else
            {
                string errorDetails = result.errors.Count > 0 
                    ? "\n\nErrores:\n" + string.Join("\n", result.errors) 
                    : "";

                EditorUtility.DisplayDialog(
                    "Error al Importar",
                    result.message + errorDetails,
                    "OK"
                );
            }

            Repaint();
        }
    }

    /// <summary>
    /// Utilidad para ejecutar corrutinas en el Editor
    /// </summary>
    public static class EditorCoroutineUtility
    {
        public static void StartCoroutine(IEnumerator routine, object owner)
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= callback;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error en corrutina del editor: {e.Message}");
                    EditorApplication.update -= callback;
                }
            };

            EditorApplication.update += callback;
        }
    }
}
