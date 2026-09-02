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
                if (go != null && (go.name == "Museum_Strict_Triggers" || go.name.StartsWith("Museum_Strict_Triggers")))
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
                aSource.playOnAwake = true;
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

            // 5. ★ 10층->9층 복도 액자 낮에는 일반 그림, 밤에 상호작용 글씨 표시로 설정!
            SetupHallwayPaintingsDayNight();

            // 6. ★ 10층부터 1층까지 엄격한 Y축 고유 트리거 및 체크포인트 시스템 재배치
            SetupStrictFloorTriggers();

            // 7. 각 층마다 4~5명의 대화 가능한 관람객 NPC 스폰
            SetupDaytimeVisitorNPCs();

            // 8. 3/5/7/9층 생명의 화병 & 3~9층 계단 앞 수수께끼 관리인 NPC 및 복도 차단 콜라이더 설치!
            SetupRiddleGuardsAndSaveVases();

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

                // 2) ★ 복도 모퉁이를 돌 때 층 입장 트리거 (10층 제외)
                // 계단을 내려와 복도 끝 모퉁이를 도는 순간 100% 감지되어 입장 콘솔 및 이상현상 콘솔 출력!
                if (f != 10)
                {
                    Vector3 cornerPos = isEvenFloor ? new Vector3(14.0f, floorY + 1.5f, -16.0f) : new Vector3(-14.0f, floorY + 1.5f, 16.0f);

                    GameObject enterHallTrig = new GameObject($"EnterMainHallTrigger_{f}F");
                    enterHallTrig.transform.SetParent(trigRoot.transform);
                    enterHallTrig.transform.position = cornerPos;
                    
                    BoxCollider box = enterHallTrig.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(8.0f, 3.5f, 8.0f);

                    IbCircularTrigger ct = enterHallTrig.AddComponent<IbCircularTrigger>();
                    ct.triggerType = FloorTriggerType.EnterMainHall;
                    ct.floorLevel = f;
                    ct.maxYDifference = 2.5f;
                }

                // 3) 다음 층으로 내려가는 계단 입구 체크포인트 (f > 1)
                // 이상현상 없을 시 아무 일도 안 일어나고 계단을 자연스럽게 걸어 내려감!
                // 이상현상 있을 시 장미 1개 차감 & 현재 층 착지 체크포인트(arrivalPos)로 루프!
                if (f > 1)
                {
                    Vector3 downPos = isEvenFloor ? new Vector3(17.5f, floorY + 1.2f, 21.75f) : new Vector3(-17.5f, floorY + 1.2f, -21.75f);

                    GameObject downTrig = new GameObject($"StairDownTrigger_{f}F_to_{f - 1}F");
                    downTrig.transform.SetParent(trigRoot.transform);
                    downTrig.transform.position = downPos;

                    BoxCollider box = downTrig.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(5.0f, 3.5f, 5.0f);

                    IbCircularTrigger ct = downTrig.AddComponent<IbCircularTrigger>();
                    ct.triggerType = FloorTriggerType.StairsDown;
                    ct.floorLevel = f;
                    ct.maxYDifference = 2.5f;
                }

                // 4) 되돌아가기 판정 트리거 (스폰 복도로 되돌아왔을 때, 1 < f < 10)
                if (f > 1 && f != 10)
                {
                    Vector3 turnPos = isEvenFloor ? new Vector3(1.0f, floorY + 1.2f, -21.75f) : new Vector3(1.0f, floorY + 1.2f, 21.75f);

                    GameObject turnTrig = new GameObject($"TurnBackTrigger_{f}F");
                    turnTrig.transform.SetParent(trigRoot.transform);
                    turnTrig.transform.position = turnPos;

                    BoxCollider box = turnTrig.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(6.0f, 3.5f, 5.0f);

                    IbCircularTrigger ct = turnTrig.AddComponent<IbCircularTrigger>();
                    ct.triggerType = FloorTriggerType.TurnBack;
                    ct.floorLevel = f;
                    ct.maxYDifference = 2.5f;
                }
            }
        }

        private static void SetupPrecisePictureLightsAboveCanvases()
        {
            // 1. 기존 Custom 조명들 전부 완전 삭제!
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.name == "FloorLights" || go.name.StartsWith("SpotLight_") || go.name == "Museum_Custom_Lighting" || go.name.StartsWith("PictureLight_") || go.name.StartsWith("Fixture_"))
                {
                    Object.DestroyImmediate(go);
                }
            }

            GameObject lightingRoot = new GameObject("Museum_Custom_Lighting");

            // 조명 기구용 황동/골드 머티리얼
            Material fixtureMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            fixtureMat.name = "Mat_Brass_LightFixture";
            fixtureMat.color = new Color(0.88f, 0.75f, 0.42f);
            if (fixtureMat.HasProperty("_Metallic")) fixtureMat.SetFloat("_Metallic", 0.85f);
            if (fixtureMat.HasProperty("_Smoothness")) fixtureMat.SetFloat("_Smoothness", 0.75f);

            // 2. 모든 액자의 캔버스 그림 정면 상단 핀조명 (3000 lux 고정)
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

                Vector3 mountPos = canvasPos + (-canvasFacingDir * 0.12f) + Vector3.up * 1.35f;
                Vector3 lampHeadPos = mountPos + (canvasFacingDir * 0.65f) + Vector3.up * 0.15f;

                GameObject fixtureGo = new GameObject($"Fixture_{artName}");
                fixtureGo.transform.SetParent(lightingRoot.transform);

                GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                basePlate.name = "MountBase";
                basePlate.transform.SetParent(fixtureGo.transform);
                basePlate.transform.position = mountPos;
                basePlate.transform.rotation = Quaternion.LookRotation(canvasFacingDir, Vector3.up);
                basePlate.transform.localScale = new Vector3(0.32f, 0.10f, 0.05f);
                basePlate.GetComponent<MeshRenderer>().material = fixtureMat;
                Object.DestroyImmediate(basePlate.GetComponent<Collider>());

                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arm.name = "SupportArm";
                arm.transform.SetParent(fixtureGo.transform);
                arm.transform.position = (mountPos + lampHeadPos) / 2f;
                arm.transform.rotation = Quaternion.FromToRotation(Vector3.up, (lampHeadPos - mountPos).normalized);
                arm.transform.localScale = new Vector3(0.035f, (lampHeadPos - mountPos).magnitude / 2f, 0.035f);
                arm.GetComponent<MeshRenderer>().material = fixtureMat;
                Object.DestroyImmediate(arm.GetComponent<Collider>());

                GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shade.name = "LampShade";
                shade.transform.SetParent(fixtureGo.transform);
                shade.transform.position = lampHeadPos;
                shade.transform.rotation = Quaternion.LookRotation(art.transform.right, Vector3.up) * Quaternion.Euler(0, 0, 90f);
                shade.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
                shade.GetComponent<MeshRenderer>().material = fixtureMat;
                Object.DestroyImmediate(shade.GetComponent<Collider>());

                GameObject spotLightGo = new GameObject($"PictureLight_{artName}");
                spotLightGo.transform.SetParent(fixtureGo.transform);
                spotLightGo.transform.position = lampHeadPos;
                spotLightGo.transform.LookAt(canvasPos + Vector3.up * 0.1f);

                Light l = spotLightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 6.5f;
                l.spotAngle = 80f;
                l.innerSpotAngle = 45f;
                l.color = new Color(1f, 0.97f, 0.92f);
                l.shadows = LightShadows.Soft;

                var hdLight = spotLightGo.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = 300000f; // 낮 기본 조명 강도 300000 lux!
                hdLight.useScreenSpaceShadows = true;
            }

            // 3. 조각상 / 장미 / 10층 거대 동상 바닥 업라이트 & 동상 안쪽 조명 (3000 lux)
            foreach (var art in artworks)
            {
                if (art == null) continue;
                string artName = art.gameObject.name;

                if (artName.Contains("Statue") || artName.Contains("Rose"))
                {
                    CreateFloorCanUplight(lightingRoot.transform, $"{artName}_Uplight_1",
                        art.transform.position + new Vector3(-1.8f, 0.08f, -1.1f),
                        art.transform.position + new Vector3(0, 1.6f, 0),
                        3000f, fixtureMat);

                    CreateFloorCanUplight(lightingRoot.transform, $"{artName}_Uplight_2",
                        art.transform.position + new Vector3(1.8f, 0.08f, 1.1f),
                        art.transform.position + new Vector3(0, 1.6f, 0),
                        3000f, fixtureMat);
                }
                else if (artName.Contains("Monument") || artName.Contains("RESONANCE"))
                {
                    // 10층 거대 조형물 전용: 바닥 업라이트 조명 2개 (좌측 앞, 우측 뒤)
                    CreateFloorCanUplight(lightingRoot.transform, "Monument_Uplight_Left",
                        art.transform.position + new Vector3(-4.5f, 0.08f, -3.2f),
                        art.transform.position + new Vector3(0, 4.0f, 0),
                        3000f, fixtureMat);

                    CreateFloorCanUplight(lightingRoot.transform, "Monument_Uplight_Right",
                        art.transform.position + new Vector3(4.5f, 0.08f, -3.2f),
                        art.transform.position + new Vector3(0, 4.0f, 0),
                        3000f, fixtureMat);

                    // RESONANCE 명판 안쪽/아래에서 바닥을 비추는 조명
                    GameObject signUnderLight = new GameObject("RESONANCE_Inside_Sign_Light");
                    signUnderLight.transform.SetParent(lightingRoot.transform);
                    signUnderLight.transform.position = art.transform.position + new Vector3(0, 0.45f, -3.8f);
                    Light sul = signUnderLight.AddComponent<Light>();
                    sul.type = LightType.Point;
                    sul.range = 5.0f;
                    sul.color = new Color(1.0f, 0.95f, 0.85f);
                    var hdSul = signUnderLight.AddComponent<HDAdditionalLightData>();
                    hdSul.intensity = 3000f;
                }
            }
        }

        private static void CreateFloorCanUplight(Transform parent, string name, Vector3 pos, Vector3 targetPos, float intensity, Material fixtureMat)
        {
            GameObject uplightGo = new GameObject(name);
            uplightGo.transform.SetParent(parent);
            uplightGo.transform.position = pos;

            // 1) 바닥 원통형 캔 기구 (Floor Can Fixture)
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Fixture_Can";
            can.transform.SetParent(uplightGo.transform);
            can.transform.localPosition = new Vector3(0, 0.06f, 0);
            can.transform.localScale = new Vector3(0.40f, 0.06f, 0.40f);
            can.GetComponent<MeshRenderer>().material = fixtureMat;
            Object.DestroyImmediate(can.GetComponent<Collider>());

            // 2) 위를 향해 빛을 쏘는 상향 스팟 라이트
            GameObject spotGo = new GameObject("UpSpotLight");
            spotGo.transform.SetParent(uplightGo.transform);
            spotGo.transform.position = pos + Vector3.up * 0.12f;
            spotGo.transform.LookAt(targetPos);

            Light l = spotGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 14.0f;
            l.spotAngle = 60f;
            l.innerSpotAngle = 35f;
            l.color = new Color(1f, 0.96f, 0.90f);
            l.shadows = LightShadows.Soft;

            var hdLight = spotGo.AddComponent<HDAdditionalLightData>();
            hdLight.intensity = intensity;
            hdLight.useScreenSpaceShadows = true;
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

        [MenuItem("Tools/Ib Museum/🌙 Toggle Blue Night Mode (푸른 밤 모드 즉시 테스트)", false, 3)]
        public static void ToggleNightModeTest()
        {
            IbGameManager gm = Object.FindFirstObjectByType<IbGameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에 IbGameManager가 없습니다.", "확인");
                return;
            }

            bool toNight = (gm.currentPhase == GamePhase.Prologue_Day);
            gm.currentPhase = toNight ? GamePhase.Night_Loop : GamePhase.Prologue_Day;
            gm.SetDayEnvironment(!toNight);

            Debug.Log($"[GameLogic Utility] 조명 모드가 {(toNight ? "푸른 밤(Night)" : "대낮(Day)")}으로 전환되었습니다!");
        }
    }
}
