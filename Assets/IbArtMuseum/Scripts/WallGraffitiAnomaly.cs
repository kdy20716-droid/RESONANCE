using UnityEngine;

namespace IbArtMuseum
{
    public class WallGraffitiAnomaly : IbAnomalyBase
    {
        [Header("Graffiti Targets")]
        [Tooltip("벽면에 나타날 낙서/데칼 게임오브젝트들")]
        public GameObject[] graffitiObjects;

        private void Awake()
        {
            anomalyName = "벽면의 붉은 크레파스 낙서 (Red Crayon Graffiti)";
            description = "벽면에 평소에 없던 기괴한 붉은 글씨와 그림 낙서가 나타납니다.";
        }

        public override void ActivateAnomaly()
        {
            if (graffitiObjects != null)
            {
                foreach (var obj in graffitiObjects)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }
        }

        public override void DeactivateAnomaly()
        {
            if (graffitiObjects != null)
            {
                foreach (var obj in graffitiObjects)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
    }
}
