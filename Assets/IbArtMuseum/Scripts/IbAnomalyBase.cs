using UnityEngine;

namespace IbArtMuseum
{
    public abstract class IbAnomalyBase : MonoBehaviour
    {
        [Header("Anomaly Info")]
        [Tooltip("디버그 및 식별용 이상현상 이름")]
        public string anomalyName = "미술관 이상현상";

        [TextArea(2, 4)]
        [Tooltip("이상현상에 대한 설명")]
        public string description = "게르테나 미술관의 기괴한 변화";

        /// <summary>
        /// 이상현상 발생 시 호출되는 메서드
        /// </summary>
        public abstract void ActivateAnomaly();

        /// <summary>
        /// 정상 복도 상태로 복구될 때 호출되는 메서드
        /// </summary>
        public abstract void DeactivateAnomaly();
    }
}
