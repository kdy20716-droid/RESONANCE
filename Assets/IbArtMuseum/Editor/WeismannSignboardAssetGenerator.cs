using UnityEditor;
using UnityEngine;
using System.IO;

namespace IbArtMuseum
{
    public static class WeismannSignboardAssetGenerator
    {
        [MenuItem("Tools/Ib Museum/Generate Weismann Gallery Signboard Assets in modeling folder", false, 10)]
        public static void GenerateWeismannSignboardAssets()
        {
            string folderPath = "Assets/modeling";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "modeling");
            }

            // 1. 클래식 미술관 텍스처 생성
            Texture2D signboardTex = GenerateClassicWeismannSignboardTexture();
            byte[] pngBytes = signboardTex.EncodeToPNG();
            string texPath = Path.Combine(folderPath, "Tex_WeismannGallery_Signboard.png");
            File.WriteAllBytes(texPath, pngBytes);
            AssetDatabase.ImportAsset(texPath);

            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }

            Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            // 2. 머티리얼 생성
            Shader litShader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");

            // 간판 캔버스 머티리얼 (클래식 브러시드 브라스 & 딥 우드)
            Material signboardMat = new Material(litShader);
            signboardMat.name = "Mat_WeismannGallery_Signboard";
            signboardMat.mainTexture = loadedTex;
            if (signboardMat.HasProperty("_Smoothness")) signboardMat.SetFloat("_Smoothness", 0.72f);
            if (signboardMat.HasProperty("_Metallic")) signboardMat.SetFloat("_Metallic", 0.45f);
            string matPath = Path.Combine(folderPath, "Mat_WeismannGallery_Signboard.mat");
            AssetDatabase.CreateAsset(signboardMat, matPath);

            // 원목 베이스 머티리얼 (마호가니 다크 우드)
            Material woodMat = new Material(litShader);
            woodMat.name = "Mat_Weismann_DarkWood";
            woodMat.color = new Color(0.12f, 0.08f, 0.05f);
            if (woodMat.HasProperty("_Smoothness")) woodMat.SetFloat("_Smoothness", 0.65f);
            string woodMatPath = Path.Combine(folderPath, "Mat_Weismann_DarkWood.mat");
            AssetDatabase.CreateAsset(woodMat, woodMatPath);

            // 황동 테두리 머티리얼 (앤틱 골드)
            Material brassMat = new Material(litShader);
            brassMat.name = "Mat_Weismann_Brass";
            brassMat.color = new Color(0.88f, 0.72f, 0.28f);
            if (brassMat.HasProperty("_Smoothness")) brassMat.SetFloat("_Smoothness", 0.90f);
            if (brassMat.HasProperty("_Metallic")) brassMat.SetFloat("_Metallic", 0.90f);
            string brassMatPath = Path.Combine(folderPath, "Mat_Weismann_Brass.mat");
            AssetDatabase.CreateAsset(brassMat, brassMatPath);

            // 3. 간판 프리팹 1: 그랜드 벽걸이/정문용 간판 (Grand Wall Signboard)
            GameObject grandSignGo = new GameObject("WeismannGallery_GrandSignboard");
            BuildGrandSignboardHierarchy(grandSignGo, woodMat, brassMat, signboardMat);
            string grandPrefabPath = Path.Combine(folderPath, "WeismannGallery_GrandSignboard.prefab");
            PrefabUtility.SaveAsPrefabAsset(grandSignGo, grandPrefabPath);
            Object.DestroyImmediate(grandSignGo);

            // 4. 간판 프리팹 2: 전시대/스탠드용 컴팩트 명판 (Compact Desk Plaque)
            GameObject plaqueGo = new GameObject("WeismannGallery_DeskPlaque");
            BuildDeskPlaqueHierarchy(plaqueGo, woodMat, brassMat, signboardMat);
            string plaquePrefabPath = Path.Combine(folderPath, "WeismannGallery_DeskPlaque.prefab");
            PrefabUtility.SaveAsPrefabAsset(plaqueGo, plaquePrefabPath);
            Object.DestroyImmediate(plaqueGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "클래식 바이스만 갤러리 간판 생성 완료!",
                "파란 선을 제거하고 깊이감 있는 앤틱 골드 & 클래식 우드 스타일로 정돈된 간판 에셋이 생성되었습니다!\n\n" +
                "📁 생성된 에셋:\n" +
                "1. WeismannGallery_GrandSignboard.prefab (벽걸이/정문용 웅장한 클래식 간판)\n" +
                "2. WeismannGallery_DeskPlaque.prefab (전시대/좌대용 클래식 앤틱 명판)\n" +
                "3. Tex_WeismannGallery_Signboard.png (클래식 골드 인그레이빙 텍스처)\n\n" +
                "Project 창의 'Assets/modeling' 폴더에서 씬으로 드래그하여 배치해보세요!",
                "확인"
            );
        }

        private static void BuildGrandSignboardHierarchy(GameObject root, Material woodMat, Material brassMat, Material signMat)
        {
            float width = 4.2f;
            float height = 1.2f;
            float depth = 0.12f;

            // 1. 다크 마호가니 원목 베이스 보드
            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "Wood_Backboard";
            backboard.transform.SetParent(root.transform);
            backboard.transform.localPosition = Vector3.zero;
            backboard.transform.localScale = new Vector3(width, height, depth);
            backboard.GetComponent<MeshRenderer>().material = woodMat;

            // 2. 황동 외곽 몰딩 프레임 (상/하/좌/우)
            CreateFrameBorder(root.transform, width, height, depth, brassMat);

            // 3. 황동 장식 볼트 (4개 모서리)
            float boltOffsetX = (width / 2f) - 0.16f;
            float boltOffsetY = (height / 2f) - 0.16f;
            Vector3[] boltPos = new Vector3[]
            {
                new Vector3(-boltOffsetX, boltOffsetY, depth / 2f + 0.02f),
                new Vector3(boltOffsetX, boltOffsetY, depth / 2f + 0.02f),
                new Vector3(-boltOffsetX, -boltOffsetY, depth / 2f + 0.02f),
                new Vector3(boltOffsetX, -boltOffsetY, depth / 2f + 0.02f)
            };

            for (int i = 0; i < boltPos.Length; i++)
            {
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"BrassBolt_{i + 1}";
                bolt.transform.SetParent(root.transform);
                bolt.transform.localPosition = boltPos[i];
                bolt.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                bolt.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                bolt.GetComponent<MeshRenderer>().material = brassMat;
            }

            // 4. 'WEISMANN GALLERY' 텍스처 캔버스 (전면)
            GameObject canvasQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvasQuad.name = "Signboard_Canvas";
            canvasQuad.transform.SetParent(root.transform);
            canvasQuad.transform.localPosition = new Vector3(0, 0, (depth / 2f) + 0.015f);
            canvasQuad.transform.localRotation = Quaternion.identity;
            canvasQuad.transform.localScale = new Vector3(width - 0.25f, height - 0.25f, 1f);
            canvasQuad.GetComponent<MeshRenderer>().material = signMat;
        }

        private static void BuildDeskPlaqueHierarchy(GameObject root, Material woodMat, Material brassMat, Material signMat)
        {
            float width = 1.6f;
            float height = 0.55f;

            // 1. 삼각 원목 스탠드 베이스
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = "Wood_Stand_Base";
            stand.transform.SetParent(root.transform);
            stand.transform.localPosition = new Vector3(0, 0.05f, 0);
            stand.transform.localScale = new Vector3(width + 0.1f, 0.08f, 0.35f);
            stand.GetComponent<MeshRenderer>().material = woodMat;

            // 2. 빗각 황동 지지대
            GameObject angledBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            angledBoard.name = "Angled_Brass_Plaque";
            angledBoard.transform.SetParent(root.transform);
            angledBoard.transform.localPosition = new Vector3(0, height / 2f + 0.05f, 0);
            angledBoard.transform.localRotation = Quaternion.Euler(18f, 0, 0);
            angledBoard.transform.localScale = new Vector3(width, height, 0.04f);
            angledBoard.GetComponent<MeshRenderer>().material = brassMat;

            // 3. 전면 명판 캔버스
            GameObject canvasQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvasQuad.name = "Plaque_Canvas";
            canvasQuad.transform.SetParent(angledBoard.transform);
            canvasQuad.transform.localPosition = new Vector3(0, 0, 0.52f);
            canvasQuad.transform.localRotation = Quaternion.identity;
            canvasQuad.transform.localScale = new Vector3(0.92f, 0.88f, 1f);
            canvasQuad.GetComponent<MeshRenderer>().material = signMat;
        }

        private static void CreateFrameBorder(Transform parent, float w, float h, float d, Material mat)
        {
            float borderThick = 0.07f;

            // 상단
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Border_Top";
            top.transform.SetParent(parent);
            top.transform.localPosition = new Vector3(0, (h / 2f) - (borderThick / 2f), (d / 2f) + 0.01f);
            top.transform.localScale = new Vector3(w, borderThick, 0.03f);
            top.GetComponent<MeshRenderer>().material = mat;

            // 하단
            GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottom.name = "Border_Bottom";
            bottom.transform.SetParent(parent);
            bottom.transform.localPosition = new Vector3(0, -(h / 2f) + (borderThick / 2f), (d / 2f) + 0.01f);
            bottom.transform.localScale = new Vector3(w, borderThick, 0.03f);
            bottom.GetComponent<MeshRenderer>().material = mat;

            // 좌측
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "Border_Left";
            left.transform.SetParent(parent);
            left.transform.localPosition = new Vector3(-(w / 2f) + (borderThick / 2f), 0, (d / 2f) + 0.01f);
            left.transform.localScale = new Vector3(borderThick, h, 0.03f);
            left.GetComponent<MeshRenderer>().material = mat;

            // 우측
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "Border_Right";
            right.transform.SetParent(parent);
            right.transform.localPosition = new Vector3((w / 2f) - (borderThick / 2f), 0, (d / 2f) + 0.01f);
            right.transform.localScale = new Vector3(borderThick, h, 0.03f);
            right.GetComponent<MeshRenderer>().material = mat;
        }

        private static Texture2D GenerateClassicWeismannSignboardTexture()
        {
            int w = 2048;
            int h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color bgDark = new Color(0.08f, 0.06f, 0.05f); // 고급스러운 다크 에보니 우드
            Color bgGradient = new Color(0.15f, 0.11f, 0.08f);
            Color goldMain = new Color(0.95f, 0.82f, 0.38f); // 앤틱 골드 인그레이빙
            Color goldDim = new Color(0.75f, 0.60f, 0.24f);
            Color goldShadow = new Color(0.35f, 0.25f, 0.10f);

            // 1. 깊이감 있는 클래식 다크 우드 결 배경
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float woodGrain = Mathf.Sin(y * 0.15f + Mathf.Cos(x * 0.01f) * 4f) * 0.02f;
                    Color c = Color.Lerp(bgDark, bgGradient, Mathf.Clamp01(t + woodGrain));

                    // 이중 클래식 골드 테두리 (Double Classic Border)
                    if ((x >= 24 && x <= 30) || (x >= w - 30 && x <= w - 24) ||
                        (y >= 24 && y <= 30) || (y >= h - 30 && y <= h - 24) ||
                        (x >= 40 && x <= 43) || (x >= w - 43 && x <= w - 40) ||
                        (y >= 40 && y <= 43) || (y >= h - 43 && y <= h - 40))
                    {
                        c = goldDim;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // 2. 4개 모서리 클래식 문양 (Corner Accents)
            DrawCornerFlourish(tex, 44, 44, goldMain);
            DrawCornerFlourish(tex, w - 44, 44, goldMain);
            DrawCornerFlourish(tex, 44, h - 44, goldMain);
            DrawCornerFlourish(tex, w - 44, h - 44, goldMain);

            // 3. 메인 타이틀: "W E I S M A N N   G A L L E R Y"
            string title = "WEISMANN GALLERY";
            int titleCharW = 62;
            int titleCharH = 110;
            int titleThick = 10;
            int titleSpacing = 95;
            int titleTotalW = title.Length * titleSpacing;
            int titleStartX = (w - titleTotalW) / 2;
            int titleStartY = 240;

            for (int i = 0; i < title.Length; i++)
            {
                if (title[i] != ' ')
                {
                    int cx = titleStartX + i * titleSpacing;
                    // 골드 텍스트 그림자 및 본문
                    DrawLetter(tex, title[i], cx + 2, titleStartY - 2, titleCharW, titleCharH, titleThick, goldShadow);
                    DrawLetter(tex, title[i], cx, titleStartY, titleCharW, titleCharH, titleThick, goldMain);
                }
            }

            // 4. 서브 타이틀: "CARL WEISMANN (1942 - )"
            string sub = "CARL WEISMANN (1942 - )";
            int subCharW = 28;
            int subCharH = 45;
            int subThick = 5;
            int subSpacing = 42;
            int subTotalW = sub.Length * subSpacing;
            int subStartX = (w - subTotalW) / 2;
            int subStartY = 135;

            for (int i = 0; i < sub.Length; i++)
            {
                if (sub[i] != ' ')
                {
                    int cx = subStartX + i * subSpacing;
                    DrawLetter(tex, sub[i], cx + 1, subStartY - 1, subCharW, subCharH, subThick, goldShadow);
                    DrawLetter(tex, sub[i], cx, subStartY, subCharW, subCharH, subThick, goldDim);
                }
            }

            // 5. 상/하단 클래식 디바이더 장식선
            int divYTop = titleStartY + titleCharH + 28;
            DrawLine(tex, titleStartX - 20, divYTop, titleStartX + titleTotalW - 20, divYTop, 4, goldDim);
            DrawBrushCircle(tex, w / 2, divYTop, 8, goldMain);
            DrawBrushCircle(tex, w / 2 - 25, divYTop, 4, goldDim);
            DrawBrushCircle(tex, w / 2 + 25, divYTop, 4, goldDim);

            int divYBottom = subStartY - 28;
            DrawLine(tex, subStartX - 20, divYBottom, subStartX + subTotalW - 20, divYBottom, 3, goldDim);
            DrawBrushCircle(tex, w / 2, divYBottom, 6, goldMain);

            tex.Apply();
            return tex;
        }

        private static void DrawCornerFlourish(Texture2D tex, int cx, int cy, Color col)
        {
            DrawBrushCircle(tex, cx, cy, 6, col);
            DrawBrushCircle(tex, cx + (cx < 100 ? 12 : -12), cy, 4, col);
            DrawBrushCircle(tex, cx, cy + (cy < 100 ? 12 : -12), 4, col);
        }

        private static void DrawBrushCircle(Texture2D tex, int cx, int cy, int radius, Color col)
        {
            int r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= r2)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            Color src = tex.GetPixel(px, py);
                            tex.SetPixel(px, py, Color.Lerp(src, col, col.a));
                        }
                    }
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int thick, Color col)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int steps = Mathf.Max(dx, dy);

            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0 : (float)i / steps;
                int x = (int)Mathf.Lerp(x0, x1, t);
                int y = (int)Mathf.Lerp(y0, y1, t);
                DrawBrushCircle(tex, x, y, thick / 2, col);
            }
        }

        private static void DrawLetter(Texture2D tex, char c, int x, int y, int w, int h, int thick, Color col)
        {
            char upper = char.ToUpper(c);
            switch (upper)
            {
                case 'A':
                    DrawLine(tex, x, y, x + w / 2, y + h, thick, col);
                    DrawLine(tex, x + w / 2, y + h, x + w, y, thick, col);
                    DrawLine(tex, x + w / 4, y + h / 2, x + (w * 3) / 4, y + h / 2, thick, col);
                    break;
                case 'B':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + (w * 3) / 4, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + (w * 3) / 4, y + h / 2, thick, col);
                    DrawLine(tex, x, y, x + (w * 3) / 4, y, thick, col);
                    DrawLine(tex, x + (w * 3) / 4, y + h, x + w, y + (h * 3) / 4, thick, col);
                    DrawLine(tex, x + w, y + (h * 3) / 4, x + (w * 3) / 4, y + h / 2, thick, col);
                    DrawLine(tex, x + (w * 3) / 4, y + h / 2, x + w, y + h / 4, thick, col);
                    DrawLine(tex, x + w, y + h / 4, x + (w * 3) / 4, y, thick, col);
                    break;
                case 'C':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'D':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + (w * 2) / 3, y + h, thick, col);
                    DrawLine(tex, x, y, x + (w * 2) / 3, y, thick, col);
                    DrawLine(tex, x + (w * 2) / 3, y + h, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x + w, y + h / 2, x + (w * 2) / 3, y, thick, col);
                    break;
                case 'E':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + (w * 3) / 4, y + h / 2, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'F':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + (w * 3) / 4, y + h / 2, thick, col);
                    break;
                case 'G':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y + h / 2, x + w, y + h / 2, thick, col);
                    break;
                case 'H':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    break;
                case 'I':
                    DrawLine(tex, x + w / 2, y, x + w / 2, y + h, thick, col);
                    DrawLine(tex, x + w / 4, y + h, x + (w * 3) / 4, y + h, thick, col);
                    DrawLine(tex, x + w / 4, y, x + (w * 3) / 4, y, thick, col);
                    break;
                case 'L':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'M':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w / 2, y + h / 2, thick, col);
                    DrawLine(tex, x + w, y + h, x + w / 2, y + h / 2, thick, col);
                    break;
                case 'N':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y, thick, col);
                    break;
                case 'O':
                case '0':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'P':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w, y + h, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    break;
                case 'R':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w, y + h, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y, thick, col);
                    break;
                case 'S':
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h, x, y + h / 2, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x + w, y + h / 2, x + w, y, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'T':
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w / 2, y, x + w / 2, y + h, thick, col);
                    break;
                case 'U':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case 'W':
                    DrawLine(tex, x, y + h, x + w / 4, y, thick, col);
                    DrawLine(tex, x + w / 4, y, x + w / 2, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y + h / 2, x + (w * 3) / 4, y, thick, col);
                    DrawLine(tex, x + (w * 3) / 4, y, x + w, y + h, thick, col);
                    break;
                case 'Y':
                    DrawLine(tex, x, y + h, x + w / 2, y + h / 2, thick, col);
                    DrawLine(tex, x + w, y + h, x + w / 2, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y, x + w / 2, y + h / 2, thick, col);
                    break;
                case '1':
                    DrawLine(tex, x + w / 2, y, x + w / 2, y + h, thick, col);
                    DrawLine(tex, x + w / 4, y + (int)(h * 0.7f), x + w / 2, y + h, thick, col);
                    DrawLine(tex, x + w / 4, y, x + (w * 3) / 4, y, thick, col);
                    break;
                case '9':
                    DrawLine(tex, x, y + h / 2, x, y + h, thick, col);
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w, y, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    break;
                case '4':
                    DrawLine(tex, x, y + h / 3, x, y + h, thick, col);
                    DrawLine(tex, x, y + h / 3, x + w, y + h / 3, thick, col);
                    DrawLine(tex, x + (w * 2) / 3, y, x + (w * 2) / 3, y + h, thick, col);
                    break;
                case '2':
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w, y + h / 2, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x, y, x, y + h / 2, thick, col);
                    DrawLine(tex, x, y, x + w, y, thick, col);
                    break;
                case '(':
                    DrawLine(tex, x + w / 2, y + h, x + w / 4, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 4, y + h / 2, x + w / 2, y, thick, col);
                    break;
                case ')':
                    DrawLine(tex, x + w / 4, y + h, x + w / 2, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y + h / 2, x + w / 4, y, thick, col);
                    break;
                case '-':
                    DrawLine(tex, x, y + h / 2, x + w, y + h / 2, thick, col);
                    break;
            }
        }
    }
}
