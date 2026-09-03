using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IbArtMuseum
{
    /// <summary>
    /// 게임 시작 시 첫 화면(1층 프롤로그 vs 10층 심야 루프) 선택 모달 UI 컨트롤러
    /// 절차적 UI로 Canvas 상에 자동 생성되어 즉시 사용 가능합니다.
    /// </summary>
    public class IbStartMenuUI : MonoBehaviour
    {
        public static IbStartMenuUI Instance { get; private set; }

        [Header("UI State")]
        public GameObject startMenuPanel;
        public CanvasGroup panelCanvasGroup;
        public bool IsMenuOpen => startMenuPanel != null && startMenuPanel.activeSelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            EnsureEventSystem();
            EnsureProceduralUI();
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        public void EnsureProceduralUI()
        {
            if (startMenuPanel != null) return;

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject cGo = new GameObject("Canvas_StartMenu");
                canvas = cGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cGo.AddComponent<CanvasScaler>();
                cGo.AddComponent<GraphicRaycaster>();
            }

            // 1. 전체 화면 어두운 배경 오버레이
            startMenuPanel = new GameObject("Ib_StartMenu_Panel");
            startMenuPanel.transform.SetParent(canvas.transform, false);

            RectTransform overlayRt = startMenuPanel.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;

            Image overlayBg = startMenuPanel.AddComponent<Image>();
            overlayBg.color = new Color(0.02f, 0.03f, 0.07f, 0.88f); // 깊고 몽환적인 딤 배경

            panelCanvasGroup = startMenuPanel.AddComponent<CanvasGroup>();

            // 2. 중앙 럭셔리 모달 카드 프레임
            GameObject modalCard = new GameObject("ModalCard");
            modalCard.transform.SetParent(startMenuPanel.transform, false);

            RectTransform cardRt = modalCard.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(720f, 480f);

            Image cardBg = modalCard.AddComponent<Image>();
            cardBg.color = new Color(0.05f, 0.08f, 0.16f, 0.97f); // 갤러리 다크네이비

            // 외곽 골드 테두리 장식
            Outline cardOutline = modalCard.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.75f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // 3. 메인 타이틀: RESONANCE
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(modalCard.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 175f);
            titleRt.sizeDelta = new Vector2(680f, 50f);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "RESONANCE";
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.88f, 0.45f); // 앤틱 골드
            titleText.characterSpacing = 8f;

            // 4. 서브타이틀: The Neural Gallery
            GameObject subObj = new GameObject("SubTitleText");
            subObj.transform.SetParent(modalCard.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 130f);
            subRt.sizeDelta = new Vector2(680f, 35f);
            TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "THE NEURAL GALLERY";
            subText.fontSize = 18;
            subText.fontStyle = FontStyles.Normal;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.55f, 0.75f, 1.0f); // 몽환적인 스카이블루
            subText.characterSpacing = 5f;

            // 5. 구분선 (Gold Divider Line)
            GameObject divObj = new GameObject("DividerLine");
            divObj.transform.SetParent(modalCard.transform, false);
            RectTransform divRt = divObj.AddComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0, 95f);
            divRt.sizeDelta = new Vector2(580f, 2f);
            Image divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(0.85f, 0.70f, 0.35f, 0.4f);

            // 6. 안내 메시지
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(modalCard.transform, false);
            RectTransform guideRt = guideObj.AddComponent<RectTransform>();
            guideRt.anchoredPosition = new Vector2(0, 65f);
            guideRt.sizeDelta = new Vector2(650f, 30f);
            TextMeshProUGUI guideText = guideObj.AddComponent<TextMeshProUGUI>();
            guideText.text = "<color=#D0D8E8>Select Your Starting Floor</color>";
            guideText.fontSize = 16;
            guideText.alignment = TextAlignmentOptions.Center;

            // 7. [1층부터 시작] 버튼 (버튼 1)
            CreateMenuButton(
                modalCard.transform,
                "Button_Floor1",
                new Vector2(0, -10f),
                new Vector2(580f, 90f),
                new Color(0.12f, 0.16f, 0.28f, 0.95f),
                new Color(0.85f, 0.70f, 0.35f, 0.8f),
                "<b><size=19><color=#FFDF80>[ 1F ]  START FROM PROLOGUE (1F Lobby)</color></size></b>\n<size=13><color=#B8C4D8>Begin from the 1F gallery reception with Lina's parents and full story.</color></size>",
                () => OnSelectFloor1()
            );

            // 8. [10층부터 시작] 버튼 (버튼 2)
            CreateMenuButton(
                modalCard.transform,
                "Button_Floor10",
                new Vector2(0, -125f),
                new Vector2(580f, 90f),
                new Color(0.16f, 0.12f, 0.28f, 0.95f),
                new Color(0.65f, 0.50f, 0.95f, 0.8f),
                "<b><size=19><color=#D4B8FF>[ 10F ]  START FROM 10F (Night Loop Quickplay)</color></size></b>\n<size=13><color=#C8B8E8>Spawn at 10F monument and immediately begin the 8th Exit descent loop.</color></size>",
                () => OnSelectFloor10()
            );

            // 9. 하단 힌트
            GameObject footObj = new GameObject("FooterText");
            footObj.transform.SetParent(modalCard.transform, false);
            RectTransform footRt = footObj.AddComponent<RectTransform>();
            footRt.anchoredPosition = new Vector2(0, -210f);
            footRt.sizeDelta = new Vector2(650f, 25f);
            TextMeshProUGUI footText = footObj.AddComponent<TextMeshProUGUI>();
            footText.text = "<size=12><color=#607088>Exit 8 Anomaly Loop × Ib Psychological Mystery Adventure</color></size>";
            footText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMenuButton(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor, string buttonRichText, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("BtnText");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-20f, -10f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = buttonRichText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.lineSpacing = 6f;
        }

        public void ShowStartMenu()
        {
            EnsureProceduralUI();
            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(true);
                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 1f;
                    panelCanvasGroup.interactable = true;
                    panelCanvasGroup.blocksRaycasts = true;
                }
            }

            // 마우스 커서 해제 & 플레이어 이동 정지
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = false;
            }
        }

        public void HideStartMenu(System.Action onComplete = null)
        {
            StartCoroutine(FadeOutRoutine(onComplete));
        }

        private IEnumerator FadeOutRoutine(System.Action onComplete)
        {
            if (panelCanvasGroup != null)
            {
                float t = 0f;
                float duration = 0.22f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
                    yield return null;
                }
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            if (startMenuPanel != null) startMenuPanel.SetActive(false);

            // 마우스 커서 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            onComplete?.Invoke();
        }

        private void OnSelectFloor1()
        {
            HideStartMenu(() =>
            {
                if (IbGameManager.Instance != null)
                {
                    IbGameManager.Instance.FullRestartToPrologue();
                }
            });
        }

        private void OnSelectFloor10()
        {
            HideStartMenu(() =>
            {
                if (IbGameManager.Instance != null)
                {
                    IbGameManager.Instance.StartFrom10FNight();
                }
            });
        }
    }
}
