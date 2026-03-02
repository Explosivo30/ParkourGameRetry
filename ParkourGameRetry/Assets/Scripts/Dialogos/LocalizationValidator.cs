using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dialogo
{
    /// <summary>
    /// Sistema de validación de traducciones
    /// Encuentra traducciones faltantes, textos demasiado largos, etc.
    /// </summary>
    public class LocalizationValidator
    {
        /// <summary>
        /// Resultado de validación para una entrada
        /// </summary>
        public class ValidationResult
        {
            public LocalizationEntry entry;
            public List<ValidationIssue> issues = new List<ValidationIssue>();
            
            public bool HasIssues => issues.Count > 0;
            public bool HasCriticalIssues => issues.Any(i => i.severity == IssueSeverity.Critical);
        }

        /// <summary>
        /// Problema individual encontrado
        /// </summary>
        public class ValidationIssue
        {
            public IssueSeverity severity;
            public IssueType type;
            public string message;
            public SystemLanguage? affectedLanguage;
        }

        public enum IssueSeverity
        {
            Info,       // Información útil
            Warning,    // Advertencia, debería revisarse
            Critical    // Error crítico que debe corregirse
        }

        public enum IssueType
        {
            MissingTranslation,
            ExceedsCharacterLimit,
            EmptyID,
            DuplicateID,
            InvalidVariableFormat,
            MissingContext
        }

        /// <summary>
        /// Valida toda la base de datos de localización
        /// </summary>
        public static List<ValidationResult> ValidateDatabase(LocalizationDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("LocalizationDatabase es null");
                return new List<ValidationResult>();
            }

            var results = new List<ValidationResult>();
            var allEntries = database.GetAllEntries();
            var seenIDs = new HashSet<string>();

            foreach (var entry in allEntries)
            {
                if (entry == null) continue;

                var result = new ValidationResult { entry = entry };

                // Verificar ID vacío
                if (string.IsNullOrEmpty(entry.localizationID))
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Critical,
                        type = IssueType.EmptyID,
                        message = "La entrada no tiene ID"
                    });
                }
                else
                {
                    // Verificar ID duplicado
                    if (seenIDs.Contains(entry.localizationID))
                    {
                        result.issues.Add(new ValidationIssue
                        {
                            severity = IssueSeverity.Critical,
                            type = IssueType.DuplicateID,
                            message = $"ID duplicado: '{entry.localizationID}'"
                        });
                    }
                    seenIDs.Add(entry.localizationID);
                }

                // Verificar contexto faltante
                if (string.IsNullOrEmpty(entry.context))
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Info,
                        type = IssueType.MissingContext,
                        message = "Falta contexto para traductores"
                    });
                }

                // Verificar traducciones faltantes
                foreach (var lang in LocalizationManager.SupportedLanguages)
                {
                    string text = entry.localizedString.GetText(lang);
                    
                    if (string.IsNullOrEmpty(text))
                    {
                        result.issues.Add(new ValidationIssue
                        {
                            severity = IssueSeverity.Warning,
                            type = IssueType.MissingTranslation,
                            message = $"Falta traducción en {lang}",
                            affectedLanguage = lang
                        });
                    }
                    else
                    {
                        // Verificar límite de caracteres
                        if (entry.maxCharacters > 0 && text.Length > entry.maxCharacters)
                        {
                            result.issues.Add(new ValidationIssue
                            {
                                severity = IssueSeverity.Warning,
                                type = IssueType.ExceedsCharacterLimit,
                                message = $"Excede límite en {lang}: {text.Length}/{entry.maxCharacters} chars",
                                affectedLanguage = lang
                            });
                        }

                        // Verificar formato de variables si está habilitado
                        if (entry.supportsVariables)
                        {
                            if (!ValidateVariableFormat(text))
                            {
                                result.issues.Add(new ValidationIssue
                                {
                                    severity = IssueSeverity.Warning,
                                    type = IssueType.InvalidVariableFormat,
                                    message = $"Formato de variables inválido en {lang}",
                                    affectedLanguage = lang
                                });
                            }
                        }
                    }
                }

                if (result.HasIssues)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Valida todos los diálogos del proyecto
        /// </summary>
        public static List<DialogueValidationResult> ValidateAllDialogues()
        {
            var results = new List<DialogueValidationResult>();

#if UNITY_EDITOR
            // Encontrar todos los DialoguesAssetMenu en el proyecto
            string[] guids = AssetDatabase.FindAssets("t:DialoguesAssetMenu");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialoguesAssetMenu dialogue = AssetDatabase.LoadAssetAtPath<DialoguesAssetMenu>(path);
                
                if (dialogue != null)
                {
                    var result = ValidateDialogue(dialogue);
                    if (result.HasIssues)
                    {
                        results.Add(result);
                    }
                }
            }
#endif

            return results;
        }

        /// <summary>
        /// Valida un diálogo específico
        /// </summary>
        public static DialogueValidationResult ValidateDialogue(DialoguesAssetMenu dialogue)
        {
            var result = new DialogueValidationResult
            {
                dialogue = dialogue,
                dialogueName = dialogue.name
            };

            foreach (var node in dialogue.GetAllNodes())
            {
                if (node == null) continue;

                var nodeResult = ValidateDialogueNode(node);
                if (nodeResult.HasIssues)
                {
                    result.nodeResults.Add(nodeResult);
                }
            }

            return result;
        }

        /// <summary>
        /// Valida un nodo de diálogo individual
        /// </summary>
        public static NodeValidationResult ValidateDialogueNode(DialogoNode node)
        {
            var result = new NodeValidationResult
            {
                node = node,
                nodeName = node.name
            };

            // Verificar traducciones de nombre
            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                string speakerName = node.GetSpeakerName(lang);
                if (string.IsNullOrEmpty(speakerName))
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Warning,
                        type = IssueType.MissingTranslation,
                        message = $"Falta nombre del hablante en {lang}",
                        affectedLanguage = lang
                    });
                }

                string dialogue = node.GetDialogo(lang);
                if (string.IsNullOrEmpty(dialogue))
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Critical,
                        type = IssueType.MissingTranslation,
                        message = $"Falta diálogo en {lang}",
                        affectedLanguage = lang
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Genera un reporte de traducción completo
        /// </summary>
        public static TranslationReport GenerateReport(LocalizationDatabase database)
        {
            var report = new TranslationReport();
            var allEntries = database.GetAllEntries();

            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                var langStats = new LanguageStatistics
                {
                    language = lang,
                    totalEntries = allEntries.Count
                };

                foreach (var entry in allEntries)
                {
                    string text = entry.localizedString.GetText(lang);
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        langStats.translatedEntries++;
                        langStats.totalCharacters += text.Length;
                        langStats.totalWords += CountWords(text);
                    }
                }

                langStats.completionPercentage = allEntries.Count > 0 
                    ? (float)langStats.translatedEntries / allEntries.Count * 100f 
                    : 0f;

                report.languageStatistics.Add(langStats);
            }

            return report;
        }

        /// <summary>
        /// Valida el formato de variables en un texto
        /// </summary>
        private static bool ValidateVariableFormat(string text)
        {
            // Buscar patrones como {0}, {1}, {playerName}
            int openBraces = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    openBraces++;
                }
                else if (text[i] == '}')
                {
                    openBraces--;
                    if (openBraces < 0) return false; // } sin {
                }
            }

            return openBraces == 0; // Todas las llaves cerradas
        }

        /// <summary>
        /// Cuenta palabras en un texto
        /// </summary>
        private static int CountWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, 
                System.StringSplitOptions.RemoveEmptyEntries);
            
            return words.Length;
        }
    }

    /// <summary>
    /// Resultado de validación para un diálogo completo
    /// </summary>
    public class DialogueValidationResult
    {
        public DialoguesAssetMenu dialogue;
        public string dialogueName;
        public List<NodeValidationResult> nodeResults = new List<NodeValidationResult>();
        
        public bool HasIssues => nodeResults.Any(r => r.HasIssues);
        public bool HasCriticalIssues => nodeResults.Any(r => r.HasCriticalIssues);
        
        public int TotalIssues => nodeResults.Sum(r => r.issues.Count);
        public int CriticalIssues => nodeResults.Sum(r => r.issues.Count(i => i.severity == LocalizationValidator.IssueSeverity.Critical));
    }

    /// <summary>
    /// Resultado de validación para un nodo
    /// </summary>
    public class NodeValidationResult
    {
        public DialogoNode node;
        public string nodeName;
        public List<LocalizationValidator.ValidationIssue> issues = new List<LocalizationValidator.ValidationIssue>();
        
        public bool HasIssues => issues.Count > 0;
        public bool HasCriticalIssues => issues.Any(i => i.severity == LocalizationValidator.IssueSeverity.Critical);
    }

    /// <summary>
    /// Reporte completo de estadísticas de traducción
    /// </summary>
    public class TranslationReport
    {
        public List<LanguageStatistics> languageStatistics = new List<LanguageStatistics>();
        public System.DateTime generatedAt = System.DateTime.Now;
    }

    /// <summary>
    /// Estadísticas para un idioma
    /// </summary>
    public class LanguageStatistics
    {
        public SystemLanguage language;
        public int totalEntries;
        public int translatedEntries;
        public float completionPercentage;
        public int totalCharacters;
        public int totalWords;
    }
}
