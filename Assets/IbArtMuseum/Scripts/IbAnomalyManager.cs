using System.Collections.Generic;
using UnityEngine;

namespace IbArtMuseum
{
    public class IbAnomalyManager : MonoBehaviour
    {
        [Header("Registered Anomalies")]
        [Tooltip("미술관 내에 존재하는 모든 이상현상 목록")]
        public List<IbAnomalyBase> anomalies = new List<IbAnomalyBase>();

        [Header("Debug")]
        [SerializeField] private IbAnomalyBase currentActiveAnomaly = null;

        public IbAnomalyBase CurrentActiveAnomaly => currentActiveAnomaly;

        private void Awake()
        {
            RefreshAnomaliesList();
        }

        public void RefreshAnomaliesList()
        {
            anomalies.Clear();
            anomalies.AddRange(FindObjectsByType<IbAnomalyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        public bool HasActiveAnomaly()
        {
            return currentActiveAnomaly != null;
        }

        public void DecideAndApplyAnomaly(int floorIndex)
        {
            DeactivateAllAnomalies();

            if (anomalies.Count == 0)
            {
                RefreshAnomaliesList();
            }

            int floorLevel = 10 - floorIndex; // 10, 9, 8, ..., 1

            // 10층(시작), 9층(프롤로그 분위기 층), 1층(엔딩)은 항상 정상 갤러리 (이상현상 0% 보장!)
            if (floorLevel >= 9 || floorLevel <= 1)
            {
                currentActiveAnomaly = null;
                Debug.Log($"<color=#66FF66>[이상현상 시스템] {floorLevel}층: ✅ 이상현상 없음 (정상 갤러리)</color>");
                return;
            }

            // ★ 8층부터 2층까지는 50대 50 (50% 확률)로 이상현상 발생!
            bool spawnAnomaly = (floorLevel >= 2 && floorLevel <= 8) && (Random.value < 0.50f);

            if (spawnAnomaly && anomalies.Count > 0)
            {
                // 현재 층에 위치한 이상현상을 우선 검색
                float floorY = (floorLevel - 1) * 7.0f;
                List<IbAnomalyBase> floorAnomalies = new List<IbAnomalyBase>();
                foreach (var a in anomalies)
                {
                    if (a != null && Mathf.Abs(a.transform.position.y - floorY) < 6.0f)
                    {
                        floorAnomalies.Add(a);
                    }
                }

                if (floorAnomalies.Count > 0)
                {
                    currentActiveAnomaly = floorAnomalies[Random.Range(0, floorAnomalies.Count)];
                }
                else
                {
                    currentActiveAnomaly = anomalies[Random.Range(0, anomalies.Count)];
                }

                if (currentActiveAnomaly != null)
                {
                    currentActiveAnomaly.ActivateAnomaly();
                    Debug.Log($"<color=#FF5555>[이상현상 시스템] {floorLevel}층 [50:50 발생]: ⚠️ 이상현상 발생! [{currentActiveAnomaly.anomalyName}] (발견 시 뒤돌아서 유턴해야 탈출 가능!)</color>");
                }
            }
            else
            {
                currentActiveAnomaly = null;
                Debug.Log($"<color=#66FF66>[이상현상 시스템] {floorLevel}층: ✅ 이상현상 없음 (정상 갤러리) (계단으로 내려가야 함!)</color>");
            }
        }

        public void DeactivateAllAnomalies()
        {
            if (anomalies.Count == 0)
            {
                RefreshAnomaliesList();
            }

            foreach (var anomaly in anomalies)
            {
                if (anomaly != null)
                {
                    anomaly.DeactivateAnomaly();
                }
            }
            currentActiveAnomaly = null;
        }
    }
}
