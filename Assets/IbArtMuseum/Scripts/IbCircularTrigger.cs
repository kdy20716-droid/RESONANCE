using UnityEngine;

namespace IbArtMuseum
{
    public enum FloorTriggerType
    {
        FloorArrival,  // 계단 다 내려와 해당 층 시작 체크포인트 도착 (이상현상 적용 & 층 체크포인트 기록)
        EnterMainHall, // 전시장 중앙 진입 (탐색 시작 플래그 활성화)
        StairsDown,    // 다음 층으로 내려가는 계단 입구/계단 체크포인트 (이상현상 무시 판정)
        TurnBack       // 전시장을 보고 다시 스폰 복도로 되돌아감 (되돌아가기 판정)
    }

    public class IbCircularTrigger : MonoBehaviour
    {
        public FloorTriggerType triggerType = FloorTriggerType.StairsDown;
        public int floorLevel = 10;
        public float cooldown = 1.0f;
        public float maxYDifference = 1.8f; // ★ Y축 높이 차이가 1.8m 이상이면 절대 작동하지 않음

        private float _lastTriggerTime = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                // Y축 높이 검사 (다른 층 플레이어 간섭 원천 차단!)
                float yDiff = Mathf.Abs(other.transform.position.y - transform.position.y);
                if (yDiff > maxYDifference) return;

                if (Time.time - _lastTriggerTime < cooldown) return;
                _lastTriggerTime = Time.time;

                if (IbGameManager.Instance == null) return;

                switch (triggerType)
                {
                    case FloorTriggerType.FloorArrival:
                        IbGameManager.Instance.OnFloorArrival(floorLevel);
                        break;

                    case FloorTriggerType.EnterMainHall:
                        IbGameManager.Instance.OnPlayerEnteredMainHall(floorLevel);
                        break;

                    case FloorTriggerType.StairsDown:
                        IbGameManager.Instance.OnDescendedStairs(floorLevel);
                        break;

                    case FloorTriggerType.TurnBack:
                        IbGameManager.Instance.OnTurnedBackOnAnomaly(floorLevel);
                        break;
                }
            }
        }
    }
}
