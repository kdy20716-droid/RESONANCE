using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IbArtMuseum
{
    /// <summary>
    /// 3층부터 10층까지 다음 층으로 가는 계단 입구를 막는 투명 벽 및 수수께끼 액자
    /// </summary>
    public class IbRiddleBarrier : MonoBehaviour
    {
        [Header("Barrier Config")]
        public int floorLevel = 3;
        public bool isUnlocked = false;

        [Header("Riddle Content (English)")]
        public string riddleTitle = "Riddle of the 3rd Floor";
        [TextArea(3, 6)]
        public string riddleQuestion = "How many human figures are depicted across all the paintings you passed on 1F and 2F?";
        public string[] acceptedAnswers = new string[] { "7", "seven", "7 people" };

        [Header("Scene Objects")]
        public GameObject invisibleBarrierCollider; // 투명 충돌체
        public GameObject barrierParticleEffect;   // 푸른빛 차단 이펙트
        public GameObject riddlePaintingFrame;     // 계단 앞 수수께끼 액자
        public Light riddleFrameLight;             // 액자 조명

        [Header("Audio")]
        public AudioClip unlockSound;
        public AudioClip errorSound;

        private AudioSource audioSource;
        private bool isPlayerNearby = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            SetupDefaultRiddleForFloor();
        }

        private void Start()
        {
            UpdateBarrierState();
        }

        private void Update()
        {
            if (isPlayerNearby && !isUnlocked && Input.GetKeyDown(KeyCode.E))
            {
                if (IbRiddleInputUI.Instance != null && !IbRiddleInputUI.Instance.IsOpen)
                {
                    IbRiddleInputUI.Instance.OpenRiddleUI(this, riddleTitle, riddleQuestion);
                }
            }
        }

        public void SetupDefaultRiddleForFloor()
        {
            switch (floorLevel)
            {
                case 3:
                    riddleTitle = "Riddle of the 3rd Floor (1F~2F Observation)";
                    riddleQuestion = "How many human figures are depicted across all the portraits you passed on the lower floors?";
                    acceptedAnswers = new string[] { "7", "seven", "7 people", "seven people" };
                    break;
                case 4:
                    riddleTitle = "Riddle of the 4th Floor (Color of Memory)";
                    riddleQuestion = "What is the primary color of the dress worn by the silent lady in the 3F grand portrait?";
                    acceptedAnswers = new string[] { "red", "crimson", "scarlet", "red dress" };
                    break;
                case 5:
                    riddleTitle = "Riddle of the 5th Floor (Neuro-Art Concept)";
                    riddleQuestion = "Which human organ did Carl Weismann attempt to synchronize with art through Resonance?";
                    acceptedAnswers = new string[] { "brain", "mind", "brainwave", "neural", "soul" };
                    break;
                case 6:
                    riddleTitle = "Riddle of the 6th Floor (Tragedy & Tears)";
                    riddleQuestion = "What liquid is oozing down from the cursed painting on this floor?";
                    acceptedAnswers = new string[] { "blood", "red blood", "blood drops", "paint", "tears" };
                    break;
                case 7:
                    riddleTitle = "Riddle of the 7th Floor (Withered Petals)";
                    riddleQuestion = "How many petals have fallen on the marble floor from the withered rose vase on 4F?";
                    acceptedAnswers = new string[] { "5", "five", "5 petals", "five petals" };
                    break;
                case 8:
                    riddleTitle = "Riddle of the 8th Floor (The Creator's Name)";
                    riddleQuestion = "What is the surname of the genius founder who established this vast neural gallery?";
                    acceptedAnswers = new string[] { "weismann", "carl weismann", "carl" };
                    break;
                case 9:
                    riddleTitle = "Riddle of the 9th Floor (The Final Key)";
                    riddleQuestion = "What single word represents the phenomenon that connects soul, mind, and the 10F kinetic rings?";
                    acceptedAnswers = new string[] { "resonance", "the resonance", "resonance harmonic" };
                    break;
            }
        }

        public bool CheckAnswer(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            string normalizedInput = input.Trim().ToLowerInvariant();
            foreach (var ans in acceptedAnswers)
            {
                if (ans.Trim().ToLowerInvariant() == normalizedInput)
                {
                    return true;
                }
            }
            return false;
        }

        public void OnSolveSuccess()
        {
            isUnlocked = true;
            UpdateBarrierState();

            if (audioSource != null && unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            if (IbMuseumUI.Instance != null)
            {
                IbMuseumUI.Instance.ShowDialogueBox(
                    "투명 벽 해제", 
                    $"<size=22><color=#55FF88><b>정답입니다!</b></color>\n투명한 장벽이 흩어지며 {floorLevel + 1}층으로 향하는 계단이 열렸습니다.</size>"
                );
            }

            Debug.Log($"<color=#55FF88><b>[퍼즐 해제] {floorLevel}층 수수께끼 정답! 투명 벽이 해제되었습니다.</b></color>");
        }

        private void UpdateBarrierState()
        {
            if (invisibleBarrierCollider != null)
            {
                invisibleBarrierCollider.SetActive(!isUnlocked);
            }

            if (barrierParticleEffect != null)
            {
                barrierParticleEffect.SetActive(!isUnlocked);
            }

            if (riddleFrameLight != null)
            {
                riddleFrameLight.color = isUnlocked ? Color.green : new Color(1f, 0.4f, 0.4f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                isPlayerNearby = true;
                if (!isUnlocked && IbMuseumUI.Instance != null)
                {
                    IbMuseumUI.Instance.SetInteractPromptVisible(true, $"[ E ] 수수께끼 액자 풀기 ({floorLevel}F ➔ {floorLevel + 1}F)");
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
