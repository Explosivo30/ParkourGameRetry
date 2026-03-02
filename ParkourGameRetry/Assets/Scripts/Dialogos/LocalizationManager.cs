using System.Collections.Generic;
using UnityEngine;

namespace Dialogo
{
    /// <summary>
    /// Gestor global de localización
    /// Controla el idioma actual del juego
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager instance;
        
        [SerializeField] 
        private SystemLanguage currentLanguage = SystemLanguage.Spanish;

        [SerializeField]
        private List<SystemLanguage> supportedLanguages = new List<SystemLanguage>()
        {
            SystemLanguage.Spanish,
            SystemLanguage.English,
            SystemLanguage.Catalan,
            SystemLanguage.French,
            SystemLanguage.German
        };

        public static SystemLanguage CurrentLanguage
        {
            get
            {
                if (instance == null)
                {
                    // Si no hay instancia, usar español por defecto
                    return SystemLanguage.Spanish;
                }
                return instance.currentLanguage;
            }
        }

        public static List<SystemLanguage> SupportedLanguages
        {
            get
            {
                if (instance == null)
                {
                    return new List<SystemLanguage> { SystemLanguage.Spanish, SystemLanguage.English };
                }
                return instance.supportedLanguages;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadLanguagePreference();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Cambia el idioma actual del juego
        /// </summary>
        public static void SetLanguage(SystemLanguage language)
        {
            if (instance != null)
            {
                instance.currentLanguage = language;
                SaveLanguagePreference(language);
                
                // Disparar evento para actualizar UI
                OnLanguageChanged?.Invoke(language);
            }
        }

        /// <summary>
        /// Evento que se dispara cuando cambia el idioma
        /// </summary>
        public static System.Action<SystemLanguage> OnLanguageChanged;

        private void LoadLanguagePreference()
        {
            if (PlayerPrefs.HasKey("GameLanguage"))
            {
                string languageString = PlayerPrefs.GetString("GameLanguage");
                if (System.Enum.TryParse(languageString, out SystemLanguage savedLanguage))
                {
                    currentLanguage = savedLanguage;
                }
            }
            else
            {
                // Usar el idioma del sistema si está soportado
                SystemLanguage systemLanguage = Application.systemLanguage;
                if (supportedLanguages.Contains(systemLanguage))
                {
                    currentLanguage = systemLanguage;
                }
            }
        }

        private static void SaveLanguagePreference(SystemLanguage language)
        {
            PlayerPrefs.SetString("GameLanguage", language.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Añade un nuevo idioma soportado
        /// </summary>
        public static void AddSupportedLanguage(SystemLanguage language)
        {
            if (instance != null && !instance.supportedLanguages.Contains(language))
            {
                instance.supportedLanguages.Add(language);
            }
        }
    }
}
