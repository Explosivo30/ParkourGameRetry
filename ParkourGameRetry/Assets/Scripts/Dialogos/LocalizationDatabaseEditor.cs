using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Dialogo.Editor
{
    /// <summary>
    /// Inspector personalizado para LocalizationDatabase
    /// Proporciona una interfaz más amigable para editar traducciones
    /// </summary>
    [CustomEditor(typeof(LocalizationDatabase))]
    public class LocalizationDatabaseEditor : UnityEditor.Editor
    {
        private LocalizationDatabase database;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private SystemLanguage selectedLanguage = SystemLanguage.Spanish;
        private string newEntryID = "";
        private bool showAdvancedOptions = false;

        // Filtros
        private bool showOnlyIncomplete = false;
        private string categoryFilter = "";

        private void OnEnable()
        {
            database = (LocalizationDatabase)target;
            selectedLanguage = DialogEditorSettings.GetEditorLanguage();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(5);
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawFilters();
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawEntries();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawAddNewEntry();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Base de Datos de Localización", EditorStyles.largeLabel);
            EditorGUILayout.LabelField($"Total de entradas: {database.GetAllEntries().Count}", EditorStyles.miniLabel);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Selector de idioma
            GUILayout.Label("Idioma:", GUILayout.Width(60));
            SystemLanguage newLanguage = (SystemLanguage)EditorGUILayout.EnumPopup(
                selectedLanguage,
                GUILayout.Width(120)
            );

            if (newLanguage != selectedLanguage)
            {
                selectedLanguage = newLanguage;
                DialogEditorSettings.SetEditorLanguage(newLanguage);
            }

            GUILayout.FlexibleSpace();

            // Botón de validación
            if (GUILayout.Button("Validar", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                LocalizationValidatorWindow.ShowWindow();
            }

            // Botón de Google Sheets
            if (GUILayout.Button("Google Sheets", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GoogleSheetsWindow.ShowWindow();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // Búsqueda
            GUILayout.Label("🔍", GUILayout.Width(20));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
            
            // Filtro de incompletos
            showOnlyIncomplete = EditorGUILayout.Toggle("Solo incompletos", showOnlyIncomplete, GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();

            // Filtro de categoría
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Categoría:", GUILayout.Width(70));
            categoryFilter = EditorGUILayout.TextField(categoryFilter);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                categoryFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawEntries()
        {
            var entries = database.GetAllEntries()
                .Where(e => PassesFilters(e))
                .ToList();

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay entradas que coincidan con los filtros", MessageType.Info);
                return;
            }

            foreach (var entry in entries)
            {
                DrawEntry(entry);
                EditorGUILayout.Space(3);
            }
        }

        private bool PassesFilters(LocalizationEntry entry)
        {
            // Filtro de búsqueda
            if (!string.IsNullOrEmpty(searchFilter))
            {
                bool matchesID = entry.localizationID.ToLower().Contains(searchFilter.ToLower());
                bool matchesText = entry.localizedString.GetText(selectedLanguage).ToLower().Contains(searchFilter.ToLower());
                bool matchesContext = entry.context != null && entry.context.ToLower().Contains(searchFilter.ToLower());

                if (!matchesID && !matchesText && !matchesContext)
                {
                    return false;
                }
            }

            // Filtro de categoría
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                if (entry.category == null || !entry.category.Equals(categoryFilter, System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Filtro de incompletos
            if (showOnlyIncomplete)
            {
                bool hasEmptyTranslation = false;
                foreach (var lang in LocalizationManager.SupportedLanguages)
                {
                    if (string.IsNullOrEmpty(entry.localizedString.GetText(lang)))
                    {
                        hasEmptyTranslation = true;
                        break;
                    }
                }

                if (!hasEmptyTranslation)
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawEntry(LocalizationEntry entry)
        {
            // Color de fondo según estado
            Color bgColor = Color.white;
            string currentText = entry.localizedString.GetText(selectedLanguage);
            
            if (string.IsNullOrEmpty(currentText))
            {
                bgColor = new Color(1f, 0.8f, 0.8f); // Rojo claro si falta traducción
            }
            else if (entry.maxCharacters > 0 && currentText.Length > entry.maxCharacters)
            {
                bgColor = new Color(1f, 0.9f, 0.7f); // Naranja si excede límite
            }

            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header con ID y opciones
            EditorGUILayout.BeginHorizontal();
            
            // ID (no editable directamente, usar rename)
            EditorGUILayout.LabelField(entry.localizationID, EditorStyles.boldLabel);
            
            // Indicador de idioma actual
            GUILayout.Label($"[{GetLanguageShortCode(selectedLanguage)}]", EditorStyles.miniLabel, GUILayout.Width(30));

            GUILayout.FlexibleSpace();

            // Botón de opciones avanzadas
            if (GUILayout.Button("⚙️", GUILayout.Width(25)))
            {
                showAdvancedOptions = !showAdvancedOptions;
            }

            // Botón de borrar
            if (GUILayout.Button("🗑️", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Confirmar", 
                    $"¿Eliminar la entrada '{entry.localizationID}'?", 
                    "Sí", "No"))
                {
                    database.RemoveEntry(entry.localizationID);
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Opciones avanzadas (colapsable)
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Categoría:", GUILayout.Width(70));
                entry.category = EditorGUILayout.TextField(entry.category);
                EditorGUILayout.EndHorizontal();

                entry.context = EditorGUILayout.TextArea(entry.context, GUILayout.Height(40));

                EditorGUILayout.BeginHorizontal();
                entry.maxCharacters = EditorGUILayout.IntField("Máx. Caracteres:", entry.maxCharacters);
                entry.supportsVariables = EditorGUILayout.Toggle("Variables", entry.supportsVariables);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            else if (!string.IsNullOrEmpty(entry.context))
            {
                // Mostrar contexto aunque esté colapsado
                EditorGUILayout.LabelField(entry.context, EditorStyles.wordWrappedMiniLabel);
            }

            // Texto en el idioma seleccionado
            EditorGUILayout.LabelField($"Texto ({selectedLanguage}):");
            
            string newText = EditorGUILayout.TextArea(
                entry.localizedString.GetText(selectedLanguage),
                GUILayout.MinHeight(50)
            );

            if (newText != entry.localizedString.GetText(selectedLanguage))
            {
                Undo.RecordObject(database, "Edit Localization Text");
                entry.localizedString.SetText(selectedLanguage, newText);
                EditorUtility.SetDirty(database);
            }

            // Indicadores
            EditorGUILayout.BeginHorizontal();

            // Contador de caracteres
            if (entry.maxCharacters > 0)
            {
                bool overLimit = currentText.Length > entry.maxCharacters;
                GUIStyle counterStyle = new GUIStyle(EditorStyles.miniLabel);
                counterStyle.normal.textColor = overLimit ? Color.red : Color.gray;
                
                GUILayout.Label(
                    $"{currentText.Length}/{entry.maxCharacters} chars",
                    counterStyle,
                    GUILayout.Width(80)
                );
            }

            // Indicador de variables
            if (entry.supportsVariables)
            {
                GUILayout.Label("🔤 Variables", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            // Progreso de traducciones
            int translatedCount = 0;
            int totalLanguages = LocalizationManager.SupportedLanguages.Count;

            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                if (!string.IsNullOrEmpty(entry.localizedString.GetText(lang)))
                {
                    translatedCount++;
                }
            }

            GUIStyle progressStyle = new GUIStyle(EditorStyles.miniLabel);
            if (translatedCount == totalLanguages)
            {
                progressStyle.normal.textColor = Color.green;
            }
            else if (translatedCount == 0)
            {
                progressStyle.normal.textColor = Color.red;
            }

            GUILayout.Label($"{translatedCount}/{totalLanguages} idiomas", progressStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawAddNewEntry()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Añadir Nueva Entrada", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("ID:", GUILayout.Width(30));
            newEntryID = EditorGUILayout.TextField(newEntryID);

            if (GUILayout.Button("Generar ID", GUILayout.Width(100)))
            {
                newEntryID = database.GenerateUniqueID("NEW_ENTRY");
            }

            if (GUILayout.Button("➕ Crear", GUILayout.Width(80)))
            {
                if (string.IsNullOrEmpty(newEntryID))
                {
                    EditorUtility.DisplayDialog("Error", "El ID no puede estar vacío", "OK");
                }
                else if (database.ContainsID(newEntryID))
                {
                    EditorUtility.DisplayDialog("Error", $"Ya existe una entrada con el ID '{newEntryID}'", "OK");
                }
                else
                {
                    Undo.RecordObject(database, "Add Localization Entry");
                    database.CreateOrUpdateEntry(newEntryID);
                    newEntryID = "";
                    EditorUtility.SetDirty(database);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private string GetLanguageShortCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Spanish: return "ES";
                case SystemLanguage.English: return "EN";
                case SystemLanguage.Catalan: return "CA";
                case SystemLanguage.French: return "FR";
                case SystemLanguage.German: return "DE";
                case SystemLanguage.Italian: return "IT";
                case SystemLanguage.Portuguese: return "PT";
                default: return language.ToString().Substring(0, 2).ToUpper();
            }
        }
    }
}
