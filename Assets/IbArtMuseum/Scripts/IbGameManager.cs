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

        private bool hasTriggeredCutscene = false;
        private bool isTransitioning = false;
        private Coroutine prologueRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeStrictFloorCheckpoints();
        }

        private void Start()
        {
            FullRestartToPrologue();
        }

        // 10층부터 1층까지 정확한 Y축 고유 좌표 초기화
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
                    spPos = new Vector3(0f, floorY + 0.1f, -8.0f);
                    spRot = Quaternion.Euler(0, 0f, 0);
                }
                else if (isEvenFloor)
                {
                    // 짝수층: 북쪽 계단 착지 3걸음 앞 (X = 0.5m, Y = 고유 층 높이, Z = 21.75m), 서쪽 바라봄
                    spPos = new Vector3(0.5f, floorY + 0.1f, 21.75f);
                    spRot = Quaternion.Euler(0, -90f, 0);
                }
                else
                {
                    // 홀수층: 남쪽 계단 착지 3걸음 앞 (X = -0.5m, Y = 고유 층 높이, Z = -21.75m), 동쪽 바라봄
                    spPos = new Vector3(-0.5f, floorY + 0.1f, -21.75f);
                    spRot = Quaternion.Euler(0, 90f, 0);
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
            uiManager?.PlayGlitchFlash(0.2f, new Color(0.6f, 0.8f, 1.0f, 0.9f));

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

            uiManager?.ShowDialogueBox("Darkness", "<size=22><color=#87CEEB><i>A deep blue night descends over the gallery. The stars shine through the skylight. You must find your way down to 1F.</i></color></size>");
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
        // - 노란색 원을 밟고 되돌아왔다면 ➔ 유턴 판정 (이상현상 회피 성공 vs 정상 층 오답)
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

            // Case A: 노란색 원을 밟았고 초록색 원은 밟지 않은 채 현재 층 파란색 원으로 되돌아온 경우 (유턴 감지!)
            if (hasPassedYellow && !hasPassedGreen && floorLevel == currentTrackingFloor)
            {
                OnTurnedBackOnAnomaly(floorLevel);
                return;
            }

            // Case B: 초록색 원을 찍고 아랫층 파란색 원을 찍은 경우 (하강 감지!)
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

            // 새로운 층 착지 완료 ➔ 상태 리셋 (노란색 원을 밟기 전까지는 대기)
            currentTrackingFloor = floorLevel;
            currentFloorIndex = 10 - floorLevel;
            hasPassedYellow = false;
            hasPassedGreen = false;
            isCurrentFloorAnomalyActive = false;
            anomalyManager?.DeactivateAllAnomalies();

            Debug.Log($"<color=#55FFFF><b>[8번 출구] 🔵 {floorLevel}층 도착 (파란색 원)!</b> 복도 4번째 액자 앞 노란색 원으로 가시면 이상현상 테스트가 시작됩니다.</color>");
        }

        // 2) 🟡 노란색 원: 복도 4번째 액자 앞 (이상현상 테스트 시작!)
        // - 파란색 원을 밟고 온 뒤 이 원을 밟으면 이 층의 이상현상이 나타나거나 안 나타나게 결정!
        public void OnPlayerEnteredMainHall() => OnPlayerEnteredMainHall(currentTrackingFloor);
        public void OnPlayerEnteredMainHall(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;
            if (hasPassedYellow) return; // 이미 이번 층 테스트가 시작되었다면 중복 실행 방지

            hasPassedYellow = true;
            currentTrackingFloor = floorLevel;
            currentFloorIndex = 10 - floorLevel;

            // ★ 9층은 분위기용 시작 층이므로 무조건 이상현상 0% (정상 갤러리 보장!)
            if (floorLevel >= 9)
            {
                isCurrentFloorAnomalyActive = false;
                anomalyManager?.DeactivateAllAnomalies();
                Debug.Log($"<color=#66FF66><b>[8번 출구] 🟡 {floorLevel}층 시작 분위기 층 (이상현상 없음)</b> ➔ 초록색 계단을 통해 8층으로 내려가세요!</color>");
                return;
            }

            // 8층 ~ 2층: 이상현상 주사위 롤링 (테스트 개시)
            anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);
            isCurrentFloorAnomalyActive = (anomalyManager != null && anomalyManager.HasActiveAnomaly());

            if (isCurrentFloorAnomalyActive)
            {
                string aName = (anomalyManager.CurrentActiveAnomaly != null) ? anomalyManager.CurrentActiveAnomaly.anomalyName : "알 수 없는 이상현상";
                Debug.Log($"<color=#FF5555><b>[8번 출구] 🟡 {floorLevel}층 이상현상 발생: [{aName}]!</b> (⚠️ 이상현상을 발견했으니 뒤돌아서 파란색 원으로 유턴해야 탈출 가능!)</color>");
            }
            else
            {
                Debug.Log($"<color=#66FF66><b>[8번 출구] 🟡 {floorLevel}층 정상 갤러리!</b> (✅ 이상현상이 없으니 초록색 계단을 통해 다음 층으로 내려가세요!)</color>");
            }
        }

        // 3) 🟢 초록색 원: 계단 하단 (하강 감지 센서)
        // - 이상현상이 없으면 아무 일도 안 일어남
        // - 이상현상이 있는데 밟으면 "이상현상 간파 실패!" 콘솔 출력
        public void OnDescendedStairs() => OnDescendedStairs(currentTrackingFloor);
        public void OnDescendedStairs(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            hasPassedGreen = true;

            if (isCurrentFloorAnomalyActive)
            {
                // 이상현상이 있는데 초록색 원을 밟음 -> 콘솔로 경고!
                Debug.LogWarning($"<color=#FF4444><b>[8번 출구] ❌ 이상현상 간파 실패! ({floorLevel}층에 이상현상이 있었는데 계단을 내려갔습니다. 아랫층 파란색 원을 밟으면 9->8층으로 루프 리셋됩니다!)</b></color>");
            }
            else
            {
                // 이상현상이 없으므로 아무 일도 일어나지 않음 (정상 통과)
                Debug.Log($"<color=#44FF44><b>[8번 출구] 🟢 정상 층 확인 중... (이상현상 없음. 계단을 마저 내려가세요.)</b></color>");
            }
        }

        // 4) 🔵 파란색 원 유턴 판정 (노란색 원을 밟고 난 뒤 초록색 원을 안 밟고 다시 파란색 원으로 되돌아옴)
        // - 이상현상이 있었을 때 유턴하면 정답 (다음 층으로 심리스 이동!)
        // - 이상현상이 없었는데 유턴하면 오답 (9->8층 파란색 원으로 루프 리셋!)
        public void OnTurnedBackOnAnomaly() => OnTurnedBackOnAnomaly(currentTrackingFloor);
        public void OnTurnedBackOnAnomaly(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;
            if (isCurrentFloorAnomalyActive)
            {
                // [정답 - 이상현상을 올바르게 간파하고 유턴함! -> 다음 하강 층으로 심리스 텔레포트 전진!]
                int nextFloor = floorLevel - 1;
                Debug.Log($"<color=#33FF33><b>[8번 출구] 🌟 이상현상 간파 성공!</b> ({floorLevel}층 이상현상을 확인하고 유턴했습니다. {nextFloor}층 파란색 원으로 심리스 전진합니다!)</color>");
                StartCoroutine(SeamlessAdvanceToNextFloorRoutine(nextFloor));
            }
            else
            {
                // [★ 꼼수 차단 / 정상 층 오답]
                // 텔레포트 성공 후 뒤돌아 꼼수를 부리려 하거나 정상 층인데 유턴한 경우 -> 직전 층({floorLevel + 1}층) 복도로 강제 롤백!
                int rollbackFloor = Mathf.Min(floorLevel + 1, 8); // 8층 초과는 방지
                Debug.LogWarning($"<color=#FF8800><b>[8번 출구 꼼수 차단] 🚫 꼼수 감지!</b> ({floorLevel}층은 정상 갤러리였는데 뒤돌아갔습니다. 직전 층인 {rollbackFloor}층 계단 복도로 강제 롤백 텔레포트됩니다!)</color>");
                StartCoroutine(RollbackToPreviousFloorHallwayRoutine(rollbackFloor));
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

        private IEnumerator EndingEscapeSequenceRoutine()
        {
            isTransitioning = true;
            currentPhase = GamePhase.Prologue_Day;

            SetDayEnvironment(true);

            if (player != null && prologueSpawnPoint_1F != null)
            {
                player.Teleport(prologueSpawnPoint_1F.position, prologueSpawnPoint_1F.rotation);
                if (prologueParentsTarget != null)
                {
                    player.LookAtTarget(prologueParentsTarget.position);
                }
                player.CanMove = false;
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
            uiManager?.ShowEnding(true);
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
            uiManager?.PlayGlitchFlash(0.2f, new Color(1f, 0f, 0f, 0.5f));

            if (roseLife <= 0)
            {
                if (IbMainMenuManager.Instance != null)
                {
                    IbMainMenuManager.Instance.ShowGameOver();
                }
                else
                {
                    // 사망 시 완전 처음이 아닌 10층 공명 후 밤 상태(8번 출구 모드)로 즉시 재시작!
                    RespawnAt10FNight();
                }
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
        /// 이상현상 발생 시 유턴하면 다음 목표 층으로 심리스 텔레포트 전진!
        /// </summary>
        private IEnumerator SeamlessAdvanceToNextFloorRoutine(int targetFloorLevel)
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

            yield return new WaitForSeconds(0.12f);
            isTransitioning = false;
        }

        /// <summary>
        /// 8번 출구 오답 시: 9층에서 8층으로 내려가는 계단 착지 지점(8층 파란색 원)으로 즉시 루프 리셋!
        /// </summary>
        private IEnumerator LoopResetTo8FStairRoutine()
        {
            isTransitioning = true;
            hasPassedYellow = false;
            hasPassedGreen = false;
            isCurrentFloorAnomalyActive = false;
            anomalyManager?.DeactivateAllAnomalies();

            // 8층 (floorIndex = 2, 즉 9층에서 내려온 8층 파란색 원)
            currentTrackingFloor = 8;
            currentFloorIndex = 2;

            if (floorCheckpoints != null && floorCheckpoints.Length > 2)
            {
                FloorCheckpoint cp = floorCheckpoints[2];
                if (player != null) player.Teleport(cp.spawnPosition, cp.spawnRotation);
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
