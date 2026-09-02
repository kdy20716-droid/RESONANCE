using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IbArtMuseum
{
    [RequireComponent(typeof(CharacterController))]
    public class IbPlayerController : MonoBehaviour
    {
        public static IbPlayerController LocalPlayer { get; private set; }

        [Header("Movement Settings")]
        public float walkSpeed = 3.8f;
        public float sprintSpeed = 6.0f;
        public float gravity = -15f;

        [Header("Look Settings")]
        public Transform playerCamera;
        public float mouseSensitivity = 0.06f; // 적정 마우스 감도로 부드럽게 조정
        public float upperLookLimit = 80f;
        public float lowerLookLimit = -80f;

        [Header("Interaction Settings")]
        public float interactDistance = 3.8f;
        public LayerMask interactLayerMask = ~0;

        [Header("Head Bob Settings")]
        public bool enableHeadBob = true;
        public float bobFrequency = 2.0f;
        public float bobHorizontalAmplitude = 0.03f;
        public float bobVerticalAmplitude = 0.04f;

        [Header("BGM Settings")]
        public AudioSource bgmAudioSource;
        public AudioClip dayBgmClip;   // emotion.mp3 (낮 BGM)
        public AudioClip nightBgmClip; // RESONANCE.mp3 (밤 BGM)
        public AudioClip bgmClip;      // 기본/호환 클립
        [Range(0f, 1f)] public float bgmVolume = 0.05f; // 은은한 볼륨 0.05로 통일

        private CharacterController _characterController;
        private float _verticalRotation = 0f;
        private Vector3 _velocity;
        private float _defaultCameraPosY = 1.65f; // 시점 높이 1.65m
        private float _bobTimer = 0f;
        private bool _canMove = true;

        private IbInteractableArtwork _currentHoveredInteractable = null;

        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }

        private void Awake()
        {
            LocalPlayer = this;
            _characterController = GetComponent<CharacterController>();

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            if (playerCamera != null)
            {
                playerCamera.localPosition = new Vector3(0, 1.65f, 0);
                _defaultCameraPosY = 1.65f;
            }

            // 캐릭터에 BGM AudioSource 구성
            if (bgmAudioSource == null)
            {
                bgmAudioSource = GetComponent<AudioSource>();
                if (bgmAudioSource == null) bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (bgmAudioSource != null)
            {
                bgmAudioSource.loop = true;
                bgmAudioSource.spatialBlend = 0f; // 2D BGM (균일한 볼륨)
                bgmAudioSource.volume = bgmVolume;
                bgmAudioSource.playOnAwake = false; // 씬 뷰에서 소리 안 나도록 false!

                AudioClip initialClip = dayBgmClip ?? bgmClip;
                if (initialClip != null)
                {
                    bgmAudioSource.clip = initialClip;
                }
            }
        }

        private void Start()
        {
            LockCursor();

            // 게임 시작 시 BGM 지속 무한 재생
            if (bgmAudioSource != null && bgmAudioSource.clip != null && !bgmAudioSource.isPlaying)
            {
                bgmAudioSource.volume = bgmVolume;
                bgmAudioSource.Play();
            }
        }

        /// <summary>
        /// 낮/밤 상태에 따라 BGM을 즉시 자연스럽게 전환 (낮: emotion.mp3 / 밤: RESONANCE.mp3)
        /// </summary>
        public void SwitchBGM(bool isDay)
        {
            if (!Application.isPlaying) return; // 에디터 모드에서는 절대 재생하지 않음!

            AudioClip targetClip = isDay ? (dayBgmClip ?? bgmClip) : (nightBgmClip ?? bgmClip);
            if (bgmAudioSource != null && targetClip != null)
            {
                if (bgmAudioSource.clip != targetClip)
                {
                    bgmAudioSource.clip = targetClip;
                    bgmAudioSource.volume = bgmVolume;
                    bgmAudioSource.loop = true;
                    bgmAudioSource.Play();
                }
                else if (!bgmAudioSource.isPlaying)
                {
                    bgmAudioSource.volume = bgmVolume;
                    bgmAudioSource.Play();
                }
            }
        }

        private void Update()
        {
            HandleInteractionRaycast();

            if (!_canMove)
            {
                // 이동 불가 시에도 중력은 적용하여 바닥 유지
                if (_characterController.isGrounded && _velocity.y < 0) _velocity.y = -2f;
                _velocity.y += gravity * Time.deltaTime;
                _characterController.Move(_velocity * Time.deltaTime);
                return;
            }

            HandleLook();
            HandleMovement();
            HandleHeadBob();
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 dir = (targetPosition - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            if (playerCamera != null)
            {
                _verticalRotation = 0f;
                playerCamera.localRotation = Quaternion.identity;
            }
        }

        private void HandleInteractionRaycast()
        {
            if (playerCamera == null) return;

            // 대화창이 열려있으면 조사 프롬프트 숨김
            if (IbMuseumUI.Instance != null && IbMuseumUI.Instance.IsDialogueActive)
            {
                _currentHoveredInteractable = null;
                IbMuseumUI.Instance.SetInteractPromptVisible(false);
                return;
            }

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide);

            IbInteractableArtwork interactable = null;
            IbRiddleGuardNPC guard = null;
            IbSaveVase vase = null;
            float closestDistance = float.MaxValue;

            if (hits != null && hits.Length > 0)
            {
                // 거리순으로 정렬하여 가장 가까운 유효 상호작용 대상 선택
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    var art = hit.collider.GetComponentInParent<IbInteractableArtwork>() ?? hit.collider.GetComponent<IbInteractableArtwork>();
                    var grd = hit.collider.GetComponentInParent<IbRiddleGuardNPC>() ?? hit.collider.GetComponent<IbRiddleGuardNPC>();
                    var vs = hit.collider.GetComponentInParent<IbSaveVase>() ?? hit.collider.GetComponent<IbSaveVase>();

                    if (grd != null && !grd.IsCleared)
                    {
                        guard = grd;
                        break;
                    }
                    if (vs != null)
                    {
                        vase = vs;
                        break;
                    }
                    if (art != null)
                    {
                        interactable = art;
                        break;
                    }
                }
            }

            _currentHoveredInteractable = interactable;

            if (guard != null && !guard.IsCleared)
            {
                IbMuseumUI.Instance?.SetInteractPromptVisible(true, "[ E ]");
                if (CheckInteractKeyPressed())
                {
                    guard.Interact();
                }
            }
            else if (vase != null)
            {
                IbMuseumUI.Instance?.SetInteractPromptVisible(true, "[ E ]");
                if (CheckInteractKeyPressed())
                {
                    vase.Interact();
                }
            }
            else if (_currentHoveredInteractable != null)
            {
                IbMuseumUI.Instance?.SetInteractPromptVisible(true, "[ E ]");

                // E 키 입력 시 상호작용 발동!
                if (CheckInteractKeyPressed())
                {
                    _currentHoveredInteractable.Interact();
                }
            }
            else
            {
                IbMuseumUI.Instance?.SetInteractPromptVisible(false);
            }
        }

        private bool CheckInteractKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
            if (Input.GetKeyDown(KeyCode.E)) return true;
            return false;
        }

        private void HandleLook()
        {
            float mouseDeltaX = 0f;
            float mouseDeltaY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                mouseDeltaX = delta.x * mouseSensitivity;
                mouseDeltaY = delta.y * mouseSensitivity;
            }
#else
            mouseDeltaX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * 10f;
            mouseDeltaY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 10f;
#endif

            transform.Rotate(Vector3.up * mouseDeltaX);

            if (playerCamera != null)
            {
                _verticalRotation -= mouseDeltaY;
                _verticalRotation = Mathf.Clamp(_verticalRotation, lowerLookLimit, upperLookLimit);
                playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            float h = 0f;
            float v = 0f;
            bool isSprinting = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.leftShiftKey.isPressed) isSprinting = true;
            }
#else
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift);
#endif

            Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void HandleHeadBob()
        {
            if (!enableHeadBob || playerCamera == null) return;

            Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
            if (horizontalVelocity.magnitude > 0.1f && _characterController.isGrounded)
            {
                _bobTimer += Time.deltaTime * bobFrequency * (_characterController.velocity.magnitude / walkSpeed);
                float newY = _defaultCameraPosY + Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude;
                float newX = Mathf.Cos(_bobTimer) * bobHorizontalAmplitude;

                playerCamera.localPosition = new Vector3(newX, newY, playerCamera.localPosition.z);
            }
            else
            {
                _bobTimer = 0f;
                playerCamera.localPosition = new Vector3(
                    Mathf.Lerp(playerCamera.localPosition.x, 0f, Time.deltaTime * 5f),
                    Mathf.Lerp(playerCamera.localPosition.y, _defaultCameraPosY, Time.deltaTime * 5f),
                    playerCamera.localPosition.z
                );
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = position;
            transform.rotation = rotation;
            _velocity = Vector3.zero;

            if (playerCamera != null)
            {
                _verticalRotation = 0f;
                playerCamera.localRotation = Quaternion.identity;
            }

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }
        }
    }
}
