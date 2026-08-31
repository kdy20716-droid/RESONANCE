using UnityEngine;
using System.IO;
using UnityEditor;

namespace IbArtMuseum
{
    public static class IbTextureGenerator
    {
        public static Texture2D GenerateLadyPortrait(bool bleeding)
        {
            int w = 512;
            int h = 768;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color bgTop = new Color(0.08f, 0.05f, 0.07f);
            Color bgBottom = new Color(0.18f, 0.12f, 0.14f);
            Color dressCol = new Color(0.12f, 0.04f, 0.06f);
            Color skinCol = new Color(0.92f, 0.86f, 0.80f);
            Color hairCol = new Color(0.15f, 0.10f, 0.08f);
            Color bloodCol = new Color(0.85f, 0.05f, 0.05f);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color currentBg = Color.Lerp(bgBottom, bgTop, t);

                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.45f) / (h * 0.45f);

                    Color col = currentBg;

                    // 몸체/드레스
                    if (ny < 0.1f && Mathf.Abs(nx) < (0.35f - ny * 0.4f))
                    {
                        col = dressCol;
                    }

                    // 머리카락
                    float hairDist = (nx * nx * 1.2f) + ((ny - 0.25f) * (ny - 0.25f) * 1.1f);
                    if (hairDist < 0.12f)
                    {
                        col = hairCol;
                    }

                    // 얼굴 (타원)
                    float faceDist = (nx * nx * 1.6f) + ((ny - 0.22f) * (ny - 0.22f) * 1.5f);
                    if (faceDist < 0.06f)
                    {
                        col = skinCol;

                        // 눈 (2개)
                        float eyeY = 0.25f;
                        float leftEyeX = -0.08f;
                        float rightEyeX = 0.08f;

                        if (Mathf.Abs(ny - eyeY) < 0.018f && (Mathf.Abs(nx - leftEyeX) < 0.035f || Mathf.Abs(nx - rightEyeX) < 0.035f))
                        {
                            col = Color.black;
                        }

                        // 입술
                        if (Mathf.Abs(ny - 0.13f) < 0.015f && Mathf.Abs(nx) < 0.05f)
                        {
                            col = new Color(0.7f, 0.2f, 0.25f);
                        }
                    }

                    // 피 흘림 이상현상 (Bleeding)
                    if (bleeding)
                    {
                        float leftEyeX = -0.08f;
                        float rightEyeX = 0.08f;

                        if ((Mathf.Abs(nx - leftEyeX) < 0.022f || Mathf.Abs(nx - rightEyeX) < 0.022f) && ny < 0.25f && ny > -0.3f)
                        {
                            float noise = Mathf.Sin(y * 0.2f) * 0.005f;
                            if (Mathf.Abs(nx - leftEyeX + noise) < 0.015f || Mathf.Abs(nx - rightEyeX + noise) < 0.015f)
                            {
                                col = bloodCol;
                            }
                        }
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateMaryPortrait()
        {
            int w = 512;
            int h = 768;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color bgCol = new Color(0.20f, 0.26f, 0.18f);
            Color dressCol = new Color(0.18f, 0.45f, 0.22f);
            Color skinCol = new Color(0.96f, 0.88f, 0.82f);
            Color hairCol = new Color(0.92f, 0.78f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.45f) / (h * 0.45f);

                    Color col = bgCol;

                    if (ny < 0.05f && Mathf.Abs(nx) < (0.32f - ny * 0.35f))
                    {
                        col = dressCol;
                    }

                    float hairDist = (nx * nx * 1.1f) + ((ny - 0.22f) * (ny - 0.22f) * 1.0f);
                    if (hairDist < 0.14f)
                    {
                        col = hairCol;
                    }

                    float faceDist = (nx * nx * 1.5f) + ((ny - 0.20f) * (ny - 0.20f) * 1.4f);
                    if (faceDist < 0.055f)
                    {
                        col = skinCol;

                        // 눈 (푸른 눈동자)
                        if (Mathf.Abs(ny - 0.23f) < 0.02f && (Mathf.Abs(nx - -0.08f) < 0.03f || Mathf.Abs(nx - 0.08f) < 0.03f))
                        {
                            col = new Color(0.2f, 0.5f, 0.8f);
                        }

                        // 미소 띤 입
                        if (Mathf.Abs(ny - 0.12f) < 0.012f && Mathf.Abs(nx) < 0.045f)
                        {
                            col = new Color(0.85f, 0.3f, 0.35f);
                        }
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateGarryPortrait()
        {
            int w = 512;
            int h = 768;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color bgCol = new Color(0.10f, 0.10f, 0.14f);
            Color coatCol = new Color(0.22f, 0.18f, 0.26f);
            Color skinCol = new Color(0.90f, 0.86f, 0.82f);
            Color hairCol = new Color(0.65f, 0.55f, 0.75f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.45f) / (h * 0.45f);

                    Color col = bgCol;

                    if (ny < 0.1f && Mathf.Abs(nx) < (0.35f - ny * 0.4f))
                    {
                        col = coatCol;
                    }

                    float hairDist = (nx * nx * 1.1f) + ((ny - 0.25f) * (ny - 0.25f) * 1.1f);
                    if (hairDist < 0.13f)
                    {
                        col = hairCol;
                    }

                    float faceDist = (nx * nx * 1.6f) + ((ny - 0.22f) * (ny - 0.22f) * 1.5f);
                    if (faceDist < 0.06f)
                    {
                        col = skinCol;

                        // 날카로운 눈
                        if (Mathf.Abs(ny - 0.25f) < 0.015f && (Mathf.Abs(nx - -0.08f) < 0.035f || Mathf.Abs(nx - 0.08f) < 0.035f))
                        {
                            col = new Color(0.85f, 0.85f, 0.2f);
                        }
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateRedGraffiti()
        {
            int w = 512;
            int h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color clear = new Color(0, 0, 0, 0);
            Color redCrayon = new Color(0.90f, 0.05f, 0.08f, 0.92f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // 기괴한 눈동자 및 도망쳐(RUN) 형태의 크레파스 낙서 패턴
            for (float angle = 0; angle < Mathf.PI * 2f; angle += 0.02f)
            {
                int cx = w / 2 + (int)(Mathf.Cos(angle) * 120f + Mathf.Sin(angle * 5f) * 8f);
                int cy = h / 2 + (int)(Mathf.Sin(angle) * 60f + Mathf.Cos(angle * 3f) * 6f);
                DrawBrushCircle(tex, cx, cy, 6, redCrayon);
            }

            // 동공
            DrawBrushCircle(tex, w / 2, h / 2, 22, redCrayon);

            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateResonanceSignboard()
        {
            int w = 1024;
            int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color woodDark = new Color(0.12f, 0.08f, 0.05f);
            Color goldCol = new Color(0.92f, 0.78f, 0.35f);
            Color borderCol = new Color(0.75f, 0.60f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = woodDark;
                    if (x < 16 || x > w - 16 || y < 16 || y > h - 16 ||
                        x == 24 || x == w - 24 || y == 24 || y == h - 24)
                    {
                        c = borderCol;
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            // "R E S O N A N C E" 글자를 픽셀 아트로 정밀 드로잉
            string title = "RESONANCE";
            int startX = 140;
            int startY = 85;
            int charWidth = 65;
            int charHeight = 85;
            int thick = 8;
            int spacing = 95;

            for (int i = 0; i < title.Length; i++)
            {
                int cx = startX + i * spacing;
                DrawLetter(tex, title[i], cx, startY, charWidth, charHeight, thick, goldCol);
            }

            tex.Apply();
            return tex;
        }

        // ==================== 4대 가이드 영문 텍스트 액자 텍스처 ====================
        public static Texture2D GenerateGuidePaintingTexture(string line1, string line2, string line3 = "")
        {
            int w = 1024;
            int h = 1024;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color canvasBg = new Color(0.96f, 0.95f, 0.92f); // 우아한 미술관 화이트 캔버스
            Color textColor = new Color(0.08f, 0.08f, 0.09f); // 깊은 블랙 잉크
            Color innerBorder = new Color(0.85f, 0.82f, 0.76f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = canvasBg;
                    if (x < 24 || x > w - 24 || y < 24 || y > h - 24 ||
                        x == 36 || x == w - 36 || y == 36 || y == h - 36)
                    {
                        c = innerBorder;
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            // 텍스트 블록 렌더링
            int baseY = 650;
            if (!string.IsNullOrEmpty(line1))
            {
                DrawStringCentered(tex, line1, baseY, 36, 48, 5, textColor);
            }
            if (!string.IsNullOrEmpty(line2))
            {
                DrawStringCentered(tex, line2, baseY - 140, 32, 44, 5, textColor);
            }
            if (!string.IsNullOrEmpty(line3))
            {
                DrawStringCentered(tex, line3, baseY - 260, 28, 40, 4, textColor);
            }

            tex.Apply();
            return tex;
        }

        // ==================== 장미 3개, 2개, 1개 UI 텍스처 (화분 없이 장미꽃만) ====================
        public static Texture2D GenerateRoseLifeHUDTexture(int roseCount)
        {
            int w = 512;
            int h = 180;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // 장미꽃 위치들 (최대 3송이)
            int[] roseX = new int[] { 85, 256, 427 };
            int centerY = 90;

            for (int i = 0; i < 3; i++)
            {
                if (i < roseCount)
                {
                    DrawSingleRoseBloom(tex, roseX[i], centerY, 42f, false);
                }
                else
                {
                    // 잃어버린 장미 (회색/투명 잔상)
                    DrawSingleRoseBloom(tex, roseX[i], centerY, 38f, true);
                }
            }

            tex.Apply();
            return tex;
        }

        private static void DrawSingleRoseBloom(Texture2D tex, int cx, int cy, float radius, bool withered)
        {
            Color roseMain = withered ? new Color(0.25f, 0.22f, 0.22f, 0.4f) : new Color(0.92f, 0.08f, 0.12f, 1f);
            Color roseDark = withered ? new Color(0.15f, 0.12f, 0.12f, 0.5f) : new Color(0.60f, 0.02f, 0.05f, 1f);
            Color roseHighlight = withered ? new Color(0.35f, 0.30f, 0.30f, 0.4f) : new Color(1.0f, 0.35f, 0.38f, 1f);
            Color leafGreen = withered ? new Color(0.18f, 0.18f, 0.15f, 0.4f) : new Color(0.12f, 0.55f, 0.18f, 1f);

            // 1. 녹색 잎사귀 (양옆)
            DrawBrushCircle(tex, cx - (int)(radius * 0.8f), cy - (int)(radius * 0.4f), (int)(radius * 0.4f), leafGreen);
            DrawBrushCircle(tex, cx + (int)(radius * 0.8f), cy - (int)(radius * 0.4f), (int)(radius * 0.4f), leafGreen);

            // 2. 바깥 꽃잎 레이어
            DrawBrushCircle(tex, cx, cy, (int)radius, roseDark);
            DrawBrushCircle(tex, cx - 8, cy + 4, (int)(radius * 0.75f), roseMain);
            DrawBrushCircle(tex, cx + 8, cy - 4, (int)(radius * 0.70f), roseMain);

            // 3. 중심 나선형 꽃잎 겹침
            DrawBrushCircle(tex, cx, cy + 6, (int)(radius * 0.55f), roseHighlight);
            DrawBrushCircle(tex, cx - 4, cy + 8, (int)(radius * 0.40f), roseDark);
            DrawBrushCircle(tex, cx + 3, cy + 10, (int)(radius * 0.25f), roseHighlight);
        }

        private static void DrawStringCentered(Texture2D tex, string str, int centerY, int charW, int charH, int thick, Color col)
        {
            int totalLen = str.Length * (charW + 12);
            int startX = (tex.width - totalLen) / 2;

            for (int i = 0; i < str.Length; i++)
            {
                int x = startX + i * (charW + 12);
                DrawLetter(tex, str[i], x, centerY - (charH / 2), charW, charH, thick, col);
            }
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
                            tex.SetPixel(px, py, col);
                        }
                    }
                }
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
                case 'J':
                    DrawLine(tex, x + (w * 3) / 4, y, x + (w * 3) / 4, y + h, thick, col);
                    DrawLine(tex, x, y, x + (w * 3) / 4, y, thick, col);
                    DrawLine(tex, x, y, x, y + h / 3, thick, col);
                    break;
                case 'K':
                    DrawLine(tex, x, y, x, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y + h, thick, col);
                    DrawLine(tex, x, y + h / 2, x + w, y, thick, col);
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
                case 'V':
                    DrawLine(tex, x, y + h, x + w / 2, y, thick, col);
                    DrawLine(tex, x + w, y + h, x + w / 2, y, thick, col);
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
                    DrawLine(tex, x + w / 2, y + h / 2, x + w / 2, y, thick, col);
                    break;
                case '\'':
                case ',':
                    DrawLine(tex, x + w / 2, y + (c == ',' ? h / 4 : h), x + w / 2, y + (c == ',' ? 0 : (h * 3) / 4), thick, col);
                    break;
                case '.':
                    DrawBrushCircle(tex, x + w / 2, y + thick, thick, col);
                    break;
                case '?':
                    DrawLine(tex, x, y + h, x + w, y + h, thick, col);
                    DrawLine(tex, x + w, y + h, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y + h / 2, x + w, y + h / 2, thick, col);
                    DrawLine(tex, x + w / 2, y + h / 4, x + w / 2, y + h / 2, thick, col);
                    DrawBrushCircle(tex, x + w / 2, y + thick, thick, col);
                    break;
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
    }
}
