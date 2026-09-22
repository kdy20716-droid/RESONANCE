using System;
using System.Collections;
using UnityEngine;

namespace IbArtMuseum
{
    /// <summary>
    /// 3F~9F 각 층 계단 앞 복도를 막아서는 수수께끼 관리인 NPC (비비기 방지 콜라이더 & 비켜서기 연출)
    /// </summary>
    public class IbRiddleGuardNPC : MonoBehaviour
    {
        [Header("Floor Config")]
        public int floorLevel = 3;
        public bool isCleared = false;

        [Header("Blocking Collider (Prevents slipping through)")]
        public Collider blockingCollider;

        [Header("Riddle Content (English)")]
        public string riddleTitle = "Museum Curator's Test";
        [TextArea(3, 6)]
        public string riddleQuestion = "How many human figures are depicted across all lower floor portraits?";
        public string[] acceptedAnswers = new string[] { "7", "seven", "7 people" };

        [Header("Audio")]
        public AudioClip correctSound;
        public AudioClip voiceSound;

        private AudioSource audioSource;
        private bool isPlayerNearby = false;
        private Vector3 initialPosition;
        private Vector3 stepAsideTargetPosition;
        public bool IsCleared => isCleared;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            initialPosition = transform.position;
            // 반대편 벽(분리벽 기둥) 쪽으로 비켜서서 통로를 완벽 개방
            stepAsideTargetPosition = initialPosition + (floorLevel % 2 == 0 ? Vector3.left * 1.8f : Vector3.right * 1.8f);

            SetupDefaultRiddleForFloor();
        }

        private void Start()
        {
            UpdateGuardState();
        }

        private void Update()
        {
            if (isPlayerNearby && !isCleared && Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        public void Interact()
        {
            if (isCleared) return;

            var ui = IbRiddleInputUI.Instance;
            if (ui == null) ui = UnityEngine.Object.FindFirstObjectByType<IbRiddleInputUI>();
            if (ui == null)
            {
                GameObject uiGo = new GameObject("IbRiddleInputUI_Controller");
                ui = uiGo.AddComponent<IbRiddleInputUI>();
            }

            if (ui != null && !ui.IsOpen)
            {
                if (audioSource != null && voiceSound != null) audioSource.PlayOneShot(voiceSound);
                ui.OpenRiddleUI(null, riddleTitle, riddleQuestion, this);
            }
        }

        public void SetupDefaultRiddleForFloor()
        {
            switch (floorLevel)
            {
                case 3:
                    riddleTitle = "3F Curator's Trial [이성: 관찰의 문]";
                    riddleQuestion = "To proceed upward, answer my question:\n1~2층에 걸린 모든 초상화에 등장하는 '사람'의 총 수는 몇 명인가요?\n(How many human figures are depicted across all lower portraits?)";
                    acceptedAnswers = new string[] { "7", "seven", "7 people", "seven people", "7명", "일곱", "일곱명", "7개" };
                    break;
                case 4:
                    riddleTitle = "4F Curator's Trial [감정: 색채의 기억]";
                    riddleQuestion = "Observe carefully:\n3층 대형 초상화 속 침묵하는 여인이 입고 있는 드레스의 메인 색상은 무엇인가요?\n(What is the primary color of the dress in the 3F grand portrait?)";
                    acceptedAnswers = new string[] { "red", "crimson", "scarlet", "red dress", "빨강", "빨간색", "붉은색", "적색", "빨강색" };
                    break;
                case 5:
                    riddleTitle = "5F Curator's Trial [무의식: 뇌와 신경]";
                    riddleQuestion = "Tell me the truth:\n바이스만 박사가 예술과의 공명(Resonance)을 시도했던 인간의 신체 기관은 무엇인가요?\n(Which human organ did founder Weismann attempt to synchronize?)";
                    acceptedAnswers = new string[] { "brain", "mind", "brainwave", "neural", "soul", "뇌", "두뇌", "마음", "영혼" };
                    break;
                case 6:
                    riddleTitle = "6F Curator's Trial [집착: 저주받은 캔버스]";
                    riddleQuestion = "Answer without fear:\n복도 벽면의 액자에서 흘러내리고 있는 붉은 액체의 정체는 무엇인가요?\n(What red liquid is oozing down from the cursed portrait?)";
                    acceptedAnswers = new string[] { "blood", "red blood", "paint", "tears", "피", "물감", "붉은 물감", "눈물" };
                    break;
                case 7:
                    riddleTitle = "7F Curator's Trial [인공: 떨어진 장미]";
                    riddleQuestion = "A delicate question:\n4층 시든 장미 화병에서 바닥으로 떨어진 꽃잎은 총 몇 장인가요?\n(How many petals have fallen on the floor from the withered rose vase?)";
                    acceptedAnswers = new string[] { "5", "five", "5 petals", "five petals", "5개", "다섯", "다섯개", "다섯 잎" };
                    break;
                case 8:
                    riddleTitle = "8F Curator's Trial [자아: 창립자의 이름]";
                    riddleQuestion = "State the identity:\n이 거대한 신경 갤러리와 공명 타워를 설계한 천재 뇌과학자의 성(Surname)은 무엇인가요?\n(What is the surname of the founder who created this neural museum?)";
                    acceptedAnswers = new string[] { "weismann", "carl weismann", "carl", "바이스만", "칼 바이스만" };
                    break;
                case 9:
                    riddleTitle = "9F Curator's Trial [경고: 최후의 열쇠]";
                    riddleQuestion = "The final threshold before the 10th floor:\n이 미술관 전체를 진동시키며 영혼을 동조시키는 단 하나의 핵심 단어는 무엇인가요?\n(What single word represents the harmonic phenomenon vibrating through this gallery?)";
                    acceptedAnswers = new string[] { "resonance", "the resonance", "공명", "레조넌스" };
                    break;
            }
        }

        public bool CheckAnswer(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            string normalized = input.Trim().ToLowerInvariant();
            foreach (var ans in acceptedAnswers)
            {
                if (ans.Trim().ToLowerInvariant() == normalized) return true;
            }
            return false;
        }

        public void OnSolveSuccess()
        {
            isCleared = true;
            UpdateGuardState();

            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }

            // 부드럽게 옆으로 비켜서는 코루틴 시작
            StartCoroutine(StepAsideSmoothRoutine());

            if (IbMuseumUI.Instance != null)
            {
                IbMuseumUI.Instance.ShowDialogueBox(
                    $"미술관 관리인 ({floorLevel}F)",
                    $"<size=22><color=#55FF88><b>\"정답입니다.\"</b></color>\n\"바이스만 갤러리의 특별 인가를 확인했습니다. {floorLevel + 1}층으로 올라가십시오.\"</size>"
                );
            }
        }

        private IEnumerator StepAsideSmoothRoutine()
        {
            float elapsed = 0f;
            float duration = 1.8f; // 천천히 부드럽게 비켜섬
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, stepAsideTargetPosition, t);
                yield return null;
            }

            transform.position = stepAsideTargetPosition;
        }

        private void UpdateGuardState()
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = !isCleared;
            }

            if (isCleared)
            {
                transform.position = stepAsideTargetPosition;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                isPlayerNearby = true;
                if (!isCleared && IbMuseumUI.Instance != null)
                {
                    IbMuseumUI.Instance.SetInteractPromptVisible(true, "[ E ]");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                isPlayerNearby = false;
                if (IbMuseumUI.Instance != null)
                {
                    IbMuseumUI.Instance.SetInteractPromptVisible(false);
                }
            }
        }
    }
}
