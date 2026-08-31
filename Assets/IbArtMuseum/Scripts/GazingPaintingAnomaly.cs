using UnityEngine;

namespace IbArtMuseum
{
    public class GazingPaintingAnomaly : IbAnomalyBase
    {
        [Header("Gazing References")]
        [Tooltip("플레이어를 따라 회전할 눈동자/머리 트랜스폼")]
        public Transform eyeOrHeadTransform;

        [Tooltip("추적할 플레이어 (비어있으면 자동 탐색)")]
        public Transform playerTransform;

        [Header("Gazing Settings")]
        public float rotationSpeed = 5f;
        public float maxAngle = 60f;

        private Quaternion _originalRotation;
        private bool _isActive = false;

        private void Awake()
        {
            anomalyName = "시선 추적 명화 (Gazing Eyes)";
            description = "그림 속 인물의 눈동자가 플레이어의 이동 동선을 실시간으로 응시합니다.";

            if (eyeOrHeadTransform != null)
            {
                _originalRotation = eyeOrHeadTransform.localRotation;
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = FindFirstObjectByType<IbPlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        private void Update()
        {
            if (!_isActive || eyeOrHeadTransform == null || playerTransform == null) return;

            // 플레이어 방향 계산
            Vector3 directionToPlayer = playerTransform.position - eyeOrHeadTransform.position;
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                eyeOrHeadTransform.rotation = Quaternion.Slerp(eyeOrHeadTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        public override void ActivateAnomaly()
        {
            _isActive = true;
        }

        public override void DeactivateAnomaly()
        {
            _isActive = false;
            if (eyeOrHeadTransform != null)
            {
                eyeOrHeadTransform.localRotation = _originalRotation;
            }
        }
    }
}
