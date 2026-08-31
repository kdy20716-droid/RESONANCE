using UnityEngine;

namespace IbArtMuseum
{
    public class IbLoopTrigger : MonoBehaviour
    {
        [Tooltip("체크 시 계단 하강 트리거, 해제 시 뒤돌아가기 트리거")]
        public bool isForwardTrigger = true;

        [Tooltip("중복 트리거 방지 쿨다운(초)")]
        public float cooldown = 1.5f;

        private float _lastTriggerTime = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                if (Time.time - _lastTriggerTime < cooldown)
                {
                    return;
                }

                _lastTriggerTime = Time.time;

                if (IbGameManager.Instance != null)
                {
                    if (isForwardTrigger)
                    {
                        IbGameManager.Instance.OnDescendedStairs();
                    }
                    else
                    {
                        IbGameManager.Instance.OnTurnedBackOnAnomaly();
                    }
                }
            }
        }
    }
}
