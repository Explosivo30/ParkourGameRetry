using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Dialogo.Editor
{
    /// <summary>
    /// Ventana del editor para mostrar resultados de validación y estadísticas
    /// </summary>
    public class LocalizationValidatorWindow : EditorWindow
    {
        private LocalizationDatabase database;
        private Vector2 scrollPosition;
        private List<LocalizationValidator.ValidationResult> validationResults;
        private TranslationReport report;
        private bool showOnlyIssues = true;
        private bool showCriticalOnly = false;
        private LocalizationValidator.IssueType filterType = (LocalizationValidator.IssueType)(-1);

        // Tabs
        private int selectedTab = 0;
        private string[] tabNames = { "Validación", "Estadísticas", "Diálogos" };

        // Dialogue validation
        private List<DialogueValidationResult> dialogueResults;

        [MenuItem("Window/Localization Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationValidatorWindow>("Localization Validator");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawHeader();
            
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawValidationTab();
                    break;
                case 1:
                    DrawStatisticsTab();
                    break;
                case 2:
                    DrawDialoguesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Base de Datos:", GUILayout.Width(100));
            database = (LocalizationDatabase)EditorGUILayout.ObjectField(
                database, 
                typeof(LocalizationDatabase), 
                false, 
                GUILayout.Width(200)
            );

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Validar Todo", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RunValidation();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationTab()
        {
            if (database == null)
            {
                EditorGUILayout.HelpBox("Selecciona una LocalizationDatabase para comenzar", MessageType.Info);
                return;
            }

            // Filtros
            EditorGUILayout.BeginHorizontal();
            showOnlyIssues = EditorGUILayout.Toggle("Solo con problemas", showOnlyIssues);
            showCriticalOnly = EditorGUILayout.Toggle("Solo críticos", showCriticalOnly);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (validationResults == null || validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("Haz clic en 'Validar Todo' para verificar traducciones", MessageType.Info);
                return;
            }

            // Resumen
            int totalIssues = validationResults.Sum(r => r.issues.Count);
            int criticalIssues = validationResults.Sum(r => r.issues.Count(i => i.severity == LocalizationValidator.IssueSeverity.Critical));
            int warnings = validationResults.Sum(r => r.issues.Count(i => i.severity == LocalizationValidator.IssueSeverity.Warning));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Resumen de Validación", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Entradas revisadas: {database.GetAllEntries().Count}");
            EditorGUILayout.LabelField($"Problemas críticos: {criticalIssues}", 
                criticalIssues > 0 ? new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } } : EditorStyles.label);
            EditorGUILayout.LabelField($"Advertencias: {warnings}",
                warnings > 0 ? new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.5f, 0f) } } : EditorStyles.label);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Lista de problemas
            foreach (var result in validationResults)
            {
                if (showOnlyIssues && !result.HasIssues) continue;
                if (showCriticalOnly && !result.HasCriticalIssues) continue;

                DrawValidationResult(result);
            }
        }

        private void DrawValidationResult(LocalizationValidator.ValidationResult result)
        {
            Color bgColor = Color.white;
            if (result.HasCriticalIssues)
                bgColor = new Color(1f, 0.7f, 0.7f);
            else if (result.HasIssues)
                bgColor = new Color(1f, 0.9f, 0.7f);

            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Título
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(result.entry.localizationID, EditorStyles.boldLabel);
            
            if (result.HasCriticalIssues)
            {
                GUILayout.Label("⚠️ CRÍTICO", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
            }
            
            EditorGUILayout.EndHorizontal();

            // Contexto
            if (!string.IsNullOrEmpty(result.entry.context))
            {
                EditorGUILayout.LabelField("Contexto: " + result.entry.context, EditorStyles.wordWrappedMiniLabel);
            }

            // Problemas
            foreach (var issue in result.issues)
            {
                DrawIssue(issue);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawIssue(LocalizationValidator.ValidationIssue issue)
        {
            EditorGUILayout.BeginHorizontal();

            string icon = "";
            Color color = Color.white;

            switch (issue.severity)
            {
                case LocalizationValidator.IssueSeverity.Critical:
                    icon = "⛔";
                    color = Color.red;
                    break;
                case LocalizationValidator.IssueSeverity.Warning:
                    icon = "⚠️";
                    color = new Color(1f, 0.5f, 0f);
                    break;
                case LocalizationValidator.IssueSeverity.Info:
                    icon = "ℹ️";
                    color = Color.cyan;
                    break;
            }

            GUILayout.Label(icon, GUILayout.Width(20));
            GUILayout.Label(issue.message, new GUIStyle(EditorStyles.label) { normal = { textColor = color } });

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatisticsTab()
        {
            if (database == null)
            {
                EditorGUILayout.HelpBox("Selecciona una LocalizationDatabase para ver estadísticas", MessageType.Info);
                return;
            }

            if (report == null)
            {
                report = LocalizationValidator.GenerateReport(database);
            }

            EditorGUILayout.LabelField("Estadísticas de Traducción", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Generado: {report.generatedAt:dd/MM/yyyy HH:mm}");
            EditorGUILayout.Space(10);

            // Mostrar estadísticas por idioma
            foreach (var langStats in report.languageStatistics)
            {
                DrawLanguageStatistics(langStats);
            }

            // Botón de actualizar
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Actualizar Estadísticas", GUILayout.Height(30)))
            {
                report = LocalizationValidator.GenerateReport(database);
                Repaint();
            }
        }

        private void DrawLanguageStatistics(LanguageStatistics stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Nombre del idioma
            EditorGUILayout.LabelField(GetLanguageDisplayName(stats.language), EditorStyles.boldLabel);

            // Barra de progreso
            Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, stats.completionPercentage / 100f, 
                $"{stats.translatedEntries}/{stats.totalEntries} ({stats.completionPercentage:F1}%)");

            // Detalles
            EditorGUILayout.LabelField($"Palabras: {stats.totalWords:N0}");
            EditorGUILayout.LabelField($"Caracteres: {stats.totalCharacters:N0}");

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawDialoguesTab()
        {
            EditorGUILayout.LabelField("Validación de Diálogos", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Validar Todos los Diálogos", GUILayout.Height(30)))
            {
                dialogueResults = LocalizationValidator.ValidateAllDialogues();
                Repaint();
            }

            EditorGUILayout.Space(10);

            if (dialogueResults == null || dialogueResults.Count == 0)
            {
                EditorGUILayout.HelpBox("Haz clic en 'Validar Todos los Diálogos' para comenzar", MessageType.Info);
                return;
            }

            // Resumen
            int totalDialogues = dialogueResults.Count;
            int dialoguesWithIssues = dialogueResults.Count(d => d.HasIssues);
            int totalIssues = dialogueResults.Sum(d => d.TotalIssues);
            int criticalIssues = dialogueResults.Sum(d => d.CriticalIssues);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Diálogos con problemas: {dialoguesWithIssues}/{totalDialogues}");
            EditorGUILayout.LabelField($"Problemas críticos: {criticalIssues}",
                criticalIssues > 0 ? new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } } : EditorStyles.label);
            EditorGUILayout.LabelField($"Total de problemas: {totalIssues}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Lista de diálogos con problemas
            foreach (var result in dialogueResults.Where(d => d.HasIssues))
            {
                DrawDialogueResult(result);
            }
        }

        private void DrawDialogueResult(DialogueValidationResult result)
        {
            Color bgColor = result.HasCriticalIssues ? new Color(1f, 0.7f, 0.7f) : new Color(1f, 0.9f, 0.7f);
            
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Título con botón para abrir
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(result.dialogueName, EditorStyles.boldLabel);
            
            if (GUILayout.Button("Abrir", GUILayout.Width(60)))
            {
                Selection.activeObject = result.dialogue;
                DialogEditor.ShowEditorWindow();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Problemas: {result.TotalIssues} (Críticos: {result.CriticalIssues})");

            // Mostrar nodos con problemas
            foreach (var nodeResult in result.nodeResults.Where(n => n.HasIssues))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Nodo: {nodeResult.nodeName}", EditorStyles.miniBoldLabel);
                
                foreach (var issue in nodeResult.issues)
                {
                    DrawIssue(issue);
                }
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void RunValidation()
        {
            if (database == null)
            {
                EditorUtility.DisplayDialog("Error", "Selecciona una LocalizationDatabase primero", "OK");
                return;
            }

            validationResults = LocalizationValidator.ValidateDatabase(database);
            report = LocalizationValidator.GenerateReport(database);
            Repaint();

            int totalIssues = validationResults.Sum(r => r.issues.Count);
            int criticalIssues = validationResults.Sum(r => r.issues.Count(i => i.severity == LocalizationValidator.IssueSeverity.Critical));

            if (totalIssues == 0)
            {
                EditorUtility.DisplayDialog("Validación Completa", 
                    "¡No se encontraron problemas! Todas las traducciones están correctas.", 
                    "¡Genial!");
            }
            else
            {
                EditorUtility.DisplayDialog("Validación Completa",
                    $"Se encontraron {totalIssues} problemas:\n" +
                    $"- {criticalIssues} críticos\n" +
                    $"- {totalIssues - criticalIssues} advertencias",
                    "Ver Resultados");
            }
        }

        private string GetLanguageDisplayName(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Spanish: return "🇪🇸 Español";
                case SystemLanguage.English: return "🇬🇧 English";
                case SystemLanguage.Catalan: return "🏴 Català";
                case SystemLanguage.French: return "🇫🇷 Français";
                case SystemLanguage.German: return "🇩🇪 Deutsch";
                case SystemLanguage.Italian: return "🇮🇹 Italiano";
                case SystemLanguage.Portuguese: return "🇵🇹 Português";
                default: return language.ToString();
            }
        }
    }
}
