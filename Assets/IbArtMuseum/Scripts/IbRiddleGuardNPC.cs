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
            // 벽면 쪽으로 살짝 물러서며 통과를 허용
            stepAsideTargetPosition = initialPosition + Vector3.left * 1.2f;

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
                    riddleTitle = "3F Curator's Trial (Observation)";
                    riddleQuestion = "To proceed upward, answer my question:\nHow many human figures are depicted across all the portraits on the 1st and 2nd floors?";
                    acceptedAnswers = new string[] { "7", "seven", "7 people", "seven people" };
                    break;
                case 4:
                    riddleTitle = "4F Curator's Trial (Memory of Color)";
                    riddleQuestion = "Observe carefully:\nWhat is the primary color of the elegant dress worn by the silent lady in the 3F grand portrait?";
                    acceptedAnswers = new string[] { "red", "crimson", "scarlet", "red dress" };
                    break;
                case 5:
                    riddleTitle = "5F Curator's Trial (Neuro-Science)";
                    riddleQuestion = "Tell me the truth:\nWhich human organ did founder Carl Weismann attempt to synchronize with art through Resonance?";
                    acceptedAnswers = new string[] { "brain", "mind", "brainwave", "neural", "soul" };
                    break;
                case 6:
                    riddleTitle = "6F Curator's Trial (The Cursed Canvas)";
                    riddleQuestion = "Answer without fear:\nWhat red liquid is oozing down from the cursed portrait in this hall?";
                    acceptedAnswers = new string[] { "blood", "red blood", "paint", "tears" };
                    break;
                case 7:
                    riddleTitle = "7F Curator's Trial (Withered Petals)";
                    riddleQuestion = "A delicate question:\nHow many petals have fallen on the floor from the withered rose vase on 4F?";
                    acceptedAnswers = new string[] { "5", "five", "5 petals", "five petals" };
                    break;
                case 8:
                    riddleTitle = "8F Curator's Trial (The Master's Name)";
                    riddleQuestion = "State the identity:\nWhat is the surname of the genius founder who created this neural museum?";
                    acceptedAnswers = new string[] { "weismann", "carl weismann", "carl" };
                    break;
                case 9:
                    riddleTitle = "9F Curator's Trial (The Final Key)";
                    riddleQuestion = "The final threshold before the 10th floor:\nWhat single word represents the harmonic phenomenon vibrating through this entire gallery?";
                    acceptedAnswers = new string[] { "resonance", "the resonance" };
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
            float duration = 1.0f;
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
