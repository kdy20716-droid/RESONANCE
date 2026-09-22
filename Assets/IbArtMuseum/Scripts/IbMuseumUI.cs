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

        [Header("Floor Notification Banner")]
        public GameObject floorBannerObj;
        public TMP_Text floorBannerTitle;
        public TMP_Text floorBannerSub;

        [Header("Choice Dialog Box")]
        public GameObject choiceBox;
        public TMP_Text choiceTitleText;
        public TMP_Text choiceDescText;
        public Button choiceBtn1;
        public TMP_Text choiceBtn1Text;
        public Button choiceBtn2;
        public TMP_Text choiceBtn2Text;

        public bool IsDialogueActive => dialogueBox != null && dialogueBox.activeSelf;
        public bool IsChoiceActive => choiceBox != null && choiceBox.activeSelf;

        private System.Action onDialogueClosedCallback;
        private System.Action onChoice1Action;
        private System.Action onChoice2Action;
        private bool isWaitingForAnyKey = false;
        private Coroutine floorBannerRoutine;

        public enum EndingType
        {
            Happy_Escape,    // ☀️ 해피 엔딩: 현실 귀환 (Return to Reality)
            Sad_Canvas,      // 🥀 새드 엔딩: 영원한 캔버스 (The Eternal Canvas)
            Bad_Usurpation   // 💀 배드 엔딩: 육체 잠식 (Echo's Rebirth)
        }

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
            if (IsChoiceActive)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    OnChoice1Selected();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Escape))
                {
                    OnChoice2Selected();
                }
                return;
            }

            if (IsDialogueActive && isWaitingForAnyKey)
            {
                if (CheckAnyKeyPressed())
                {
                    HideDialogueBox();
                }
            }
        }

        public void ShowChoiceDialogue(string title, string content, string opt1, System.Action onOpt1, string opt2, System.Action onOpt2)
        {
            EnsureChoiceUI();

            if (choiceBox != null) choiceBox.SetActive(true);
            if (choiceTitleText != null) choiceTitleText.text = title;
            if (choiceDescText != null) choiceDescText.text = content;
            if (choiceBtn1Text != null) choiceBtn1Text.text = opt1;
            if (choiceBtn2Text != null) choiceBtn2Text.text = opt2;

            onChoice1Action = onOpt1;
            onChoice2Action = onOpt2;

            SetInteractPromptVisible(false);

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideChoiceDialogue()
        {
            if (choiceBox != null) choiceBox.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = true;
            }
        }

        private void OnChoice1Selected()
        {
            var action = onChoice1Action;
            onChoice1Action = null;
            onChoice2Action = null;
            HideChoiceDialogue();
            action?.Invoke();
        }

        private void OnChoice2Selected()
        {
            var action = onChoice2Action;
            onChoice1Action = null;
            onChoice2Action = null;
            HideChoiceDialogue();
            action?.Invoke();
        }

        public void EnsureChoiceUI()
        {
            if (choiceBox != null) return;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            choiceBox = new GameObject("Museum_Choice_Dialog");
            choiceBox.transform.SetParent(canvas.transform, false);

            RectTransform overlayRt = choiceBox.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;

            Image overlayBg = choiceBox.AddComponent<Image>();
            overlayBg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);

            // 중앙 카드
            GameObject card = new GameObject("ChoiceCard");
            card.transform.SetParent(choiceBox.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(680f, 380f);

            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.75f, 1.0f, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            // 타이틀
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(card.transform, false);
            RectTransform tRt = titleGo.AddComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 140f);
            tRt.sizeDelta = new Vector2(620f, 45f);
            choiceTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            choiceTitleText.fontSize = 26;
            choiceTitleText.fontStyle = FontStyles.Bold;
            choiceTitleText.alignment = TextAlignmentOptions.Center;
            choiceTitleText.color = new Color(0.9f, 0.8f, 0.4f);

            // 본문
            GameObject descGo = new GameObject("Desc");
            descGo.transform.SetParent(card.transform, false);
            RectTransform dRt = descGo.AddComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0, 35f);
            dRt.sizeDelta = new Vector2(600f, 140f);
            choiceDescText = descGo.AddComponent<TextMeshProUGUI>();
            choiceDescText.fontSize = 18;
            choiceDescText.alignment = TextAlignmentOptions.Center;
            choiceDescText.color = new Color(0.9f, 0.92f, 0.95f);

            // 버튼 1 (선택지 1: 손을 잡는다)
            GameObject btn1Go = new GameObject("Btn_Choice1");
            btn1Go.transform.SetParent(card.transform, false);
            RectTransform b1Rt = btn1Go.AddComponent<RectTransform>();
            b1Rt.anchoredPosition = new Vector2(-150f, -120f);
            b1Rt.sizeDelta = new Vector2(260f, 55f);
            Image b1Img = btn1Go.AddComponent<Image>();
            b1Img.color = new Color(0.12f, 0.28f, 0.45f, 0.95f);
            choiceBtn1 = btn1Go.AddComponent<Button>();
            choiceBtn1.onClick.AddListener(OnChoice1Selected);

            GameObject b1TextGo = new GameObject("Text");
            b1TextGo.transform.SetParent(btn1Go.transform, false);
            RectTransform b1tRt = b1TextGo.AddComponent<RectTransform>();
            b1tRt.anchorMin = Vector2.zero;
            b1tRt.anchorMax = Vector2.one;
            b1tRt.sizeDelta = Vector2.zero;
            choiceBtn1Text = b1TextGo.AddComponent<TextMeshProUGUI>();
            choiceBtn1Text.fontSize = 18;
            choiceBtn1Text.fontStyle = FontStyles.Bold;
            choiceBtn1Text.alignment = TextAlignmentOptions.Center;
            choiceBtn1Text.color = new Color(0.5f, 0.9f, 1.0f);

            // 버튼 2 (선택지 2: 거절하고 지나친다)
            GameObject btn2Go = new GameObject("Btn_Choice2");
            btn2Go.transform.SetParent(card.transform, false);
            RectTransform b2Rt = btn2Go.AddComponent<RectTransform>();
            b2Rt.anchoredPosition = new Vector2(150f, -120f);
            b2Rt.sizeDelta = new Vector2(260f, 55f);
            Image b2Img = btn2Go.AddComponent<Image>();
            b2Img.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);
            choiceBtn2 = btn2Go.AddComponent<Button>();
            choiceBtn2.onClick.AddListener(OnChoice2Selected);

            GameObject b2TextGo = new GameObject("Text");
            b2TextGo.transform.SetParent(btn2Go.transform, false);
            RectTransform b2tRt = b2TextGo.AddComponent<RectTransform>();
            b2tRt.anchorMin = Vector2.zero;
            b2tRt.anchorMax = Vector2.one;
            b2tRt.sizeDelta = Vector2.zero;
            choiceBtn2Text = b2TextGo.AddComponent<TextMeshProUGUI>();
            choiceBtn2Text.fontSize = 18;
            choiceBtn2Text.alignment = TextAlignmentOptions.Center;
            choiceBtn2Text.color = new Color(0.8f, 0.8f, 0.85f);
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

        public void ShowFloorNotification(int floor, string themeName, string themeDesc)
        {
            if (floorBannerRoutine != null) StopCoroutine(floorBannerRoutine);
            floorBannerRoutine = StartCoroutine(FloorNotificationRoutine(floor, themeName, themeDesc));
        }

        private IEnumerator FloorNotificationRoutine(int floor, string themeName, string themeDesc)
        {
            if (floorBannerObj != null)
            {
                if (floorBannerTitle != null) floorBannerTitle.text = $"{floor}F {themeName.ToUpper()}";
                if (floorBannerSub != null) floorBannerSub.text = themeDesc;
                floorBannerObj.SetActive(true);
                yield return new WaitForSeconds(3.5f);
                floorBannerObj.SetActive(false);
            }
            floorBannerRoutine = null;
        }

        public void ShowEnding(EndingType type)
        {
            if (endingPanel != null)
            {
                endingPanel.SetActive(true);
                switch (type)
                {
                    case EndingType.Happy_Escape:
                        if (endingTitleText != null)
                            endingTitleText.text = "<color=#FCD34D>☀️ HAPPY ENDING: 현실 귀환</color>";
                        if (endingDescText != null)
                            endingDescText.text = "<b>\"리나, 다 봤니? 이제 집에 가자.\"</b>\n\n에코의 뒤틀린 8번 출구 루프를 모두 간파하고 마침내 1층 갤러리 문을 열고 탈출했습니다.\n따스한 아침 햇살 아래 기다리던 부모님과 재회하며 현실의 품으로 무사히 돌아갑니다.";
                        break;

                    case EndingType.Sad_Canvas:
                        if (endingTitleText != null)
                            endingTitleText.text = "<color=#93C5FD>🥀 SAD ENDING: 영원한 캔버스</color>";
                        if (endingDescText != null)
                            endingDescText.text = "<b>\"가지 마... 나랑 같이 그림 속에서 영원히 놀자...\"</b>\n\n애원하는 에코의 손을 맞잡은 순간, 당신의 영혼은 캔버스 속으로 흡수되었습니다.\n이제 1층 갤러리 캔버스에는 에코와 당신이 손을 잡고 행복하게 미소 짓는 모습이 영원히 걸려 있습니다.";
                        break;

                    case EndingType.Bad_Usurpation:
                        if (endingTitleText != null)
                            endingTitleText.text = "<color=#EF4444>💀 BAD ENDING: 육체 잠식</color>";
                        if (endingDescText != null)
                            endingDescText.text = "<b>\"고마워... 네 몸 덕분에 드디어 바깥 세상으로 나갈 수 있게 되었어.\"</b>\n\n에코의 붉은 장미 저주에 모든 영혼이 침식되었습니다.\n당신의 의식은 어둠 속으로 영원히 소멸했고, 인공소녀 에코가 당신의 육체를 차지한 채 미술관 문을 열고 세상 밖으로 걸어나갑니다.";
                        break;
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void ShowEnding(bool victory, int score = 0)
        {
            ShowEnding(victory ? EndingType.Happy_Escape : EndingType.Bad_Usurpation);
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
