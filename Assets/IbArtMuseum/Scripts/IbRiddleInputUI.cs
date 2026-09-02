using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IbArtMuseum
{
    /// <summary>
    /// 수수께끼 관리인 NPC 정답 입력을 위한 UI 팝업 컨트롤러 (ESC 취소, Enter 제출, 절차적 UI 자동 생성 지원)
    /// </summary>
    public class IbRiddleInputUI : MonoBehaviour
    {
        public static IbRiddleInputUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject riddleModalPanel;
        public TMP_Text titleText;
        public TMP_Text questionText;
        public TMP_InputField answerInputField;
        public TMP_Text feedbackText;
        public Button submitButton;
        public Button cancelButton;

        private IbRiddleBarrier currentBarrier;
        private IbRiddleGuardNPC currentGuardNPC;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            EnsureProceduralUI();
        }

        private void Start()
        {
            if (riddleModalPanel != null) riddleModalPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen) return;

            // ESC 키로 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseRiddleUI();
                return;
            }

            // Enter 키로 제출
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmitClicked();
            }
        }

        private void EnsureProceduralUI()
        {
            if (riddleModalPanel != null) return;

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // 모달 루트 패널
            riddleModalPanel = new GameObject("Procedural_RiddleModalPanel");
            riddleModalPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = riddleModalPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 380f);

            Image bg = riddleModalPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(riddleModalPanel.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 140f);
            titleRt.sizeDelta = new Vector2(620f, 40f);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.3f);

            // Question
            GameObject qObj = new GameObject("QuestionText");
            qObj.transform.SetParent(riddleModalPanel.transform, false);
            RectTransform qRt = qObj.AddComponent<RectTransform>();
            qRt.anchoredPosition = new Vector2(0, 50f);
            qRt.sizeDelta = new Vector2(620f, 100f);
            questionText = qObj.AddComponent<TextMeshProUGUI>();
            questionText.fontSize = 20;
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.color = Color.white;

            // Input Field (텍스트 입력창)
            GameObject inputObj = new GameObject("AnswerInputField");
            inputObj.transform.SetParent(riddleModalPanel.transform, false);
            RectTransform inRt = inputObj.AddComponent<RectTransform>();
            inRt.anchoredPosition = new Vector2(0, -35f);
            inRt.sizeDelta = new Vector2(500f, 50f);
            Image inBg = inputObj.AddComponent<Image>();
            inBg.color = new Color(0.08f, 0.12f, 0.20f, 1f);

            // Placeholder
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRt = phObj.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;
            phRt.offsetMin = new Vector2(15, 5);
            phRt.offsetMax = new Vector2(-15, -5);
            TMP_Text phText = phObj.AddComponent<TextMeshProUGUI>();
            phText.text = "Type your answer in English here...";
            phText.fontSize = 18;
            phText.fontStyle = FontStyles.Italic;
            phText.color = new Color(0.6f, 0.65f, 0.75f, 0.6f);
            phText.alignment = TextAlignmentOptions.Left;

            // Input Text Component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.offsetMin = new Vector2(15, 5);
            textRt.offsetMax = new Vector2(-15, -5);
            TMP_Text inText = textObj.AddComponent<TextMeshProUGUI>();
            inText.fontSize = 20;
            inText.color = new Color(0.3f, 1.0f, 0.4f);
            inText.alignment = TextAlignmentOptions.Left;

            answerInputField = inputObj.AddComponent<TMP_InputField>();
            answerInputField.textComponent = inText;
            answerInputField.placeholder = phText;
            answerInputField.fontAsset = inText.font;

            // Feedback Text
            GameObject fbObj = new GameObject("FeedbackText");
            fbObj.transform.SetParent(riddleModalPanel.transform, false);
            RectTransform fbRt = fbObj.AddComponent<RectTransform>();
            fbRt.anchoredPosition = new Vector2(0, -85f);
            fbRt.sizeDelta = new Vector2(600f, 30f);
            feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
            feedbackText.fontSize = 14;
            feedbackText.alignment = TextAlignmentOptions.Center;

            // Buttons Container
            GameObject btnGroup = new GameObject("BtnGroup");
            btnGroup.transform.SetParent(riddleModalPanel.transform, false);
            RectTransform btnRt = btnGroup.AddComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0, -135f);
            btnRt.sizeDelta = new Vector2(480f, 45f);

            // Submit Button
            GameObject subBtnObj = new GameObject("SubmitButton");
            subBtnObj.transform.SetParent(btnGroup.transform, false);
            RectTransform sRt = subBtnObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(90f, 0);
            sRt.sizeDelta = new Vector2(160f, 40f);
            Image subBg = subBtnObj.AddComponent<Image>();
            subBg.color = new Color(0.85f, 0.65f, 0.15f);
            submitButton = subBtnObj.AddComponent<Button>();

            GameObject subTextObj = new GameObject("Text");
            subTextObj.transform.SetParent(subBtnObj.transform, false);
            TMP_Text subT = subTextObj.AddComponent<TextMeshProUGUI>();
            subT.text = "Submit [Enter]";
            subT.fontSize = 16;
            subT.color = Color.black;
            subT.alignment = TextAlignmentOptions.Center;

            // Cancel Button
            GameObject canBtnObj = new GameObject("CancelButton");
            canBtnObj.transform.SetParent(btnGroup.transform, false);
            RectTransform cRt = canBtnObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(-90f, 0);
            cRt.sizeDelta = new Vector2(160f, 40f);
            Image canBg = canBtnObj.AddComponent<Image>();
            canBg.color = new Color(0.25f, 0.3f, 0.4f);
            cancelButton = canBtnObj.AddComponent<Button>();

            GameObject canTextObj = new GameObject("Text");
            canTextObj.transform.SetParent(canBtnObj.transform, false);
            TMP_Text canT = canTextObj.AddComponent<TextMeshProUGUI>();
            canT.text = "Cancel [ESC]";
            canT.fontSize = 16;
            canT.color = Color.white;
            canT.alignment = TextAlignmentOptions.Center;

            submitButton.onClick.AddListener(OnSubmitClicked);
            cancelButton.onClick.AddListener(CloseRiddleUI);

            riddleModalPanel.SetActive(false);
        }

        public void OpenRiddleUI(IbRiddleBarrier barrier, string title, string question, IbRiddleGuardNPC guardNPC = null)
        {
            EnsureProceduralUI();

            currentBarrier = barrier;
            currentGuardNPC = guardNPC;
            isOpen = true;

            if (riddleModalPanel != null) riddleModalPanel.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (questionText != null) questionText.text = question;
            if (feedbackText != null) feedbackText.text = "<color=#AAAAAA>Type your answer in English and press [Enter] to submit (Press [ESC] to cancel)</color>";

            if (answerInputField != null)
            {
                answerInputField.text = "";
                answerInputField.Select();
                answerInputField.ActivateInputField();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = false;
            }
        }

        public void OnSubmitClicked()
        {
            if (answerInputField == null) return;

            string inputAnswer = answerInputField.text.Trim();
            if (string.IsNullOrEmpty(inputAnswer))
            {
                if (feedbackText != null) feedbackText.text = "<color=#FF5555>Please type an answer before submitting.</color>";
                return;
            }

            bool isCorrect = false;
            if (currentGuardNPC != null)
            {
                isCorrect = currentGuardNPC.CheckAnswer(inputAnswer);
            }
            else if (currentBarrier != null)
            {
                isCorrect = currentBarrier.CheckAnswer(inputAnswer);
            }

            if (isCorrect)
            {
                if (feedbackText != null) feedbackText.text = "<color=#55FF55><b>Correct! The curator is stepping aside...</b></color>";
                StartCoroutine(CloseAfterSuccessRoutine());
            }
            else
            {
                if (feedbackText != null) feedbackText.text = "<color=#FF4444><b>Incorrect answer.</b> Observe the gallery artworks closely and try again!</color>";
                answerInputField.text = "";
                answerInputField.Select();
                answerInputField.ActivateInputField();
            }
        }

        private IEnumerator CloseAfterSuccessRoutine()
        {
            yield return new WaitForSeconds(0.6f);
            CloseRiddleUI();
            if (currentGuardNPC != null)
            {
                currentGuardNPC.OnSolveSuccess();
            }
            else if (currentBarrier != null)
            {
                currentBarrier.OnSolveSuccess();
            }
        }

        public void CloseRiddleUI()
        {
            isOpen = false;
            if (riddleModalPanel != null) riddleModalPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.CanMove = true;
            }
        }
    }
}
