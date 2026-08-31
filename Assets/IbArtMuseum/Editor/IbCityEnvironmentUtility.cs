using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace IbArtMuseum
{
    public class IbCityEnvironmentUtility : EditorWindow
    {
        [MenuItem("Tools/Ib Museum/Add City & Exterior Trees (도심 건물 및 가로수 추가)", false, 2)]
        [MenuItem("GameObject/Ib Museum/Add City & Exterior Trees (도심 건물 및 가로수 추가)", false, 2)]
        public static void AddCityAndExteriorEnvironment()
        {
            GameObject existingEnv = GameObject.Find("_City_Exterior_Environment");
            if (existingEnv != null)
            {
                DestroyImmediate(existingEnv);
            }

            GameObject envRoot = new GameObject("_City_Exterior_Environment");
            Undo.RegisterCreatedObjectUndo(envRoot, "Create Aligned City Environment");

            // 1. 머티리얼 준비
            Material asphaltMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteRough_Mat.mat", new Color(0.16f, 0.16f, 0.18f), 0.2f, 0.0f);
            Material sidewalkMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteSmooth_Mat.mat", new Color(0.85f, 0.84f, 0.82f), 0.35f, 0.0f);
            Material laneMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteSmooth_Mat.mat", new Color(0.95f, 0.95f, 0.90f), 0.2f, 0.0f);
            
            Material bldgWhiteMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteSmooth_Mat.mat", new Color(0.92f, 0.92f, 0.94f), 0.4f, 0.0f);
            Material bldgDarkMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteTaupe_Mat.mat", new Color(0.22f, 0.24f, 0.26f), 0.5f, 0.2f);
            Material bldgGlassMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/Glass_Mat.mat", new Color(0.12f, 0.25f, 0.35f, 0.75f), 0.95f, 0.8f);

            Material trunkMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteTaupe_Mat.mat", new Color(0.28f, 0.18f, 0.10f), 0.1f, 0.0f);
            Material leavesMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/ConcreteMatteGrey_Mat.mat", new Color(0.18f, 0.52f, 0.20f), 0.15f, 0.0f);
            Material carMat = LoadOrCreateMaterial("Assets/SampleSceneAssets/Materials/General/Brass_Mat.mat", new Color(0.08f, 0.08f, 0.10f), 0.9f, 0.8f);

            // 2. 도시 지면 베이스
            GameObject groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "City_Ground_Base";
            groundGo.transform.SetParent(envRoot.transform);
            groundGo.transform.position = new Vector3(-38f, -0.05f, 0);
            groundGo.transform.localScale = new Vector3(35f, 1f, 35f);
            groundGo.GetComponent<MeshRenderer>().material = sidewalkMat;

            // 3. 가운데 일자 도로 (X = -38m, 폭 16m)
            float roadX = -38f;
            float roadWidth = 16f;
            float roadLength = 260f;

            GameObject roadGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadGo.name = "Main_Straight_Avenue";
            roadGo.transform.SetParent(envRoot.transform);
            roadGo.transform.position = new Vector3(roadX, 0.02f, 0);
            roadGo.transform.localScale = new Vector3(roadWidth, 0.1f, roadLength);
            roadGo.GetComponent<MeshRenderer>().material = asphaltMat;

            // 도로 중앙선
            for (int i = -10; i <= 10; i++)
            {
                GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lane.name = $"LaneDash_{i}";
                lane.transform.SetParent(envRoot.transform);
                lane.transform.position = new Vector3(roadX, 0.08f, i * 12f);
                lane.transform.localScale = new Vector3(0.4f, 0.02f, 6.0f);
                lane.GetComponent<MeshRenderer>().material = laneMat;
            }

            // 도로 양쪽 보도블럭
            float leftSidewalkX = roadX - (roadWidth / 2f) - 3f;  // X = -49m
            float rightSidewalkX = roadX + (roadWidth / 2f) + 3f; // X = -27m

            GameObject leftWalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWalk.name = "Left_Sidewalk";
            leftWalk.transform.SetParent(envRoot.transform);
            leftWalk.transform.position = new Vector3(leftSidewalkX, 0.12f, 0);
            leftWalk.transform.localScale = new Vector3(6f, 0.2f, roadLength);
            leftWalk.GetComponent<MeshRenderer>().material = sidewalkMat;

            GameObject rightWalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWalk.name = "Right_Sidewalk";
            rightWalk.transform.SetParent(envRoot.transform);
            rightWalk.transform.position = new Vector3(rightSidewalkX, 0.12f, 0);
            rightWalk.transform.localScale = new Vector3(6f, 0.2f, roadLength);
            rightWalk.GetComponent<MeshRenderer>().material = sidewalkMat;

            // 4. 건물 5개 정확한 좌우 배치
            // 도로 좌측 라인 (X = -75m): 건물 3개
            float leftBldgX = -75f;
            CreateModernBuilding(envRoot.transform, "Building_Left_1", new Vector3(leftBldgX, 0, 75f), new Vector3(32f, 55f, 36f), bldgWhiteMat, bldgDarkMat, bldgGlassMat);
            CreateModernBuilding(envRoot.transform, "Building_Left_2 (Across)", new Vector3(leftBldgX, 0, 0f), new Vector3(34f, 75f, 38f), bldgDarkMat, bldgWhiteMat, bldgGlassMat);
            CreateModernBuilding(envRoot.transform, "Building_Left_3", new Vector3(leftBldgX, 0, -75f), new Vector3(32f, 60f, 36f), bldgWhiteMat, bldgDarkMat, bldgGlassMat);

            // 도로 우측 라인 (X = 0m): 건물 ➔ 대형 Ib 미술관(가로 44m, 세로 56m) ➔ 건물
            float rightBldgX = 0f;
            CreateModernBuilding(envRoot.transform, "Building_Right_Top", new Vector3(rightBldgX, 0, 75f), new Vector3(38f, 65f, 38f), bldgWhiteMat, bldgDarkMat, bldgGlassMat);
            CreateModernBuilding(envRoot.transform, "Building_Right_Bottom", new Vector3(rightBldgX, 0, -75f), new Vector3(38f, 50f, 38f), bldgDarkMat, bldgWhiteMat, bldgGlassMat);

            // 5. 미술관 주변 & 도로변 가로수 나무 심기
            CreateNeatGardenTreesGrand(envRoot.transform, trunkMat, leavesMat, leftSidewalkX, rightSidewalkX);

            // 6. 도로 위 컷씬용 자동차 (미술관 정문 앞 도로변 정차)
            CreateCinematicCar(envRoot.transform, new Vector3(roadX + 4.5f, 0.1f, -6f), Quaternion.Euler(0, 0f, 0), carMat, bldgGlassMat);

            Selection.activeGameObject = envRoot;

            EditorUtility.DisplayDialog(
                "대형 도심 환경 정렬 완료!",
                "확장된 대형 미술관 규격(44m x 56m)에 맞추어 도심 환경과 도로, 가로수가 완벽하게 정렬되었습니다!\n\n" +
                "★ 배치 구조:\n" +
                "1. [가운데]: 일자 도로 (차선 및 컷씬용 자동차)\n" +
                "2. [좌측 3동]: 건물 1 ➔ 건물 2 ➔ 건물 3\n" +
                "3. [우측 라인]: 건물 4 ➔ [대형 Ib 미술관 타워] ➔ 건물 5\n" +
                "4. [조경]: 대형 미술관 둘레와 도로변 가로수 정원\n\n" +
                "씬 뷰에서 미술관 외부를 둘러보세요!",
                "확인"
            );
        }

        private static void CreateModernBuilding(Transform parent, string name, Vector3 pos, Vector3 size, Material bodyMat, Material trimMat, Material glassMat)
        {
            GameObject bldg = new GameObject(name);
            bldg.transform.SetParent(parent);
            bldg.transform.position = pos;

            GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainBody.name = "Facade_Body";
            mainBody.transform.SetParent(bldg.transform);
            mainBody.transform.localPosition = new Vector3(0, size.y / 2f, 0);
            mainBody.transform.localScale = size;
            mainBody.GetComponent<MeshRenderer>().material = bodyMat;

            GameObject glassPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassPanel.name = "Glass_CurtainWall";
            glassPanel.transform.SetParent(bldg.transform);
            glassPanel.transform.localPosition = new Vector3(0, size.y / 2f, 0);
            glassPanel.transform.localScale = new Vector3(size.x + 0.3f, size.y * 0.85f, size.z * 0.7f);
            glassPanel.GetComponent<MeshRenderer>().material = glassMat;

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Rooftop_Structure";
            roof.transform.SetParent(bldg.transform);
            roof.transform.localPosition = new Vector3(0, size.y + 2.5f, 0);
            roof.transform.localScale = new Vector3(size.x * 0.7f, 5.0f, size.z * 0.7f);
            roof.GetComponent<MeshRenderer>().material = trimMat;
        }

        private static void CreateNeatGardenTreesGrand(Transform parent, Material trunkMat, Material leavesMat, float leftWalkX, float rightWalkX)
        {
            GameObject treesRoot = new GameObject("Trees_And_Landscape");
            treesRoot.transform.SetParent(parent);

            // 1) 좌측 보도블럭 가로수
            for (int i = -3; i <= 3; i++)
            {
                CreateSingleTree(treesRoot.transform, new Vector3(leftWalkX, 0.2f, i * 36f), trunkMat, leavesMat, 1.15f);
            }

            // 2) 우측 보도블럭 가로수 (정문 앞 제외)
            for (int i = -3; i <= 3; i++)
            {
                if (Mathf.Abs(i) <= 0) continue;
                CreateSingleTree(treesRoot.transform, new Vector3(rightWalkX, 0.2f, i * 36f), trunkMat, leavesMat, 1.15f);
            }

            // 3) 대형 미술관 북/남/동쪽 정원수
            // 북쪽 (Z = +34m)
            for (int x = -18; x <= 18; x += 9)
            {
                CreateSingleTree(treesRoot.transform, new Vector3(x, 0, 34f), trunkMat, leavesMat, 1.25f);
            }
            // 남쪽 (Z = -34m)
            for (int x = -18; x <= 18; x += 9)
            {
                CreateSingleTree(treesRoot.transform, new Vector3(x, 0, -34f), trunkMat, leavesMat, 1.25f);
            }
            // 동쪽 (X = +28m)
            for (int z = -24; z <= 24; z += 12)
            {
                CreateSingleTree(treesRoot.transform, new Vector3(28f, 0, z), trunkMat, leavesMat, 1.25f);
            }
        }

        private static void CreateSingleTree(Transform parent, Vector3 pos, Material trunkMat, Material leavesMat, float scale)
        {
            GameObject tree = new GameObject("GardenTree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * scale;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 2.0f, 0);
            trunk.transform.localScale = new Vector3(0.45f, 2.0f, 0.45f);
            trunk.GetComponent<MeshRenderer>().material = trunkMat;

            GameObject leaves1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves1.name = "Leaves_1";
            leaves1.transform.SetParent(tree.transform);
            leaves1.transform.localPosition = new Vector3(0, 4.2f, 0);
            leaves1.transform.localScale = new Vector3(3.2f, 2.2f, 3.2f);
            leaves1.GetComponent<MeshRenderer>().material = leavesMat;

            GameObject leaves2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves2.name = "Leaves_2";
            leaves2.transform.SetParent(tree.transform);
            leaves2.transform.localPosition = new Vector3(0, 5.6f, 0);
            leaves2.transform.localScale = new Vector3(2.4f, 2.0f, 2.4f);
            leaves2.GetComponent<MeshRenderer>().material = leavesMat;
        }

        private static void CreateCinematicCar(Transform parent, Vector3 pos, Quaternion rot, Material carBodyMat, Material glassMat)
        {
            GameObject carRoot = new GameObject("Cinematic_Family_Car");
            carRoot.transform.SetParent(parent);
            carRoot.transform.position = pos;
            carRoot.transform.rotation = rot;

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(carRoot.transform);
            chassis.transform.localPosition = new Vector3(0, 0.5f, 0);
            chassis.transform.localScale = new Vector3(2.2f, 0.6f, 4.8f);
            chassis.GetComponent<MeshRenderer>().material = carBodyMat;

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(carRoot.transform);
            cabin.transform.localPosition = new Vector3(0, 1.15f, -0.2f);
            cabin.transform.localScale = new Vector3(1.9f, 0.7f, 2.6f);
            cabin.GetComponent<MeshRenderer>().material = glassMat;

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(carRoot.transform);
            roof.transform.localPosition = new Vector3(0, 1.52f, -0.2f);
            roof.transform.localScale = new Vector3(1.85f, 0.1f, 2.5f);
            roof.GetComponent<MeshRenderer>().material = carBodyMat;

            Vector3[] wheelOffsets = new Vector3[]
            {
                new Vector3(-1.15f, 0.35f, 1.4f),
                new Vector3(1.15f, 0.35f, 1.4f),
                new Vector3(-1.15f, 0.35f, -1.4f),
                new Vector3(1.15f, 0.35f, -1.4f)
            };

            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i + 1}";
                wheel.transform.SetParent(carRoot.transform);
                wheel.transform.localPosition = wheelOffsets[i];
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                wheel.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
                wheel.GetComponent<MeshRenderer>().material = carBodyMat;
            }
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
    }
}
