using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace IbArtMuseum
{
    public enum GamePhase
    {
        Prologue_Day,       // 1층에서 시작하여 10층으로 자유롭게 올라가며 관람
        Cutscene_Awakening, // 10층 동상 [E] 상호작용 시 링 가속 회전 컷씬
        Night_Loop          // 밤으로 전환되어 10층 -> 1층 하강 루프 탈출 게임
    }

    [System.Serializable]
    public struct FloorCheckpoint
    {
        public int floorLevel;          // 10F, 9F, 8F, ..., 1F
        public Vector3 spawnPosition;   // 계단 다 내려와서 3걸음 앞 정확한 월드 좌표
        public Quaternion spawnRotation;
    }

    public class IbGameManager : MonoBehaviour
    {
        public static IbGameManager Instance { get; private set; }

        [Header("Game State")]
        public GamePhase currentPhase = GamePhase.Prologue_Day;
        public int currentFloorIndex = 0; // 0 = 10F, 1 = 9F, ..., 9 = 1F
        public int roseLife = 3;
        public float floorHeight = 7.0f;
        public bool hasExploredCurrentFloor = false; // 전시장 내부 탐색 완료 여부

        [Header("References")]
        public IbPlayerController player;
        public IbAnomalyManager anomalyManager;
        public IbAudioAmbience audioAmbience;
        public IbMuseumUI uiManager;

        [Header("Environment Controls")]
        public Light sunDirectionalLight;
        public Transform prologueSpawnPoint_1F;
        public Transform prologueParentsTarget;

        [Header("10F Monument Cutscene References")]
        public Transform monumentRingsRoot;
        public GameObject monumentSoulCore;
        public GameObject monumentGlowLight;
        public Transform monumentTriggerTarget;

        [Header("Ceiling Light Materials (Day / Night)")]
        public Material dayCeilingLightMat;
        public Material nightCeilingBlackMat;

        [Header("Strict Floor Checkpoints (10F -> 1F)")]
        public FloorCheckpoint[] floorCheckpoints = new FloorCheckpoint[10];
        public Transform[] floorSpawnPoints = new Transform[10];

        [Header("8th Exit State Machine (Yellow / Green / Blue)")]
        public bool hasPassedYellow = false;            // 노란색 원(복도 4번째 액자) 밟았는지 여부
        public bool hasPassedGreen = false;             // 초록색 원(계단 하단) 밟았는지 여부
        public bool isCurrentFloorAnomalyActive = false; // 이번 층에 이상현상이 발동되었는지 여부
        public int currentTrackingFloor = 10;           // 현재 진행 중인 층수
        public bool hasExploredCorridor = false;         // 파란색 원을 벗어나 복도로 나갔는지 여부 (유턴 감지 핵심 플래그)

        private bool hasTriggeredCutscene = false;
        private bool isTransitioning = false;
        private Coroutine prologueRoutine;

        public static readonly (string nameKo, string nameEn, string desc)[] FloorThemes = new (string, string, string)[]
        {
            ("감각", "Sensation", "평화로운 관람과 외부 세계의 입구"),       // 1F
            ("기억", "Memory", "행복했던 과거의 가족 초상화"),               // 2F
            ("이성", "Reason", "논리와 관찰의 첫 번째 수수께끼"),             // 3F
            ("감정", "Emotion", "슬픔의 비와 흘러내리는 기억"),              // 4F
            ("무의식", "Subconscious", "억압된 트라우마와 심연"),           // 5F
            ("집착", "Obsession", "딸을 향한 광기와 붉은 캔버스"),           // 6F
            ("인공", "Artificial", "기계 인형과 인공 생명"),                 // 7F
            ("자아", "Ego", "영혼의 복제와 정체성 혼란"),                   // 8F
            ("경고", "Warning", "접근 금지 구역과 시스템 경고"),             // 9F
            ("공명", "Resonance", "싱귤래리티와 에코의 각성")                // 10F
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeStrictFloorCheckpoints();
        }

        private void Start()
        {
            // 첫 화면: 1층(처음부터 시작) vs 10층(10층부터 시작) 선택 메뉴 모달 표시
            if (IbStartMenuUI.Instance == null)
            {
                gameObject.AddComponent<IbStartMenuUI>();
            }
            IbStartMenuUI.Instance.ShowStartMenu();
        }

        private void Update()
        {
            if (isTransitioning || currentPhase != GamePhase.Night_Loop || player == null) return;

            // 플레이어가 파란색 원에서 2.5m 이상 벗어나 복도로 걸어나가면 즉시 복도 탐색 플래그 활성화!
            // (노란색 원을 안 밟아도 1~2번 액자에서 이상현상을 보고 유턴하면 무조건 유턴으로 인정되도록 보장!)
            if (!hasExploredCorridor && floorCheckpoints != null && currentFloorIndex < floorCheckpoints.Length)
            {
                Vector3 cpPos = floorCheckpoints[currentFloorIndex].spawnPosition;
                float distFromStart = Vector2.Distance(
                    new Vector2(player.transform.position.x, player.transform.position.z),
                    new Vector2(cpPos.x, cpPos.z)
                );
                if (distFromStart > 2.5f)
                {
                    hasExploredCorridor = true;
                }
            }
        }

        /// <summary>
        /// 10층부터 바로 시작 (공명 각성 푸른 밤 & 8번 출구 루프 즉시 진입)
        /// </summary>
        public void StartFrom10FNight()
        {
            if (prologueRoutine != null) StopCoroutine(prologueRoutine);

            currentPhase = GamePhase.Night_Loop;
            currentFloorIndex = 0; // 10F
            currentTrackingFloor = 10;
            roseLife = 3;
            hasExploredCurrentFloor = false;
            hasTriggeredCutscene = true;
            isTransitioning = false;

            // 1. 완벽한 푸른 밤(Night) 환경 세팅 (RESONANCE.mp3 BGM, 조명 3000 lux)
            SetDayEnvironment(false);

            // 2. 10층 동상 코어 및 링 조명 끄기
            if (monumentRingsRoot != null) monumentRingsRoot.gameObject.SetActive(false);
            if (monumentSoulCore != null) monumentSoulCore.SetActive(false);
            if (monumentGlowLight != null) monumentGlowLight.SetActive(false);

            // 3. UI 정리 & 장미 3송이 라이프 HUD 활성화
            uiManager?.HideEnding();
            uiManager?.SetRoseLife(3);

            // 4. 플레이어 10층 공명 조형물 정면 스폰 (북쪽 바라봄)
            Vector3 pos10F = (floorCheckpoints != null && floorCheckpoints.Length > 0) ? floorCheckpoints[0].spawnPosition : new Vector3(0f, 9 * floorHeight + 0.15f, -8.0f);
            Quaternion rot10F = (floorCheckpoints != null && floorCheckpoints.Length > 0) ? floorCheckpoints[0].spawnRotation : Quaternion.Euler(0, 0f, 0);

            if (player != null)
            {
                player.Teleport(pos10F, rot10F);
                player.CanMove = true;
            }

            // 5. 화면 글리치 및 알림 대사
            uiManager?.PlayGlitchFlash(0.2f, new Color(0.4f, 0.7f, 1.0f, 0.4f));
            uiManager?.ShowDialogueBox("10F RESONANCE", "<size=20><color=#87CEEB><i>10F Weismann Gallery Top Floor.\nThe blue resonance has awakened. Descend through the stairs to 1F to escape.</i></color></size>");
            Debug.Log("<color=#55FFFF><b>[Start] Started directly from 10F! Blue night 8th Exit loop mode activated.</b></color>");
        }

        // 10층부터 1층까지 파란색 원 바로 살짝 위 정확한 고유 좌표 및 시선 각도 초기화
        public void InitializeStrictFloorCheckpoints()
        {
            floorCheckpoints = new FloorCheckpoint[10];

            for (int f = 0; f < 10; f++)
            {
                int floorNum = 10 - f;
                float floorY = (floorNum - 1) * 7.0f;
                bool isEvenFloor = (floorNum % 2 == 0);

                Vector3 spPos;
                Quaternion spRot;

                if (floorNum == 10)
                {
                    spPos = new Vector3(0f, floorY + 0.15f, -8.0f);
                    spRot = Quaternion.Euler(0, 0f, 0);
                }
                else if (isEvenFloor)
                {
                    // 짝수층(8F, 6F, 4F, 2F): 남쪽 계단 착지 지점 파란색 원 바로 살짝 위 (X = 1.0m, Z = -21.75m)
                    // 복도(+X, 동쪽) 및 벽면 대형 층수 숫자(Floor 8 Plate 등)를 정면으로 바라봄!
                    spPos = new Vector3(1.0f, floorY + 0.15f, -21.75f);
                    spRot = Quaternion.Euler(0, 90f, 0);
                }
                else
                {
                    // 홀수층(9F, 7F, 5F, 3F, 1F): 북쪽 계단 착지 지점 파란색 원 바로 살짝 위 (X = 1.0m, Z = 21.75m)
                    // 복도(-X, 서쪽) 및 벽면 대형 층수 숫자를 정면으로 바라봄!
                    spPos = new Vector3(1.0f, floorY + 0.15f, 21.75f);
                    spRot = Quaternion.Euler(0, -90f, 0);
                }

                // 씬에 배치된 실제 FloorArrivalTrigger(파란색 원) 오브젝트가 있다면 좌표를 정확히 일치시킴!
                GameObject trigObj = GameObject.Find($"FloorArrivalTrigger_{floorNum}F");
                if (trigObj != null)
                {
                    spPos.x = trigObj.transform.position.x;
                    spPos.z = trigObj.transform.position.z;
                    spPos.y = floorY + 0.15f; // 바닥 파란색 링 바로 살짝 위
                }

                // 씬에 SpawnPoint 오브젝트가 있다면 동기화
                if (floorSpawnPoints != null && f < floorSpawnPoints.Length && floorSpawnPoints[f] != null)
                {
                    floorSpawnPoints[f].position = spPos;
                    floorSpawnPoints[f].rotation = spRot;
                }

                floorCheckpoints[f] = new FloorCheckpoint
                {
                    floorLevel = floorNum,
                    spawnPosition = spPos,
                    spawnRotation = spRot
                };
            }
        }

        // ==================== 1. 프롤로그 완전 리셋 ====================
        public void FullRestartToPrologue()
        {
            if (prologueRoutine != null) StopCoroutine(prologueRoutine);

            currentPhase = GamePhase.Prologue_Day;
            currentFloorIndex = 0;
            roseLife = 3;
            hasExploredCurrentFloor = false;
            hasTriggeredCutscene = false;
            isTransitioning = false;

            // 낮 환경 복구
            SetDayEnvironment(true);

            // 동상 링 복구
            if (monumentRingsRoot != null) monumentRingsRoot.gameObject.SetActive(true);
            if (monumentSoulCore != null) monumentSoulCore.SetActive(true);
            if (monumentGlowLight != null) monumentGlowLight.SetActive(true);

            // UI 정리 & 장미 HUD 숨김
            uiManager?.HideEnding();
            uiManager?.HideRoseLife();

            // 플레이어 1층 스폰 & 부모님 쪽으로 시선 고정
            if (prologueSpawnPoint_1F != null && player != null)
            {
                player.Teleport(prologueSpawnPoint_1F.position, prologueSpawnPoint_1F.rotation);
                if (prologueParentsTarget != null)
                {
                    player.LookAtTarget(prologueParentsTarget.position);
                }
                player.CanMove = false;
            }

            // 3단계 순차 대화 시작
            prologueRoutine = StartCoroutine(PrologueThreeStepDialogueRoutine());
        }

        private IEnumerator PrologueThreeStepDialogueRoutine()
        {
            yield return new WaitForSeconds(0.4f);

            // 1단계 대사: 어머니
            bool step1Done = false;
            uiManager?.ShowDialogueBox("Mother", "<size=24><b><color=#F08080>Mother:</color></b> \"Well, Lina, you're always so impatient.\"</size>", () => step1Done = true);
            while (!step1Done) yield return null;
            yield return new WaitForSeconds(0.15f);

            // 2단계 대사: 어머니
            bool step2Done = false;
            uiManager?.ShowDialogueBox("Mother", "<size=24><b><color=#F08080>Mother:</color></b> \"We'll take our time looking around, so go ahead and explore first.\"</size>", () => step2Done = true);
            while (!step2Done) yield return null;
            yield return new WaitForSeconds(0.15f);

            // 3단계 대사: 아버지
            bool step3Done = false;
            uiManager?.ShowDialogueBox("Father", "<size=24><b><color=#87CEEB>Father:</color></b> \"Never run or do anything dangerous in the museum, understand Lina?\"</size>", () => step3Done = true);
            while (!step3Done) yield return null;

            if (player != null)
            {
                player.CanMove = true;
            }
            prologueRoutine = null;
        }

        // ==================== 2. 10층 동상 [E] 상호작용 컷씬 (푸른 밤 전환) ====================
        public void TriggerResonanceMonumentInteraction()
        {
            if (hasTriggeredCutscene || currentPhase != GamePhase.Prologue_Day) return;
            StartCoroutine(MonumentAwakeningCutsceneRoutine());
        }

        private IEnumerator MonumentAwakeningCutsceneRoutine()
        {
            hasTriggeredCutscene = true;
            currentPhase = GamePhase.Cutscene_Awakening;

            if (player != null) player.CanMove = false;

            // 1. 초기 반응 대사
            bool dlgDone = false;
            uiManager?.ShowDialogueBox("RESONANCE", "<size=22><color=#FFDF80><i>As Lina touches the monument, the harmonic rings begin to resonate with an eerie vibration...</i></color></size>", () => dlgDone = true);
            while (!dlgDone) yield return null;

            // 2. 동상 회전 시작 & 리나의 다급한 대사!
            uiManager?.ShowDialogueBox("Lina", "<size=24><b><color=#FFD700>Lina:</color></b> \"What... what is happening?! The rings are spinning out of control...!\"</size>");

            // 링 가속 회전 (0 -> 950 deg/s)
            float elapsed = 0f;
            float duration = 4.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float speed = Mathf.Lerp(30f, 950f, Mathf.Pow(elapsed / duration, 2.2f));

                if (monumentRingsRoot != null)
                {
                    monumentRingsRoot.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
                    monumentRingsRoot.Rotate(Vector3.right, (speed * 0.4f) * Time.deltaTime, Space.Self);
                }

                yield return null;
            }

            // 소멸 & 번쩍임!
            uiManager?.PlayGlitchFlash(0.3f, new Color(0.6f, 0.8f, 1.0f, 0.95f));

            if (monumentRingsRoot != null) monumentRingsRoot.gameObject.SetActive(false);
            if (monumentSoulCore != null) monumentSoulCore.SetActive(false);
            if (monumentGlowLight != null) monumentGlowLight.SetActive(false);

            // ★ 완벽한 푸른 밤(Blue Night) 환경으로 전환!
            SetDayEnvironment(false);
            currentPhase = GamePhase.Night_Loop;
            currentFloorIndex = 0; // 10층 시작
            roseLife = 3;
            hasExploredCurrentFloor = false;

            // 장미 라이프 UI 활성화 (3송이)
            uiManager?.SetRoseLife(3);

            if (player != null) player.CanMove = true;
        }

        public void SetDayEnvironment(bool isDay)
        {
            // 1. 태양광 / 하늘 색상 제어 (자연스럽고 편안한 대낮 자연광 32000 lux, 밤 12000 lux)
            if (sunDirectionalLight != null)
            {
                sunDirectionalLight.transform.rotation = Quaternion.Euler(isDay ? 48f : -3.5f, 30f, 0f);
                sunDirectionalLight.color = isDay ? new Color(1f, 0.97f, 0.94f) : new Color(0.65f, 0.78f, 1.0f);
                sunDirectionalLight.intensity = isDay ? 1.4f : 0.8f;
                var hdSun = sunDirectionalLight.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                if (hdSun != null)
                {
                    hdSun.intensity = isDay ? 32000f : 12000f; // ★ 눈부심 없이 쾌적하고 맑은 하늘!
                    hdSun.volumetricDimmer = 0.25f; // 태양광으로 인한 하늘 백화 현상 방지
                }
            }

            // 2. 10층 밤하늘 별무리(NightSky_StarField) 제어
            GameObject starField = GameObject.Find("NightSky_StarField");
            if (starField != null)
            {
                starField.SetActive(!isDay);
            }

            // 3. 10층 천창 빔
            GameObject sunShaft = GameObject.Find("SunShaft_Light_Beam");
            if (sunShaft != null)
            {
                sunShaft.SetActive(isDay);
            }

            // 4. 작품 및 액자 핀조명/스팟라이트 강도 전환: 낮 300000, 밤 3000!
            float targetPictureIntensity = isDay ? 300000f : 3000f;
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l == sunDirectionalLight || (sunShaft != null && l.gameObject == sunShaft)) continue;
                if (l.type == LightType.Spot || l.type == LightType.Point)
                {
                    if (l.gameObject.name != "CoreGlowLight")
                    {
                        l.color = new Color(1f, 0.97f, 0.92f);
                        l.intensity = targetPictureIntensity;
                        var hdL = l.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                        if (hdL != null)
                        {
                            hdL.intensity = targetPictureIntensity;
                        }
                    }
                }
            }

            // 5. Global Volume 노출 전환 (밤하늘과 실내가 쾌적하게 보이는 최적 노출 10.1)
            UnityEngine.Rendering.Volume[] volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                if (vol.isGlobal && vol.profile != null)
                {
                    if (vol.profile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out var exp))
                    {
                        exp.fixedExposure.value = isDay ? 10.2f : 10.1f;
                    }
                }
            }

            // 6. 플레이어 BGM 자동 전환 (낮: emotion.mp3 / 밤: RESONANCE.mp3)
            if (player != null)
            {
                player.SwitchBGM(isDay);
            }
            else if (IbPlayerController.LocalPlayer != null)
            {
                IbPlayerController.LocalPlayer.SwitchBGM(isDay);
            }

            // 6. 관람객 NPC 및 수수께끼 관리인 가드 NPC (낮에는 등장, 밤에는 모두 소멸 & 통로 100% 개방!)
            IbVisitorNPC[] visitors = Object.FindObjectsByType<IbVisitorNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in visitors)
            {
                if (v != null)
                {
                    v.gameObject.SetActive(isDay);
                }
            }

            IbRiddleGuardNPC[] guards = Object.FindObjectsByType<IbRiddleGuardNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in guards)
            {
                if (g != null)
                {
                    g.gameObject.SetActive(isDay); // 밤에는 가드 소멸!
                }
            }

            // 7. 모든 액자 낮/밤 시각적 머티리얼 전환
            IbInteractableArtwork[] allArtworks = Object.FindObjectsByType<IbInteractableArtwork>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var art in allArtworks)
            {
                if (art != null)
                {
                    art.UpdateDayNightVisual(isDay);
                }
            }

            // 8. ★ 1~9층 천장 3x3 직사각형 조명 (낮: light.mat 발광 + 광원 ON / 밤: black.mat 소등 + 광원 OFF!)
            GameObject ceilingLightsRoot = GameObject.Find("Museum_Ceiling_Lights");
            if (ceilingLightsRoot != null)
            {
                ceilingLightsRoot.SetActive(true); // 오브젝트는 유지하여 꺼진 전등 외형 표시

                // 1) 모든 천장 조명 컴포넌트 ON/OFF
                Light[] cLights = ceilingLightsRoot.GetComponentsInChildren<Light>(true);
                foreach (var cl in cLights)
                {
                    if (cl != null) cl.enabled = isDay;
                }

                // 2) 모든 형광등 디퓨저 패널 머티리얼을 낮(light) / 밤(black)으로 실시간 교체!
                if (nightCeilingBlackMat == null)
                {
                    nightCeilingBlackMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                    nightCeilingBlackMat.name = "Runtime_Night_Black_Mat";
                    nightCeilingBlackMat.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.04f, 1f));
                    nightCeilingBlackMat.SetFloat("_Smoothness", 0.05f);
                }

                Material targetPanelMat = isDay ? dayCeilingLightMat : nightCeilingBlackMat;
                if (targetPanelMat != null)
                {
                    MeshRenderer[] renderers = ceilingLightsRoot.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var mr in renderers)
                    {
                        if (mr != null && mr.gameObject.name.Contains("Diffuser"))
                        {
                            mr.material = targetPanelMat;
                        }
                    }
                }
            }

            // 사방이 막힌 실내 갤러리이므로 앰비언트 광을 차분하게 낮추어, 오직 실내 핀조명/스팟라이트에 의해서만 밝기가 결정되도록 설정!
            RenderSettings.ambientLight = isDay ? new Color(0.08f, 0.09f, 0.12f) : new Color(0.02f, 0.03f, 0.05f);
        }

        // ==================== 3. 정밀 8번 출구 체크포인트 루프 로직 ====================

        // 1) 🔵 파란색 원: 계단 착지 및 텔레포트 담당
        // - 초록색 원을 밟고 내려왔다면 ➔ 하강 판정 (정상 통과 vs 이상현상 무시 오답)
        // - 복도를 탐색하다가 되돌아왔다면 ➔ 유턴 판정 (이상현상 회피 성공 vs 정상 층 오답)
        public void OnFloorArrival(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            // 1층 출구 파란색 원 도달 시 엔딩 발동!
            if (floorLevel <= 1)
            {
                StartCoroutine(EndingEscapeSequenceRoutine());
                return;
            }

            // Case A: 복도를 탐색하다가(노란색 원을 밟았거나 파란색 원을 벗어남) 초록색 계단을 안 밟고 현재 층 파란색 원으로 되돌아온 경우 (유턴 감지!)
            if ((hasPassedYellow || hasExploredCorridor) && !hasPassedGreen && floorLevel == currentTrackingFloor)
            {
                OnTurnedBackOnAnomaly(floorLevel);
                return;
            }

            // Case B: 초록색 원을 찍고 아랫층 파란색 원을 찍은 경우 (계단 하강 감지!)
            if (hasPassedGreen)
            {
                if (isCurrentFloorAnomalyActive)
                {
                    // [오답 - 이상현상이 있었는데 무시하고 계단으로 내려옴 -> 9->8층 파란색 원으로 루프 리셋!]
                    Debug.LogWarning($"<color=#FF3333><b>[8번 출구] ❌ 이상현상 간파 실패!</b> ({currentTrackingFloor}층에 이상현상이 있었는데 계단으로 내려왔습니다. 9->8층 파란색 원으로 루프 리셋됩니다!)</color>");
                    StartCoroutine(LoopResetTo8FStairRoutine());
                    return;
                }
                else
                {
                    // [정답 - 정상 층이어서 자연스럽게 계단을 걸어내려와 착지 완료!]
                    Debug.Log($"<color=#33FF33><b>[8번 출구] 🟢 정상 층 통과!</b> ({currentTrackingFloor}층은 정상이었습니다. 아무 일도 일어나지 않고 자연스럽게 {floorLevel}층에 진입합니다.)</color>");
                }
            }

            // 새로운 층 착지 완료 ➔ 상태 세팅
            currentTrackingFloor = floorLevel;
            currentFloorIndex = 10 - floorLevel;
            hasPassedYellow = false;
            hasPassedGreen = false;
            hasExploredCorridor = false;

            // ★ [핵심] 착지하는 즉시 50:50 확률로 이번 층의 이상현상을 미리 결정하여 복도 1번 액자부터 적용!
            if (floorLevel >= 2 && floorLevel <= 8)
            {
                anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);
                isCurrentFloorAnomalyActive = (anomalyManager != null && anomalyManager.HasActiveAnomaly());
                Debug.Log($"<color=#55FFFF><b>[8번 출구] 🔵 {floorLevel}층 착지 완료!</b> (이상현상 활성 상태: {isCurrentFloorAnomalyActive})</color>");
            }
            else
            {
                isCurrentFloorAnomalyActive = false;
                anomalyManager?.DeactivateAllAnomalies();
                Debug.Log($"<color=#55FFFF><b>[8번 출구] 🔵 {floorLevel}층 착지 완료!</b> (정상 갤러리)</color>");
            }
        }

        // 2) 🟡 노란색 원: 복도 4번째 액자 앞 (이상현상 테스트 시작!)
        // 2) 🟡 노란색 원: 복도 4번째 액자 앞
        public void OnPlayerEnteredMainHall() => OnPlayerEnteredMainHall(currentTrackingFloor);
        public void OnPlayerEnteredMainHall(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            hasPassedYellow = true;
            hasExploredCorridor = true;
            currentTrackingFloor = floorLevel;
            currentFloorIndex = 10 - floorLevel;
        }

        // 3) 🟢 초록색 원: 계단 하단 (하강 감지 센서)
        public void OnDescendedStairs() => OnDescendedStairs(currentTrackingFloor);
        public void OnDescendedStairs(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            hasPassedGreen = true;
        }

        // 4) 🔵 파란색 원 유턴 판정
        // - 이상현상이 있었을 때 유턴하면 정답 (무조건 한 층 아래로 전진! 5F -> 4F)
        // - 이상현상이 없었는데 유턴하면 오답 (9->8층 파란색 원으로 루프 리셋!)
        public void OnTurnedBackOnAnomaly() => OnTurnedBackOnAnomaly(currentTrackingFloor);
        public void OnTurnedBackOnAnomaly(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            if (isCurrentFloorAnomalyActive)
            {
                // ★ [정답] 이상현상을 올바르게 간파하고 유턴함! -> 무조건 한 층 아래(floorLevel - 1)로 심리스 전진!
                int nextFloor = Mathf.Max(1, floorLevel - 1);
                Debug.Log($"<color=#33FF33><b>[8번 출구] 🌟 이상현상 간파 성공!</b> ({floorLevel}층 이상현상을 확인하고 유턴했습니다. {nextFloor}층 파란색 원으로 심리스 전진합니다!)</color>");
                StartCoroutine(SeamlessAdvanceToNextFloorRoutine(nextFloor));
            }
            else
            {
                // [오답 - 이상현상이 없었는데 유턴함! -> 9->8층 계단 착지 파란색 원으로 루프 리셋!]
                Debug.LogWarning($"<color=#FF4444><b>[8번 출구] ❌ 잘못된 유턴 (오답)!</b> ({floorLevel}층에 이상현상이 없었는데 뒤로 돌아갔습니다. 9->8층 계단 착지 파란색 원으로 루프 리셋됩니다!)</color>");
                StartCoroutine(LoopResetTo8FStairRoutine());
            }
        }

        /// <summary>
        /// 꼼수 차단 텔레포트: 이상현상이 없는데 유턴하거나 꼼수를 쓰면 직전 층(예: 8F->7F 복도)으로 강제 롤백
        /// </summary>
        public IEnumerator RollbackToPreviousFloorHallwayRoutine(int targetFloorLevel)
        {
            isTransitioning = true;
            hasPassedYellow = false;
            hasPassedGreen = false;
            isCurrentFloorAnomalyActive = false;
            anomalyManager?.DeactivateAllAnomalies();

            currentTrackingFloor = targetFloorLevel;
            currentFloorIndex = 10 - targetFloorLevel;

            if (currentFloorIndex < floorCheckpoints.Length)
            {
                FloorCheckpoint cp = floorCheckpoints[currentFloorIndex];
                if (player != null) player.Teleport(cp.spawnPosition, cp.spawnRotation);
            }

            yield return new WaitForSeconds(0.15f);
            isTransitioning = false;
        }

        // ==================== 4. 3대 분기 엔딩 시스템 ====================

        // 1) ☀️ 해피 엔딩: 현실 귀환 (1층 정문 출구 도달)
        public void TriggerHappyEnding()
        {
            StartCoroutine(HappyEndingRoutine());
        }

        private IEnumerator EndingEscapeSequenceRoutine()
        {
            yield return StartCoroutine(HappyEndingRoutine());
        }

        private IEnumerator HappyEndingRoutine()
        {
            isTransitioning = true;
            if (player != null) player.CanMove = false;

            // 1. 등 뒤에서 들려오는 에코의 다급한 애원
            bool echoPlea = false;
            uiManager?.ShowDialogueBox("ECHO", "<size=24><b><color=#67E8F9>ECHO:</color></b> \"가지 마... 제발 나 혼자 두지 마 리나...!\"</size>", () => echoPlea = true);
            while (!echoPlea) yield return null;
            yield return new WaitForSeconds(0.2f);

            // 2. 문을 박차고 아침 햇살 속으로 탈출!
            uiManager?.PlayGlitchFlash(0.5f, Color.white);
            currentPhase = GamePhase.Prologue_Day;
            SetDayEnvironment(true);

            if (player != null && prologueSpawnPoint_1F != null)
            {
                player.Teleport(prologueSpawnPoint_1F.position, prologueSpawnPoint_1F.rotation);
                if (prologueParentsTarget != null)
                {
                    player.LookAtTarget(prologueParentsTarget.position);
                }
            }

            yield return new WaitForSeconds(0.4f);

            bool step1Done = false;
            uiManager?.ShowDialogueBox("Mother", "<size=24><b><color=#F08080>Mother:</color></b> \"Why are you so late, Lina? Did you have that much fun exploring?\"</size>", () => step1Done = true);
            while (!step1Done) yield return null;
            yield return new WaitForSeconds(0.15f);

            bool step2Done = false;
            uiManager?.ShowDialogueBox("Father", "<size=24><b><color=#87CEEB>Father:</color></b> \"The museum is closing now. Let's head home together, Lina.\"</size>", () => step2Done = true);
            while (!step2Done) yield return null;

            yield return new WaitForSeconds(0.3f);
            uiManager?.ShowEnding(IbMuseumUI.EndingType.Happy_Escape);
        }

        // 2) 🥀 새드 엔딩: 영원한 캔버스 (1층 로비 에코 캔버스 손잡기)
        public void TriggerSadEnding()
        {
            StartCoroutine(SadEndingRoutine());
        }

        private IEnumerator SadEndingRoutine()
        {
            isTransitioning = true;
            if (player != null) player.CanMove = false;

            bool step1 = false;
            uiManager?.ShowDialogueBox("ECHO", "<size=24><b><color=#67E8F9>ECHO:</color></b> \"...내 손을 잡아주는 거야? 정말로...?\"</size>", () => step1 = true);
            while (!step1) yield return null;
            yield return new WaitForSeconds(0.15f);

            bool step2 = false;
            uiManager?.ShowDialogueBox("ECHO", "<size=24><b><color=#67E8F9>ECHO:</color></b> \"고마워, 리나... 이제 우리 영원히 외롭지 않아. 함께 그림 속에서 웃자.\"</size>", () => step2 = true);
            while (!step2) yield return null;

            uiManager?.PlayGlitchFlash(0.7f, new Color(0.4f, 0.65f, 1f, 0.95f));
            yield return new WaitForSeconds(0.3f);

            uiManager?.ShowEnding(IbMuseumUI.EndingType.Sad_Canvas);
        }

        // 3) 💀 배드 엔딩: 육체 잠식 (장미 체력 0 소진 시)
        public void TriggerBadEnding()
        {
            StartCoroutine(BadEndingRoutine());
        }

        private IEnumerator BadEndingRoutine()
        {
            isTransitioning = true;
            if (player != null) player.CanMove = false;

            uiManager?.PlayGlitchFlash(0.8f, new Color(0.85f, 0.05f, 0.05f, 0.95f));

            bool step1 = false;
            uiManager?.ShowDialogueBox("ECHO", "<size=24><b><color=#EF4444>ECHO:</color></b> \"후후... 드디어 몸을 찾았어.\"</size>", () => step1 = true);
            while (!step1) yield return null;
            yield return new WaitForSeconds(0.15f);

            bool step2 = false;
            uiManager?.ShowDialogueBox("ECHO", "<size=24><b><color=#EF4444>ECHO:</color></b> \"고마워, 리나. 네 따뜻한 심장과 두 발로... 바깥 세상을 구경하러 갈게.\"</size>", () => step2 = true);
            while (!step2) yield return null;

            yield return new WaitForSeconds(0.3f);
            uiManager?.ShowEnding(IbMuseumUI.EndingType.Bad_Usurpation);
        }

        public void HealAllRoses()
        {
            roseLife = 3;
            uiManager?.SetRoseLife(roseLife);
        }

        public void SaveGameAtVase(int floorLevel, Vector3 pos)
        {
            PlayerPrefs.SetInt("Ib_Save_Floor", floorLevel);
            PlayerPrefs.SetFloat("Ib_Save_PosX", pos.x);
            PlayerPrefs.SetFloat("Ib_Save_PosY", pos.y);
            PlayerPrefs.SetFloat("Ib_Save_PosZ", pos.z);
            PlayerPrefs.Save();
        }

        public void TakeRoseDamage()
        {
            roseLife--;
            uiManager?.SetRoseLife(roseLife);
            uiManager?.PlayGlitchFlash(0.25f, new Color(1f, 0f, 0f, 0.6f));

            if (roseLife <= 0)
            {
                TriggerBadEnding();
            }
        }

        /// <summary>
        /// 체력이 모두 닳았을 때 10층 공명 직후의 밤 루프 상태로 즉시 재시작
        /// </summary>
        public void RespawnAt10FNight()
        {
            roseLife = 3;
            currentFloorIndex = 0; // 10F
            currentPhase = GamePhase.Night_Loop;
            hasExploredCurrentFloor = false;
            uiManager?.SetRoseLife(3);

            SetDayEnvironment(false); // 밤 환경 유지

            if (floorCheckpoints != null && floorCheckpoints.Length > 0)
            {
                FloorCheckpoint cp = floorCheckpoints[0];
                if (player != null) player.Teleport(cp.spawnPosition, cp.spawnRotation);
            }

            anomalyManager?.DecideAndApplyAnomaly(0);

            Debug.Log("<color=#FFD700><b>[부활] 체력 소진으로 10층 밤 8번 출구 시작 지점에서 재시작합니다!</b></color>");
        }

        public void RespawnAtFloor(int floorLevel)
        {
            int floorIdx = Mathf.Clamp(10 - floorLevel, 0, 9);
            currentFloorIndex = floorIdx;
            roseLife = 3;
            uiManager?.SetRoseLife(roseLife);

            if (floorIdx < floorCheckpoints.Length)
            {
                FloorCheckpoint cp = floorCheckpoints[floorIdx];
                if (player != null) player.Teleport(cp.spawnPosition, cp.spawnRotation);
            }

            if (floorLevel == 10)
            {
                PlayerPrefs.SetInt("Ib_Reached_10F", 1);
                PlayerPrefs.Save();
                SetDayEnvironment(false);
            }
            else
            {
                SetDayEnvironment(true);
            }
        }

        /// <summary>
        /// 이상현상 발생 시 유턴하면 다음 목표 층으로 심리스 상대 텔레포트 전진!
        /// 파란색 원 내부에서만 진행되며, 마우스 시선 방향과 걸음걸이를 계산하여 벽을 뚫지 않고 자연스럽게 연결
        /// </summary>
        private IEnumerator SeamlessAdvanceToNextFloorRoutine(int targetFloorLevel)
        {
            isTransitioning = true;
            hasPassedYellow = false;
            hasPassedGreen = false;
            isCurrentFloorAnomalyActive = false;
            anomalyManager?.DeactivateAllAnomalies();

            int fromFloor = currentTrackingFloor;
            currentTrackingFloor = targetFloorLevel;
            currentFloorIndex = 10 - targetFloorLevel;

            if (player != null && currentFloorIndex < floorCheckpoints.Length)
            {
                FloorCheckpoint toCp = floorCheckpoints[currentFloorIndex];
                bool toIsEven = (targetFloorLevel % 2 == 0);

                // 목표 층(아랫층)의 파란색 원 위치
                Vector3 targetPos = toCp.spawnPosition;
                targetPos.y = (targetFloorLevel - 1) * floorHeight + 0.15f; // 목표 층 바닥 바로 살짝 위 안착

                // ★ 핵심: 다음 목표 층에서는 복도 안쪽(대형 층수 숫자가 있는 전시장 방향)을 정면으로 바라보도록 정렬!
                // 짝수층(4F, 6F, 8F): 복도 동쪽(+X, Yaw = 90도) ➔ 4번 숫자가 정면에 훤히 보이며 복도로 걸어나감!
                // 홀수층(3F, 5F, 7F): 복도 서쪽(-X, Yaw = -90도) ➔ 해당 층 숫자가 정면에 훤히 보이며 복도로 걸어나감!
                float corridorYaw = toIsEven ? 90f : -90f;
                Quaternion targetRot = Quaternion.Euler(0f, corridorYaw, 0f);

                // 복도 진행 방향 기준 살짝 뒤쪽(계단 착지 지점)에 스폰하여, 앞으로 복도를 걸어나가며 대형 숫자를 여유롭게 볼 수 있도록 배치!
                Vector3 forwardDir = targetRot * Vector3.forward;
                targetPos -= forwardDir * 0.50f;

                // 파란색 원 내부 안전 클램핑 (벽 관통 100% 차단)
                targetPos.x = Mathf.Clamp(targetPos.x, toCp.spawnPosition.x - 0.85f, toCp.spawnPosition.x + 0.85f);
                targetPos.z = Mathf.Clamp(targetPos.z, toCp.spawnPosition.z - 0.55f, toCp.spawnPosition.z + 0.55f);

                // 심리스 텔레포트 실행: 카메라 상하 Pitch 보존 및 이동 속도 유지!
                player.TeleportSeamless(targetPos, targetRot, preservePitch: true, preserveVelocity: true);

                // 화면 중앙에 새로운 층수 알림 표시 (1.5초간)
                uiManager?.SetInteractPromptVisible(true, $"[ {targetFloorLevel}F ] WEISMANN GALLERY");

                // ★ 도착한 새 층(예: 4층)의 이상현상을 50:50 확률로 즉시 적용!
                if (targetFloorLevel >= 2 && targetFloorLevel <= 8)
                {
                    anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);
                    isCurrentFloorAnomalyActive = (anomalyManager != null && anomalyManager.HasActiveAnomaly());
                }
                else
                {
                    isCurrentFloorAnomalyActive = false;
                    anomalyManager?.DeactivateAllAnomalies();
                }

                Debug.Log($"<color=#33FF33><b>[8번 출구] 🌟 이상현상 간파 성공! {fromFloor}F ➔ {targetFloorLevel}F 안착 완료!</b> (정면의 {targetFloorLevel}번 숫자를 확인하고 복도를 탐색하세요.)</color>");
            }

            yield return new WaitForSeconds(0.15f);
            isTransitioning = false;

            yield return new WaitForSeconds(1.5f);
            uiManager?.SetInteractPromptVisible(false);
        }

        /// <summary>
        /// 8번 출구 오답 시: 9층에서 8층으로 내려가는 계단 착지 지점(8층 파란색 원, 8번 숫자가 보이는 층)으로 심리스 리셋!
        /// </summary>
        private IEnumerator LoopResetTo8FStairRoutine()
        {
            isTransitioning = true;
            hasPassedYellow = false;
            hasPassedGreen = false;
            isCurrentFloorAnomalyActive = false;
            anomalyManager?.DeactivateAllAnomalies();

            int fromFloor = currentTrackingFloor;
            currentTrackingFloor = 8;
            currentFloorIndex = 2; // 8F

            if (player != null && floorCheckpoints != null && floorCheckpoints.Length > 2)
            {
                int fromFloorIndex = Mathf.Clamp(10 - fromFloor, 0, floorCheckpoints.Length - 1);
                FloorCheckpoint fromCp = floorCheckpoints[fromFloorIndex];
                FloorCheckpoint toCp = floorCheckpoints[2];

                Vector3 playerPos = player.transform.position;
                Vector3 offset = playerPos - fromCp.spawnPosition;
                offset.y = 0f;
                offset = Vector3.ClampMagnitude(offset, 0.6f);

                // 8층 파란색 원 내부에서 진행 방향(동쪽 +X) 기준 살짝 뒤쪽(X = 0.5m)으로 스폰
                Vector3 targetPos = new Vector3(toCp.spawnPosition.x - 0.50f, 7 * floorHeight + 0.15f, toCp.spawnPosition.z + offset.z);
                targetPos.x = Mathf.Clamp(targetPos.x, toCp.spawnPosition.x - 0.80f, toCp.spawnPosition.x + 0.80f);
                targetPos.z = Mathf.Clamp(targetPos.z, toCp.spawnPosition.z - 0.50f, toCp.spawnPosition.z + 0.50f);

                // 시선: 정면 복도 및 8번 숫자가 시원하게 보이는 동쪽(90도) 방향
                Quaternion targetRot = Quaternion.Euler(0f, 90f, 0f);

                player.TeleportSeamless(targetPos, targetRot, preservePitch: true, preserveVelocity: false);
                Debug.Log("<color=#FF5555><b>[8번 출구] 🔄 루프 리셋: 9층->8층 계단 착지 지점 (8층 파란색 원 살짝 뒤쪽)으로 안전 이동 완료!</b></color>");
            }

            yield return new WaitForSeconds(0.15f);
            isTransitioning = false;
        }

        public void TriggerEnding(bool victory)
        {
            if (IbMainMenuManager.Instance != null && victory)
            {
                IbMainMenuManager.Instance.ShowGameClear();
            }
            else
            {
                uiManager?.ShowEnding(victory);
            }
        }
    }
}
