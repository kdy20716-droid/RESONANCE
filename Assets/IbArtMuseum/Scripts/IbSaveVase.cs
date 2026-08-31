using System.Collections;
using UnityEngine;

namespace IbArtMuseum
{
    /// <summary>
    /// 3F, 5F, 7F, 9F에 배치되는 세이브 & 1회용 체력 회복 화병
    /// </summary>
    public class IbSaveVase : MonoBehaviour
    {
        [Header("Vase Config")]
        public int floorLevel = 3;
        public bool isUsed = false;

        [Header("Visual Elements")]
        public GameObject waterVisual;
        public Light vaseGlowLight;

        [Header("Audio")]
        public AudioClip healSound;

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
        }

        private void Update()
        {
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
            {
                InteractWithVase();
            }
        }

        public void InteractWithVase()
        {
            var gm = IbGameManager.Instance;
            if (gm == null) return;

            if (isUsed)
            {
                if (IbMuseumUI.Instance != null)
                {
                    IbMuseumUI.Instance.ShowDialogueBox("유리 화병", "<size=22>물병의 물이 모두 말라있다. 더 이상 회복할 수 없다.</size>");
                }
                return;
            }

            // 1회용 사용 처리
            isUsed = true;

            // 1. 장미 체력 완충 회복
            gm.HealAllRoses();

            // 2. 세이브 포인트 갱신 (3, 5, 7, 9층)
            gm.SaveGameAtVase(floorLevel, transform.position);

            // 3. 비주얼 & 오디오 연출
            if (waterVisual != null) waterVisual.SetActive(false);
            if (vaseGlowLight != null) vaseGlowLight.intensity = 50f;

            if (audioSource != null && healSound != null)
            {
                audioSource.PlayOneShot(healSound);
            }

            // 4. UI 알림 출력
            if (IbMuseumUI.Instance != null)
            {
                IbMuseumUI.Instance.ShowDialogueBox(
                    $"생명의 화병 ({floorLevel}F)", 
                    $"<size=22>화병에 담긴 신비로운 물에 장미를 담갔다.\n<color=#FF5577><b>장미가 싱싱하게 되살아났습니다! (체력 완전 회복 & {floorLevel}층 세이브 완료)</b></color></size>"
                );
            }

            Debug.Log($"<color=#33FF88><b>[세이브 & 힐링] {floorLevel}층 화병 사용 완료! 장미 회복 및 세이브 포인트 저장</b></color>");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                isPlayerNearby = true;
                if (IbMuseumUI.Instance != null)
                {
                    string statusMsg = isUsed ? "[ E ] 물이 말라있는 화병" : "[ E ] 화병의 물에 장미 담그기 (체력 회복 & 세이브)";
                    IbMuseumUI.Instance.SetInteractPromptVisible(true, statusMsg);
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
