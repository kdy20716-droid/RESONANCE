using UnityEngine;
using UnityEngine.Events;

namespace IbArtMuseum
{
    public class IbInteractableArtwork : MonoBehaviour
    {
        [Header("Artwork Info")]
        public string artworkTitle = "Artwork Title";
        [TextArea(2, 5)]
        public string description = "A detailed description of the artwork.";
        public string author = "Carl Weismann";

        [Header("Day / Night Materials")]
        public Material dayMaterial;
        public Material nightMaterial;

        [Header("Night Anomaly Override (Optional)")]
        public string nightTitle = "";
        [TextArea(2, 5)]
        public string nightDescription = "";

        [Header("Interaction Settings")]
        public string interactPrompt = "[ E ]";
        public bool isResonanceMonument = false;

        public UnityEvent onInteracted;

        private MeshRenderer _canvasRenderer;

        private void Start()
        {
            Transform canvasT = transform.Find("Canvas");
            if (canvasT != null)
            {
                _canvasRenderer = canvasT.GetComponent<MeshRenderer>();
            }
        }

        public void Interact()
        {
            if (isResonanceMonument)
            {
                if (IbGameManager.Instance != null)
                {
                    IbGameManager.Instance.TriggerResonanceMonumentInteraction();
                }
                return;
            }

            onInteracted?.Invoke();

            bool isNight = (IbGameManager.Instance != null && IbGameManager.Instance.currentPhase == GamePhase.Night_Loop);
            string finalTitle = GetCurrentTitle(isNight);
            string finalDesc = GetCurrentDescription(isNight);

            string fullText = $"<b><color=#E6B422>《 {finalTitle} 》</color></b>\n<size=18><color=#B0A898>Artist: {author}</color></size>\n\n<color=#FFFFFF>{finalDesc}</color>";
            if (IbMuseumUI.Instance != null)
            {
                IbMuseumUI.Instance.ShowDialogueBox(finalTitle, fullText);
            }
        }

        public void UpdateDayNightVisual(bool isDay)
        {
            if (_canvasRenderer == null)
            {
                Transform canvasT = transform.Find("Canvas");
                if (canvasT != null) _canvasRenderer = canvasT.GetComponent<MeshRenderer>();
            }

            if (_canvasRenderer != null)
            {
                if (isDay && dayMaterial != null)
                {
                    _canvasRenderer.sharedMaterial = dayMaterial;
                }
                else if (!isDay && nightMaterial != null)
                {
                    _canvasRenderer.sharedMaterial = nightMaterial;
                }
            }
        }

        public string GetCurrentTitle(bool isNight)
        {
            return (isNight && !string.IsNullOrEmpty(nightTitle)) ? nightTitle : artworkTitle;
        }

        public string GetCurrentDescription(bool isNight)
        {
            return (isNight && !string.IsNullOrEmpty(nightDescription)) ? nightDescription : description;
        }
    }
}
