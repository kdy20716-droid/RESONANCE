using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

namespace IbArtMuseum
{
    public static class IbMuseumGameLogicUtility
    {
        [MenuItem("Tools/Ib Museum/🧹 Clean Duplicate Triggers (중복 트리거 완전 정리)", false, 1)]
        public static void CleanDuplicateTriggersMenu()
        {
            GameObject[] allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var go in allSceneObjects)
            {
                if (go != null && (go.name == "Museum_Strict_Triggers" || go.name.StartsWith("Museum_Strict_Triggers") || go.name.Contains("Volumetric_Light_Shaft")))
                {
                    Object.DestroyImmediate(go);
                    count++;
                }
            }

            IbCircularTrigger[] oldTrigs = Object.FindObjectsByType<IbCircularTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in oldTrigs)
            {
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // 에디터에서 재생 중인 모든 오디오 강제 중지
            AudioSource[] allAudio = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var a in allAudio)
            {
                if (a != null && a.isPlaying) a.Stop();
            }

            Debug.Log($"<color=#33FF33><b>[Clean Triggers] {count}개의 중복 Museum_Strict_Triggers 및 레거시 트리거를 깔끔하게 모두 삭제했습니다!</b></color>");
        }

        [MenuItem("Tools/Ib Museum/🎮 Setup Gameplay & GameLogic (게임 시스템 & 맞춤 조명 & 관람객 세팅)", false, 2)]
        public static void SetupGameLogicOnly()
        {
            CleanDuplicateTriggersMenu();

            IbGameManager gm = Object.FindFirstObjectByType<IbGameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에 IbGameManager가 없습니다.", "확인");
                return;
            }

            // 1. 플레이어 컨트롤러 및 UI 바인딩 검증
            IbPlayerController player = Object.FindFirstObjectByType<IbPlayerController>();
            IbMuseumUI ui = Object.FindFirstObjectByType<IbMuseumUI>();
            IbAnomalyManager am = Object.FindFirstObjectByType<IbAnomalyManager>();
            IbAudioAmbience audio = Object.FindFirstObjectByType<IbAudioAmbience>();

            gm.player = player;
            gm.uiManager = ui;
            gm.anomalyManager = am;
            gm.audioAmbience = audio;

            // ★ Assets/musics/emotion.mp3 & RESONANCE.mp3 낮/밤 BGM 캐릭터에 자동 장착!
            AudioClip emotionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/musics/emotion.mp3");
            AudioClip resonanceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/musics/RESONANCE.mp3");

            if (player != null)
            {
                player.dayBgmClip = emotionClip;
                player.nightBgmClip = resonanceClip;
                player.bgmClip = emotionClip;
                player.bgmVolume = 0.05f; // 은은하게 통일된 0.05 볼륨

                AudioSource aSource = player.GetComponent<AudioSource>();
                if (aSource == null) aSource = player.gameObject.AddComponent<AudioSource>();
                aSource.clip = emotionClip;
                aSource.loop = true;
                aSource.volume = 0.05f;
                aSource.spatialBlend = 0f;
                aSource.playOnAwake = false; // 씬 뷰에서 소리 안 나도록 false!
                player.bgmAudioSource = aSource;

                EditorUtility.SetDirty(player);
                Debug.Log("<color=#33FF33><b>[Audio Setup] 낮 BGM(emotion.mp3) & 밤 BGM(RESONANCE.mp3)이 플레이어에게 성공적으로 장착되었습니다! (볼륨 0.05)</b></color>");
            }

            // ★ UI 캔버스에 수수께끼 대화 & 정답 입력 UI (IbRiddleInputUI) 사전 구축 및 바인딩
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                IbRiddleInputUI riddleUI = canvas.GetComponentInChildren<IbRiddleInputUI>(true);
                if (riddleUI == null)
                {
                    GameObject rGo = new GameObject("IbRiddleInputUI");
                    rGo.transform.SetParent(canvas.transform, false);
                    riddleUI = rGo.AddComponent<IbRiddleInputUI>();
                }
                EditorUtility.SetDirty(riddleUI);
            }

            // 2. 10층 거대 동상 바인딩 검증
            GameObject monument = GameObject.Find("Grand_RESONANCE_Monument");
            if (monument != null)
            {
                gm.monumentTriggerTarget = monument.transform;
                Transform rings = monument.transform.Find("Resonance_Harmonic_Rings");
                if (rings != null)
                {
                    gm.monumentRingsRoot = rings;
                    Transform core = rings.Find("Creation_Soul_Core");
                    if (core != null) gm.monumentSoulCore = core.gameObject;
                    Transform glow = rings.Find("CoreGlowLight");
                    if (glow != null) gm.monumentGlowLight = glow.gameObject;
                }
            }

            // 3. 태양광 바인딩
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    gm.sunDirectionalLight = l;
                    break;
                }
            }

            // 4. ★ 액자 그림 위에서 아래로 정확하게 내리쬐는 픽처 조명 기구 및 스팟라이트 설치!
            SetupPrecisePictureLightsAboveCanvases();

            // 4-1. ★ 1~9층 천장 3x3 (9개) Area 직사각형 조명 설치 (silver 테두리 + light 형광등)
            Setup3x3CeilingLights();

            // 5. ★ 10층->9층 복도 액자 낮에는 일반 그림, 밤에 상호작용 글씨 표시로 설정!
            SetupHallwayPaintingsDayNight();

            // 6. ★ 10층부터 1층까지 엄격한 Y축 고유 트리거 및 체크포인트 시스템 재배치
            SetupStrictFloorTriggers();

            // 7. 각 층마다 4~5명의 대화 가능한 관람객 NPC 스폰
            SetupDaytimeVisitorNPCs();

            // 8. 3/5/7/9층 생명의 화병 & 3~9층 계단 앞 수수께끼 관리인 NPC 및 복도 차단 콜라이더 설치!
            SetupRiddleGuardsAndSaveVases();

            // 9. ★ HDRP 물리 Volumetric Fog & SpotLight 빛줄기(Volumetric Dimmer) 자동 원클릭 세팅!
            SetupVolumetricFogAndLightShafts();

            // 10. ★ 각 층 계단 복도 모퉁이 왼쪽 벽면에 큼직한 검은색 층수 숫자 텍스처 부착!
            SetupFloorNumberTypographyOnWalls();

            // 11. ★ 1층 액자들에만 제미나이 나노바나나 고화질 그림 적용!
            ApplyNanoBananaPaintingsTo1F();

            // 천장 조명 낮/밤 머티리얼 바인딩 (낮: light.mat, 밤: black.mat)
            gm.dayCeilingLightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/texture/light.mat");
            gm.nightCeilingBlackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/texture/black.mat");

            gm.InitializeStrictFloorCheckpoints();
            gm.SetDayEnvironment(true); // 낮으로 시작

            EditorUtility.SetDirty(gm);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#33FF33><b>[Ib GameLogic Setup] 3~9F 수수께끼 관리인 NPC, 복도 차단 콜라이더, 화병 세이브 세팅 완료!</b></color>");
        }

        private static void SetupRiddleGuardsAndSaveVases()
        {
            Transform existingGroup = GameObject.Find("Riddle_And_Save_Systems")?.transform;
            if (existingGroup != null) Object.DestroyImmediate(existingGroup.gameObject);

            GameObject rootGroup = new GameObject("Riddle_And_Save_Systems");

            for (int f = 3; f <= 9; f++)
            {
                float floorY = (f - 1) * 7.0f;
                bool isEvenFloor = (f % 2 == 0);

                // 1. 3, 5, 7, 9층 생명의 화병(Save Vase) 배치
                if (f == 3 || f == 5 || f == 7 || f == 9)
                {
                    GameObject vaseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    vaseObj.name = $"SaveVase_{f}F";
                    vaseObj.transform.SetParent(rootGroup.transform);
                    vaseObj.transform.position = new Vector3(isEvenFloor ? 4.5f : -4.5f, floorY + 0.6f, 0f);
                    vaseObj.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);

                    var col = vaseObj.GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;

                    var saveComp = vaseObj.AddComponent<IbSaveVase>();
                    saveComp.floorLevel = f;

                    Material glassMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                    glassMat.color = new Color(0.3f, 0.8f, 1.0f, 0.7f);
                    vaseObj.GetComponent<MeshRenderer>().material = glassMat;
                }

                // 2. 3F ~ 9F 전시장 저 건너편 위층 계단실 진입 통로(길목)를 완전히 가로막는 수수께끼 관리인 NPC & 길목 차단 콜라이더 배치!
                // 홀수층(3,5,7,9F) 다음 계단문: 북서쪽 (X = -16.5f, Z = +18.2f), 남쪽 바라봄 180도
                // 짝수층(4,6,8F) 다음 계단문: 남동쪽 (X = +16.5f, Z = -18.2f), 북쪽 바라봄 0도
                Vector3 guardPos = new Vector3(isEvenFloor ? 16.5f : -16.5f, floorY, isEvenFloor ? -18.2f : 18.2f);
                Quaternion guardRot = Quaternion.Euler(0, isEvenFloor ? 0f : 180f, 0);

                GameObject guardObj = new GameObject($"RiddleCuratorGuard_{f}F_to_{f+1}F");
                guardObj.transform.SetParent(rootGroup.transform);
                guardObj.transform.position = guardPos;
                guardObj.transform.rotation = guardRot;

                // 1) NPC 3D 외형 (정장 코트 바디 + 헤드)
                Material suitMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                suitMat.color = new Color(0.12f, 0.14f, 0.22f); // 짙은 네이비 정장

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Body";
                body.transform.SetParent(guardObj.transform, false);
                body.transform.localPosition = new Vector3(0, 0.9f, 0);
                body.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
                body.GetComponent<MeshRenderer>().material = suitMat;

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(guardObj.transform, false);
                head.transform.localPosition = new Vector3(0, 1.9f, 0);
                head.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                head.GetComponent<MeshRenderer>().material = suitMat;

                // 2) 구석 통로 길목(2.5m)을 완전히 틀어막아 비비기를 100% 방지하는 솔리드 박스 콜라이더
                GameObject blockWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockWall.name = "Blocking_Corridor_BoxCollider";
                blockWall.transform.SetParent(guardObj.transform, false);
                blockWall.transform.localPosition = new Vector3(0, 1.5f, 0);
                blockWall.transform.localScale = new Vector3(2.6f, 3.0f, 0.8f);
                blockWall.GetComponent<MeshRenderer>().enabled = false; // 투명화

                BoxCollider blockCol = blockWall.GetComponent<BoxCollider>();
                blockCol.isTrigger = false; // 플레이어 CharacterController가 뚫고 지나갈 수 없음!

                // 3) 상호작용 트리거 콜라이더 (가드 앞쪽 접근 시 [E] 감지)
                BoxCollider interactTrigger = guardObj.AddComponent<BoxCollider>();
                interactTrigger.isTrigger = true;
                interactTrigger.center = new Vector3(0, 1.0f, 0.8f);
                interactTrigger.size = new Vector3(2.8f, 2.2f, 2.5f);

                // 4) 수수께끼 관리인 컴포넌트 연결
                var guardComp = guardObj.AddComponent<IbRiddleGuardNPC>();
                guardComp.floorLevel = f;
                guardComp.blockingCollider = blockCol;
                guardComp.SetupDefaultRiddleForFloor();
            }
        }

        private static void SetupHallwayPaintingsDayNight()
        {
            // 씬에 이미 정상 렌더링되고 있는 다른 액자들의 실제 머티리얼을 직접 수집!
            Material fallbackSceneMat = null;
            List<Material> validSceneMats = new List<Material>();

            IbInteractableArtwork[] allArtworks = Object.FindObjectsByType<IbInteractableArtwork>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var a in allArtworks)
            {
                if (a == null) continue;
                if (a.name.StartsWith("Hallway_")) continue; // 복도 액자 제외

                Transform cT = a.transform.Find("Canvas");
                if (cT != null)
                {
                    MeshRenderer mr = cT.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.shader != null && !mr.sharedMaterial.shader.name.Contains("InternalErrorShader"))
                    {
                        validSceneMats.Add(mr.sharedMaterial);
                        if (fallbackSceneMat == null) fallbackSceneMat = mr.sharedMaterial;
                    }
                }
            }

            // 만약 못 찾았다면 씬의 기본 머티리얼 활용
            if (fallbackSceneMat == null)
            {
                Renderer anyRend = Object.FindFirstObjectByType<MeshRenderer>();
                if (anyRend != null && anyRend.sharedMaterial != null) fallbackSceneMat = anyRend.sharedMaterial;
            }

            Texture2D guideTex1 = IbTextureGenerator.GenerateGuidePaintingTexture("WELCOME TO MY MUSEUM,", "LINA");
            Texture2D guideTex2 = IbTextureGenerator.GenerateGuidePaintingTexture("LET'S PLAY", "TOGETHER");
            Texture2D guideTex3 = IbTextureGenerator.GenerateGuidePaintingTexture("IF YOU SEE ANYTHING DIFFERENT", "FROM MORNING, TURN BACK.", "THEN YOU'LL BE SAFE.");
            Texture2D guideTex4 = IbTextureGenerator.GenerateGuidePaintingTexture("IF YOU IGNORE ANOMALIES", "AND PROCEED...", "YOU MIGHT REGRET IT.");

            Material text1 = (fallbackSceneMat != null) ? new Material(fallbackSceneMat) { mainTexture = guideTex1 } : CreateImageMaterial(guideTex1, "Mat_Hallway_Text1");
            Material text2 = (fallbackSceneMat != null) ? new Material(fallbackSceneMat) { mainTexture = guideTex2 } : CreateImageMaterial(guideTex2, "Mat_Hallway_Text2");
            Material text3 = (fallbackSceneMat != null) ? new Material(fallbackSceneMat) { mainTexture = guideTex3 } : CreateImageMaterial(guideTex3, "Mat_Hallway_Text3");
            Material text4 = (fallbackSceneMat != null) ? new Material(fallbackSceneMat) { mainTexture = guideTex4 } : CreateImageMaterial(guideTex4, "Mat_Hallway_Text4");

            Material[] nightMats = new Material[] { text1, text2, text3, text4 };

            string[] dayTitles = new string[] { "Still Life in Gold", "Whispering Garden", "Silent Portrait", "Twilight Harbor" };
            string[] dayDescs = new string[]
            {
                "A peaceful classical oil painting by Carl Weismann depicting a golden still life.",
                "A serene painting of a blossoming garden bathed in morning light.",
                "A dignified portrait of an unknown aristocrat with deep, thoughtful eyes.",
                "A calming sea harbor under the warm morning sun."
            };

            string[] nightTitles = new string[] { "Welcome", "Invitation", "Rule of Survival", "Warning" };
            string[] nightDescs = new string[]
            {
                "<b><color=#E63946>\"Welcome to my museum, Lina.\"</color></b>",
                "<b><color=#E63946>\"Let's play together.\"</color></b>",
                "<b><color=#E63946>\"If you see anything different from morning, turn back. Then you'll be safe.\"</color></b>",
                "<b><color=#E63946>\"If you ignore anomalies and proceed... you might regret it.\"</color></b>"
            };

            for (int i = 1; i <= 4; i++)
            {
                GameObject pGo = GameObject.Find($"Hallway_{i}_9F") ?? GameObject.Find($"HallwayPainting_10F_to_9F_{i}");
                if (pGo != null)
                {
                    IbInteractableArtwork art = pGo.GetComponent<IbInteractableArtwork>();
                    if (art == null) art = pGo.AddComponent<IbInteractableArtwork>();

                    // 씬에 있는 정상 그림 머티리얼을 그대로 매핑!
                    Material chosenDayMat = (validSceneMats.Count > 0) ? validSceneMats[(i - 1) % validSceneMats.Count] : fallbackSceneMat;

                    art.dayMaterial = chosenDayMat;
                    art.nightMaterial = nightMats[(i - 1) % nightMats.Length];

                    art.artworkTitle = dayTitles[i - 1];
                    art.description = dayDescs[i - 1];
                    art.author = "Carl Weismann";
                    art.interactPrompt = "[ E ] Inspect";

                    art.nightTitle = nightTitles[i - 1];
                    art.nightDescription = nightDescs[i - 1];

                    // Canvas 렌더러에 씬의 정상 그림 머티리얼을 즉시 직접 대입!
                    Transform canvasT = pGo.transform.Find("Canvas");
                    if (canvasT != null)
                    {
                        MeshRenderer rend = canvasT.GetComponent<MeshRenderer>();
                        if (rend != null && chosenDayMat != null)
                        {
                            rend.sharedMaterial = chosenDayMat;
                            EditorUtility.SetDirty(rend);
                        }
                    }

                    art.UpdateDayNightVisual(true);
                    EditorUtility.SetDirty(art);
                }
            }
        }

        private static void SetupStrictFloorTriggers()
        {
            // 씬에 남아있는 모든 Museum_Strict_Triggers 그룹과 기존 트리거 완벽 정리!
            GameObject[] allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allSceneObjects)
            {
                if (go != null && (go.name == "Museum_Strict_Triggers" || go.name.StartsWith("Museum_Strict_Triggers")))
                {
                    Object.DestroyImmediate(go);
                }
            }

            IbCircularTrigger[] oldTrigs = Object.FindObjectsByType<IbCircularTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in oldTrigs)
            {
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            GameObject trigRoot = new GameObject("Museum_Strict_Triggers");
            float floorHeight = 7.0f;

            for (int f = 1; f <= 10; f++)
            {
                float floorY = (f - 1) * floorHeight;
                bool isEvenFloor = (f % 2 == 0);

                // 1) 층 착지 체크포인트 (계단을 다 내려왔을 때 층 도달 & 이상현상 생성)
                Vector3 arrivalPos;
                if (f == 10) arrivalPos = new Vector3(0f, floorY + 1.2f, -8.0f);
                else if (isEvenFloor) arrivalPos = new Vector3(1.0f, floorY + 1.2f, -21.75f);
                else arrivalPos = new Vector3(1.0f, floorY + 1.2f, 21.75f);

                GameObject arrivalTrig = new GameObject($"FloorArrivalTrigger_{f}F");
                arrivalTrig.transform.SetParent(trigRoot.transform);
                arrivalTrig.transform.position = arrivalPos;
                BoxCollider arrBox = arrivalTrig.AddComponent<BoxCollider>();
                arrBox.isTrigger = true;
                arrBox.size = new Vector3(8.0f, 3.5f, 6.0f);
                IbCircularTrigger arrCT = arrivalTrig.AddComponent<IbCircularTrigger>();
                arrCT.triggerType = FloorTriggerType.FloorArrival;
                arrCT.floorLevel = f;
                arrCT.maxYDifference = 2.5f;

                // [트리거 자식으로 빛 링 장착: 클릭 & 이동 시 함께 이동!]
                CreateFloorGlowRingIndicator(arrivalTrig.transform, "GlowRing_Indicator",
                    new Vector3(0, -1.18f, 0), 3.5f, new Color(0.2f, 0.75f, 1.0f));

                // 2) ★ 계단 복도 4번째 액자 위치 트리거 (EnterMainHallTrigger - 노란색 원)
                // 복도를 걸어가 4번째 액자 앞을 지나갈 때 감지되어 콘솔창에 층수 및 이상현상 안내 출력!
                if (f != 10)
                {
                    // 4번째 액자(Hallway_4_...F) 정면 바닥 좌표 (X = ±12.5m)
                    Vector3 fourthPaintingPos = isEvenFloor ? new Vector3(12.5f, floorY + 1.2f, -21.75f) : new Vector3(-12.5f, floorY + 1.2f, 21.75f);

                    GameObject enterHallTrig = new GameObject($"EnterMainHallTrigger_{f}F");
                    enterHallTrig.transform.SetParent(trigRoot.transform);
                    enterHallTrig.transform.position = fourthPaintingPos;
                    
                    BoxCollider box = enterHallTrig.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(4.5f, 3.5f, 4.5f);

                    IbCircularTrigger ct = enterHallTrig.AddComponent<IbCircularTrigger>();
                    ct.triggerType = FloorTriggerType.EnterMainHall;
                    ct.floorLevel = f;
                    ct.maxYDifference = 2.5f;

                    // [트리거 자식으로 황금빛 노란색 링 장착: 4번째 액자 앞]
                    CreateFloorGlowRingIndicator(enterHallTrig.transform, "GlowRing_Indicator",
                        new Vector3(0, -1.18f, 0), 3.2f, new Color(1.0f, 0.85f, 0.2f));
                }

                // 3) ★ 계단 하단부 체크포인트 트리거 (StairDownTrigger - 초록색 원, f > 1)
                // 계단을 조금 더 내려왔을 때(하단 1/3 지점) 정답/오답 판정 발동!
                if (f > 1)
                {
                    // 계단 중앙보다 조금 더 아래쪽 발판 좌표 (X = ±7.0m, Y = floorY - 4.5m)
                    Vector3 stairLowerPos = isEvenFloor ? new Vector3(7.0f, floorY - 4.5f, 21.75f) : new Vector3(-7.0f, floorY - 4.5f, -21.75f);

                    GameObject downTrig = new GameObject($"StairDownTrigger_{f}F_to_{f - 1}F");
                    downTrig.transform.SetParent(trigRoot.transform);
                    downTrig.transform.position = stairLowerPos;

                    BoxCollider box = downTrig.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(7.0f, 4.0f, 4.5f);

                    IbCircularTrigger ct = downTrig.AddComponent<IbCircularTrigger>();
                    ct.triggerType = FloorTriggerType.StairsDown;
                    ct.floorLevel = f;
                    ct.maxYDifference = 2.5f;

                    // [트리거 자식으로 에메랄드 초록색 링 장착: 계단 하단 발판]
                    CreateFloorGlowRingIndicator(downTrig.transform, "GlowRing_Indicator",
                        new Vector3(0, -1.18f, 0), 3.0f, new Color(0.2f, 1.0f, 0.4f));
                }
            }
        }

        private static void CreateFloorGlowRingIndicator(Transform parent, string name, Vector3 localPos, float diameter, Color ringColor)
        {
            GameObject ringGo = new GameObject(name);
            ringGo.transform.SetParent(parent);
            ringGo.transform.localPosition = localPos;

            Material ringMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            ringMat.name = $"Mat_{name}";
            ringMat.color = ringColor;
            if (ringMat.HasProperty("_EmissiveColor"))
            {
                ringMat.SetColor("_EmissiveColor", ringColor * 6.0f);
                ringMat.EnableKeyword("_EMISSION");
            }
            if (ringMat.HasProperty("_Smoothness")) ringMat.SetFloat("_Smoothness", 0.9f);

            // 1. 외곽 링
            GameObject outerCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerCyl.name = "OuterRing";
            outerCyl.transform.SetParent(ringGo.transform);
            outerCyl.transform.localPosition = Vector3.zero;
            outerCyl.transform.localScale = new Vector3(diameter, 0.012f, diameter);
            outerCyl.GetComponent<MeshRenderer>().material = ringMat;
            Object.DestroyImmediate(outerCyl.GetComponent<Collider>()); // 콜라이더 완전 제거 (충돌 없음!)

            // 2. 중심 빛 코어 닷
            GameObject centerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centerDot.name = "CenterGlowDot";
            centerDot.transform.SetParent(ringGo.transform);
            centerDot.transform.localPosition = new Vector3(0, 0.02f, 0);
            centerDot.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
            centerDot.GetComponent<MeshRenderer>().material = ringMat;
            Object.DestroyImmediate(centerDot.GetComponent<Collider>());
        }

        private static void SetupPrecisePictureLightsAboveCanvases()
        {
            // 1. 기존 Custom 조명들 전부 완전 삭제!
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.name == "FloorLights" || go.name.StartsWith("SpotLight_") || go.name == "Museum_Custom_Lighting" ||
                    go.name.StartsWith("PictureLight_") || go.name.StartsWith("Fixture_") || go.name.Contains("RESONANCE_Inside_Sign_Light") ||
                    go.name == "Museum_Ceiling_Downlights" || go.name == "Museum_White_Ceilings")
                {
                    Object.DestroyImmediate(go);
                }
            }

            GameObject lightingRoot = new GameObject("Museum_Custom_Lighting");

            // 조명 기구용 럭셔리 황동/골드 머티리얼
            Material brassMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            brassMat.name = "Mat_Brass_LuxuryGold";
            brassMat.color = new Color(0.92f, 0.78f, 0.40f);
            if (brassMat.HasProperty("_Metallic")) brassMat.SetFloat("_Metallic", 0.92f);
            if (brassMat.HasProperty("_Smoothness")) brassMat.SetFloat("_Smoothness", 0.85f);

            // 어두운 메탈릭 브론즈 머티리얼 (베젤 및 마운트 디테일용)
            Material darkBronzeMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            darkBronzeMat.name = "Mat_DarkBronze_Accent";
            darkBronzeMat.color = new Color(0.22f, 0.18f, 0.14f);
            if (darkBronzeMat.HasProperty("_Metallic")) darkBronzeMat.SetFloat("_Metallic", 0.85f);
            if (darkBronzeMat.HasProperty("_Smoothness")) darkBronzeMat.SetFloat("_Smoothness", 0.75f);

            // 실제 빛이 뿜어져 나오는 발광 전구/렌즈 머티리얼 (HDRP Emission)
            Material bulbEmissiveMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            bulbEmissiveMat.name = "Mat_LightBulb_Emissive";
            bulbEmissiveMat.color = new Color(1f, 0.98f, 0.92f);
            if (bulbEmissiveMat.HasProperty("_EmissiveColor"))
            {
                bulbEmissiveMat.SetColor("_EmissiveColor", new Color(1f, 0.96f, 0.88f) * 15.0f);
                bulbEmissiveMat.EnableKeyword("_EMISSION");
            }
            if (bulbEmissiveMat.HasProperty("_EmissionColor"))
            {
                bulbEmissiveMat.SetColor("_EmissionColor", new Color(1f, 0.96f, 0.88f) * 15.0f);
                bulbEmissiveMat.EnableKeyword("_EMISSION");
            }

            // 2. 모든 액자의 캔버스 그림 정면 상단 고풍스러운 백조목 곡선 픽처 라이트 (Swan-Neck Antique Brass Fixture)
            IbInteractableArtwork[] artworks = Object.FindObjectsByType<IbInteractableArtwork>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var art in artworks)
            {
                if (art == null) continue;
                string artName = art.gameObject.name;

                if (artName.Contains("Statue") || artName.Contains("Rose") || artName.Contains("Desk") || artName.Contains("Monument") || artName.Contains("Visitor"))
                    continue;

                Transform canvasT = art.transform.Find("Canvas");
                Vector3 canvasFacingDir = (canvasT != null) ? -canvasT.forward : -art.transform.forward;
                Vector3 canvasPos = (canvasT != null) ? canvasT.position : art.transform.position;

                Vector3 mountPos = canvasPos + (-canvasFacingDir * 0.06f) + Vector3.up * 2.15f; // 액자 상단 위로 시원하게 높게 장착!
                Vector3 lampHeadPos = mountPos + (canvasFacingDir * 0.50f) + Vector3.down * 0.18f;
                Vector3 targetAimPos = canvasPos + Vector3.up * 0.10f; // 그림 캔버스 중심 타겟
                Vector3 aimDir = (targetAimPos - lampHeadPos).normalized;
                Quaternion shadeRot = Quaternion.FromToRotation(Vector3.down, aimDir); // 그림을 향해 자연스럽게 내려다보는 틸트

                GameObject fixtureGo = new GameObject($"Fixture_{artName}");
                fixtureGo.transform.SetParent(lightingRoot.transform);

                // 1) 벽면 고풍스러운 황동 원형 마운트 로제트 (Mount Rosette Base)
                GameObject rosette = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rosette.name = "Mount_Rosette";
                rosette.transform.SetParent(fixtureGo.transform);
                rosette.transform.position = mountPos;
                rosette.transform.rotation = Quaternion.LookRotation(canvasFacingDir, Vector3.up) * Quaternion.Euler(90f, 0, 0);
                rosette.transform.localScale = new Vector3(0.09f, 0.02f, 0.09f);
                rosette.GetComponent<MeshRenderer>().material = brassMat;
                Object.DestroyImmediate(rosette.GetComponent<Collider>());

                // 2) 부드럽고 우아한 백조목(Swan-Neck) 높은 아치형 곡선 파이프
                Vector3 p0 = mountPos;
                Vector3 p1 = mountPos + Vector3.up * 0.15f + canvasFacingDir * 0.10f;
                Vector3 p2 = mountPos + Vector3.up * 0.20f + canvasFacingDir * 0.32f;
                Vector3 p3 = lampHeadPos + Vector3.up * 0.10f;
                Vector3 p4 = lampHeadPos;

                Vector3[] curvePoints = new Vector3[] { p0, p1, p2, p3, p4 };
                for (int i = 0; i < curvePoints.Length - 1; i++)
                {
                    Vector3 segStart = curvePoints[i];
                    Vector3 segEnd = curvePoints[i + 1];

                    GameObject pipeSeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pipeSeg.name = $"Gooseneck_Seg_{i + 1}";
                    pipeSeg.transform.SetParent(fixtureGo.transform);
                    pipeSeg.transform.position = (segStart + segEnd) / 2f;
                    pipeSeg.transform.rotation = Quaternion.FromToRotation(Vector3.up, (segEnd - segStart).normalized);
                    pipeSeg.transform.localScale = new Vector3(0.018f, (segEnd - segStart).magnitude / 2f, 0.018f);
                    pipeSeg.GetComponent<MeshRenderer>().material = brassMat;
                    Object.DestroyImmediate(pipeSeg.GetComponent<Collider>());
                }

                // 3) 고풍스러운 황동 돔/벨 쉐이드 갓 (그림을 바라보도록 틸트 회전)
                GameObject bellShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bellShade.name = "Antique_Bell_Shade";
                bellShade.transform.SetParent(fixtureGo.transform);
                bellShade.transform.position = lampHeadPos;
                bellShade.transform.rotation = shadeRot;
                bellShade.transform.localScale = new Vector3(0.16f, 0.07f, 0.16f);
                bellShade.GetComponent<MeshRenderer>().material = brassMat;
                Object.DestroyImmediate(bellShade.GetComponent<Collider>());

                // 4) 갓 상단 장식 볼 핀 (Top Finial Hinge)
                GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                finial.name = "Shade_Top_Finial";
                finial.transform.SetParent(fixtureGo.transform);
                finial.transform.position = lampHeadPos - (aimDir * 0.045f);
                finial.transform.localScale = new Vector3(0.036f, 0.036f, 0.036f);
                finial.GetComponent<MeshRenderer>().material = darkBronzeMat;
                Object.DestroyImmediate(finial.GetComponent<Collider>());

                // 5) ★ 갓 내부 매립형 발광 렌즈 (그림을 바라보는 각도)
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bulb.name = "Recessed_Emissive_Bulb";
                bulb.transform.SetParent(fixtureGo.transform);
                bulb.transform.position = lampHeadPos + (aimDir * 0.028f);
                bulb.transform.rotation = shadeRot;
                bulb.transform.localScale = new Vector3(0.13f, 0.014f, 0.13f);
                bulb.GetComponent<MeshRenderer>().material = bulbEmissiveMat;
                Object.DestroyImmediate(bulb.GetComponent<Collider>());

                // 6) ★ 갓 내부 렌즈에서 캔버스 중앙을 향해 정확히 투사되는 스팟라이트 광원!
                GameObject spotLightGo = new GameObject($"PictureLight_{artName}");
                spotLightGo.transform.SetParent(fixtureGo.transform);
                spotLightGo.transform.position = lampHeadPos + (aimDir * 0.040f);
                spotLightGo.transform.LookAt(targetAimPos);

                Light l = spotLightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 7.5f;
                l.spotAngle = 75f;
                l.innerSpotAngle = 50f; // ★ Inner Spot Angle 50도 고정!
                l.color = new Color(1f, 0.97f, 0.92f);
                l.shadows = LightShadows.Soft;

                var hdLight = spotLightGo.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = 300000f; // 낮 기본 조명 강도 300000 lux!
                hdLight.volumetricDimmer = 4.0f; // 선명한 빛줄기 4배 증폭!
                hdLight.volumetricShadowDimmer = 1.0f;
                hdLight.useScreenSpaceShadows = true;
            }

            // 3. 조각상 / 장미 / 10층 거대 동상 바닥 매립형 각도조절 업라이트 (발광 투광 렌즈 포함)
            foreach (var art in artworks)
            {
                if (art == null) continue;
                string artName = art.gameObject.name;

                if (artName.Contains("Statue") || artName.Contains("Rose"))
                {
                    // 조각상/장미 좌대에서 충분히 뒤로 떨어져 부드럽게 전체를 비추도록 오프셋 확장!
                    CreateFloorCanUplight(lightingRoot.transform, $"{artName}_Uplight_1",
                        art.transform.position + new Vector3(-2.8f, 0.02f, -2.2f),
                        art.transform.position + new Vector3(0, 1.6f, 0),
                        3000f, brassMat, darkBronzeMat, bulbEmissiveMat);

                    CreateFloorCanUplight(lightingRoot.transform, $"{artName}_Uplight_2",
                        art.transform.position + new Vector3(2.8f, 0.02f, 2.2f),
                        art.transform.position + new Vector3(0, 1.6f, 0),
                        3000f, brassMat, darkBronzeMat, bulbEmissiveMat);
                }
                else if (artName.Contains("Monument") || artName.Contains("RESONANCE"))
                {
                    // 10층 거대 조형물 전용: 조형물 좌대(반경 5.2m)에서 8.5m로 시원하게 멀리 떨어뜨리고, 사진 기준 시계방향으로 대각선 배치!
                    Vector3 coreTarget = art.transform.position + new Vector3(0, 4.2f, 0);

                    // 1) 좌측 뒤쪽 (북서쪽 - 사진 기준 시계방향으로 회전 및 8.5m 이격)
                    CreateFloorCanUplight(lightingRoot.transform, "Monument_Uplight_Left",
                        art.transform.position + new Vector3(-7.5f, 0.02f, 3.8f),
                        coreTarget, 3000f, brassMat, darkBronzeMat, bulbEmissiveMat);

                    // 2) 우측 앞쪽 (남동쪽 - 8.5m 이격)
                    CreateFloorCanUplight(lightingRoot.transform, "Monument_Uplight_Right",
                        art.transform.position + new Vector3(7.5f, 0.02f, -3.8f),
                        coreTarget, 3000f, brassMat, darkBronzeMat, bulbEmissiveMat);
                }
            }
        }

        private static void CreateFloorCanUplight(Transform parent, string name, Vector3 pos, Vector3 targetPos, float intensity, Material brassMat, Material darkBronzeMat, Material bulbEmissiveMat)
        {
            GameObject uplightGo = new GameObject(name);
            uplightGo.transform.SetParent(parent);
            uplightGo.transform.position = pos;

            Vector3 aimDir = (targetPos - pos).normalized;

            // 1) 바닥 고정 외장 브론즈 베젤 림 (Floor Heavy Trim Bezel)
            GameObject floorBezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorBezel.name = "Floor_Bezel_Rim";
            floorBezel.transform.SetParent(uplightGo.transform);
            floorBezel.transform.localPosition = new Vector3(0, 0.02f, 0);
            floorBezel.transform.localScale = new Vector3(0.44f, 0.02f, 0.44f);
            floorBezel.GetComponent<MeshRenderer>().material = darkBronzeMat;
            Object.DestroyImmediate(floorBezel.GetComponent<Collider>());

            // 2) 내부 황동 회전 링 (Inner Brass Gimbal Ring)
            GameObject gimbalRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gimbalRing.name = "Inner_Gimbal_Ring";
            gimbalRing.transform.SetParent(uplightGo.transform);
            gimbalRing.transform.localPosition = new Vector3(0, 0.035f, 0);
            gimbalRing.transform.localScale = new Vector3(0.36f, 0.02f, 0.36f);
            gimbalRing.GetComponent<MeshRenderer>().material = brassMat;
            Object.DestroyImmediate(gimbalRing.GetComponent<Collider>());

            // 3) 조각상을 향해 실제로 기울어진 투광 프로젝터 캔 (Angled Projector Can Head)
            GameObject projectorCan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            projectorCan.name = "Angled_Projector_Can";
            projectorCan.transform.SetParent(uplightGo.transform);
            Vector3 canCenter = new Vector3(0, 0.08f, 0) + (aimDir * 0.04f);
            projectorCan.transform.localPosition = canCenter;
            projectorCan.transform.rotation = Quaternion.FromToRotation(Vector3.up, aimDir);
            projectorCan.transform.localScale = new Vector3(0.24f, 0.12f, 0.24f);
            projectorCan.GetComponent<MeshRenderer>().material = brassMat;
            Object.DestroyImmediate(projectorCan.GetComponent<Collider>());

            // 4) ★ 캔 입구에서 실제로 빛을 뿜는 발광 투광 렌즈 유리 (Emissive Lens Glass)
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Emissive_Spot_Lens";
            lens.transform.SetParent(uplightGo.transform);
            Vector3 lensPos = canCenter + (aimDir * 0.065f);
            lens.transform.localPosition = lensPos;
            lens.transform.rotation = Quaternion.FromToRotation(Vector3.up, aimDir);
            lens.transform.localScale = new Vector3(0.20f, 0.012f, 0.20f);
            lens.GetComponent<MeshRenderer>().material = bulbEmissiveMat;
            Object.DestroyImmediate(lens.GetComponent<Collider>());

            // 5) ★ 바로 이 발광 렌즈 표면에서 정확히 타겟을 향해 위로 뻗어나가는 상향 스팟 라이트!
            GameObject spotGo = new GameObject("UpSpotLight");
            spotGo.transform.SetParent(uplightGo.transform);
            spotGo.transform.position = uplightGo.transform.TransformPoint(lensPos) + aimDir * 0.03f;
            spotGo.transform.LookAt(targetPos);

            Light l = spotGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 14.0f;
            l.spotAngle = 70f;
            l.innerSpotAngle = 50f; // ★ Inner Spot Angle 50도 고정!
            l.color = new Color(1f, 0.96f, 0.90f);
            l.shadows = LightShadows.Soft;

            var hdLight = spotGo.AddComponent<HDAdditionalLightData>();
            hdLight.intensity = intensity;
            hdLight.volumetricDimmer = 4.0f; // 빛줄기 400% 선명화!
            hdLight.volumetricShadowDimmer = 1.0f;
            hdLight.useScreenSpaceShadows = true;
        }

        private static void Setup3x3CeilingLights()
        {
            // 1. 기존 천장 조명 루트 정리
            GameObject existingCeilingRoot = GameObject.Find("Museum_Ceiling_Lights");
            if (existingCeilingRoot != null)
            {
                Object.DestroyImmediate(existingCeilingRoot);
            }

            GameObject ceilingRoot = new GameObject("Museum_Ceiling_Lights");

            // 유저가 직접 만드신 texture 폴더의 silver 및 light 머티리얼 로드!
            Material silverFrameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/texture/silver.mat") ??
                                      AssetDatabase.LoadAssetAtPath<Material>("Assets/SampleSceneAssets/Materials/General/Aluminium_Mat.mat");
            Material lightEmissiveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/texture/light.mat");

            if (silverFrameMat == null)
            {
                silverFrameMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard")) { name = "silver", color = new Color(0.85f, 0.86f, 0.88f) };
                if (silverFrameMat.HasProperty("_Metallic")) silverFrameMat.SetFloat("_Metallic", 0.85f);
                if (silverFrameMat.HasProperty("_Smoothness")) silverFrameMat.SetFloat("_Smoothness", 0.75f);
            }

            if (lightEmissiveMat == null)
            {
                lightEmissiveMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard")) { name = "light", color = Color.white };
                if (lightEmissiveMat.HasProperty("_EmissiveColor"))
                {
                    lightEmissiveMat.SetColor("_EmissiveColor", Color.white * 11.3f);
                    lightEmissiveMat.EnableKeyword("_EMISSION");
                }
            }

            // ★ 3×3 정사각 그리드 (층마다 9개 균일 배치)
            Vector2[] lightPoints = new Vector2[]
            {
                new Vector2(-8.5f, -8.5f), new Vector2(0f, -8.5f), new Vector2(8.5f, -8.5f),
                new Vector2(-8.5f,  0.0f), new Vector2(0f,  0.0f), new Vector2(8.5f,  0.0f),
                new Vector2(-8.5f,  8.5f), new Vector2(0f,  8.5f), new Vector2(8.5f,  8.5f)
            };

            for (int floor = 1; floor <= 9; floor++)
            {
                float floorY = (floor - 1) * 7.0f;
                float ceilingY = floorY + 6.18f; // 천장 슬래브 하단에 완벽 밀착!

                GameObject floorGroup = new GameObject($"Floor_{floor}F_Ceiling_Lights");
                floorGroup.transform.SetParent(ceilingRoot.transform);

                for (int i = 0; i < lightPoints.Length; i++)
                {
                    Vector3 panelPos = new Vector3(lightPoints[i].x, ceilingY, lightPoints[i].y);

                    GameObject panelGo = new GameObject($"Rect_White_Panel_{floor}F_{i + 1}");
                    panelGo.transform.SetParent(floorGroup.transform);
                    panelGo.transform.position = panelPos;

                    // 1) 테두리: silver 머티리얼 적용 + 90도 회전된 직사각형 프레임
                    GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    frame.name = "White_Frame";
                    frame.transform.SetParent(panelGo.transform);
                    frame.transform.position = panelPos;
                    frame.transform.localScale = new Vector3(0.45f, 0.035f, 2.2f); // ★ Z축 방향 회전
                    frame.GetComponent<MeshRenderer>().material = silverFrameMat;
                    Object.DestroyImmediate(frame.GetComponent<Collider>());

                    // 2) 형광등 디퓨저: light 머티리얼 적용
                    GameObject diffuser = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    diffuser.name = "Emissive_Diffuser_Panel";
                    diffuser.transform.SetParent(panelGo.transform);
                    diffuser.transform.position = panelPos + Vector3.down * 0.012f;
                    diffuser.transform.localScale = new Vector3(0.35f, 0.015f, 2.05f); // ★ Z축 방향 회전
                    diffuser.GetComponent<MeshRenderer>().material = lightEmissiveMat;
                    Object.DestroyImmediate(diffuser.GetComponent<Collider>());

                    // 3) Area (Rectangle) 조명 컴포넌트 장착
                    GameObject areaLightGo = new GameObject("PanelAreaLight");
                    areaLightGo.transform.SetParent(panelGo.transform);
                    areaLightGo.transform.position = panelPos + Vector3.down * 0.025f;
                    areaLightGo.transform.rotation = Quaternion.Euler(90f, 0, 0); // 수직 하향 투사

                    Light sl = areaLightGo.AddComponent<Light>();
                    sl.type = LightType.Rectangle; // Area Light!
                    sl.areaSize = new Vector2(0.45f, 2.2f);
                    sl.range = 8.5f;
                    sl.color = new Color(0.98f, 0.98f, 1.0f);
                    sl.shadows = LightShadows.Soft;

                    var hdLightData = areaLightGo.AddComponent<HDAdditionalLightData>();
                    hdLightData.shapeWidth = 0.45f;
                    hdLightData.shapeHeight = 2.2f;
                    hdLightData.range = 8.5f;
                    hdLightData.intensity = 5500f; // 에어리어 라이트 자연스러운 조도
                    hdLightData.volumetricDimmer = 2.0f;
                    hdLightData.volumetricShadowDimmer = 0.8f;
                    hdLightData.useScreenSpaceShadows = true;
                }
            }

            Debug.Log("<color=#FFFFFF><b>[Ceiling Lights 3x3 Area] 1~9층 전 층에 3x3(9개) Area 직사각형 조명(silver 테두리 + light 형광등) 생성 완료!</b></color>");
        }

        [MenuItem("Tools/Ib Museum/🔢 Setup Floor Numbers On Walls (층수 숫자 벽면 즉시 부착)", false, 2)]
        public static void SetupFloorNumberTypographyOnWalls()
        {
            GameObject existingRoot = GameObject.Find("Museum_Floor_Numbers");
            if (existingRoot != null) Object.DestroyImmediate(existingRoot);

            GameObject numbersRoot = new GameObject("Museum_Floor_Numbers");

            for (int f = 1; f <= 9; f++)
            {
                float floorY = (f - 1) * 7.0f;
                bool isEvenFloor = (f % 2 == 0);

                string texPath = $"Assets/texture/Floor_Numbers/{f}.png";
                TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (ti != null)
                {
                    bool needReimport = false;
                    if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; needReimport = true; }
                    if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; needReimport = true; }
                    if (needReimport) ti.SaveAndReimport();
                }

                Texture2D numTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (numTex == null) continue;

                // 초고대비 Opaque 머티리얼 (순백색 배경 + 초대형 칠흑 블랙 숫자)
                Material numMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                numMat.name = $"Mat_FloorNumber_{f}F";
                numMat.SetTexture("_BaseColorMap", numTex);
                numMat.mainTexture = numTex;
                numMat.SetColor("_BaseColor", Color.white);
                numMat.SetFloat("_Smoothness", 0.05f); // 무광 매트 갤러리 질감
                numMat.SetFloat("_Metallic", 0.0f);

                // ★ 유저 요구사항 100% 반영:
                // 1) 위치: 오른쪽 복도 분리벽(Partition_Arrival_Hallway)의 "전시장 안쪽 면"에 부착!
                //    - 복도를 걸어갈 때는 벽 뒤편이라 절대 안 보임!
                //    - 모퉁이를 돌거나 반대편 계단에서 올라왔을 때 전시장 벽면으로 정면에서 훤히 보임!
                // 2) 크기: 벽 사이즈에 맞춰 가로 5.2m x 세로 5.2m 초대형으로 확대!
                Vector3 mainWallPos;
                Quaternion mainWallRot;
                if (isEvenFloor)
                {
                    // 짝수층: 남쪽 분리벽의 북쪽(전시장) 면 (Z = -18.86m), 북쪽(+Z)을 정면으로 바라봄
                    mainWallPos = new Vector3(8.5f, floorY + 3.1f, -18.86f);
                    mainWallRot = Quaternion.Euler(0, 0f, 0);
                }
                else
                {
                    // 홀수층: 북쪽 분리벽의 남쪽(전시장) 면 (Z = 18.86m), 남쪽(-Z)을 정면으로 바라봄
                    mainWallPos = new Vector3(-8.5f, floorY + 3.1f, 18.86f);
                    mainWallRot = Quaternion.Euler(0, 180f, 0);
                }

                GameObject numPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                numPlate.name = $"Floor_{f}F_Large_Number_Plate";
                numPlate.transform.SetParent(numbersRoot.transform);
                numPlate.transform.position = mainWallPos;
                numPlate.transform.rotation = mainWallRot;
                numPlate.transform.localScale = new Vector3(5.2f, 5.2f, 0.04f); // 벽면 가득 차는 초대형 크기!
                numPlate.GetComponent<MeshRenderer>().material = numMat;
                Object.DestroyImmediate(numPlate.GetComponent<Collider>());
            }

            Debug.Log("<color=#33FF33><b>[Floor Numbers] 1~9층 전시장 벽면에 초대형(5.2m x 5.2m) 층수 숫자 판넬 배치 완료! (복도 비가시 / 전시장 & 반대편 계단 시야 완벽 확보)</b></color>");
        }

        [MenuItem("Tools/Ib Museum/🎨 Apply 1F NanoBanana Paintings (1층 나노바나나 그림 즉시 적용)", false, 4)]
        public static void ApplyNanoBananaPaintingsTo1F()
        {
            var paintingsMap = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "Hallway_1_1F", "Assets/IbArtMuseum/Artworks_1F/Hallway_1_1F.jpg" },
                { "Hallway_2_1F", "Assets/IbArtMuseum/Artworks_1F/Hallway_2_1F.jpg" },
                { "Hallway_3_1F", "Assets/IbArtMuseum/Artworks_1F/Hallway_3_1F.jpg" },
                { "Hallway_4_1F", "Assets/IbArtMuseum/Artworks_1F/Hallway_4_1F.jpg" },
                { "BleedingLady", "Assets/IbArtMuseum/Artworks_1F/WomanOfAbyss_1F.jpg" },
                { "GazingEyes", "Assets/IbArtMuseum/Artworks_1F/TheWatcher_1F.jpg" },
                { "FlippedMary", "Assets/IbArtMuseum/Artworks_1F/SmilingMary_1F.jpg" }
            };

            Shader litShader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");

            foreach (var kvp in paintingsMap)
            {
                string targetName = kvp.Key;
                string texPath = kvp.Value;

                TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (ti != null)
                {
                    bool needReimport = false;
                    if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; needReimport = true; }
                    if (ti.sRGBTexture != true) { ti.sRGBTexture = true; needReimport = true; }
                    if (needReimport) ti.SaveAndReimport();
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null)
                {
                    Debug.LogWarning($"[NanoBanana 1F] 텍스처를 찾을 수 없습니다: {texPath}");
                    continue;
                }

                string matPath = $"Assets/IbArtMuseum/Artworks_1F/Mat_{System.IO.Path.GetFileNameWithoutExtension(texPath)}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(litShader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }

                mat.SetTexture("_BaseColorMap", tex);
                mat.mainTexture = tex;
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.15f);
                mat.SetFloat("_Metallic", 0.0f);
                EditorUtility.SetDirty(mat);

                // 1층 오브젝트 찾기
                GameObject targetGo = null;
                if (targetName.StartsWith("Hallway"))
                {
                    targetGo = GameObject.Find(targetName);
                }
                else
                {
                    // 1층 메인 홀 액자 검색
                    GameObject art1F = GameObject.Find("Artworks_1F");
                    if (art1F != null)
                    {
                        Transform child = art1F.transform.Find(targetName);
                        if (child != null) targetGo = child.gameObject;
                    }
                    if (targetGo == null)
                    {
                        GameObject floor1 = GameObject.Find("Floor_1F");
                        if (floor1 != null)
                        {
                            var allTransforms = floor1.GetComponentsInChildren<Transform>(true);
                            foreach (var t in allTransforms)
                            {
                                if (t.name == targetName)
                                {
                                    targetGo = t.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (targetGo != null)
                {
                    Transform canvasT = targetGo.transform.Find("Canvas");
                    if (canvasT != null)
                    {
                        MeshRenderer mr = canvasT.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.material = mat;
                            EditorUtility.SetDirty(mr);
                            Debug.Log($"<color=#FFDF80><b>[NanoBanana 1F] 🎨 {targetName} 액자에 나노바나나 그림 적용 완료!</b></color>");
                        }
                    }

                    BleedingPaintingAnomaly bpa = targetGo.GetComponent<BleedingPaintingAnomaly>();
                    if (bpa != null)
                    {
                        bpa.normalMaterial = mat;
                        EditorUtility.SetDirty(bpa);
                    }

                    IbInteractableArtwork art = targetGo.GetComponent<IbInteractableArtwork>();
                    if (art != null)
                    {
                        art.dayMaterial = mat;
                        EditorUtility.SetDirty(art);
                    }
                }
                else
                {
                    Debug.LogWarning($"[NanoBanana 1F] 1층 액자 오브젝트를 찾을 수 없습니다: {targetName}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#33FF33><b>[NanoBanana 1F] 1층 7개 모든 액자에 제미나이 나노바나나 고화질 그림 적용 완료!</b></color>");
        }

        private static void SetupDaytimeVisitorNPCs()
        {
            GameObject existingVisitors = GameObject.Find("Museum_Daytime_Visitors");
            if (existingVisitors != null)
            {
                Object.DestroyImmediate(existingVisitors);
            }

            GameObject visitorsRoot = new GameObject("Museum_Daytime_Visitors");

            Color[] coatColors = new Color[]
            {
                new Color(0.18f, 0.28f, 0.45f),
                new Color(0.55f, 0.42f, 0.28f),
                new Color(0.25f, 0.38f, 0.28f),
                new Color(0.60f, 0.25f, 0.30f),
                new Color(0.35f, 0.35f, 0.40f)
            };

            string[] visitorNames = new string[]
            {
                "Art Critic Arthur",
                "Gallery Visitor Sophia",
                "Student Oliver",
                "Tourist Emma",
                "Professor Julian"
            };

            string[] visitorDialogues = new string[]
            {
                "The brushwork in this gallery has a strange, living presence... almost like a neural pulse.",
                "Carl Weismann was truly a pioneer. Combining neuroscience with classical oil painting is remarkable.",
                "I've been staring at this portrait for ten minutes. It feels as if its gaze follows me.",
                "The atmosphere here in the morning is so serene and quiet.",
                "Did you know the founder coded his own soul into the neural network before creating this gallery?"
            };

            float floorHeight = 7.0f;

            // ★ 10층(f == 10)은 아침에도 관람객이 없도록 1층~9층까지만 생성!
            for (int f = 1; f <= 9; f++)
            {
                float floorY = (f - 1) * floorHeight;
                GameObject floorVisitors = new GameObject($"Visitors_{f}F");
                floorVisitors.transform.SetParent(visitorsRoot.transform);

                Vector3[] spawnLocs = new Vector3[]
                {
                    new Vector3(-6.5f, floorY, 6.0f),
                    new Vector3(6.5f, floorY, -4.0f),
                    new Vector3(0f, floorY, -6.5f),
                    new Vector3(-5.0f, floorY, -5.0f),
                    new Vector3(4.5f, floorY, 5.5f)
                };

                for (int i = 0; i < 5; i++)
                {
                    CreateVisitorNPCInstance(
                        $"Visitor_{f}F_{i + 1}",
                        floorVisitors.transform,
                        spawnLocs[i],
                        coatColors[i % coatColors.Length],
                        visitorNames[i % visitorNames.Length],
                        visitorDialogues[i % visitorDialogues.Length]
                    );
                }
            }
        }

        private static GameObject CreateVisitorNPCInstance(string name, Transform parent, Vector3 pos, Color coatColor, string vName, string dialogue)
        {
            GameObject npc = new GameObject(name);
            npc.transform.SetParent(parent);
            npc.transform.position = pos;
            npc.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            Material mat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            mat.color = coatColor;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(npc.transform);
            body.transform.localPosition = new Vector3(0, 0.85f, 0);
            body.transform.localScale = new Vector3(0.48f, 0.85f, 0.38f);
            body.GetComponent<MeshRenderer>().material = mat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npc.transform);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            head.transform.localScale = new Vector3(0.32f, 0.38f, 0.32f);
            head.GetComponent<MeshRenderer>().material = mat;

            BoxCollider col = npc.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.0f, 0);
            col.size = new Vector3(1.2f, 2.0f, 1.2f);

            IbVisitorNPC vComp = npc.AddComponent<IbVisitorNPC>();
            vComp.SetDialogue(vName, dialogue);

            return npc;
        }

        private static Material CreateImageMaterial(Texture2D tex, string name)
        {
            Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = name;
            mat.mainTexture = tex;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.4f);
            return mat;
        }

        private static void SetupVolumetricFogAndLightShafts()
        {
            // 1. 씬의 Global Volume에 HDRP 물리 Volumetric Fog 오버라이드 완벽 설정!
            Volume globalVolume = Object.FindFirstObjectByType<Volume>();
            if (globalVolume == null)
            {
                GameObject volGo = new GameObject("Museum_Global_Volume");
                globalVolume = volGo.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.priority = 1f;
            }

            if (globalVolume.profile == null)
            {
                globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            if (!globalVolume.profile.TryGet<Fog>(out var fog))
            {
                fog = globalVolume.profile.Add<Fog>(true);
            }

            if (fog != null)
            {
                fog.enabled.overrideState = true;
                fog.enabled.value = true;

                fog.enableVolumetricFog.overrideState = true;
                fog.enableVolumetricFog.value = true;

                fog.albedo.overrideState = true;
                fog.albedo.value = new Color(0.98f, 0.98f, 1.0f);

                // 전체 실내는 뿌옇지 않고 맑고 투명하게 (가시거리 65m)
                fog.meanFreePath.overrideState = true;
                fog.meanFreePath.value = 65.0f;

                // 전방 산란 계수(Anisotropy)를 높여 빛줄기 경계선을 칼같이 선명하게!
                fog.anisotropy.overrideState = true;
                fog.anisotropy.value = 0.75f;

                fog.baseHeight.overrideState = true;
                fog.baseHeight.value = -2.0f;

                fog.maximumHeight.overrideState = true;
                fog.maximumHeight.value = 75.0f; // 10층 타워 높이 전체 커버

                EditorUtility.SetDirty(globalVolume.profile);
            }

            // 2. 조명 유형별 최적 Volumetric Dimmer 설정 (태양광은 0.2로 하늘 백화 방지, 실내 스팟/업라이트는 4.0으로 쨍한 빛줄기)
            HDAdditionalLightData[] allHdLights = Object.FindObjectsByType<HDAdditionalLightData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hdL in allHdLights)
            {
                if (hdL != null)
                {
                    Light l = hdL.GetComponent<Light>();
                    if (l != null && l.type == LightType.Directional)
                    {
                        hdL.volumetricDimmer = 0.20f; // 태양광으로 인한 하늘 백화 및 눈부심 원천 차단!
                        hdL.volumetricShadowDimmer = 0.8f;
                    }
                    else
                    {
                        hdL.volumetricDimmer = 4.0f; // 실내 스팟/업라이트 빛줄기는 400% 선명하게!
                        hdL.volumetricShadowDimmer = 1.0f;
                    }
                    EditorUtility.SetDirty(hdL);
                }
            }

            // 3. 모든 액자 그림의 BoxCollider를 벽 앞으로 시원하게 돌출시켜 정면 [E] 상호작용 100% 보장!
            IbInteractableArtwork[] artworks = Object.FindObjectsByType<IbInteractableArtwork>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var art in artworks)
            {
                if (art == null) continue;
                BoxCollider col = art.GetComponent<BoxCollider>();
                if (col == null) col = art.gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(Mathf.Max(col.size.x, 2.0f), Mathf.Max(col.size.y, 2.6f), 0.60f); // 앞뒤 두께 0.6m
                col.center = new Vector3(col.center.x, col.center.y, -0.20f); // 벽면 앞쪽으로 돌출!
                EditorUtility.SetDirty(col);
            }

            Debug.Log("<color=#33FF33><b>[Volumetric Lighting & Colliders] 태양광 눈부심 차단, 실내 빛줄기 4배 선명화, 전 액자 정면 [E] 감지 콜라이더 최적화 완료!</b></color>");
        }

        [MenuItem("Tools/Ib Museum/☀️ Switch to Bright Day Mode (대낮 모드로 전환)", false, 3)]
        public static void SwitchToDayMode()
        {
            IbGameManager gm = Object.FindFirstObjectByType<IbGameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에 IbGameManager가 없습니다.", "확인");
                return;
            }

            gm.currentPhase = GamePhase.Prologue_Day;
            gm.SetDayEnvironment(true);
            Debug.Log("<color=#FFFF55><b>[Ib Museum] ☀️ 대낮(Day) 모드로 전환되었습니다! (천장 조명 점등 & light.mat 발광)</b></color>");
        }

        [MenuItem("Tools/Ib Museum/🌙 Switch to Dark Night Mode (심야 모드로 전환)", false, 4)]
        public static void SwitchToNightMode()
        {
            IbGameManager gm = Object.FindFirstObjectByType<IbGameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에 IbGameManager가 없습니다.", "확인");
                return;
            }

            gm.currentPhase = GamePhase.Night_Loop;
            gm.SetDayEnvironment(false);
            Debug.Log("<color=#55FFFF><b>[Ib Museum] 🌙 심야(Night) 모드로 전환되었습니다! (천장 조명 소등 & black.mat 패널 전환)</b></color>");
        }

        [MenuItem("Tools/Ib Museum/🔄 Toggle Day ⇄ Night Mode (낮/밤 상호 토글)", false, 5)]
        public static void ToggleNightModeTest()
        {
            IbGameManager gm = Object.FindFirstObjectByType<IbGameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에 IbGameManager가 없습니다.", "확인");
                return;
            }

            bool toNight = (gm.currentPhase == GamePhase.Prologue_Day);
            if (toNight) SwitchToNightMode();
            else SwitchToDayMode();
        }
    }
}
