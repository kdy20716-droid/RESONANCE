using UnityEngine;

namespace IbArtMuseum
{
    public class RotatingStatueAnomaly : IbAnomalyBase
    {
        [Header("Statue Target")]
        [Tooltip("회전할 조각상 또는 조각상의 머리 트랜스폼")]
        public Transform statueTransform;

        [Tooltip("이상현상 시 회전 각도 (기본: 180도 회전하여 플레이어를 바라봄)")]
        public Vector3 rotatedEulerAngles = new Vector3(0f, 180f, 0f);

        private Quaternion _originalRotation;

        private void Awake()
        {
            anomalyName = "고개를 돌린 조각상 (Turned Statue)";
            description = "벽면을 바라보던 조각상이 통로 쪽을 향해 몸이나 고개를 돌리고 있습니다.";

            if (statueTransform == null)
            {
                statueTransform = transform;
            }

            _originalRotation = statueTransform.localRotation;
        }

        public override void ActivateAnomaly()
        {
            if (statueTransform != null)
            {
                statueTransform.localRotation = _originalRotation * Quaternion.Euler(rotatedEulerAngles);
            }
        }

        public override void DeactivateAnomaly()
        {
            if (statueTransform != null)
            {
                statueTransform.localRotation = _originalRotation;
            }
        }
    }
}
