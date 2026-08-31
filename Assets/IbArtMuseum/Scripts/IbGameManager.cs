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

        [Header("Strict Floor Checkpoints (10F -> 1F)")]
        public FloorCheckpoint[] floorCheckpoints = new FloorCheckpoint[10];
        public Transform[] floorSpawnPoints = new Transform[10];

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
            // 1. 태양광 / 하늘 색상 제어 (밤 뷰를 살짝 더 밝고 화사하게: 13500 lux, Exposure 10.1)
            if (sunDirectionalLight != null)
            {
                sunDirectionalLight.transform.rotation = Quaternion.Euler(isDay ? 50f : -3.5f, 30f, 0f);
                sunDirectionalLight.color = isDay ? new Color(1f, 0.96f, 0.92f) : new Color(0.65f, 0.78f, 1.0f);
                sunDirectionalLight.intensity = isDay ? 3.0f : 1.2f;
                var hdSun = sunDirectionalLight.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                if (hdSun != null)
                {
                    hdSun.intensity = isDay ? 80000f : 13500f; // ★ 밤 뷰 밝기 상향!
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

            // 4. 실내 조명(핀조명, 업라이트)은 intensity 3000 고정 및 자연스러운 갤러리 백색 유지!
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l == sunDirectionalLight || (sunShaft != null && l.gameObject == sunShaft)) continue;
                if (l.type == LightType.Spot || l.type == LightType.Point)
                {
                    if (l.gameObject.name != "CoreGlowLight")
                    {
                        l.color = new Color(1f, 0.97f, 0.92f);
                        var hdL = l.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                        if (hdL != null)
                        {
                            hdL.intensity = 3000f;
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

            // 6. 관람객 NPC (낮에는 1~9층 등장, 밤에는 모두 소멸!)
            IbVisitorNPC[] visitors = Object.FindObjectsByType<IbVisitorNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in visitors)
            {
                if (v != null)
                {
                    v.gameObject.SetActive(isDay);
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

            RenderSettings.ambientLight = isDay ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.08f, 0.12f, 0.22f);
        }

        // ==================== 3. 정밀 8번 출구 체크포인트 루프 로직 ====================

        // 1) 계단을 다 내려와 해당 층 착지 체크포인트에 도달했을 때
        public void OnFloorArrival(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            currentFloorIndex = 10 - floorLevel;
            hasExploredCurrentFloor = false; // 전시장 탐색 상태 리셋

            // 1층 출구 도달 시 탈출 성공 엔딩 시퀀스 발동!
            if (currentFloorIndex >= 9)
            {
                StartCoroutine(EndingEscapeSequenceRoutine());
                return;
            }

            // 이번 층의 이상현상 결정 및 적용
            anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);
        }

        // 2) ★ 플레이어가 계단을 내려와 복도 모퉁이를 돌 때 (층 입장 & 이상현상 콘솔 출력!)
        public void OnPlayerEnteredMainHall() => OnPlayerEnteredMainHall(10 - currentFloorIndex);
        public void OnPlayerEnteredMainHall(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            int expectedFloor = 10 - currentFloorIndex;
            // 만약 계단 착지 트리거를 건너뛰었더라도 현재 층을 정확히 갱신!
            if (floorLevel != expectedFloor)
            {
                currentFloorIndex = 10 - floorLevel;
                anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);
            }

            if (!hasExploredCurrentFloor)
            {
                hasExploredCurrentFloor = true;

                bool hadAnomaly = (anomalyManager != null && anomalyManager.HasActiveAnomaly());
                string aName = (hadAnomaly && anomalyManager.CurrentActiveAnomaly != null) ? anomalyManager.CurrentActiveAnomaly.anomalyName : "이상현상";

                if (hadAnomaly)
                {
                    Debug.Log($"<color=#FF5555><b>[8번 출구] 🏛️ {floorLevel}층 입장!</b> (⚠️ <b>이상현상 발생: [{aName}]</b> ➔ 발견 후 되돌아가야 탈출 가능!)</color>");
                }
                else
                {
                    Debug.Log($"<color=#66FF66><b>[8번 출구] 🏛️ {floorLevel}층 입장!</b> (✅ <b>문제없음 (정상 갤러리)</b> ➔ 다음 계단으로 내려가세요!)</color>");
                }
            }
        }

        // 3) 다음 층으로 내려가는 계단 쪽 체크포인트를 밟았을 때
        public void OnDescendedStairs() => OnDescendedStairs(10 - currentFloorIndex);
        public void OnDescendedStairs(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            int expectedFloor = 10 - currentFloorIndex;
            if (floorLevel != expectedFloor) return;

            bool hadAnomaly = (anomalyManager != null && anomalyManager.HasActiveAnomaly());

            if (!hadAnomaly)
            {
                // [정답 - 문제없음 진행!]
                Debug.Log($"<color=#33FF33><b>[8번 출구] 문제없음 진행!</b> ({floorLevel}층은 정상 갤러리입니다. 계단을 걸어 {floorLevel - 1}층으로 내려갑니다.)</color>");
            }
            else
            {
                // [오답 - 이상현상 있었는데 그냥 내려감]
                Debug.LogWarning($"<color=#FF3333><b>[8번 출구] 이상현상 무시 오답!</b> ({floorLevel}층에 이상현상이 있었는데 내려갔습니다. 장미 1개 차감 후 {floorLevel}층 시작 체크포인트로 루프합니다.)</color>");
                OnSeamlessWrongChoiceMade();
            }
        }

        // 4) 이상현상을 확인하고 왔던 스폰 복도로 되돌아갈 때
        public void OnTurnedBackOnAnomaly() => OnTurnedBackOnAnomaly(10 - currentFloorIndex);
        public void OnTurnedBackOnAnomaly(int floorLevel)
        {
            if (isTransitioning) return;
            if (currentPhase != GamePhase.Night_Loop) return;

            int expectedFloor = 10 - currentFloorIndex;
            if (floorLevel != expectedFloor) return;

            if (!hasExploredCurrentFloor) return; // 전시장 입장 전에 복도에서 서성일 때는 작동 안 함!

            bool hadAnomaly = (anomalyManager != null && anomalyManager.HasActiveAnomaly());

            if (hadAnomaly)
            {
                // [정답 - 이상현상! 되돌아갑니다]
                Debug.Log($"<color=#33FF33><b>[8번 출구] 이상현상! 되돌아갑니다.</b> ({floorLevel}층 이상현상 파훼 성공! {floorLevel - 1}층으로 진행합니다.)</color>");
                StartCoroutine(SeamlessAdvanceToNextFloorRoutine());
            }
            else
            {
                // [오답 - 정상 층인데 되돌아감]
                Debug.LogWarning($"<color=#FF3333><b>[8번 출구] 정상 층 오답!</b> ({floorLevel}층에는 이상현상이 없었는데 되돌아갔습니다. 장미 1개 차감 후 {floorLevel}층 시작 체크포인트로 루프합니다.)</color>");
                OnSeamlessWrongChoiceMade();
            }
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
                    OnSeamlessWrongChoiceMade();
                }
            }
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

        private void OnSeamlessWrongChoiceMade()
        {
            roseLife--;
            uiManager?.SetRoseLife(roseLife);

            if (roseLife > 0)
            {
                StartCoroutine(SeamlessRetryCurrentFloorRoutine());
            }
            else
            {
                if (IbMainMenuManager.Instance != null)
                {
                    IbMainMenuManager.Instance.ShowGameOver();
                }
                else
                {
                    uiManager?.PlayGlitchFlash(0.35f, Color.black);
                    uiManager?.ShowDialogueBox("Game Over", "<size=22><color=#E63946>All roses have withered. Returning to the beginning...</color></size>", () => FullRestartToPrologue());
                }
            }
        }

        private IEnumerator SeamlessAdvanceToNextFloorRoutine()
        {
            isTransitioning = true;
            hasExploredCurrentFloor = false;

            currentFloorIndex++;

            if (currentFloorIndex < floorCheckpoints.Length)
            {
                FloorCheckpoint cp = floorCheckpoints[currentFloorIndex];
                player.Teleport(cp.spawnPosition, cp.spawnRotation);
            }

            anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);

            yield return new WaitForSeconds(0.15f);
            isTransitioning = false;
        }

        private IEnumerator SeamlessRetryCurrentFloorRoutine()
        {
            isTransitioning = true;
            hasExploredCurrentFloor = false;

            if (currentFloorIndex < floorCheckpoints.Length)
            {
                FloorCheckpoint cp = floorCheckpoints[currentFloorIndex];
                player.Teleport(cp.spawnPosition, cp.spawnRotation);
            }

            anomalyManager?.DecideAndApplyAnomaly(currentFloorIndex);

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
