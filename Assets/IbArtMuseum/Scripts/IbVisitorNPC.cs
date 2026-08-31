using UnityEngine;

namespace IbArtMuseum
{
    public class IbVisitorNPC : MonoBehaviour
    {
        [Header("Visitor Info")]
        public string visitorName = "Visitor";
        [TextArea(2, 4)]
        public string dialogue = "The artwork here is truly fascinating...";

        private IbInteractableArtwork _interactable;

        private void Awake()
        {
            _interactable = GetComponent<IbInteractableArtwork>();
            if (_interactable == null)
            {
                _interactable = gameObject.AddComponent<IbInteractableArtwork>();
            }

            _interactable.artworkTitle = visitorName;
            _interactable.author = "Gallery Visitor";
            _interactable.description = $"\"{dialogue}\"";
            _interactable.interactPrompt = "[ E ] Talk";
        }

        public void SetDialogue(string name, string text)
        {
            visitorName = name;
            dialogue = text;

            if (_interactable != null)
            {
                _interactable.artworkTitle = visitorName;
                _interactable.description = $"\"{dialogue}\"";
            }
        }
    }
}
