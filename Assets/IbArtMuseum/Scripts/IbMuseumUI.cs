using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IbArtMuseum
{
    public class IbMuseumUI : MonoBehaviour
    {
        public static IbMuseumUI Instance { get; private set; }

        [Header("Dialogue Box (Bottom Center)")]
        public GameObject dialogueBox;
        public TMP_Text dialogueContentText;
        public TMP_Text dialogueContinueText;

        [Header("Interact Prompt (Screen Center)")]
        public GameObject interactPrompt;
        public TMP_Text interactPromptText;

        [Header("Rose Life HUD (Top-Right)")]
        public GameObject roseLifeContainer;
        public Image roseLifeImage;
        public Sprite[] roseLifeSprites; // 0, 1, 2, 3

        [Header("Screen Transitions")]
        public CanvasGroup blackoutCanvasGroup;
        public Image glitchImage;

        [Header("Ending Panel")]
        public GameObject endingPanel;
        public TMP_Text endingTitleText;
        public TMP_Text endingDescText;
        public Button restartButton;

        public bool IsDialogueActive => dialogueBox != null && dialogueBox.activeSelf;

        private System.Action onDialogueClosedCallback;
        private bool isWaitingForAnyKey = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Update()
        {
            if (IsDialogueActive && isWaitingForAnyKey)
            {
                if (CheckAnyKeyPressed())
                {
                    HideDialogueBox();
                }
            }
        }

        public bool CheckAnyKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
#endif
            if (Input.anyKeyDown) return true;
            return false;
        }

        public void ShowDialogueBox(string title, string content, System.Action onClosed = null)
        {
            if (dialogueBox != null) dialogueBox.SetActive(true);
            if (dialogueContentText != null) dialogueContentText.text = content;
            if (dialogueContinueText != null) dialogueContinueText.text = "<size=18><color=#D4AF37>Press any key to continue ▾</color></size>";

            onDialogueClosedCallback = onClosed;
            isWaitingForAnyKey = true;

            SetInteractPromptVisible(false);

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = false;
            }
        }

        public void HideDialogueBox()
        {
            isWaitingForAnyKey = false;
            if (dialogueBox != null) dialogueBox.SetActive(false);

            var cb = onDialogueClosedCallback;
            onDialogueClosedCallback = null;
            cb?.Invoke();

            if (IbPlayerController.LocalPlayer != null && (IbGameManager.Instance == null || IbGameManager.Instance.currentPhase != GamePhase.Prologue_Day || !isWaitingForAnyKey))
            {
                IbPlayerController.LocalPlayer.CanMove = true;
            }
        }

        public void SetInteractPromptVisible(bool visible, string text = "[ E ] Inspect")
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(visible && !IsDialogueActive);
                if (interactPromptText != null && visible)
                {
                    interactPromptText.text = text;
                }
            }
        }

        public void SetRoseLife(int life)
        {
            if (roseLifeContainer != null)
            {
                roseLifeContainer.SetActive(life > 0);
            }

            if (roseLifeImage != null && roseLifeSprites != null && life >= 0 && life < roseLifeSprites.Length)
            {
                roseLifeImage.sprite = roseLifeSprites[life];
            }
        }

        public void HideRoseLife()
        {
            if (roseLifeContainer != null)
            {
                roseLifeContainer.SetActive(false);
            }
        }

        public void PlayGlitchFlash(float duration = 0.12f, Color? flashColor = null)
        {
            StartCoroutine(GlitchFlashRoutine(duration, flashColor ?? Color.black));
        }

        private IEnumerator GlitchFlashRoutine(float duration, Color col)
        {
            if (blackoutCanvasGroup != null && glitchImage != null)
            {
                glitchImage.color = col;
                blackoutCanvasGroup.alpha = 0.95f;
                yield return new WaitForSeconds(duration);
                blackoutCanvasGroup.alpha = 0f;
            }
        }

        public void ShowEnding(bool victory, int score = 0)
        {
            if (endingPanel != null)
            {
                endingPanel.SetActive(true);
                if (endingTitleText != null)
                {
                    endingTitleText.text = victory ? "<color=#E6B422>ESCAPE SUCCESS</color>" : "<color=#E63946>TRAPPED IN RESONANCE</color>";
                }
                if (endingDescText != null)
                {
                    endingDescText.text = victory ?
                        "Lina saw through all anomalies and safely escaped the Weismann Gallery." :
                        "Lina lost all roses and was trapped forever in the virtual museum.";
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void HideEnding()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnRestartClicked()
        {
            HideEnding();
            if (IbStartMenuUI.Instance != null)
            {
                IbStartMenuUI.Instance.ShowStartMenu();
            }
            else
            {
                IbGameManager.Instance?.FullRestartToPrologue();
            }
        }
    }
}
