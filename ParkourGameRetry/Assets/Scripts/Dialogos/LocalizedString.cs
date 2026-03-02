using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogo
{
    /// <summary>
    /// Almacena un texto en múltiples idiomas
    /// </summary>
    [Serializable]
    public class LocalizedString
    {
        [SerializeField] 
        private List<LanguageEntry> translations = new List<LanguageEntry>();

        public LocalizedString()
        {
            // Inicializar con español por defecto
            if (translations.Count == 0)
            {
                translations.Add(new LanguageEntry(SystemLanguage.Spanish, ""));
                translations.Add(new LanguageEntry(SystemLanguage.English, ""));
            }
        }

        /// <summary>
        /// Obtiene el texto en el idioma actual del sistema de localización
        /// </summary>
        public string GetText(params object[] variables)
        {
            return GetText(LocalizationManager.CurrentLanguage, variables);
        }

        /// <summary>
        /// Obtiene el texto en un idioma específico
        /// </summary>
        public string GetText(SystemLanguage language, params object[] variables)
        {
            // Buscar el idioma solicitado
            LanguageEntry entry = translations.Find(t => t.language == language);
            
            if (entry != null && !string.IsNullOrEmpty(entry.text))
            {
                return FormatText(entry.text, variables);
            }

            // Fallback: buscar español
            entry = translations.Find(t => t.language == SystemLanguage.Spanish);
            if (entry != null && !string.IsNullOrEmpty(entry.text))
            {
                return FormatText(entry.text, variables);
            }

            // Último recurso: devolver el primero que tenga texto
            entry = translations.Find(t => !string.IsNullOrEmpty(t.text));
            return entry != null ? FormatText(entry.text, variables) : "";
        }

        /// <summary>
        /// Formatea el texto reemplazando variables
        /// </summary>
        private string FormatText(string text, params object[] variables)
        {
            if (variables == null || variables.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(text, variables);
            }
            catch (System.FormatException e)
            {
                Debug.LogWarning($"Error al formatear texto localizado: {e.Message}. Texto: {text}");
                return text;
            }
        }

        /// <summary>
        /// Establece el texto para un idioma específico
        /// </summary>
        public void SetText(SystemLanguage language, string text)
        {
            LanguageEntry entry = translations.Find(t => t.language == language);
            
            if (entry != null)
            {
                entry.text = text;
            }
            else
            {
                translations.Add(new LanguageEntry(language, text));
            }
        }

        /// <summary>
        /// Obtiene todas las traducciones
        /// </summary>
        public List<LanguageEntry> GetAllTranslations()
        {
            return translations;
        }

        /// <summary>
        /// Asegura que existe una entrada para el idioma dado
        /// </summary>
        public void EnsureLanguageExists(SystemLanguage language)
        {
            if (!translations.Exists(t => t.language == language))
            {
                translations.Add(new LanguageEntry(language, ""));
            }
        }
    }

    /// <summary>
    /// Entrada individual de idioma
    /// </summary>
    [Serializable]
    public class LanguageEntry
    {
        public SystemLanguage language;
        [TextArea(3, 10)]
        public string text;

        public LanguageEntry(SystemLanguage language, string text)
        {
            this.language = language;
            this.text = text;
        }
    }
}
