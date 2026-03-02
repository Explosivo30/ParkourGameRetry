using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Dialogo.Editor
{
    public class DialogEditor : EditorWindow
    {
        DialoguesAssetMenu selectedDialogue = null;
        
        [NonSerialized]
        GUIStyle nodeStyle;

        [NonSerialized]
        GUIStyle playerNodeStyle;

        [NonSerialized] 
        GUIStyle textAreaStyle;

        [NonSerialized]
        GUIStyle toolbarStyle;

        [NonSerialized]
        DialogoNode draggingNode = null;

        [NonSerialized]
        Vector2 draggingOffset;

        [NonSerialized]
        DialogoNode creatingNode = null;

        [NonSerialized]
        DialogoNode deletingNode = null;

        [NonSerialized]
        DialogoNode linkingParentNode = null;

        Vector2 scrollPosition;
        
        [NonSerialized]
        bool draggingCanvas = false;

        [NonSerialized]
        Vector2 draggingCanvasOffset;

        // Idioma actual del editor
        private SystemLanguage currentEditorLanguage;

        const float canvasSize = 4000;
        const float backgroundSize = 50;
        const float toolbarHeight = 40;


        [MenuItem("Window/Editor Dialogo")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditor),false,"Dialog Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            DialoguesAssetMenu dialogue = EditorUtility.InstanceIDToObject(instanceID) as DialoguesAssetMenu;
            if(dialogue != null)
            {
                ShowEditorWindow();
                return true;
            }
            
            return false;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += SelectionChanged;
            currentEditorLanguage = DialogEditorSettings.GetEditorLanguage();

            // ESTILOS DE NODO
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.normal.textColor = Color.white;
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            nodeStyle.border = new RectOffset(12, 12, 12, 12);

            playerNodeStyle = new GUIStyle();
            playerNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            playerNodeStyle.normal.textColor = Color.white;
            playerNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            playerNodeStyle.border = new RectOffset(12, 12, 12, 12);

            // Estilo para el área de texto
            textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;

            // Estilo para la toolbar
            toolbarStyle = new GUIStyle(EditorStyles.toolbar);
        }

        private void SelectionChanged()
        {
            DialoguesAssetMenu newDialogue = Selection.activeObject as DialoguesAssetMenu;
            if(newDialogue != null)
            {
                selectedDialogue = newDialogue;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if(selectedDialogue == null)
            {
                EditorGUILayout.LabelField("Dialogue not selected");
            }
            else
            {
                DrawToolbar();
                ProcessEvents();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);
                Texture2D backgroundTex = Resources.Load("background") as Texture2D;
                Rect textCoords = new Rect(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
                GUI.DrawTextureWithTexCoords(canvas, backgroundTex, textCoords);

                foreach (DialogoNode node in selectedDialogue.GetAllNodes())
                {
                    DrawConnections(node);
                }

                foreach (DialogoNode node in selectedDialogue.GetAllNodes())
                {
                    DrawNode(node);
                }

                EditorGUILayout.EndScrollView();

                if(creatingNode != null)
                {
                    Undo.RecordObject(selectedDialogue, "Dialogo Añadido");
                    selectedDialogue.CreateNode(creatingNode);
                    creatingNode = null;
                }

                if(deletingNode != null)
                {
                    selectedDialogue.DeleteNode(deletingNode);
                    deletingNode = null;
                }
            }
        }

        /// <summary>
        /// Dibuja la barra de herramientas superior con selector de idioma
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(toolbarHeight));

            GUILayout.Label("Idioma de Edición:", GUILayout.Width(120));

            // Selector de idioma
            SystemLanguage newLanguage = (SystemLanguage)EditorGUILayout.EnumPopup(
                currentEditorLanguage, 
                GUILayout.Width(120)
            );

            if (newLanguage != currentEditorLanguage)
            {
                currentEditorLanguage = newLanguage;
                DialogEditorSettings.SetEditorLanguage(newLanguage);
                Repaint();
            }

            GUILayout.FlexibleSpace();

            // Botón para asegurar que todos los nodos tienen todos los idiomas
            if (GUILayout.Button("Verificar Idiomas", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                EnsureAllNodesHaveAllLanguages();
            }

            // Botón para exportar a CSV (futura funcionalidad)
            if (GUILayout.Button("Exportar CSV", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ExportToCSV();
            }

            // Botón para importar desde CSV (futura funcionalidad)
            if (GUILayout.Button("Importar CSV", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ImportFromCSV();
            }

            GUILayout.Space(10);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Asegura que todos los nodos tienen entradas para todos los idiomas
        /// </summary>
        private void EnsureAllNodesHaveAllLanguages()
        {
            if (selectedDialogue == null) return;

            Undo.RecordObject(selectedDialogue, "Ensure All Languages");
            
            foreach (DialogoNode node in selectedDialogue.GetAllNodes())
            {
                Undo.RecordObject(node, "Ensure All Languages");
                node.EnsureAllLanguages();
            }

            EditorUtility.DisplayDialog(
                "Verificación Completa", 
                "Todos los nodos ahora tienen entradas para todos los idiomas soportados.", 
                "OK"
            );
        }

        /// <summary>
        /// Exporta todos los diálogos a un archivo CSV para traductores
        /// </summary>
        private void ExportToCSV()
        {
            if (selectedDialogue == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Exportar Diálogos a CSV",
                "",
                selectedDialogue.name + "_localization.csv",
                "csv"
            );

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                List<string> lines = new List<string>();
                
                // Encabezados
                List<SystemLanguage> languages = LocalizationManager.SupportedLanguages;
                string header = "NodeID,Field,IsPlayerSpeaking";
                foreach (var lang in languages)
                {
                    header += "," + lang.ToString();
                }
                lines.Add(header);

                // Datos de cada nodo
                foreach (DialogoNode node in selectedDialogue.GetAllNodes())
                {
                    // Línea para el nombre del hablante
                    string speakerLine = $"{node.name},SpeakerName,{node.IsPlayerSpeaking()}";
                    foreach (var lang in languages)
                    {
                        string text = node.GetSpeakerName(lang).Replace("\"", "\"\""); // Escapar comillas
                        speakerLine += $",\"{text}\"";
                    }
                    lines.Add(speakerLine);

                    // Línea para el diálogo
                    string dialogLine = $"{node.name},Dialogue,{node.IsPlayerSpeaking()}";
                    foreach (var lang in languages)
                    {
                        string text = node.GetDialogo(lang).Replace("\"", "\"\""); // Escapar comillas
                        text = text.Replace("\n", "\\n"); // Escapar saltos de línea
                        dialogLine += $",\"{text}\"";
                    }
                    lines.Add(dialogLine);
                }

                System.IO.File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);

                EditorUtility.DisplayDialog(
                    "Exportación Exitosa",
                    $"Diálogos exportados a:\n{path}\n\nAhora puedes compartir este archivo con traductores.",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", "Error al exportar: " + e.Message, "OK");
            }
        }

        /// <summary>
        /// Importa traducciones desde un archivo CSV
        /// </summary>
        private void ImportFromCSV()
        {
            if (selectedDialogue == null) return;

            string path = EditorUtility.OpenFilePanel(
                "Importar Traducciones desde CSV",
                "",
                "csv"
            );

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
                
                if (lines.Length < 2)
                {
                    EditorUtility.DisplayDialog("Error", "El archivo CSV está vacío o mal formateado.", "OK");
                    return;
                }

                // Parsear encabezados
                string[] headers = ParseCSVLine(lines[0]);
                List<SystemLanguage> languages = new List<SystemLanguage>();
                
                for (int i = 3; i < headers.Length; i++) // Saltar NodeID, Field, IsPlayerSpeaking
                {
                    if (System.Enum.TryParse(headers[i], out SystemLanguage lang))
                    {
                        languages.Add(lang);
                    }
                }

                Undo.RecordObject(selectedDialogue, "Import Translations");

                // Procesar cada línea
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < 4) continue;

                    string nodeID = values[0];
                    string field = values[1];

                    DialogoNode node = selectedDialogue.GetNodeByName(nodeID);
                    if (node == null) continue;

                    Undo.RecordObject(node, "Import Translations");

                    // Importar traducciones
                    for (int langIndex = 0; langIndex < languages.Count && (langIndex + 3) < values.Length; langIndex++)
                    {
                        string text = values[langIndex + 3];
                        text = text.Replace("\\n", "\n"); // Restaurar saltos de línea

                        if (field == "SpeakerName")
                        {
                            node.SetSpeakerName(text, languages[langIndex]);
                        }
                        else if (field == "Dialogue")
                        {
                            node.SetDialogo(text, languages[langIndex]);
                        }
                    }
                }

                EditorUtility.DisplayDialog(
                    "Importación Exitosa",
                    "Traducciones importadas correctamente.",
                    "OK"
                );

                Repaint();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", "Error al importar: " + e.Message, "OK");
            }
        }

        /// <summary>
        /// Parsea una línea CSV respetando comillas
        /// </summary>
        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);
            return result.ToArray();
        }

        void ProcessEvents()
        {
            if(Event.current.type == EventType.MouseDown && draggingNode == null)
            {
                draggingNode = GetNodeAtPoint(Event.current.mousePosition + scrollPosition);
                if(draggingNode != null)
                {
                    draggingOffset = draggingNode.GetRect().position - Event.current.mousePosition;
                    Selection.activeObject = draggingNode;
                }
                else
                {
                    draggingCanvas = true;
                    draggingCanvasOffset = Event.current.mousePosition + scrollPosition;
                    Selection.activeObject = selectedDialogue;
                }
            }
            else if(Event.current.type == EventType.MouseDrag && draggingNode != null)
            {
                draggingNode.SetPosition(Event.current.mousePosition + draggingOffset);
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseDrag && draggingCanvas)
            {
                scrollPosition = draggingCanvasOffset - Event.current.mousePosition;
                GUI.changed = true;
            }
            else if(Event.current.type == EventType.MouseUp && draggingNode != null)
            {
                draggingNode = null;
            }
            else if (Event.current.type == EventType.MouseUp && draggingCanvas)
            {
                draggingCanvas = false;
            }
        }

        private void DrawNode(DialogoNode node)
        {
            GUIStyle style = nodeStyle;
            if (node.IsPlayerSpeaking())
            {
                style = playerNodeStyle;
            }

            // CALCULO DINAMICO DE ALTURA
            float nodeWidth = 200f;
            float textWidth = nodeWidth - 40f;
            
            // Obtener texto en el idioma actual del editor
            string currentDialogText = node.GetDialogo(currentEditorLanguage);
            GUIContent content = new GUIContent(currentDialogText);
            float textHeight = textAreaStyle.CalcHeight(content, textWidth);

            float baseHeight = 130f;
            node.SetSize(new Vector2(nodeWidth, baseHeight + textHeight));

            //DIBUJADO
            GUILayout.BeginArea(node.GetRect(), style);

            // Indicador de idioma actual (pequeño)
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{GetLanguageShortCode(currentEditorLanguage)}]", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            // Zona Superior: Nombre
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nombre:", GUILayout.Width(50));
            string newName = EditorGUILayout.TextField(node.GetSpeakerName(currentEditorLanguage));
            node.SetSpeakerName(newName, currentEditorLanguage);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Zona Central: Texto
            GUILayout.Label("Diálogo:");
            string newText = EditorGUILayout.TextArea(currentDialogText, textAreaStyle, GUILayout.Height(textHeight + 10));
            node.SetDialogo(newText, currentEditorLanguage);

            GUILayout.Space(5);

            // Zona Inferior: Botones
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                deletingNode = node;
            }

            DrawLinkButton(node);

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                creatingNode = node;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Obtiene código corto del idioma para mostrar en el nodo
        /// </summary>
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
                case SystemLanguage.Japanese: return "JP";
                case SystemLanguage.Chinese: return "CN";
                default: return language.ToString().Substring(0, 2).ToUpper();
            }
        }

        private void DrawLinkButton(DialogoNode node)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("Link"))
                {
                    linkingParentNode = node;
                }
            }
            else if(linkingParentNode == node)
            {
                if(GUILayout.Button("Cancel")) 
                {
                    linkingParentNode = null;
                } 
            }
            else if (linkingParentNode.GetRespuestas().Contains(node.name))
            {
                if (GUILayout.Button("Unlink"))
                { 
                    linkingParentNode.RemoveRespuesta(node.name);
                    linkingParentNode = null;
                }
            }
            else
            {
                if (GUILayout.Button("respuesta"))
                {
                    linkingParentNode.AddRespuesta(node.name);
                    linkingParentNode = null;
                }
            }
        }

        private void DrawConnections(DialogoNode node)
        {
            foreach(DialogoNode childNode in selectedDialogue.GetAllChildren(node))
            {
                if (childNode == null) continue;
                Vector3 startPosition = new Vector2(node.GetRect().xMax, node.GetRect().center.y);
                Vector3 endPosition = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                Vector3 controlPointOffset = endPosition - startPosition;
                controlPointOffset.y = 0;
                controlPointOffset.x *= 0.8f;
                Handles.DrawBezier(
                    startPosition, endPosition,
                    startPosition + controlPointOffset,
                    endPosition - controlPointOffset,
                    Color.white, null, 5f);
            }
        }

        private DialogoNode GetNodeAtPoint(Vector2 point)
        {
            DialogoNode foundNode = null;
            foreach(DialogoNode node in selectedDialogue.GetAllNodes())
            {
                if (node.GetRect().Contains(point))
                {
                    foundNode = node;
                }
            }

            return foundNode;
        }
    }
}
