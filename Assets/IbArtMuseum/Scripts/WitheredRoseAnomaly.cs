using UnityEngine;

namespace IbArtMuseum
{
    public class WitheredRoseAnomaly : IbAnomalyBase
    {
        [Header("Rose References")]
        [Tooltip("정상 상태 장미 오브젝트 (싱싱한 붉은 장미)")]
        public GameObject freshRoseObject;

        [Tooltip("이상현상 상태 장미 오브젝트 (시들거나 검게 변한 장미)")]
        public GameObject witheredRoseObject;

        [Tooltip("바닥에 떨어진 꽃잎들 오브젝트")]
        public GameObject fallenPetalsObject;

        private void Awake()
        {
            anomalyName = "시들어버린 붉은 장미 (Withered Rose)";
            description = "꽃병에 꽂혀있던 아름다운 붉은 장미가 검게 시들고 꽃잎이 바닥에 흩뿌려져 있습니다.";
        }

        public override void ActivateAnomaly()
        {
            if (freshRoseObject != null) freshRoseObject.SetActive(false);
            if (witheredRoseObject != null) witheredRoseObject.SetActive(true);
            if (fallenPetalsObject != null) fallenPetalsObject.SetActive(true);
        }

        public override void DeactivateAnomaly()
        {
            if (freshRoseObject != null) freshRoseObject.SetActive(true);
            if (witheredRoseObject != null) witheredRoseObject.SetActive(false);
            if (fallenPetalsObject != null) fallenPetalsObject.SetActive(false);
        }
    }
}
