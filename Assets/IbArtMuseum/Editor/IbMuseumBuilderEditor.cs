using UnityEditor;
using UnityEngine;

namespace IbArtMuseum
{
    [CustomEditor(typeof(IbMuseumBuilder))]
    public class IbMuseumBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("✨ [10층 원형 미술관 타워 완전 새로 구축하기] ✨", GUILayout.Height(45)))
            {
                IbMuseumSetupUtility.CleanAndRebuild10FloorMuseum();
            }

            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.95f);
            if (GUILayout.Button("🏙️ [외부 도심 환경 & 가로수 정원 추가하기] 🏙️", GUILayout.Height(40)))
            {
                IbCityEnvironmentUtility.AddCityAndExteriorEnvironment();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "★ [외부 도심 환경 & 가로수 추가] 버튼은 현재 수정하신 미술관 맵을 그대로 유지하면서, 컷씬 촬영을 위한 외부 도심 빌딩 스카이라인, 도로, 자동차, 가로수 정원을 주변에 즉시 배치합니다!",
                MessageType.Info
            );
        }
    }
}
