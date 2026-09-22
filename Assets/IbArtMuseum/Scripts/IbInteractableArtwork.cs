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
        public bool isEchoSadEndingCanvas = false; // 1층 에코 캔버스: 손을 잡으면 새드 엔딩 발동

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

            if (isEchoSadEndingCanvas)
            {
                bool isNightTime = (IbGameManager.Instance != null && IbGameManager.Instance.currentPhase == GamePhase.Night_Loop);
                if (isNightTime)
                {
                    // 공명 후 2층: 에코가 손을 내밀고 선택지 UI가 표시됨!
                    if (IbMuseumUI.Instance != null)
                    {
                        IbMuseumUI.Instance.ShowChoiceDialogue(
                            "《 공명하는 에코 (Resonating Echo) 》",
                            "캔버스 속 인공소녀 에코가 액자 밖으로 손을 내밀며 당신을 애절하게 바라보고 있습니다.\n\n<color=#67E8F9><b>\"...나를 혼자 두지 마. 이제 혼자는 너무 무서워...\n나와 영원히 함께해줄래?\"</b></color>",
                            "[ 1 ] 에코의 손을 잡는다 (Stay)",
                            () => {
                                IbGameManager.Instance?.TriggerSadEnding();
                            },
                            "[ 2 ] 거절하고 지나친다 (Leave)",
                            () => {
                                IbMuseumUI.Instance?.ShowDialogueBox("ECHO", "<color=#67E8F9>\"...가지 마... 부탁이야...\"</color>\n\n<color=#A0AEC0>에코의 애원을 뒤로하고 차가운 계단을 향해 발걸음을 옮깁니다.</color>");
                            }
                        );
                    }
                    return;
                }
                else
                {
                    // 올라갈 때(낮): 일반 명화 설명만 표시됨!
                    string dayTitle = "어린 딸의 초상 (Portrait of a Young Girl)";
                    string dayFull = $"<b><color=#E6B422>《 {dayTitle} 》</color></b>\n<size=18><color=#B0A898>Artist: Carl Weismann</color></size>\n\n<color=#FFFFFF>바이스만 박사가 생전 가장 아꼈던 어린 딸의 초상화.\n맑고 순수한 눈망울로 환하게 웃고 있다.</color>";
                    if (IbMuseumUI.Instance != null)
                    {
                        IbMuseumUI.Instance.ShowDialogueBox(dayTitle, dayFull);
                    }
                    return;
                }
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
