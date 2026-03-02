using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dialogo
{
    /// <summary>
    /// Base de datos centralizada de todas las traducciones del juego
    /// Permite reutilizar textos y gestionar traducciones globalmente
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "Torbellino Studio/Localization Database", order = 1)]
    public class LocalizationDatabase : ScriptableObject
    {
        [SerializeField]
        private List<LocalizationEntry> entries = new List<LocalizationEntry>();

        private Dictionary<string, LocalizationEntry> entryLookup = new Dictionary<string, LocalizationEntry>();

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }

        /// <summary>
        /// Obtiene el texto localizado por su ID
        /// </summary>
        public string GetText(string localizationID, params object[] variables)
        {
            BuildLookup();

            if (string.IsNullOrEmpty(localizationID))
            {
                return "";
            }

            if (entryLookup.TryGetValue(localizationID, out LocalizationEntry entry))
            {
                string text = entry.localizedString.GetText();
                
                // Aplicar variables si las hay
                if (variables != null && variables.Length > 0)
                {
                    try
                    {
                        text = string.Format(text, variables);
                    }
                    catch (FormatException e)
                    {
                        Debug.LogError($"Error al formatear texto '{localizationID}': {e.Message}");
                    }
                }

                return text;
            }

            Debug.LogWarning($"Localization ID '{localizationID}' no encontrado en la base de datos");
            return $"[MISSING: {localizationID}]";
        }

        /// <summary>
        /// Obtiene el texto localizado en un idioma específico
        /// </summary>
        public string GetText(string localizationID, SystemLanguage language, params object[] variables)
        {
            BuildLookup();

            if (entryLookup.TryGetValue(localizationID, out LocalizationEntry entry))
            {
                string text = entry.localizedString.GetText(language);
                
                if (variables != null && variables.Length > 0)
                {
                    try
                    {
                        text = string.Format(text, variables);
                    }
                    catch (FormatException e)
                    {
                        Debug.LogError($"Error al formatear texto '{localizationID}': {e.Message}");
                    }
                }

                return text;
            }

            return $"[MISSING: {localizationID}]";
        }

        /// <summary>
        /// Obtiene una entrada completa por su ID
        /// </summary>
        public LocalizationEntry GetEntry(string localizationID)
        {
            BuildLookup();
            
            if (entryLookup.TryGetValue(localizationID, out LocalizationEntry entry))
            {
                return entry;
            }

            return null;
        }

        /// <summary>
        /// Verifica si existe un ID en la base de datos
        /// </summary>
        public bool ContainsID(string localizationID)
        {
            BuildLookup();
            return entryLookup.ContainsKey(localizationID);
        }

        /// <summary>
        /// Obtiene todas las entradas
        /// </summary>
        public List<LocalizationEntry> GetAllEntries()
        {
            return entries;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Crea o actualiza una entrada en la base de datos
        /// </summary>
        public LocalizationEntry CreateOrUpdateEntry(string localizationID, string context = "", int maxCharacters = 0)
        {
            BuildLookup();

            if (entryLookup.TryGetValue(localizationID, out LocalizationEntry existing))
            {
                return existing;
            }

            LocalizationEntry newEntry = new LocalizationEntry
            {
                localizationID = localizationID,
                context = context,
                maxCharacters = maxCharacters,
                localizedString = new LocalizedString()
            };

            UnityEditor.Undo.RecordObject(this, "Add Localization Entry");
            entries.Add(newEntry);
            entryLookup[localizationID] = newEntry;
            UnityEditor.EditorUtility.SetDirty(this);

            return newEntry;
        }

        /// <summary>
        /// Elimina una entrada de la base de datos
        /// </summary>
        public void RemoveEntry(string localizationID)
        {
            BuildLookup();

            if (entryLookup.TryGetValue(localizationID, out LocalizationEntry entry))
            {
                UnityEditor.Undo.RecordObject(this, "Remove Localization Entry");
                entries.Remove(entry);
                entryLookup.Remove(localizationID);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Renombra un ID (útil para refactoring)
        /// </summary>
        public void RenameID(string oldID, string newID)
        {
            BuildLookup();

            if (entryLookup.TryGetValue(oldID, out LocalizationEntry entry))
            {
                if (entryLookup.ContainsKey(newID))
                {
                    Debug.LogError($"Ya existe una entrada con el ID '{newID}'");
                    return;
                }

                UnityEditor.Undo.RecordObject(this, "Rename Localization ID");
                entry.localizationID = newID;
                entryLookup.Remove(oldID);
                entryLookup[newID] = entry;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Genera un ID único basado en un prefijo
        /// </summary>
        public string GenerateUniqueID(string prefix)
        {
            BuildLookup();

            string baseID = prefix.ToUpper().Replace(" ", "_");
            string candidateID = baseID;
            int counter = 1;

            while (entryLookup.ContainsKey(candidateID))
            {
                candidateID = $"{baseID}_{counter}";
                counter++;
            }

            return candidateID;
        }

        /// <summary>
        /// Encuentra entradas duplicadas (mismo texto en todos los idiomas)
        /// </summary>
        public List<DuplicateGroup> FindDuplicates()
        {
            var duplicates = new List<DuplicateGroup>();
            var textGroups = new Dictionary<string, List<LocalizationEntry>>();

            foreach (var entry in entries)
            {
                // Crear una clave con todos los textos concatenados
                string key = "";
                foreach (var lang in LocalizationManager.SupportedLanguages)
                {
                    key += entry.localizedString.GetText(lang) + "|";
                }

                if (!textGroups.ContainsKey(key))
                {
                    textGroups[key] = new List<LocalizationEntry>();
                }
                textGroups[key].Add(entry);
            }

            // Encontrar grupos con más de una entrada
            foreach (var group in textGroups)
            {
                if (group.Value.Count > 1)
                {
                    duplicates.Add(new DuplicateGroup
                    {
                        entries = group.Value,
                        sharedText = group.Value[0].localizedString.GetText()
                    });
                }
            }

            return duplicates;
        }

#endif

        private void BuildLookup()
        {
            if (entryLookup == null)
            {
                entryLookup = new Dictionary<string, LocalizationEntry>();
            }

            if (entryLookup.Count != entries.Count)
            {
                entryLookup.Clear();
                foreach (var entry in entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.localizationID))
                    {
                        if (entryLookup.ContainsKey(entry.localizationID))
                        {
                            Debug.LogError($"ID duplicado en LocalizationDatabase: '{entry.localizationID}'");
                        }
                        else
                        {
                            entryLookup[entry.localizationID] = entry;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Entrada individual en la base de datos de localización
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        [Tooltip("ID único para este texto (ej: QUEST_SHOP_GREETING)")]
        public string localizationID;

        [Tooltip("Descripción del contexto para traductores")]
        [TextArea(2, 4)]
        public string context;

        [Tooltip("Número máximo de caracteres permitido (0 = sin límite)")]
        public int maxCharacters;

        [Tooltip("Categoría para organización (ej: UI, Dialogos, Tutoriales)")]
        public string category;

        [Tooltip("Permite usar variables como {0}, {1}, {playerName}")]
        public bool supportsVariables = false;

        [Tooltip("El texto localizado en todos los idiomas")]
        public LocalizedString localizedString;

        /// <summary>
        /// Verifica si el texto excede el límite de caracteres en algún idioma
        /// </summary>
        public bool IsOverCharacterLimit()
        {
            if (maxCharacters <= 0) return false;

            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                string text = localizedString.GetText(lang);
                if (text.Length > maxCharacters)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Obtiene lista de idiomas donde excede el límite
        /// </summary>
        public List<SystemLanguage> GetOverLimitLanguages()
        {
            var result = new List<SystemLanguage>();
            if (maxCharacters <= 0) return result;

            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                string text = localizedString.GetText(lang);
                if (text.Length > maxCharacters)
                {
                    result.Add(lang);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Grupo de entradas duplicadas
    /// </summary>
    public class DuplicateGroup
    {
        public List<LocalizationEntry> entries;
        public string sharedText;
    }
}
