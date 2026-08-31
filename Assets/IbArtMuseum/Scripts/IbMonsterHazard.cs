using UnityEngine;

namespace IbArtMuseum
{
    /// <summary>
    /// 액자에서 튀어나오는 괴물 또는 촉수 트랩 (접촉 시 장미 체력 차감)
    /// </summary>
    public class IbMonsterHazard : MonoBehaviour
    {
        [Header("Hazard Config")]
        public string hazardName = "Shadow Creature";
        public bool isOneHitDestroy = true;
        public GameObject attackEffectPrefab;
        public AudioClip attackSound;

        private bool hasHit = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            if (other.CompareTag("Player") || other.GetComponent<IbPlayerController>() != null)
            {
                hasHit = true;

                var gm = IbGameManager.Instance;
                if (gm != null)
                {
                    gm.TakeRoseDamage();
                    Debug.Log($"<color=#FF3333><b>[피격!] {hazardName}에게 피격당해 장미 1송이가 차감되었습니다!</b></color>");
                }

                if (attackSound != null)
                {
                    AudioSource.PlayClipAtPoint(attackSound, transform.position);
                }

                if (attackEffectPrefab != null)
                {
                    Instantiate(attackEffectPrefab, transform.position, Quaternion.identity);
                }

                if (isOneHitDestroy)
                {
                    Destroy(gameObject, 0.2f);
                }
            }
        }
    }
}
