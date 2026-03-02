using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dialogo
{
    /// <summary>
    /// UI simple para cambiar el idioma del juego
    /// Agrega este componente a un Canvas con botones o dropdown
    /// </summary>
    public class LanguageSelectorUI : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Dropdown languageDropdown;

        [SerializeField]
        private Button spanishButton;

        [SerializeField]
        private Button englishButton;

        [SerializeField]
        private Button catalanButton;

        private void Start()
        {
            // Si usas Dropdown
            if (languageDropdown != null)
            {
                SetupDropdown();
            }

            // Si usas botones individuales
            if (spanishButton != null)
            {
                spanishButton.onClick.AddListener(() => ChangeLanguage(SystemLanguage.Spanish));
            }

            if (englishButton != null)
            {
                englishButton.onClick.AddListener(() => ChangeLanguage(SystemLanguage.English));
            }

            if (catalanButton != null)
            {
                catalanButton.onClick.AddListener(() => ChangeLanguage(SystemLanguage.Catalan));
            }
        }

        private void SetupDropdown()
        {
            languageDropdown.ClearOptions();

            var languages = LocalizationManager.SupportedLanguages;
            var options = new System.Collections.Generic.List<string>();

            foreach (var lang in languages)
            {
                options.Add(GetLanguageDisplayName(lang));
            }

            languageDropdown.AddOptions(options);

            // Seleccionar el idioma actual
            int currentIndex = languages.IndexOf(LocalizationManager.CurrentLanguage);
            if (currentIndex >= 0)
            {
                languageDropdown.value = currentIndex;
            }

            // Listener para cambios
            languageDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnDropdownChanged(int index)
        {
            var languages = LocalizationManager.SupportedLanguages;
            if (index >= 0 && index < languages.Count)
            {
                ChangeLanguage(languages[index]);
            }
        }

        private void ChangeLanguage(SystemLanguage language)
        {
            LocalizationManager.SetLanguage(language);
            Debug.Log($"Idioma cambiado a: {language}");
        }

        private string GetLanguageDisplayName(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Spanish: return "Español";
                case SystemLanguage.English: return "English";
                case SystemLanguage.Catalan: return "Català";
                case SystemLanguage.French: return "Français";
                case SystemLanguage.German: return "Deutsch";
                case SystemLanguage.Italian: return "Italiano";
                case SystemLanguage.Portuguese: return "Português";
                default: return language.ToString();
            }
        }
    }
}
