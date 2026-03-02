using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// UI de diálogo que se actualiza automáticamente al cambiar el idioma.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] TMP_Text speakerNameText;
        [SerializeField] TMP_Text dialogueText;
        [SerializeField] Button nextButton;
        [SerializeField] GameObject dialoguePanel;

        private Dialogo.DialoguesAssetMenu currentDialogue;
        private Dialogo.DialogoNode currentNode;

        // ─────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            Dialogo.LocalizationManager.OnLanguageChanged += OnLanguageChanged;

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
        }

        private void OnDisable()
        {
            Dialogo.LocalizationManager.OnLanguageChanged -= OnLanguageChanged;

            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnNextClicked);
        }

        // ─────────────────────────────────────────────────────────────────
        //  API pública
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inicia y muestra un diálogo desde el principio.
        /// Llama a este método desde el script que inicia la conversación.
        /// </summary>
        public void StartDialogue(Dialogo.DialoguesAssetMenu dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("[DialogueUI] El diálogo pasado es null.");
                return;
            }

            currentDialogue = dialogue;
            currentNode = dialogue.GetRootNode();

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            UpdateUI();
        }

        /// <summary>
        /// Actualiza la UI con el nodo actual.
        /// </summary>
        public void UpdateUI()
        {
            if (currentNode == null) return;

            if (speakerNameText != null)
                speakerNameText.text = currentNode.GetSpeakerName();

            if (dialogueText != null)
                dialogueText.text = currentNode.GetDialogo();

            if (nextButton != null)
            {
                TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    bool hasNext = HasNextNode();
                    buttonText.text = hasNext
                        ? GetLocalizedText("Siguiente", "Next")
                        : GetLocalizedText("Cerrar", "Close");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Eventos
        // ─────────────────────────────────────────────────────────────────

        private void OnNextClicked()
        {
            if (currentDialogue == null || currentNode == null) return;

            Dialogo.DialogoNode nextNode = GetNextNode();

            if (nextNode != null)
            {
                currentNode = nextNode;
                UpdateUI();
            }
            else
            {
                CloseDialogue();
            }
        }

        private void CloseDialogue()
        {
            currentNode = null;
            currentDialogue = null;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            UpdateUI();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private bool HasNextNode()
        {
            if (currentDialogue == null || currentNode == null) return false;
            foreach (var _ in currentDialogue.GetAllChildren(currentNode))
                return true;
            return false;
        }

        private Dialogo.DialogoNode GetNextNode()
        {
            if (currentDialogue == null || currentNode == null) return null;
            foreach (var child in currentDialogue.GetAllChildren(currentNode))
                return child;
            return null;
        }

        private string GetLocalizedText(string spanish, string english)
        {
            return Dialogo.LocalizationManager.CurrentLanguage == SystemLanguage.English
                ? english
                : spanish;
        }
    }
}