using UnityEngine;

namespace IbArtMuseum
{
    public class BleedingPaintingAnomaly : IbAnomalyBase
    {
        [Header("References")]
        [Tooltip("그림 메쉬의 Renderer")]
        public Renderer paintingRenderer;

        [Tooltip("정상 상태 머티리얼")]
        public Material normalMaterial;

        [Tooltip("피/눈물 흘리는 이상현상 머티리얼")]
        public Material bleedingMaterial;

        [Tooltip("피 흘리는 오버레이 또는 파티클 오브젝트 (선택사항)")]
        public GameObject bloodDecalOrParticle;

        private void Awake()
        {
            anomalyName = "피 흘리는 여인의 초상 (Bleeding Portrait)";
            description = "명화 속 여인의 눈에서 검붉은 피가 흘러내립니다.";

            if (paintingRenderer == null)
            {
                paintingRenderer = GetComponent<Renderer>();
            }

            if (normalMaterial == null && paintingRenderer != null)
            {
                normalMaterial = paintingRenderer.sharedMaterial;
            }
        }

        public override void ActivateAnomaly()
        {
            if (paintingRenderer != null && bleedingMaterial != null)
            {
                paintingRenderer.material = bleedingMaterial;
            }

            if (bloodDecalOrParticle != null)
            {
                bloodDecalOrParticle.SetActive(true);
            }
        }

        public override void DeactivateAnomaly()
        {
            if (paintingRenderer != null && normalMaterial != null)
            {
                paintingRenderer.material = normalMaterial;
            }

            if (bloodDecalOrParticle != null)
            {
                bloodDecalOrParticle.SetActive(false);
            }
        }
    }
}
