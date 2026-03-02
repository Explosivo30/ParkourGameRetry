using UnityEditor;
using UnityEngine;

namespace Dialogo.Editor
{
    /// <summary>
    /// Configuración del editor de diálogos
    /// Controla qué idioma se muestra al editar
    /// </summary>
    public static class DialogEditorSettings
    {
        private const string PREF_KEY = "DialogEditor_CurrentLanguage";
        private static SystemLanguage? cachedLanguage;

        /// <summary>
        /// Obtiene el idioma actual del editor
        /// </summary>
        public static SystemLanguage GetEditorLanguage()
        {
            if (cachedLanguage.HasValue)
            {
                return cachedLanguage.Value;
            }

            string savedLanguage = EditorPrefs.GetString(PREF_KEY, SystemLanguage.Spanish.ToString());
            
            if (System.Enum.TryParse(savedLanguage, out SystemLanguage language))
            {
                cachedLanguage = language;
                return language;
            }

            cachedLanguage = SystemLanguage.Spanish;
            return SystemLanguage.Spanish;
        }

        /// <summary>
        /// Establece el idioma actual del editor
        /// </summary>
        public static void SetEditorLanguage(SystemLanguage language)
        {
            cachedLanguage = language;
            EditorPrefs.SetString(PREF_KEY, language.ToString());
        }
    }
}
