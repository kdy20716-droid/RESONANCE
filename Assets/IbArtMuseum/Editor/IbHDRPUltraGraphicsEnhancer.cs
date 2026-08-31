using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace IbArtMuseum
{
    public static class IbHDRPUltraGraphicsEnhancer
    {
        [MenuItem("Tools/Ib Museum/🌟 Apply Ultra Cinematic HDRP Graphics (최고급 그래픽 원클릭 적용)", false, 20)]
        public static void ApplyUltraGraphics()
        {
            // 1. 퀄리티 레벨을 'High Fidelity (최고 품질)'로 자동 승격!
            string[] qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; i++)
            {
                if (qualityNames[i].Contains("High") || qualityNames[i].Contains("Fidelity") || qualityNames[i].Contains("HighFidelity"))
                {
                    QualitySettings.SetQualityLevel(i, true);
                    Debug.Log($"[HDRP Enhancer] 퀄리티 레벨을 최고 품질({qualityNames[i]})로 승격했습니다!");
                    break;
                }
            }

            // 2. Global Volume 탐색 및 최고 품질 프로필 구축
            Volume globalVolume = null;
            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                if (vol.isGlobal)
                {
                    globalVolume = vol;
                    break;
                }
            }

            if (globalVolume == null)
            {
                GameObject volGo = new GameObject("HDRP_GlobalVolume_UltraCinematic");
                globalVolume = volGo.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.priority = 100f;
            }

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "IbMuseum_UltraCinematic_Profile";

            // 1) ACES Tonemapping
            Tonemapping tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // 2) Exposure
            Exposure exposure = profile.Add<Exposure>();
            exposure.mode.overrideState = true;
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.overrideState = true;
            exposure.fixedExposure.value = 10.2f;

            // 3) Bloom
            Bloom bloom = profile.Add<Bloom>();
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.45f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;

            // 4) Ambient Occlusion (GTAO)
            ScreenSpaceAmbientOcclusion ao = profile.Add<ScreenSpaceAmbientOcclusion>();
            ao.intensity.overrideState = true;
            ao.intensity.value = 1.35f;
            ao.directLightingStrength.overrideState = true;
            ao.directLightingStrength.value = 0.4f;
            ao.radius.overrideState = true;
            ao.radius.value = 2.0f;

            // 5) Screen Space Reflections (SSR)
            ScreenSpaceReflection ssr = profile.Add<ScreenSpaceReflection>();
            ssr.enabled.overrideState = true;
            ssr.enabled.value = true;
            ssr.quality.overrideState = true;
            ssr.quality.value = 2; // High

            // 6) Screen Space Global Illumination (SSGI)
            GlobalIllumination gi = profile.Add<GlobalIllumination>();
            gi.enable.overrideState = true;
            gi.enable.value = true;
            gi.quality.overrideState = true;
            gi.quality.value = 2; // High

            // 7) ★ 최적 밸런스 Volumetric Fog (방은 맑고 빛줄기만 선명하게 산란)
            Fog fog = profile.Add<Fog>();
            fog.enabled.overrideState = true;
            fog.enabled.value = true;
            fog.enableVolumetricFog.overrideState = true;
            fog.enableVolumetricFog.value = true;
            fog.albedo.overrideState = true;
            fog.albedo.value = new Color(0.96f, 0.96f, 1.0f);
            
            // meanFreePath = 65m (너무 뿌옇지 않으면서 빛줄기 입자만 형성)
            fog.meanFreePath.overrideState = true;
            fog.meanFreePath.value = 65.0f;

            // anisotropy = 0.82 (조명을 바라볼 때 강력한 빛줄기 산란)
            fog.anisotropy.overrideState = true;
            fog.anisotropy.value = 0.82f;

            fog.baseHeight.overrideState = true;
            fog.baseHeight.value = 0f;
            fog.maximumHeight.overrideState = true;
            fog.maximumHeight.value = 80f;

            // 8) Contact Shadows
            ContactShadows contactShadows = profile.Add<ContactShadows>();
            contactShadows.enable.overrideState = true;
            contactShadows.enable.value = true;
            contactShadows.length.overrideState = true;
            contactShadows.length.value = 0.18f;

            // 9) Vignette
            Vignette vignette = profile.Add<Vignette>();
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;

            string profilePath = "Assets/IbArtMuseum/IbMuseum_UltraCinematic_Profile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            globalVolume.sharedProfile = profile;

            // 3. 모든 조명에 Volumetric Light 활성화 & 적정 세기 설정
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                l.shadows = LightShadows.Soft;
                var hdLight = l.GetComponent<HDAdditionalLightData>();
                if (hdLight != null)
                {
                    hdLight.useScreenSpaceShadows = true;
                    hdLight.volumetricDimmer = 2.5f; // 적정 빛줄기 강도 2.5x
                    hdLight.shadowDimmer = 1.0f;
                }
            }

            // 4. 중앙 전시대/조각상 스팟 조명에 Local Volumetric Fog 추가 장착!
            SetupLocalFogOnMainExhibits();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "🌟 High Fidelity & 빛줄기 적용 완료!",
                "1. 프로젝트 퀄리티 레벨이 [3HDRPHighFidelity (최고 품질)]로 승격되었습니다!\n" +
                "2. HDRP 하이엔드 볼륨 렌더러가 활성화되어 스팟 조명 아래 자연스러운 빛줄기가 맺힙니다!\n\n" +
                "💡 씬 뷰 상단 툴바에서 'Fog' 아이콘이 켜져 있는지 꼭 확인해주세요!",
                "확인"
            );
        }

        private static void SetupLocalFogOnMainExhibits()
        {
            GameObject[] exhibits = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var go in exhibits)
            {
                if (go.name == "RoseExhibit" || go.name == "GuertenaStatue")
                {
                    Transform existingFog = go.transform.Find("LocalVolumetricFog_Zone");
                    if (existingFog != null) Object.DestroyImmediate(existingFog.gameObject);

                    GameObject localFogGo = new GameObject("LocalVolumetricFog_Zone");
                    localFogGo.transform.SetParent(go.transform);
                    localFogGo.transform.localPosition = new Vector3(0, 3.0f, 0);

                    LocalVolumetricFog localFog = localFogGo.AddComponent<LocalVolumetricFog>();
                    localFog.parameters.size = new Vector3(4.5f, 6.5f, 4.5f);
                    localFog.parameters.albedo = new Color(0.95f, 0.95f, 1.0f);
                    localFog.parameters.meanFreePath = 18.0f; // 국소적으로만 짙은 입자
                    localFog.parameters.anisotropy = 0.85f;
                }
            }
        }
    }
}
