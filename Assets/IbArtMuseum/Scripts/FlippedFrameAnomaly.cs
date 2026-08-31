using UnityEngine;

namespace IbArtMuseum
{
    public class FlippedFrameAnomaly : IbAnomalyBase
    {
        [Header("Frame Target")]
        [Tooltip("뒤집힐 액자 트랜스폼")]
        public Transform frameTransform;

        [Tooltip("이상현상 시 회전 오프셋 (기본: 180도 뒤집힘)")]
        public Vector3 flipRotationOffset = new Vector3(0f, 0f, 180f);

        private Quaternion _originalRotation;

        private void Awake()
        {
            anomalyName = "거꾸로 걸린 명화 (Flipped Frame)";
            description = "벽에 똑바로 걸려있던 액자가 180도 뒤집혀 걸려있습니다.";

            if (frameTransform == null)
            {
                frameTransform = transform;
            }

            _originalRotation = frameTransform.localRotation;
        }

        public override void ActivateAnomaly()
        {
            if (frameTransform != null)
            {
                frameTransform.localRotation = _originalRotation * Quaternion.Euler(flipRotationOffset);
            }
        }

        public override void DeactivateAnomaly()
        {
            if (frameTransform != null)
            {
                frameTransform.localRotation = _originalRotation;
            }
        }
    }
}
