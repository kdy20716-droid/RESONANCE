using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace IbArtMuseum
{
    /// <summary>
    /// 메인 메뉴, 설정 모달, 사망 화면(다잉 메시지), 클리어 엔딩 씬 통합 관리자
    /// </summary>
    public class IbMainMenuManager : MonoBehaviour
    {
        public static IbMainMenuManager Instance { get; private set; }

        [Header("Menu Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsModalPanel;
        public GameObject gameOverPanel;
        public GameObject gameClearPanel;

        [Header("Menu Buttons")]
        public Button newGameButton;
        public Button continueButton;
        public Button floor10QuickButton;
        public Button settingsButton;
        public Button exitButton;

        [Header("Game Over UI")]
        public Text dyingMessageText;
        public Button retryFromSaveButton;
        public Button returnToMenuButton;

        [Header("Game Clear UI")]
        public Button clearReturnToMenuButton;

        [Header("Settings Sliders")]
        public Slider masterVolumeSlider;
        public Slider bgmVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider mouseSensitivitySlider;
        public Button closeSettingsButton;

        private readonly string[] dyingMessages = new string[]
        {
            "\"왜... 뒤를 돌아보지 않았어...?\"",
            "\"리나, 영원히 이 미술관에서 함께 놀자.\"",
            "\"붉은 장미의 모든 꽃잎이 바닥에 떨어졌습니다.\"",
            "\"그녀의 캔버스 속으로 영혼이 빨려 들어갑니다...\"",
            "\"바이스만의 공명이 당신의 의식을 집어삼켰습니다.\"",
            "\"모든 것이 어둠 속으로 침식되었습니다.\""
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            InitMenuUI();
        }

        public void InitMenuUI()
        {
            // 1. 이어하기 버튼 활성화 여부
            bool hasSave = PlayerPrefs.HasKey("Ib_Save_Floor");
            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
            }

            // 2. 10층부터 시작 버튼 (10층 도달 이력 있는 플레이어만 활성화)
            bool reachedFloor10 = PlayerPrefs.GetInt("Ib_Reached_10F", 0) == 1;
            if (floor10QuickButton != null)
            {
                floor10QuickButton.gameObject.SetActive(reachedFloor10);
            }

            // 버튼 리스너 바인딩
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (floor10QuickButton != null) floor10QuickButton.onClick.AddListener(OnFloor10QuickClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitGameClicked);

            if (retryFromSaveButton != null) retryFromSaveButton.onClick.AddListener(OnRetryFromSaveClicked);
            if (returnToMenuButton != null) returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            if (clearReturnToMenuButton != null) clearReturnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);

            // 볼륨/감도 초기화
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("Ib_MasterVol", 1f);
                masterVolumeSlider.onValueChanged.AddListener(val => { AudioListener.volume = val; PlayerPrefs.SetFloat("Ib_MasterVol", val); });
            }
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = PlayerPrefs.GetFloat("Ib_MouseSens", 2f);
                mouseSensitivitySlider.onValueChanged.AddListener(val => PlayerPrefs.SetFloat("Ib_MouseSens", val));
            }
        }

        public void OnNewGameClicked()
        {
            PlayerPrefs.DeleteKey("Ib_Save_Floor");
            PlayerPrefs.SetInt("Ib_StartFloor", 1);
            PlayerPrefs.Save();
            LoadGameplayScene();
        }

        public void OnContinueClicked()
        {
            int saveFloor = PlayerPrefs.GetInt("Ib_Save_Floor", 1);
            PlayerPrefs.SetInt("Ib_StartFloor", saveFloor);
            PlayerPrefs.Save();
            LoadGameplayScene();
        }

        public void OnFloor10QuickClicked()
        {
            PlayerPrefs.SetInt("Ib_StartFloor", 10);
            PlayerPrefs.Save();
            LoadGameplayScene();
        }

        public void OpenSettings()
        {
            if (settingsModalPanel != null) settingsModalPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsModalPanel != null) settingsModalPanel.SetActive(false);
            PlayerPrefs.Save();
        }

        public void OnExitGameClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowGameOver()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (dyingMessageText != null)
                {
                    int randIdx = Random.Range(0, dyingMessages.Length);
                    dyingMessageText.text = dyingMessages[randIdx];
                }
            }
        }

        public void ShowGameClear()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (gameClearPanel != null)
            {
                gameClearPanel.SetActive(true);
            }
        }

        public void OnRetryFromSaveClicked()
        {
            int saveFloor = PlayerPrefs.GetInt("Ib_Save_Floor", 1);
            if (IbGameManager.Instance != null)
            {
                IbGameManager.Instance.RespawnAtFloor(saveFloor);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                OnContinueClicked();
            }
        }

        public void OnReturnToMenuClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadGameplayScene()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (IbGameManager.Instance != null)
            {
                int startFloor = PlayerPrefs.GetInt("Ib_StartFloor", 1);
                IbGameManager.Instance.RespawnAtFloor(startFloor);
            }
        }
    }
}
