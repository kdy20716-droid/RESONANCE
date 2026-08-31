using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace IbArtMuseum
{
    public class IbMuseumSetupUtility : EditorWindow
    {
        [MenuItem("Tools/Ib Museum/🏛️ Clean & Rebuild 10-Floor Architecture (미술관 건축 전용 빌드)", false, 1)]
        public static void CleanAndRebuild10FloorMuseum()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = currentScene.GetRootGameObjects();

            bool confirm = EditorUtility.DisplayDialog(
                "10층 미술관 타워 구축 (RESONANCE)",
                "1층 부모님 프롤로그, 10층 동상 공명 컷씬(밤 전환), 3송이 장미 라이프 시스템 및 9층 4대 가이드 액자가 적용된 'RESONANCE'를 새로 구축하시겠습니까?",
                "예 (완전 새로 구축)",
                "취소"
            );

            if (!confirm) return;

            foreach (var root in rootObjects)
            {
                if (root != null) DestroyImmediate(root);
            }

            // 1. 루트 오브젝트 생성
            GameObject rootGo = new GameObject("_IbMuseum_10FloorTower");
            Undo.RegisterCreatedObjectUndo(rootGo, "Build RESONANCE Story Museum");

            // 2. HDRP 환경 라이팅 & 볼륨 복구
            SetupHDRPEnvironmentLighting(rootGo);

            // 3. 머티리얼 및 텍스처 준비
            Material floorMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/MarbleFlooring_Mat.mat", new Color(0.90f, 0.87f, 0.84f), 0.90f, 0.1f);
            Material wallMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteSmooth_Mat.mat", new Color(0.96f, 0.94f, 0.91f), 0.2f, 0.0f);
            Material ceilingMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteMatteGrey_Mat.mat", new Color(0.24f, 0.24f, 0.26f), 0.1f, 0.0f);
            Material goldFrameMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/Brass_Mat.mat", new Color(0.88f, 0.70f, 0.25f), 0.9f, 0.85f);
            Material darkWoodMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteTaupe_Mat.mat", new Color(0.18f, 0.12f, 0.08f), 0.4f, 0.0f);
            Material woodStairMat = CreateAntiqueWoodMaterial();
            Material glassMat = LoadOrCreateGlassMaterial();
            Material ropeRedMat = CreateVelvetRopeMaterial();

            Texture2D ladyPortraitTex = IbTextureGenerator.GenerateLadyPortrait(false);
            Texture2D ladyBleedingTex = IbTextureGenerator.GenerateLadyPortrait(true);
            Texture2D maryPortraitTex = IbTextureGenerator.GenerateMaryPortrait();
            Texture2D garryPortraitTex = IbTextureGenerator.GenerateGarryPortrait();
            Texture2D graffitiTex = IbTextureGenerator.GenerateRedGraffiti();
            Texture2D resonanceSignTex = IbTextureGenerator.GenerateResonanceSignboard();

            // 4대 가이드 텍스트 액자 텍스처 생성
            Texture2D guideTex1 = IbTextureGenerator.GenerateGuidePaintingTexture("WELCOME TO MY MUSEUM,", "LINA");
            Texture2D guideTex2 = IbTextureGenerator.GenerateGuidePaintingTexture("LET'S PLAY", "TOGETHER");
            Texture2D guideTex3 = IbTextureGenerator.GenerateGuidePaintingTexture("IF YOU SEE ANYTHING DIFFERENT", "FROM MORNING, TURN BACK.", "THEN YOU'LL BE SAFE.");
            Texture2D guideTex4 = IbTextureGenerator.GenerateGuidePaintingTexture("IF YOU IGNORE ANOMALIES", "AND PROCEED...", "YOU MIGHT REGRET IT.");

            Material ladyMat = CreateImageMaterial(ladyPortraitTex, "Mat_Lady_Normal");
            Material ladyBleedMat = CreateImageMaterial(ladyBleedingTex, "Mat_Lady_Bleed");
            Material maryMat = CreateImageMaterial(maryPortraitTex, "Mat_Mary");
            Material garryMat = CreateImageMaterial(garryPortraitTex, "Mat_Garry");
            Material graffitiMat = CreateTransparentMaterial(graffitiTex, "Mat_Graffiti");
            Material resonanceSignMat = CreateImageMaterial(resonanceSignTex, "Mat_Sign_Resonance");

            Material guideMat1 = CreateImageMaterial(guideTex1, "Mat_Guide_1");
            Material guideMat2 = CreateImageMaterial(guideTex2, "Mat_Guide_2");
            Material guideMat3 = CreateImageMaterial(guideTex3, "Mat_Guide_3");
            Material guideMat4 = CreateImageMaterial(guideTex4, "Mat_Guide_4");

            // 4. 건축 파라미터
            int totalFloors = 10;
            float floorHeight = 7.0f;     // 층간 높이 7m
            float slabThickness = 0.8f;   // 0.8m 단일 슬래브 두께
            float corridorHeight = floorHeight - slabThickness; // 실내 층고 6.2m

            float bldgWidth = 36.0f;  // X축 너비 36m (halfW = 18m)
            float bldgLength = 48.0f; // Z축 길이 48m (halfL = 24m)
            float wallThickness = 0.8f;

            float halfW = bldgWidth / 2f;   // 18.0m
            float halfL = bldgLength / 2f;  // 24.0m

            GameObject towerGo = new GameObject("Museum_10Floor_Architecture");
            towerGo.transform.SetParent(rootGo.transform);

            // 5. 시스템 매니저 생성
            GameObject gmGo = new GameObject("IbGameManager");
            gmGo.transform.SetParent(rootGo.transform);
            IbGameManager gm = gmGo.AddComponent<IbGameManager>();
            gm.floorHeight = floorHeight;

            GameObject amGo = new GameObject("IbAnomalyManager");
            amGo.transform.SetParent(rootGo.transform);
            IbAnomalyManager am = amGo.AddComponent<IbAnomalyManager>();
            gm.anomalyManager = am;

            GameObject audioGo = new GameObject("IbAudioAmbience");
            audioGo.transform.SetParent(rootGo.transform);
            IbAudioAmbience audioAmbience = audioGo.AddComponent<IbAudioAmbience>();
            gm.audioAmbience = audioAmbience;

            // 6. 플레이어 생성 (1층 안내데스크 앞 부모님과 시작)
            Vector3 startPos1F = new Vector3(-8.0f, 0.1f, 0f);
            Quaternion startRot1F = Quaternion.Euler(0, 90f, 0); // 동쪽(안내데스크 및 부모님)을 바라봄

            GameObject playerGo = new GameObject("IbPlayer (Lina)");
            playerGo.transform.SetParent(rootGo.transform);
            playerGo.transform.position = startPos1F;
            playerGo.transform.rotation = startRot1F;
            playerGo.tag = "Player";

            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.8f, 0);

            GameObject camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(playerGo.transform);
            camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            var hdCamData = camGo.AddComponent<HDAdditionalCameraData>();
            hdCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;

            IbPlayerController player = playerGo.AddComponent<IbPlayerController>();
            player.playerCamera = camGo.transform;
            gm.player = player;

            // 7. 10개 층 빌드 (10F ~ 1F)
            for (int f = 0; f < totalFloors; f++)
            {
                int floorNumber = 10 - f; // 10F, 9F, ... 1F
                float floorY = (9 - f) * floorHeight;
                bool isEvenFloor = (floorNumber % 2 == 0);

                GameObject floorRoot = new GameObject($"Floor_{floorNumber}F");
                floorRoot.transform.SetParent(towerGo.transform);

                // 스폰 지점 (계단을 다 내려온 뒤 정확히 3걸음 앞 복도 지점!)
                Vector3 spawnPos;
                Quaternion spawnRot;
                if (floorNumber == 10)
                {
                    spawnPos = new Vector3(0f, floorY + 0.1f, -8.0f);
                    spawnRot = Quaternion.Euler(0, 0f, 0);
                }
                else if (isEvenFloor)
                {
                    // 짝수층: 북쪽 계단을 내려와 3걸음 앞 (X = 0.5m, Z = 21.75m), 서쪽을 바라봄
                    spawnPos = new Vector3(0.5f, floorY + 0.1f, 21.75f);
                    spawnRot = Quaternion.Euler(0, -90f, 0);
                }
                else
                {
                    // 홀수층: 남쪽 계단을 내려와 3걸음 앞 (X = -0.5m, Z = -21.75m), 동쪽을 바라봄
                    spawnPos = new Vector3(-0.5f, floorY + 0.1f, -21.75f);
                    spawnRot = Quaternion.Euler(0, 90f, 0);
                }

                GameObject spawnPoint = new GameObject($"SpawnPoint_{floorNumber}F");
                spawnPoint.transform.SetParent(floorRoot.transform);
                spawnPoint.transform.position = spawnPos;
                spawnPoint.transform.rotation = spawnRot;
                gm.floorSpawnPoints[f] = spawnPoint.transform;

                // ==================== 1. 바닥 슬래브 ====================
                if (floorNumber == 1)
                {
                    CreateBox(floorRoot.transform, "Floor_Slab_1F", new Vector3(0, floorY - (slabThickness / 2f), 0), new Vector3(bldgWidth, slabThickness, bldgLength), floorMat);

                    // 1층 프롤로그 스폰 포인트 등록
                    GameObject prolSpawn = new GameObject("Prologue_SpawnPoint_1F");
                    prolSpawn.transform.SetParent(floorRoot.transform);
                    prolSpawn.transform.position = startPos1F;
                    prolSpawn.transform.rotation = startRot1F;
                    gm.prologueSpawnPoint_1F = prolSpawn.transform;
                }
                else
                {
                    CreateBox(floorRoot.transform, "Floor_Main_Hall", new Vector3(0, floorY - (slabThickness / 2f), 0), new Vector3(bldgWidth, slabThickness, 39.0f), floorMat);

                    if (isEvenFloor)
                    {
                        CreateBox(floorRoot.transform, "Floor_North_Solid", new Vector3(-7.5f, floorY - (slabThickness / 2f), 21.75f), new Vector3(21.0f, slabThickness, 4.5f), floorMat);
                        CreateBox(floorRoot.transform, "Floor_South_Solid", new Vector3(0, floorY - (slabThickness / 2f), -21.75f), new Vector3(bldgWidth, slabThickness, 4.5f), floorMat);
                    }
                    else
                    {
                        CreateBox(floorRoot.transform, "Floor_South_Solid", new Vector3(7.5f, floorY - (slabThickness / 2f), -21.75f), new Vector3(21.0f, slabThickness, 4.5f), floorMat);
                        CreateBox(floorRoot.transform, "Floor_North_Solid", new Vector3(0, floorY - (slabThickness / 2f), 21.75f), new Vector3(bldgWidth, slabThickness, 4.5f), floorMat);
                    }
                }

                // ==================== 2. 10층 전용: 천장 아치형 투명 유리 천창 ====================
                if (floorNumber == 10)
                {
                    float roofY = floorY + corridorHeight + (slabThickness / 2f);

                    CreateBox(floorRoot.transform, "Roof_North_Slab", new Vector3(0, roofY, 18f), new Vector3(bldgWidth, slabThickness, 12f), ceilingMat);
                    CreateBox(floorRoot.transform, "Roof_South_Slab", new Vector3(0, roofY, -18f), new Vector3(bldgWidth, slabThickness, 12f), ceilingMat);
                    CreateBox(floorRoot.transform, "Roof_West_Slab", new Vector3(-14f, roofY, 0), new Vector3(8f, slabThickness, 24f), ceilingMat);
                    CreateBox(floorRoot.transform, "Roof_East_Slab", new Vector3(14f, roofY, 0), new Vector3(8f, slabThickness, 24f), ceilingMat);

                    CreateHalfCylinderVaultSkylight(floorRoot.transform, floorY + corridorHeight, 20.0f, 24.0f, 4.5f, glassMat, goldFrameMat);

                    // 10층 밤하늘 별무리 (밤에 천창 밖으로 보이는 수많은 별들)
                    GameObject starFieldGo = new GameObject("NightSky_StarField");
                    starFieldGo.transform.SetParent(floorRoot.transform);
                    starFieldGo.transform.position = new Vector3(0, floorY + corridorHeight + 12.0f, 0);

                    Material starMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                    starMat.color = new Color(0.95f, 0.98f, 1.0f);
                    if (starMat.HasProperty("_EmissiveColor"))
                    {
                        starMat.EnableKeyword("_EMISSION");
                        starMat.SetColor("_EmissiveColor", new Color(0.85f, 0.92f, 1.0f) * 6f);
                    }

                    for (int s = 0; s < 45; s++)
                    {
                        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        star.name = $"Star_{s + 1}";
                        star.transform.SetParent(starFieldGo.transform);
                        float sx = Random.Range(-18f, 18f);
                        float sy = Random.Range(-2f, 6f);
                        float sz = Random.Range(-22f, 22f);
                        star.transform.localPosition = new Vector3(sx, sy, sz);
                        float sSize = Random.Range(0.25f, 0.55f);
                        star.transform.localScale = new Vector3(sSize, sSize, sSize);
                        star.GetComponent<MeshRenderer>().material = starMat;
                        Object.DestroyImmediate(star.GetComponent<Collider>());
                    }
                    starFieldGo.SetActive(false); // 낮에는 꺼져있고 밤 전환 시 활성화!

                    GameObject sunShaftGo = new GameObject("SunShaft_Light_Beam");
                    sunShaftGo.transform.SetParent(floorRoot.transform);
                    sunShaftGo.transform.position = new Vector3(0, floorY + corridorHeight + 4.0f, 0);
                    sunShaftGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                    Light shaftLight = sunShaftGo.AddComponent<Light>();
                    shaftLight.type = LightType.Spot;
                    shaftLight.range = 25f;
                    shaftLight.spotAngle = 75f;
                    shaftLight.color = new Color(1f, 0.98f, 0.92f);
                    shaftLight.intensity = 90f;

                    var hdShaftData = sunShaftGo.AddComponent<HDAdditionalLightData>();
                    hdShaftData.intensity = 5000f;
                }

                // ==================== 3. 4방향 외벽 (0.8m 두께) ====================
                CreateFourWalls(floorRoot.transform, floorY, corridorHeight, slabThickness, halfW, halfL, wallThickness, wallMat, floorNumber == 1);

                // ==================== 4. 계단 하단 분리벽 전용 복도 ====================
                if (floorNumber == 10)
                {
                    // 10층은 시작 층
                }
                else if (isEvenFloor)
                {
                    CreateBox(floorRoot.transform, "Partition_South_Arrival_Hallway", new Vector3(-1.5f, floorY + (corridorHeight / 2f), -19.2f), new Vector3(33.0f, corridorHeight, 0.6f), wallMat);
                }
                else
                {
                    CreateBox(floorRoot.transform, "Partition_North_Arrival_Hallway", new Vector3(1.5f, floorY + (corridorHeight / 2f), 19.2f), new Vector3(33.0f, corridorHeight, 0.6f), wallMat);
                }

                // ==================== 5. 전시장 진입 감지 트리거 (EnterMainHall) ====================
                if (floorNumber != 10)
                {
                    GameObject enterHallTrig = new GameObject($"EnterMainHallTrigger_{floorNumber}F");
                    enterHallTrig.transform.SetParent(floorRoot.transform);
                    enterHallTrig.transform.position = new Vector3(isEvenFloor ? 14.0f : -14.0f, floorY + 1.5f, 0f);
                    BoxCollider enterBox = enterHallTrig.AddComponent<BoxCollider>();
                    enterBox.isTrigger = true;
                    enterBox.size = new Vector3(6.0f, 4.0f, 20.0f);
                    IbCircularTrigger enterCTrig = enterHallTrig.AddComponent<IbCircularTrigger>();
                    enterCTrig.triggerType = FloorTriggerType.EnterMainHall;
                }

                // ==================== 6. 계단실 및 안전하게 정돈된 'ㄱ'자 펜스 & 정밀 루프 트리거 ====================
                if (floorNumber > 1)
                {
                    if (isEvenFloor)
                    {
                        CreateStraightWoodStaircase(floorRoot.transform, floorY, floorHeight, new Vector3(18.0f, floorY, 21.75f), new Vector3(3.0f, floorY - floorHeight, 21.75f), 4.5f, 22, woodStairMat);

                        CreateRefinedStanchionRopeFence(floorRoot.transform, floorY,
                            new Vector3(14.5f, floorY, 19.05f),
                            new Vector3(2.55f, floorY, 19.05f),
                            new Vector3(2.55f, floorY, 23.5f),
                            goldFrameMat, darkWoodMat, ropeRedMat);

                        // 계단 하향 트리거 (계단을 다 내려온 뒤 정확히 3걸음 앞 지점: X = 0.5m)
                        float stairBottomY = floorY - floorHeight;
                        Vector3 downTrigPos = new Vector3(0.5f, stairBottomY + 1.2f, 21.75f);
                        GameObject downTrig = new GameObject($"StairDownTrigger_{floorNumber}F_to_{floorNumber - 1}F");
                        downTrig.transform.SetParent(floorRoot.transform);
                        downTrig.transform.position = downTrigPos;
                        BoxCollider boxD = downTrig.AddComponent<BoxCollider>();
                        boxD.isTrigger = true;
                        boxD.size = new Vector3(2.5f, 3.5f, 4.0f);
                        IbCircularTrigger cTrigD = downTrig.AddComponent<IbCircularTrigger>();
                        cTrigD.triggerType = FloorTriggerType.StairsDown;

                        // 뒤돌아가기 트리거 (전시장을 둘러본 후 스폰 복도로 되돌아와 3걸음 앞 지점 도착 시)
                        if (floorNumber != 10)
                        {
                            Vector3 turnBackPos = new Vector3(0.5f, floorY + 1.2f, -21.75f);
                            GameObject turnBackTrig = new GameObject($"TurnBackTrigger_{floorNumber}F");
                            turnBackTrig.transform.SetParent(floorRoot.transform);
                            turnBackTrig.transform.position = turnBackPos;
                            BoxCollider boxT = turnBackTrig.AddComponent<BoxCollider>();
                            boxT.isTrigger = true;
                            boxT.size = new Vector3(2.5f, 3.5f, 4.0f);
                            IbCircularTrigger cTrigT = turnBackTrig.AddComponent<IbCircularTrigger>();
                            cTrigT.triggerType = FloorTriggerType.TurnBack;
                        }
                    }
                    else
                    {
                        CreateStraightWoodStaircase(floorRoot.transform, floorY, floorHeight, new Vector3(-18.0f, floorY, -21.75f), new Vector3(-3.0f, floorY - floorHeight, -21.75f), 4.5f, 22, woodStairMat);

                        CreateRefinedStanchionRopeFence(floorRoot.transform, floorY,
                            new Vector3(-14.5f, floorY, -19.05f),
                            new Vector3(-2.55f, floorY, -19.05f),
                            new Vector3(-2.55f, floorY, -23.5f),
                            goldFrameMat, darkWoodMat, ropeRedMat);

                        // 계단 하향 트리거 (계단을 다 내려온 뒤 정확히 3걸음 앞 지점: X = -0.5m)
                        float stairBottomY = floorY - floorHeight;
                        Vector3 downTrigPos = new Vector3(-0.5f, stairBottomY + 1.2f, -21.75f);
                        GameObject downTrig = new GameObject($"StairDownTrigger_{floorNumber}F_to_{floorNumber - 1}F");
                        downTrig.transform.SetParent(floorRoot.transform);
                        downTrig.transform.position = downTrigPos;
                        BoxCollider boxD = downTrig.AddComponent<BoxCollider>();
                        boxD.isTrigger = true;
                        boxD.size = new Vector3(2.5f, 3.5f, 4.0f);
                        IbCircularTrigger cTrigD = downTrig.AddComponent<IbCircularTrigger>();
                        cTrigD.triggerType = FloorTriggerType.StairsDown;

                        // 뒤돌아가기 트리거 (전시장을 둘러본 후 스폰 복도로 되돌아와 3걸음 앞 지점 도착 시)
                        Vector3 turnBackPos = new Vector3(-0.5f, floorY + 1.2f, 21.75f);
                        GameObject turnBackTrig = new GameObject($"TurnBackTrigger_{floorNumber}F");
                        turnBackTrig.transform.SetParent(floorRoot.transform);
                        turnBackTrig.transform.position = turnBackPos;
                        BoxCollider boxT = turnBackTrig.AddComponent<BoxCollider>();
                        boxT.isTrigger = true;
                        boxT.size = new Vector3(2.5f, 3.5f, 4.0f);
                        IbCircularTrigger cTrigT = turnBackTrig.AddComponent<IbCircularTrigger>();
                        cTrigT.triggerType = FloorTriggerType.TurnBack;
                    }
                }

                // ==================== 6. 1층 전용: 안내데스크 & 부모님(NPC) & RESONANCE 정문 ====================
                if (floorNumber == 1)
                {
                    CreateLobbyReceptionDesk(floorRoot.transform, new Vector3(0, floorY, 0f), darkWoodMat, goldFrameMat, floorMat);
                    CreateParentsNPCs(floorRoot.transform, new Vector3(-3.0f, floorY, 0f), darkWoodMat, ladyMat, gm);
                    CreateFlawlessEntrancePortalPrecise(floorRoot.transform, floorY, -halfW, wallThickness, corridorHeight, darkWoodMat, goldFrameMat, glassMat, resonanceSignMat, gm);
                }

                // ==================== 7. 메인 전시장 작품 & 모든 층 복도 지그재그 액자 ====================
                if (floorNumber == 10)
                {
                    CreateGrandResonanceOriginalMonument(floorRoot.transform, new Vector3(0, floorY, 0), goldFrameMat, darkWoodMat, floorMat, resonanceSignMat, gm);
                    Build10FloorWallPaintingsOnly(floorRoot.transform, floorY, halfW, halfL, ladyMat, maryMat, garryMat, goldFrameMat);
                }
                else
                {
                    // 모든 층 계단 도착 복도에 지그재그 액자 4개씩 배치 (9층에는 밤 오버라이드 4대 대사 적용!)
                    BuildFloorHallwayPaintings(floorRoot.transform, floorY, floorNumber, isEvenFloor,
                        guideMat1, guideMat2, guideMat3, guideMat4, ladyMat, maryMat, garryMat, goldFrameMat);

                    BuildGrandHallArtworks(floorRoot.transform, floorY, corridorHeight, halfW, halfL,
                        ladyMat, ladyBleedMat, maryMat, garryMat, graffitiMat, goldFrameMat, darkWoodMat, am, floorNumber);
                }
            }

            // 8. UI Canvas 생성 (자막 + 장미 3송이 라이프 HUD)
            SetupStoryMuseumCanvas(rootGo, gm);

            Selection.activeGameObject = rootGo;

            EditorUtility.DisplayDialog("10층 미술관 구축 완료!",
                "1층 부모님 프롤로그, 10층 동상 공명 컷씬(밤 전환), 3송이 장미 라이프 시스템 및 9층 4대 가이드 액자가 완벽히 구축되었습니다!\n\n" +
                "★ 게임 진행 순서:\n" +
                "1. [1층 프롤로그]: 안내데스크 앞 부모님과 대화 후 10층까지 자유롭게 관람하며 상승\n" +
                "2. [10층 컷씬]: 동상 접근 시 링들이 점점 빠르게 회전하다 사라지며 밤으로 전환 (장미 3송이 UI 활성화)\n" +
                "3. [9층 4대 액자]: 계단 착지 복도에서 규칙을 알려주는 4개의 신비로운 액자 마주침\n" +
                "4. [장미 라이프]: 오답 시 장미 1개 차감되며 해당 층에서 재도전, 3개 모두 소진 시 1층 부모님 앞으로 완전 리셋\n\n" +
                "지금 유니티 상단의 [▶ Play] 버튼을 눌러 플레이해보세요!",
                "확인");
        }

        private static void CreateParentsNPCs(Transform parent, Vector3 pos, Material darkMat, Material dressMat, IbGameManager gm)
        {
            GameObject parentsGroup = new GameObject("Parents_NPCs");
            parentsGroup.transform.SetParent(parent);
            parentsGroup.transform.position = pos;

            if (gm != null)
            {
                gm.prologueParentsTarget = parentsGroup.transform;
            }

            // 아빠 NPC
            GameObject dad = new GameObject("Father_NPC");
            dad.transform.SetParent(parentsGroup.transform);
            dad.transform.localPosition = new Vector3(0, 0, 1.2f);
            dad.transform.localRotation = Quaternion.Euler(0, 90f, 0);

            GameObject dadBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dadBody.name = "Body";
            dadBody.transform.SetParent(dad.transform);
            dadBody.transform.localPosition = new Vector3(0, 0.9f, 0);
            dadBody.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
            dadBody.GetComponent<MeshRenderer>().material = darkMat;

            GameObject dadHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dadHead.name = "Head";
            dadHead.transform.SetParent(dad.transform);
            dadHead.transform.localPosition = new Vector3(0, 1.95f, 0);
            dadHead.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
            dadHead.GetComponent<MeshRenderer>().material = darkMat;

            // 엄마 NPC
            GameObject mom = new GameObject("Mother_NPC");
            mom.transform.SetParent(parentsGroup.transform);
            mom.transform.localPosition = new Vector3(0, 0, -1.2f);
            mom.transform.localRotation = Quaternion.Euler(0, 90f, 0);

            GameObject momBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            momBody.name = "Body";
            momBody.transform.SetParent(mom.transform);
            momBody.transform.localPosition = new Vector3(0, 0.8f, 0);
            momBody.transform.localScale = new Vector3(0.45f, 0.8f, 0.38f);
            momBody.GetComponent<MeshRenderer>().material = dressMat;

            GameObject momHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            momHead.name = "Head";
            momHead.transform.SetParent(mom.transform);
            momHead.transform.localPosition = new Vector3(0, 1.8f, 0);
            momHead.transform.localScale = new Vector3(0.32f, 0.38f, 0.32f);
            momHead.GetComponent<MeshRenderer>().material = dressMat;
        }

        private static void BuildFloorHallwayPaintings(Transform parent, float floorY, int floorNumber, bool isEvenFloor,
            Material guideM1, Material guideM2, Material guideM3, Material guideM4, Material fallbackM1, Material fallbackM2, Material fallbackM3, Material frameMat)
        {
            GameObject guideGroup = new GameObject($"Hallway_Paintings_{floorNumber}F");
            guideGroup.transform.SetParent(parent);

            // 낮에는 모든 층이 아름다운 고전 명화로 렌더링되고, 9층은 밤에 4대 가이드 텍스처로 전환!
            bool is9F = (floorNumber == 9);

            Material m1 = fallbackM1;
            Material m2 = fallbackM2;
            Material m3 = fallbackM3;
            Material m4 = fallbackM1;

            string nTitle1 = is9F ? "Welcome" : "";
            string nDesc1 = is9F ? "\"Welcome to my museum, Lina\"" : "";

            string nTitle2 = is9F ? "Invitation" : "";
            string nDesc2 = is9F ? "\"Let's play together\"" : "";

            string nTitle3 = is9F ? "Rule of Survival" : "";
            string nDesc3 = is9F ? "\"If you see anything different from morning, turn back. Then you'll be safe.\"" : "";

            string nTitle4 = is9F ? "Warning" : "";
            string nDesc4 = is9F ? "\"If you ignore anomalies and proceed... you might regret it.\"" : "";

            if (!isEvenFloor)
            {
                // 홀수층 (북쪽 복도: Z = 19.5m ~ 24.0m)
                // 액자 1: 오른쪽 외벽 (Z = 23.95m, X = 1.0m)
                Vector3 pos1 = new Vector3(1.0f, floorY + 2.5f, 23.92f);
                GameObject h1 = CreateFramedArtwork($"Hallway_1_{floorNumber}F", m1, frameMat, guideGroup.transform, pos1, Quaternion.Euler(0, 180f, 0),
                    is9F ? "Still Life in Gold" : $"Neural Impression {floorNumber}A",
                    "A classic painting mounted along the corridor wall.", nTitle1, nDesc1);
                if (is9F) { var art = h1.GetComponent<IbInteractableArtwork>(); art.dayMaterial = m1; art.nightMaterial = guideM1; }

                // 액자 2: 대각선 왼쪽 분리벽 (Z = 19.55m, X = -3.5m)
                Vector3 pos2 = new Vector3(-3.5f, floorY + 2.5f, 19.58f);
                CreateFramedArtwork($"Hallway_2_{floorNumber}F", m2, frameMat, guideGroup.transform, pos2, Quaternion.Euler(0, 0f, 0),
                    is9F ? "Exhibition Notice II" : $"Neural Impression {floorNumber}B",
                    "A classic painting mounted along the corridor partition.", nTitle2, nDesc2);

                // 액자 3: 다시 오른쪽 외벽 (Z = 23.95m, X = -8.0m)
                Vector3 pos3 = new Vector3(-8.0f, floorY + 2.5f, 23.92f);
                CreateFramedArtwork($"Hallway_3_{floorNumber}F", m3, frameMat, guideGroup.transform, pos3, Quaternion.Euler(0, 180f, 0),
                    is9F ? "Gallery Rule Notice" : $"Neural Impression {floorNumber}C",
                    "A classic painting mounted along the corridor wall.", nTitle3, nDesc3);

                // 액자 4: 다시 왼쪽 분리벽 (Z = 19.55m, X = -12.5m)
                Vector3 pos4 = new Vector3(-12.5f, floorY + 2.5f, 19.58f);
                CreateFramedArtwork($"Hallway_4_{floorNumber}F", m4, frameMat, guideGroup.transform, pos4, Quaternion.Euler(0, 0f, 0),
                    is9F ? "Cautionary Note" : $"Neural Impression {floorNumber}D",
                    "A classic painting mounted along the corridor partition.", nTitle4, nDesc4);
            }
            else
            {
                // 짝수층 (남쪽 복도: Z = -19.5m ~ -24.0m)
                Vector3 pos1 = new Vector3(-1.0f, floorY + 2.5f, -23.92f);
                CreateFramedArtwork($"Hallway_1_{floorNumber}F", m1, frameMat, guideGroup.transform, pos1, Quaternion.Euler(0, 0f, 0),
                    $"Neural Impression {floorNumber}A", "A classic painting mounted along the corridor wall.");

                Vector3 pos2 = new Vector3(3.5f, floorY + 2.5f, -19.58f);
                CreateFramedArtwork($"Hallway_2_{floorNumber}F", m2, frameMat, guideGroup.transform, pos2, Quaternion.Euler(0, 180f, 0),
                    $"Neural Impression {floorNumber}B", "A classic painting mounted along the corridor partition.");

                Vector3 pos3 = new Vector3(8.0f, floorY + 2.5f, -23.92f);
                CreateFramedArtwork($"Hallway_3_{floorNumber}F", m3, frameMat, guideGroup.transform, pos3, Quaternion.Euler(0, 0f, 0),
                    $"Neural Impression {floorNumber}C", "A classic painting mounted along the corridor wall.");

                Vector3 pos4 = new Vector3(12.5f, floorY + 2.5f, -19.58f);
                CreateFramedArtwork($"Hallway_4_{floorNumber}F", m4, frameMat, guideGroup.transform, pos4, Quaternion.Euler(0, 180f, 0),
                    $"Neural Impression {floorNumber}D", "A classic painting mounted along the corridor partition.");
            }
        }

        private static void CreateGrandResonanceOriginalMonument(Transform parent, Vector3 pos, Material brassMat, Material darkWoodMat, Material marbleMat, Material signMat, IbGameManager gm)
        {
            GameObject monument = new GameObject("Grand_RESONANCE_Monument");
            monument.transform.SetParent(parent);
            monument.transform.position = pos;

            if (gm != null)
            {
                gm.monumentTriggerTarget = monument.transform;
            }

            // 동상 E키 상호작용 컴포넌트 추가!
            IbInteractableArtwork monumentInteract = monument.AddComponent<IbInteractableArtwork>();
            monumentInteract.isResonanceMonument = true;
            monumentInteract.artworkTitle = "RESONANCE";
            monumentInteract.author = "Carl Weismann";
            monumentInteract.description = "The central harmonic soul monument of Carl Weismann. It vibrates with an eerie, living energy.";
            monumentInteract.interactPrompt = "[ E ] Touch Monument";

            BoxCollider monCol = monument.AddComponent<BoxCollider>();
            monCol.center = new Vector3(0, 3.0f, 0);
            monCol.size = new Vector3(10.5f, 6.5f, 10.5f);

            // 1. 3단 블랙 마블 좌대 (2배 확대)
            GameObject base1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            base1.name = "Pedestal_Tier1";
            base1.transform.SetParent(monument.transform);
            base1.transform.localPosition = new Vector3(0, 0.45f, 0);
            base1.transform.localScale = new Vector3(10.4f, 0.45f, 10.4f);
            base1.GetComponent<MeshRenderer>().material = darkWoodMat;

            GameObject base2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            base2.name = "Pedestal_Tier2";
            base2.transform.SetParent(monument.transform);
            base2.transform.localPosition = new Vector3(0, 1.15f, 0);
            base2.transform.localScale = new Vector3(8.4f, 0.35f, 8.4f);
            base2.GetComponent<MeshRenderer>().material = marbleMat;

            GameObject base3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            base3.name = "Pedestal_Tier3";
            base3.transform.SetParent(monument.transform);
            base3.transform.localPosition = new Vector3(0, 1.7f, 0);
            base3.transform.localScale = new Vector3(6.4f, 0.25f, 6.4f);
            base3.GetComponent<MeshRenderer>().material = darkWoodMat;

            // 2. 동상 정면 'RESONANCE' 명판 (2배 확대)
            GameObject plateQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plateQuad.name = "Signboard_RESONANCE_Monument";
            plateQuad.transform.SetParent(monument.transform);
            plateQuad.transform.localPosition = new Vector3(0, 1.35f, -4.22f);
            plateQuad.transform.localRotation = Quaternion.Euler(0, 0, 0);
            plateQuad.transform.localScale = new Vector3(3.8f, 0.75f, 1.0f);
            plateQuad.GetComponent<MeshRenderer>().material = signMat;

            // 3. 3중 하모닉 링 (Harmonic Rings - 2배 거대화)
            GameObject ringsRoot = new GameObject("Resonance_Harmonic_Rings");
            ringsRoot.transform.SetParent(monument.transform);
            ringsRoot.transform.localPosition = new Vector3(0, 4.0f, 0);

            if (gm != null)
            {
                gm.monumentRingsRoot = ringsRoot.transform;
            }

            CreateTorusRing(ringsRoot.transform, "OuterRing", Vector3.zero, Quaternion.Euler(25f, 35f, 0), 6.8f, 0.32f, brassMat);
            CreateTorusRing(ringsRoot.transform, "MidRing", Vector3.zero, Quaternion.Euler(-30f, 65f, 20f), 5.2f, 0.26f, brassMat);
            CreateTorusRing(ringsRoot.transform, "InnerRing", Vector3.zero, Quaternion.Euler(70f, -40f, 45f), 3.8f, 0.22f, brassMat);

            // 4. 중앙 창작의 영혼 발광 코어 (2배 거대화)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Creation_Soul_Core";
            core.transform.SetParent(ringsRoot.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

            Material coreMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            coreMat.color = new Color(0.95f, 0.85f, 0.45f);
            if (coreMat.HasProperty("_EmissiveColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissiveColor", new Color(1.0f, 0.85f, 0.4f) * 3.5f);
            }
            core.GetComponent<MeshRenderer>().material = coreMat;

            if (gm != null)
            {
                gm.monumentSoulCore = core;
            }

            // 코어 포인트 조명
            GameObject coreLightGo = new GameObject("CoreGlowLight");
            coreLightGo.transform.SetParent(ringsRoot.transform);
            coreLightGo.transform.localPosition = Vector3.zero;
            Light cLight = coreLightGo.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.range = 8.0f;
            cLight.color = new Color(1.0f, 0.92f, 0.75f);
            cLight.intensity = 40f;
            var hdCLight = coreLightGo.AddComponent<HDAdditionalLightData>();
            hdCLight.intensity = 1500f;

            if (gm != null)
            {
                gm.monumentGlowLight = coreLightGo;
            }
        }

        private static void CreateHalfCylinderVaultSkylight(Transform parent, float ceilingY, float widthX, float lengthZ, float archHeight, Material glassMat, Material ribMat)
        {
            GameObject vaultRoot = new GameObject("Vault_Glass_Skylight_HalfCylinder");
            vaultRoot.transform.SetParent(parent);
            vaultRoot.transform.position = new Vector3(0, ceilingY, 0);

            GameObject archGo = new GameObject("Glass_HalfCylinder_Mesh");
            archGo.transform.SetParent(vaultRoot.transform);
            archGo.transform.localPosition = Vector3.zero;

            MeshFilter mf = archGo.AddComponent<MeshFilter>();
            MeshRenderer mr = archGo.AddComponent<MeshRenderer>();
            mr.material = glassMat;

            Mesh mesh = new Mesh();
            mesh.name = "HalfCylinder_Vault_Mesh";

            int segments = 24;
            float halfW = widthX / 2f;
            float halfL = lengthZ / 2f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int z = 0; z <= 1; z++)
            {
                float zPos = (z == 0) ? -halfL : halfL;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI;
                    float x = -Mathf.Cos(angle) * halfW;
                    float y = Mathf.Sin(angle) * archHeight;

                    verts.Add(new Vector3(x, y, zPos));
                    uvs.Add(new Vector2(i / (float)segments, z));
                }
            }

            int rowSize = segments + 1;
            for (int i = 0; i < segments; i++)
            {
                int a = i;
                int b = i + 1;
                int c = i + rowSize;
                int d = i + 1 + rowSize;

                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }

            int centerIdxFront = verts.Count;
            verts.Add(new Vector3(0, 0, -halfL));
            uvs.Add(new Vector2(0.5f, 0));
            for (int i = 0; i < segments; i++)
            {
                tris.Add(centerIdxFront);
                tris.Add(i + 1);
                tris.Add(i);
                tris.Add(centerIdxFront);
                tris.Add(i);
                tris.Add(i + 1);
            }

            int centerIdxBack = verts.Count;
            verts.Add(new Vector3(0, 0, halfL));
            uvs.Add(new Vector2(0.5f, 1));
            for (int i = 0; i < segments; i++)
            {
                tris.Add(centerIdxBack);
                tris.Add(rowSize + i);
                tris.Add(rowSize + i + 1);
                tris.Add(centerIdxBack);
                tris.Add(rowSize + i + 1);
                tris.Add(rowSize + i);
            }

            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;

            // ★ 우아한 황동 아치 리브 프레임 (금테) 5개 복원!
            int ribCount = 5;
            for (int r = 0; r < ribCount; r++)
            {
                float zP = Mathf.Lerp(-halfL, halfL, r / (float)(ribCount - 1));
                GameObject ribGo = new GameObject($"Vault_GoldRib_{r + 1}");
                ribGo.transform.SetParent(vaultRoot.transform);
                ribGo.transform.localPosition = new Vector3(0, 0, zP);

                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (i / (float)segments) * Mathf.PI;
                    float angle2 = ((i + 1) / (float)segments) * Mathf.PI;

                    Vector3 p1 = new Vector3(-Mathf.Cos(angle1) * halfW, Mathf.Sin(angle1) * archHeight, 0);
                    Vector3 p2 = new Vector3(-Mathf.Cos(angle2) * halfW, Mathf.Sin(angle2) * archHeight, 0);

                    Vector3 mid = (p1 + p2) / 2f;
                    Vector3 dir = p2 - p1;

                    GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    seg.name = $"RibSeg_{i}";
                    seg.transform.SetParent(ribGo.transform);
                    seg.transform.localPosition = mid;
                    seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    seg.transform.localScale = new Vector3(0.18f, dir.magnitude / 2f, 0.18f);
                    seg.GetComponent<MeshRenderer>().material = ribMat;
                }
            }

            // 개구부 하단 황동 테두리 몰딩 (금테 베이스)
            GameObject baseNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseNorth.name = "GoldBase_North";
            baseNorth.transform.SetParent(vaultRoot.transform);
            baseNorth.transform.localPosition = new Vector3(0, 0.1f, halfL);
            baseNorth.transform.localScale = new Vector3(widthX + 0.5f, 0.25f, 0.35f);
            baseNorth.GetComponent<MeshRenderer>().material = ribMat;

            GameObject baseSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseSouth.name = "GoldBase_South";
            baseSouth.transform.SetParent(vaultRoot.transform);
            baseSouth.transform.localPosition = new Vector3(0, 0.1f, -halfL);
            baseSouth.transform.localScale = new Vector3(widthX + 0.5f, 0.25f, 0.35f);
            baseSouth.GetComponent<MeshRenderer>().material = ribMat;

            GameObject baseWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseWest.name = "GoldBase_West";
            baseWest.transform.SetParent(vaultRoot.transform);
            baseWest.transform.localPosition = new Vector3(-halfW, 0.1f, 0);
            baseWest.transform.localScale = new Vector3(0.35f, 0.25f, lengthZ);
            baseWest.GetComponent<MeshRenderer>().material = ribMat;

            GameObject baseEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseEast.name = "GoldBase_East";
            baseEast.transform.SetParent(vaultRoot.transform);
            baseEast.transform.localPosition = new Vector3(halfW, 0.1f, 0);
            baseEast.transform.localScale = new Vector3(0.35f, 0.25f, lengthZ);
            baseEast.GetComponent<MeshRenderer>().material = ribMat;
        }

        private static void CreateRefinedStanchionRopeFence(Transform parent, float floorY, Vector3 startPt, Vector3 bendPt, Vector3 endPt, Material brassMat, Material darkMat, Material ropeMat)
        {
            GameObject fenceRoot = new GameObject("Stanchion_Rope_Barrier_Refined");
            fenceRoot.transform.SetParent(parent);

            List<Vector3> postPositions = new List<Vector3>();
            postPositions.Add(startPt);
            postPositions.Add(Vector3.Lerp(startPt, bendPt, 0.5f));
            postPositions.Add(bendPt);
            postPositions.Add(endPt);

            for (int i = 0; i < postPositions.Count; i++)
            {
                CreateSingleStanchionPost(fenceRoot.transform, postPositions[i], brassMat, darkMat);

                if (i < postPositions.Count - 1)
                {
                    CreateSaggingRope(fenceRoot.transform, postPositions[i], postPositions[i + 1], ropeMat);
                }
            }
        }

        private static void CreateSingleStanchionPost(Transform parent, Vector3 pos, Material brassMat, Material darkMat)
        {
            GameObject post = new GameObject("Stanchion_Post");
            post.transform.SetParent(parent);
            post.transform.position = pos;

            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "BrassBase";
            basePlate.transform.SetParent(post.transform);
            basePlate.transform.localPosition = new Vector3(0, 0.03f, 0);
            basePlate.transform.localScale = new Vector3(0.55f, 0.03f, 0.55f);
            basePlate.GetComponent<MeshRenderer>().material = brassMat;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "MainPole";
            pole.transform.SetParent(post.transform);
            pole.transform.localPosition = new Vector3(0, 0.55f, 0);
            pole.transform.localScale = new Vector3(0.09f, 0.5f, 0.09f);
            pole.GetComponent<MeshRenderer>().material = darkMat;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "BrassCap";
            cap.transform.SetParent(post.transform);
            cap.transform.localPosition = new Vector3(0, 1.08f, 0);
            cap.transform.localScale = new Vector3(0.18f, 0.16f, 0.18f);
            cap.GetComponent<MeshRenderer>().material = brassMat;
        }

        private static void CreateSaggingRope(Transform parent, Vector3 pA, Vector3 pB, Material ropeMat)
        {
            GameObject rope = new GameObject("Velvet_Rope");
            rope.transform.SetParent(parent);

            Vector3 mid = (pA + pB) / 2f;
            mid.y = pA.y + 0.82f;

            Vector3 topA = pA + new Vector3(0, 0.98f, 0);
            Vector3 topB = pB + new Vector3(0, 0.98f, 0);

            CreateRopeSegment(rope.transform, topA, mid, ropeMat);
            CreateRopeSegment(rope.transform, mid, topB, ropeMat);
        }

        private static void CreateRopeSegment(Transform parent, Vector3 start, Vector3 end, Material ropeMat)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = "RopeSegment";
            seg.transform.SetParent(parent);

            Vector3 dir = end - start;
            float dist = dir.magnitude;
            seg.transform.position = start + dir / 2f;
            seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            seg.transform.localScale = new Vector3(0.06f, dist / 2f, 0.06f);
            seg.GetComponent<MeshRenderer>().material = ropeMat;
        }

        private static void CreateTorusRing(Transform parent, string name, Vector3 pos, Quaternion rot, float radius, float thickness, Material mat)
        {
            GameObject ring = new GameObject(name);
            ring.transform.SetParent(parent);
            ring.transform.localPosition = pos;
            ring.transform.localRotation = rot;

            int segs = 20;
            float angleDelta = 360f / segs;
            for (int i = 0; i < segs; i++)
            {
                float angle = i * angleDelta;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(rad) * radius / 2f, 0, Mathf.Cos(rad) * radius / 2f);

                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                piece.name = $"RingSeg_{i}";
                piece.transform.SetParent(ring.transform);
                piece.transform.localPosition = p;
                piece.transform.localRotation = Quaternion.Euler(0, angle + 90f, 90f);
                float segLen = (2f * Mathf.PI * (radius / 2f)) / segs * 1.1f;
                piece.transform.localScale = new Vector3(thickness, segLen / 2f, thickness);
                piece.GetComponent<MeshRenderer>().material = mat;
            }
        }

        private static void Build10FloorWallPaintingsOnly(Transform floorParent, float floorY, float halfW, float halfL, Material ladyMat, Material maryMat, Material garryMat, Material goldFrameMat)
        {
            GameObject artRoot = new GameObject("Artworks_10F_Paintings");
            artRoot.transform.SetParent(floorParent);

            Vector3 posLady = new Vector3(-halfW + 0.05f, floorY + 2.6f, 4.0f);
            CreateFramedArtwork("LadyPortrait_10F", ladyMat, goldFrameMat, artRoot.transform, posLady, Quaternion.Euler(0, 90f, 0), "Woman of Abyss");

            Vector3 posMary = new Vector3(halfW - 0.05f, floorY + 2.6f, -4.0f);
            CreateFramedArtwork("MaryPortrait_10F", maryMat, goldFrameMat, artRoot.transform, posMary, Quaternion.Euler(0, -90f, 0), "Smiling Mary");

            Vector3 posGarry = new Vector3(0f, floorY + 2.6f, halfL - 0.05f);
            CreateFramedArtwork("GarryPortrait_10F", garryMat, goldFrameMat, artRoot.transform, posGarry, Quaternion.Euler(0, 180f, 0), "The Watcher");

            Vector3 posSouth = new Vector3(0f, floorY + 2.6f, -halfL + 0.05f);
            CreateFramedArtwork("ResonanceIntro_10F", ladyMat, goldFrameMat, artRoot.transform, posSouth, Quaternion.Euler(0, 0f, 0), "Echo of Memory");
        }

        private static Material CreateVelvetRopeMaterial()
        {
            Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = "Mat_VelvetBarrierRope";
            mat.color = new Color(0.65f, 0.12f, 0.15f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            return mat;
        }

        private static void CreateFourWalls(Transform parent, float floorY, float corridorHeight, float slabThickness, float halfW, float halfL, float wallThickness, Material mat, bool is1F)
        {
            float totalH = corridorHeight + slabThickness;
            float totalW = halfW * 2f;
            float totalL = halfL * 2f;

            if (!is1F)
            {
                CreateBox(parent, "OuterWall_West", new Vector3(-halfW - (wallThickness / 2f), floorY + (corridorHeight / 2f), 0), new Vector3(wallThickness, totalH, totalL + (wallThickness * 2f)), mat);
            }
            else
            {
                float doorOpeningWidth = 7.6f;
                float sideWallZLen = (totalL - doorOpeningWidth) / 2f;
                CreateBox(parent, "OuterWall_West_North", new Vector3(-halfW - (wallThickness / 2f), floorY + (corridorHeight / 2f), (totalL / 4f) + (doorOpeningWidth / 4f)), new Vector3(wallThickness, totalH, sideWallZLen), mat);
                CreateBox(parent, "OuterWall_West_South", new Vector3(-halfW - (wallThickness / 2f), floorY + (corridorHeight / 2f), -(totalL / 4f) - (doorOpeningWidth / 4f)), new Vector3(wallThickness, totalH, sideWallZLen), mat);
            }

            CreateBox(parent, "OuterWall_East", new Vector3(halfW + (wallThickness / 2f), floorY + (corridorHeight / 2f), 0), new Vector3(wallThickness, totalH, totalL + (wallThickness * 2f)), mat);
            CreateBox(parent, "OuterWall_North", new Vector3(0, floorY + (corridorHeight / 2f), halfL + (wallThickness / 2f)), new Vector3(totalW, totalH, wallThickness), mat);
            CreateBox(parent, "OuterWall_South", new Vector3(0, floorY + (corridorHeight / 2f), -halfL - (wallThickness / 2f)), new Vector3(totalW, totalH, wallThickness), mat);
        }

        private static void CreateStraightWoodStaircase(Transform parent, float floorY, float floorHeight, Vector3 topPos, Vector3 bottomPos, float width, int stepCount, Material woodMat)
        {
            GameObject stairRoot = new GameObject("Antique_Wood_Stairs");
            stairRoot.transform.SetParent(parent);

            Vector3 delta = (bottomPos - topPos) / stepCount;
            float stepLength = Mathf.Abs(delta.x) * 1.15f;
            float stepHeight = floorHeight / stepCount;

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 stepPos = topPos + delta * (i + 0.5f);
                stepPos.y = floorY - (i + 0.5f) * stepHeight;

                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i + 1}";
                step.transform.SetParent(stairRoot.transform);
                step.transform.position = stepPos;
                step.transform.localScale = new Vector3(stepLength, stepHeight, width);
                step.GetComponent<MeshRenderer>().material = woodMat;
            }
        }

        private static void CreateFlawlessEntrancePortalPrecise(Transform parent, float floorY, float wallX, float wallThick, float corridorHeight, Material woodMat, Material goldMat, Material glassMat, Material signMat, IbGameManager gm)
        {
            GameObject portalRoot = new GameObject("Grand_Entrance_Wood_Portal");
            portalRoot.transform.SetParent(parent);
            portalRoot.transform.position = new Vector3(wallX, floorY, 0);
            portalRoot.transform.rotation = Quaternion.identity;

            float doorwayWidth = 7.6f;
            float totalHeight = corridorHeight + 0.8f; // 7.8m (천장 슬래브까지 전체 포털 높이 상향!)
            float doorHeight = 5.2f;                   // 유리문 높이 5.2m
            float headerHeight = totalHeight - doorHeight; // 우드 헤더 높이 2.6m
            float casingDepth = wallThick + 0.32f;

            // 좌측 기둥 (천장 슬래브까지 상향)
            GameObject leftCasing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftCasing.name = "WoodCasing_North";
            leftCasing.transform.SetParent(portalRoot.transform);
            leftCasing.transform.localPosition = new Vector3(0, totalHeight / 2f, doorwayWidth / 2f);
            leftCasing.transform.localScale = new Vector3(casingDepth, totalHeight, 0.55f);
            leftCasing.GetComponent<MeshRenderer>().material = woodMat;

            // 우측 기둥 (천장 슬래브까지 상향)
            GameObject rightCasing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightCasing.name = "WoodCasing_South";
            rightCasing.transform.SetParent(portalRoot.transform);
            rightCasing.transform.localPosition = new Vector3(0, totalHeight / 2f, -doorwayWidth / 2f);
            rightCasing.transform.localScale = new Vector3(casingDepth, totalHeight, 0.55f);
            rightCasing.GetComponent<MeshRenderer>().material = woodMat;

            // 상단 대형 우드 헤더 (천장 슬래브까지 빈틈없이 꽉 참)
            GameObject topHeader = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topHeader.name = "WoodCasing_TopHeader_Filled";
            topHeader.transform.SetParent(portalRoot.transform);
            topHeader.transform.localPosition = new Vector3(0, doorHeight + (headerHeight / 2f), 0);
            topHeader.transform.localScale = new Vector3(casingDepth, headerHeight, doorwayWidth + 0.6f);
            topHeader.GetComponent<MeshRenderer>().material = woodMat;

            // 정문 상단 황동 골드 트림 몰딩
            GameObject topTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topTrim.name = "WoodCasing_TopTrim_Gold";
            topTrim.transform.SetParent(portalRoot.transform);
            topTrim.transform.localPosition = new Vector3(0, totalHeight + 0.05f, 0);
            topTrim.transform.localScale = new Vector3(casingDepth + 0.15f, 0.12f, doorwayWidth + 0.9f);
            topTrim.GetComponent<MeshRenderer>().material = goldMat;

            // ★ WeismannGallery_GrandSignboard 프리팹을 상단 우드 헤더 정중앙에 시원하게 장착!
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/modeling/WeismannGallery_GrandSignboard.prefab");
            if (prefabAsset != null)
            {
                GameObject signboardInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, portalRoot.transform);
                signboardInstance.name = "WeismannGallery_GrandSignboard";
                signboardInstance.transform.localPosition = new Vector3(-casingDepth / 2f - 0.05f, doorHeight + (headerHeight / 2f), 0);
                signboardInstance.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                signboardInstance.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
            }
            else
            {
                Material weismannMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/modeling/Mat_WeismannGallery_Signboard.mat") ?? signMat;
                GameObject fallbackSign = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fallbackSign.name = "Signboard_WEISMANN_GALLERY";
                fallbackSign.transform.SetParent(portalRoot.transform);
                fallbackSign.transform.localPosition = new Vector3(-casingDepth / 2f - 0.05f, doorHeight + (headerHeight / 2f), 0);
                fallbackSign.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                fallbackSign.transform.localScale = new Vector3(4.8f, 1.2f, 1.0f);
                fallbackSign.GetComponent<MeshRenderer>().material = weismannMat;
            }

            // 정문 투명 통유리 도어 (실내가 맑고 시원하게 훤히 들여다보임)
            GameObject glassDoorNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorNorth.name = "GlassDoor_North";
            glassDoorNorth.transform.SetParent(portalRoot.transform);
            glassDoorNorth.transform.localPosition = new Vector3(0, doorHeight / 2f, 1.88f);
            glassDoorNorth.transform.localScale = new Vector3(0.04f, doorHeight, 3.75f);
            glassDoorNorth.GetComponent<MeshRenderer>().material = glassMat;

            GameObject glassDoorSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorSouth.name = "GlassDoor_South";
            glassDoorSouth.transform.SetParent(portalRoot.transform);
            glassDoorSouth.transform.localPosition = new Vector3(0, doorHeight / 2f, -1.88f);
            glassDoorSouth.transform.localScale = new Vector3(0.04f, doorHeight, 3.75f);
            glassDoorSouth.GetComponent<MeshRenderer>().material = glassMat;

            GameObject exitSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitSign.name = "ExitSign_Green";
            exitSign.transform.SetParent(portalRoot.transform);
            exitSign.transform.localPosition = new Vector3(casingDepth / 2f + 0.02f, doorHeight - 0.35f, 0);
            exitSign.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            exitSign.transform.localScale = new Vector3(1.8f, 0.5f, 0.1f);

            Material exitMat = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
            exitMat.color = new Color(0.1f, 0.9f, 0.2f);
            if (exitMat.HasProperty("_EmissiveColor"))
            {
                exitMat.EnableKeyword("_EMISSION");
                exitMat.SetColor("_EmissiveColor", new Color(0.1f, 0.9f, 0.2f) * 4f);
            }
            exitSign.GetComponent<MeshRenderer>().material = exitMat;

            GameObject winTrigGo = new GameObject("Escape_Win_Trigger");
            winTrigGo.transform.SetParent(portalRoot.transform);
            winTrigGo.transform.localPosition = new Vector3(-2.5f, 2.0f, 0);
            BoxCollider winBox = winTrigGo.AddComponent<BoxCollider>();
            winBox.isTrigger = true;
            winBox.size = new Vector3(4f, 5f, 8f);
            IbCircularTrigger winTrig = winTrigGo.AddComponent<IbCircularTrigger>();
            winTrig.triggerType = FloorTriggerType.StairsDown;
        }

        private static void BuildGrandHallArtworks(Transform floorParent, float floorY, float corridorHeight, float halfW, float halfL,
            Material ladyMat, Material ladyBleedMat, Material maryMat, Material garryMat, Material graffitiMat, Material goldFrameMat, Material darkWoodMat,
            IbAnomalyManager am, int floorNumber)
        {
            GameObject artRoot = new GameObject($"Artworks_{floorNumber}F");
            artRoot.transform.SetParent(floorParent);

            GameObject roseGroupGo = CreateRoseExhibit("RoseExhibit", artRoot.transform, new Vector3(0, floorY, 0), darkWoodMat);
            WitheredRoseAnomaly wra = roseGroupGo.GetComponent<WitheredRoseAnomaly>();

            GameObject statueGo = CreateSculpture("GuertenaStatue", artRoot.transform, new Vector3(0, floorY, 10f), Quaternion.Euler(0, 180f, 0), darkWoodMat, ladyMat);
            RotatingStatueAnomaly rsa = statueGo.AddComponent<RotatingStatueAnomaly>();
            rsa.statueTransform = statueGo.transform.Find("StatueBody");

            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Antique_Bench";
            bench.transform.SetParent(artRoot.transform);
            bench.transform.position = new Vector3(0, floorY + 0.45f, -10f);
            bench.transform.localScale = new Vector3(6f, 0.45f, 1.6f);
            bench.GetComponent<MeshRenderer>().material = darkWoodMat;

            Vector3 posLady = new Vector3(-halfW + 0.05f, floorY + 2.6f, 8.0f);
            GameObject bleedLadyGo = CreateFramedArtwork("BleedingLady", ladyMat, goldFrameMat, artRoot.transform, posLady, Quaternion.Euler(0, 90f, 0), "Woman of Abyss");
            BleedingPaintingAnomaly bpa = bleedLadyGo.AddComponent<BleedingPaintingAnomaly>();
            bpa.paintingRenderer = bleedLadyGo.transform.Find("Canvas").GetComponent<MeshRenderer>();
            bpa.normalMaterial = ladyMat;
            bpa.bleedingMaterial = ladyBleedMat;

            Vector3 posGarry = new Vector3(0f, floorY + 2.6f, 18.85f);
            GameObject gazingGo = CreateFramedArtwork("GazingEyes", garryMat, goldFrameMat, artRoot.transform, posGarry, Quaternion.Euler(0, 180f, 0), "The Watcher");
            GazingPaintingAnomaly gpa = gazingGo.AddComponent<GazingPaintingAnomaly>();
            gpa.eyeOrHeadTransform = gazingGo.transform.Find("Canvas");

            Vector3 posMary = new Vector3(halfW - 0.05f, floorY + 2.6f, 0f);
            GameObject maryFrameGo = CreateFramedArtwork("FlippedMary", maryMat, goldFrameMat, artRoot.transform, posMary, Quaternion.Euler(0, -90f, 0), "Smiling Mary");
            FlippedFrameAnomaly ffa = maryFrameGo.AddComponent<FlippedFrameAnomaly>();
            ffa.frameTransform = maryFrameGo.transform;

            Vector3 posGraffiti = new Vector3(0f, floorY + 2.6f, -18.85f);
            GameObject graffitiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            graffitiQuad.name = "RedGraffiti";
            graffitiQuad.transform.SetParent(artRoot.transform);
            graffitiQuad.transform.position = posGraffiti;
            graffitiQuad.transform.rotation = Quaternion.Euler(0, 0f, 0);
            graffitiQuad.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
            graffitiQuad.GetComponent<MeshRenderer>().material = graffitiMat;
            graffitiQuad.SetActive(false);

            WallGraffitiAnomaly wga = artRoot.AddComponent<WallGraffitiAnomaly>();
            wga.graffitiObjects = new GameObject[] { graffitiQuad };

            GameObject lightsParent = new GameObject("FloorLights");
            lightsParent.transform.SetParent(artRoot.transform);
            RedLightsAnomaly rla = lightsParent.AddComponent<RedLightsAnomaly>();

            Vector3[] lightPositions = new Vector3[]
            {
                new Vector3(0f, floorY + corridorHeight - 0.4f, 21.75f),
                new Vector3(0f, floorY + corridorHeight - 0.4f, -21.75f),
                new Vector3(-10f, floorY + corridorHeight - 0.4f, 10f),
                new Vector3(0f, floorY + corridorHeight - 0.4f, 10f),
                new Vector3(10f, floorY + corridorHeight - 0.4f, 10f),
                new Vector3(0f, floorY + corridorHeight - 0.4f, 0f),
                new Vector3(-10f, floorY + corridorHeight - 0.4f, -10f),
                new Vector3(0f, floorY + corridorHeight - 0.4f, -10f),
                new Vector3(10f, floorY + corridorHeight - 0.4f, -10f)
            };

            for (int k = 0; k < lightPositions.Length; k++)
            {
                GameObject spotGo = new GameObject($"SpotLight_{k + 1}");
                spotGo.transform.SetParent(lightsParent.transform);
                spotGo.transform.position = lightPositions[k];
                spotGo.transform.rotation = Quaternion.Euler(90f, 0, 0f);

                Light l = spotGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 16f;
                l.spotAngle = 85f;
                l.color = new Color(1f, 0.95f, 0.88f);
                l.intensity = 60f;

                var hdLight = spotGo.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = 2200f;

                rla.targetLights.Add(l);
            }

            am.anomalies.Add(bpa);
            am.anomalies.Add(wra);
            am.anomalies.Add(gpa);
            am.anomalies.Add(rsa);
            am.anomalies.Add(ffa);
            am.anomalies.Add(wga);
            am.anomalies.Add(rla);
        }

        private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = pos;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().material = mat;
        }

        private static void SetupHDRPEnvironmentLighting(GameObject rootGo)
        {
            GameObject sunGo = new GameObject("Sun_DirectionalLight");
            sunGo.transform.SetParent(rootGo.transform);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.92f);
            sunLight.intensity = 3.0f;

            var hdSunData = sunGo.AddComponent<HDAdditionalLightData>();
            hdSunData.intensity = 10000f;

            // GameManager에 Sun Light 등록
            var gm = rootGo.GetComponentInChildren<IbGameManager>();
            if (gm != null) gm.sunDirectionalLight = sunLight;

            GameObject volumeGo = new GameObject("HDRP_GlobalVolume");
            volumeGo.transform.SetParent(rootGo.transform);
            Volume vol = volumeGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "IbMuseum_HDRP_Profile";

            VisualEnvironment visualEnv = profile.Add<VisualEnvironment>();
            visualEnv.skyType.value = 0;

            Exposure exposure = profile.Add<Exposure>();
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.value = 10.5f;

            Tonemapping tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.value = TonemappingMode.ACES;

            Bloom bloom = profile.Add<Bloom>();
            bloom.intensity.value = 0.25f;

            vol.sharedProfile = profile;
        }

        private static void CreateLobbyReceptionDesk(Transform parent, Vector3 pos, Material woodMat, Material goldMat, Material marbleMat)
        {
            GameObject deskRoot = new GameObject("Information_Reception_Desk");
            deskRoot.transform.SetParent(parent);
            deskRoot.transform.position = pos;

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            counter.name = "CounterBody";
            counter.transform.SetParent(deskRoot.transform);
            counter.transform.localPosition = new Vector3(0, 0.55f, 0);
            counter.transform.localScale = new Vector3(4.5f, 0.55f, 2.4f);
            counter.GetComponent<MeshRenderer>().material = woodMat;

            GameObject topPlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topPlate.name = "MarbleTop";
            topPlate.transform.SetParent(deskRoot.transform);
            topPlate.transform.localPosition = new Vector3(0, 1.15f, 0);
            topPlate.transform.localScale = new Vector3(4.8f, 0.08f, 2.7f);
            topPlate.GetComponent<MeshRenderer>().material = marbleMat;

            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "InfoSign";
            sign.transform.SetParent(deskRoot.transform);
            sign.transform.localPosition = new Vector3(0, 1.45f, -1.15f);
            sign.transform.localScale = new Vector3(2.2f, 0.4f, 0.06f);
            sign.GetComponent<MeshRenderer>().material = goldMat;

            // 안내데스크 상호작용
            IbInteractableArtwork deskInteract = deskRoot.AddComponent<IbInteractableArtwork>();
            deskInteract.artworkTitle = "Information Reception Desk";
            deskInteract.author = "Weismann Gallery Staff";
            deskInteract.description = "The main information reception desk. Maps of the 10-floor gallery and brochures about Carl Weismann's neural art are neatly displayed.";
            deskInteract.interactPrompt = "[ E ] Inspect Desk";
            BoxCollider deskCol = deskRoot.AddComponent<BoxCollider>();
            deskCol.center = new Vector3(0, 1.0f, 0);
            deskCol.size = new Vector3(4.8f, 2.0f, 2.8f);
        }

        private static GameObject CreateFramedArtwork(string name, Material paintingMat, Material frameMat, Transform parent, Vector3 pos, Quaternion rot, string title, string desc = "", string nightTitle = "", string nightDesc = "")
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.rotation = rot;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "FrameBorder";
            frame.transform.SetParent(root.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localRotation = Quaternion.identity;
            frame.transform.localScale = new Vector3(2.2f, 3.2f, 0.06f);
            frame.GetComponent<MeshRenderer>().material = frameMat;

            GameObject canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvas.name = "Canvas";
            canvas.transform.SetParent(root.transform);
            canvas.transform.localPosition = new Vector3(0, 0, -0.035f);
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = new Vector3(2.0f, 3.0f, 1f);
            canvas.GetComponent<MeshRenderer>().material = paintingMat;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "CaptionPlate";
            plate.transform.SetParent(root.transform);
            plate.transform.localPosition = new Vector3(0, -1.85f, -0.02f);
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = new Vector3(1.2f, 0.35f, 0.03f);
            plate.GetComponent<MeshRenderer>().material = frameMat;

            // 액자 E키 상호작용 컴포넌트 & 콜라이더 추가!
            IbInteractableArtwork interact = root.AddComponent<IbInteractableArtwork>();
            interact.artworkTitle = title;
            interact.author = "Carl Weismann";
            interact.description = string.IsNullOrEmpty(desc) ? $"An enigmatic painting by Carl Weismann titled '{title}'. The strokes hold an uncanny, living presence." : desc;
            interact.nightTitle = nightTitle;
            interact.nightDescription = nightDesc;
            interact.interactPrompt = "[ E ] Inspect Artwork";

            BoxCollider rootCol = root.AddComponent<BoxCollider>();
            rootCol.center = new Vector3(0, 0, 0);
            rootCol.size = new Vector3(2.4f, 3.4f, 0.4f);

            return root;
        }

        private static GameObject CreateSculpture(string name, Transform parent, Vector3 pos, Quaternion rot, Material baseMat, Material statueMat)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.rotation = rot;

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform);
            pedestal.transform.localPosition = new Vector3(0, 0.6f, 0);
            pedestal.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
            pedestal.GetComponent<MeshRenderer>().material = baseMat;

            GameObject body = new GameObject("StatueBody");
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 1.2f, 0);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torso.name = "Torso";
            torso.transform.SetParent(body.transform);
            torso.transform.localPosition = new Vector3(0, 0.7f, 0);
            torso.transform.localScale = new Vector3(0.75f, 0.7f, 0.75f);
            torso.GetComponent<MeshRenderer>().material = statueMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(body.transform);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.65f, 0.75f, 0.65f);
            head.GetComponent<MeshRenderer>().material = statueMat;

            // 조각상 상호작용 컴포넌트
            IbInteractableArtwork interact = root.AddComponent<IbInteractableArtwork>();
            interact.artworkTitle = "Contemplative Bust";
            interact.author = "Carl Weismann";
            interact.description = "A sculpted stone bust of an introspective thinker. Looking closely, its head seems as though it might turn at any moment.";
            interact.interactPrompt = "[ E ] Inspect Sculpture";

            BoxCollider rootCol = root.AddComponent<BoxCollider>();
            rootCol.center = new Vector3(0, 1.5f, 0);
            rootCol.size = new Vector3(1.6f, 3.2f, 1.6f);

            return root;
        }

        private static GameObject CreateRoseExhibit(string name, Transform parent, Vector3 pos, Material tableMat)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "RoundTable";
            table.transform.SetParent(root.transform);
            table.transform.localPosition = new Vector3(0, 0.55f, 0);
            table.transform.localScale = new Vector3(1.6f, 0.55f, 1.6f);
            table.GetComponent<MeshRenderer>().material = tableMat;

            GameObject vase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vase.name = "Vase";
            vase.transform.SetParent(root.transform);
            vase.transform.localPosition = new Vector3(0, 1.35f, 0);
            vase.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            ApplyColorToMesh(vase, new Color(0.85f, 0.92f, 1f, 0.7f));

            GameObject freshRose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            freshRose.name = "FreshRedRose";
            freshRose.transform.SetParent(root.transform);
            freshRose.transform.localPosition = new Vector3(0, 1.75f, 0);
            freshRose.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
            ApplyColorToMesh(freshRose, new Color(0.95f, 0.05f, 0.05f));

            GameObject witheredRose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            witheredRose.name = "WitheredRose";
            witheredRose.transform.SetParent(root.transform);
            witheredRose.transform.localPosition = new Vector3(0, 1.45f, 0.15f);
            witheredRose.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
            ApplyColorToMesh(witheredRose, new Color(0.12f, 0.04f, 0.04f));
            witheredRose.SetActive(false);

            GameObject petals = GameObject.CreatePrimitive(PrimitiveType.Cube);
            petals.name = "FallenPetals";
            petals.transform.SetParent(root.transform);
            petals.transform.localPosition = new Vector3(0, 1.12f, 0);
            petals.transform.localScale = new Vector3(1.0f, 0.03f, 1.0f);
            ApplyColorToMesh(petals, new Color(0.75f, 0.05f, 0.05f));
            petals.SetActive(false);

            WitheredRoseAnomaly wra = root.AddComponent<WitheredRoseAnomaly>();
            wra.freshRoseObject = freshRose;
            wra.witheredRoseObject = witheredRose;
            wra.fallenPetalsObject = petals;

            // 붉은 장미 전시대 상호작용
            IbInteractableArtwork interact = root.AddComponent<IbInteractableArtwork>();
            interact.artworkTitle = "The Crimson Rose";
            interact.author = "Carl Weismann";
            interact.description = "A vibrant red rose resting inside a glass vase. It emits an ethereal, living fragrance that seems tied to your own soul.";
            interact.interactPrompt = "[ E ] Inspect Rose";

            BoxCollider rootCol = root.AddComponent<BoxCollider>();
            rootCol.center = new Vector3(0, 1.0f, 0);
            rootCol.size = new Vector3(1.8f, 2.2f, 1.8f);

            return root;
        }

        private static Material CreateAntiqueWoodMaterial()
        {
            Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = "Mat_AntiquePolishedWood";
            mat.color = new Color(0.28f, 0.16f, 0.09f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.70f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
            return mat;
        }

        private static Material LoadOrCreateGlassMaterial()
        {
            Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = "Mat_CrystalClearEntranceGlass";
            mat.color = new Color(0.9f, 0.96f, 1.0f, 0.08f);
            mat.SetFloat("_SurfaceType", 1); // Transparent
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.98f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);
            if (mat.HasProperty("_AlphaCutoffEnable")) mat.SetFloat("_AlphaCutoffEnable", 0);
            mat.renderQueue = 3000;
            return mat;
        }

        private static Material LoadOrCreateMaterial(string path, Color fallbackColor, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
                mat = new Material(s);
                mat.color = fallbackColor;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            }
            return mat;
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

        private static Material CreateTransparentMaterial(Texture2D tex, string name)
        {
            Shader s = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = name;
            mat.mainTexture = tex;
            mat.SetFloat("_SurfaceType", 1);
            mat.renderQueue = 3000;
            return mat;
        }

        private static void ApplyColorToMesh(GameObject go, Color col)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null)
            {
                Material m = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));
                m.color = col;
                r.material = m;
            }
        }

        private static void SetupStoryMuseumCanvas(GameObject rootGo, IbGameManager gm)
        {
            GameObject canvasGo = new GameObject("IbMuseum_Canvas");
            canvasGo.transform.SetParent(rootGo.transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            IbMuseumUI ui = canvasGo.AddComponent<IbMuseumUI>();
            gm.uiManager = ui;

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // 1. 하단 대화 및 작품 조사창 (DialogueBox)
            GameObject dlgBoxGo = new GameObject("DialogueBox");
            dlgBoxGo.transform.SetParent(canvasGo.transform);
            RectTransform drt = dlgBoxGo.AddComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, 100f);
            drt.anchorMin = new Vector2(0.5f, 0f);
            drt.anchorMax = new Vector2(0.5f, 0f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(920f, 150f);

            Image dlgBg = dlgBoxGo.AddComponent<Image>();
            dlgBg.color = new Color(0.05f, 0.05f, 0.07f, 0.88f);

            GameObject dlgContentGo = new GameObject("DialogueContentText");
            dlgContentGo.transform.SetParent(dlgBoxGo.transform);
            RectTransform dcrt = dlgContentGo.AddComponent<RectTransform>();
            dcrt.anchorMin = Vector2.zero;
            dcrt.anchorMax = Vector2.one;
            dcrt.offsetMin = new Vector2(30f, 35f);
            dcrt.offsetMax = new Vector2(-30f, -15f);

            TMP_Text dcTmp = dlgContentGo.AddComponent<TextMeshProUGUI>();
            dcTmp.alignment = TextAlignmentOptions.TopLeft;
            dcTmp.fontSize = 22f;
            dcTmp.color = Color.white;
            ui.dialogueBox = dlgBoxGo;
            ui.dialogueContentText = dcTmp;

            GameObject dlgGuideGo = new GameObject("DialogueContinueText");
            dlgGuideGo.transform.SetParent(dlgBoxGo.transform);
            RectTransform dgrt = dlgGuideGo.AddComponent<RectTransform>();
            dgrt.anchoredPosition = new Vector2(-20f, 15f);
            dgrt.anchorMin = new Vector2(1f, 0f);
            dgrt.anchorMax = new Vector2(1f, 0f);
            dgrt.pivot = new Vector2(1f, 0f);
            dgrt.sizeDelta = new Vector2(300f, 30f);

            TMP_Text dgTmp = dlgGuideGo.AddComponent<TextMeshProUGUI>();
            dgTmp.alignment = TextAlignmentOptions.BottomRight;
            dgTmp.fontSize = 17f;
            dgTmp.color = new Color(0.85f, 0.72f, 0.35f);
            dgTmp.text = "Press any key to continue ▾";
            ui.dialogueContinueText = dgTmp;
            dlgBoxGo.SetActive(false);

            // 2. 화면 중앙 [ E ] 상호작용 프롬프트 (InteractPrompt)
            GameObject promptGo = new GameObject("InteractPrompt");
            promptGo.transform.SetParent(canvasGo.transform);
            RectTransform prt = promptGo.AddComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -40f);
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(320f, 50f);

            Image prBg = promptGo.AddComponent<Image>();
            prBg.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject prTxtGo = new GameObject("PromptText");
            prTxtGo.transform.SetParent(promptGo.transform);
            RectTransform prtrt = prTxtGo.AddComponent<RectTransform>();
            prtrt.anchorMin = Vector2.zero;
            prtrt.anchorMax = Vector2.one;
            prtrt.offsetMin = Vector2.zero;
            prtrt.offsetMax = Vector2.zero;

            TMP_Text prTmp = prTxtGo.AddComponent<TextMeshProUGUI>();
            prTmp.alignment = TextAlignmentOptions.Center;
            prTmp.fontSize = 20f;
            prTmp.fontStyle = FontStyles.Bold;
            prTmp.color = new Color(1f, 0.92f, 0.65f);
            prTmp.text = "[ E ] Inspect";
            ui.interactPrompt = promptGo;
            ui.interactPromptText = prTmp;
            promptGo.SetActive(false);

            // 2. 우측 상단 장미 라이프 HUD (Rose Life 3송이)
            GameObject roseContainer = new GameObject("RoseLife_HUD");
            roseContainer.transform.SetParent(canvasGo.transform);
            RectTransform rrt = roseContainer.AddComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(-150f, -60f);
            rrt.anchorMin = new Vector2(1f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(240f, 80f);

            Image roseImg = roseContainer.AddComponent<Image>();
            ui.roseLifeContainer = roseContainer;
            ui.roseLifeImage = roseImg;

            // 장미 스프라이트들 생성 (0개, 1개, 2개, 3개)
            ui.roseLifeSprites = new Sprite[4];
            for (int r = 0; r <= 3; r++)
            {
                Texture2D rTex = IbTextureGenerator.GenerateRoseLifeHUDTexture(r);
                Sprite rSpr = Sprite.Create(rTex, new Rect(0, 0, rTex.width, rTex.height), new Vector2(0.5f, 0.5f));
                ui.roseLifeSprites[r] = rSpr;
            }
            roseImg.sprite = ui.roseLifeSprites[3];
            roseContainer.SetActive(false); // 낮에는 숨김, 밤에 활성화!

            // 3. 화면 암전/글리치 패널
            GameObject blackoutGo = new GameObject("BlackoutPanel");
            blackoutGo.transform.SetParent(canvasGo.transform);
            RectTransform brt = blackoutGo.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            CanvasGroup cg = blackoutGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            ui.blackoutCanvasGroup = cg;

            Image bImg = blackoutGo.AddComponent<Image>();
            bImg.color = Color.black;
            ui.glitchImage = bImg;

            // 4. 엔딩 패널
            GameObject endingGo = new GameObject("EndingPanel");
            endingGo.transform.SetParent(canvasGo.transform);
            RectTransform ert = endingGo.AddComponent<RectTransform>();
            ert.anchorMin = Vector2.zero;
            ert.anchorMax = Vector2.one;
            ert.offsetMin = Vector2.zero;
            ert.offsetMax = Vector2.zero;

            Image endImg = endingGo.AddComponent<Image>();
            endImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            ui.endingPanel = endingGo;

            GameObject endTitleGo = new GameObject("EndingTitle");
            endTitleGo.transform.SetParent(endingGo.transform);
            RectTransform etrt = endTitleGo.AddComponent<RectTransform>();
            etrt.anchoredPosition = new Vector2(0, 70f);
            etrt.anchorMin = new Vector2(0.5f, 0.5f);
            etrt.anchorMax = new Vector2(0.5f, 0.5f);
            etrt.pivot = new Vector2(0.5f, 0.5f);
            etrt.sizeDelta = new Vector2(700f, 100f);

            TMP_Text etTmp = endTitleGo.AddComponent<TextMeshProUGUI>();
            etTmp.alignment = TextAlignmentOptions.Center;
            etTmp.fontSize = 48f;
            etTmp.fontStyle = FontStyles.Bold;
            etTmp.color = new Color(0.9f, 0.8f, 0.5f);
            etTmp.text = "가상세계 미술관 탈출 성공";
            ui.endingTitleText = etTmp;

            GameObject endDescGo = new GameObject("EndingDesc");
            endDescGo.transform.SetParent(endingGo.transform);
            RectTransform edrt = endDescGo.AddComponent<RectTransform>();
            edrt.anchoredPosition = new Vector2(0, -10f);
            edrt.anchorMin = new Vector2(0.5f, 0.5f);
            edrt.anchorMax = new Vector2(0.5f, 0.5f);
            edrt.pivot = new Vector2(0.5f, 0.5f);
            edrt.sizeDelta = new Vector2(650f, 80f);

            TMP_Text edTmp = endDescGo.AddComponent<TextMeshProUGUI>();
            edTmp.alignment = TextAlignmentOptions.Center;
            edTmp.fontSize = 20f;
            edTmp.color = Color.white;
            edTmp.text = "소녀는 모든 기괴한 이상현상을 꿰뚫어보고 1층 출구에서 부모님과 재회했습니다.";
            ui.endingDescText = edTmp;

            GameObject btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(endingGo.transform);
            RectTransform bntrt = btnGo.AddComponent<RectTransform>();
            bntrt.anchoredPosition = new Vector2(0, -90f);
            bntrt.anchorMin = new Vector2(0.5f, 0.5f);
            bntrt.anchorMax = new Vector2(0.5f, 0.5f);
            bntrt.pivot = new Vector2(0.5f, 0.5f);
            bntrt.sizeDelta = new Vector2(240f, 60f);

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.18f, 0.15f);
            Button btn = btnGo.AddComponent<Button>();
            ui.restartButton = btn;

            GameObject btnTxtGo = new GameObject("BtnText");
            btnTxtGo.transform.SetParent(btnGo.transform);
            RectTransform btrt = btnTxtGo.AddComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = Vector2.zero;
            btrt.offsetMax = Vector2.zero;

            TMP_Text bTmp = btnTxtGo.AddComponent<TextMeshProUGUI>();
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.fontSize = 22f;
            bTmp.color = new Color(0.95f, 0.9f, 0.8f);
            bTmp.text = "다시 관람하기 (R)";

            endingGo.SetActive(false);
        }
    }
}
