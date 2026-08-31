using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IbArtMuseum
{
    public class RedLightsAnomaly : IbAnomalyBase
    {
        [Header("Lights Setup")]
        [Tooltip("영향을 받을 미술관 라이트들 (비어있으면 자동 탐색)")]
        public List<Light> targetLights = new List<Light>();

        [Tooltip("이상현상 시 조명 색상")]
        public Color anomalyColor = new Color(0.9f, 0.1f, 0.1f);

        [Tooltip("조명 깜빡임 여부")]
        public bool enableFlicker = true;

        private List<Color> _originalColors = new List<Color>();
        private List<float> _originalIntensities = new List<float>();
        private Coroutine _flickerCoroutine;

        private void Awake()
        {
            anomalyName = "붉은 정전 및 조명 이상 (Blood Red Lights)";
            description = "미술관의 조명이 스산하게 깜빡이며 핏빛 붉은색으로 물듭니다.";

            if (targetLights.Count == 0)
            {
                targetLights.AddRange(GetComponentsInChildren<Light>(true));
            }

            foreach (var l in targetLights)
            {
                if (l != null)
                {
                    _originalColors.Add(l.color);
                    _originalIntensities.Add(l.intensity);
                }
            }
        }

        public override void ActivateAnomaly()
        {
            if (enableFlicker)
            {
                if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = StartCoroutine(FlickerAndRedRoutine());
            }
            else
            {
                SetAllLights(anomalyColor);
            }
        }

        public override void DeactivateAnomaly()
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }

            RestoreLights();
        }

        private IEnumerator FlickerAndRedRoutine()
        {
            // 1. 깜빡거림 효과
            for (int i = 0; i < 5; i++)
            {
                SetLightsEnabled(false);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
                SetLightsEnabled(true);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }

            // 2. 붉은 조명으로 전환
            SetAllLights(anomalyColor);

            // 3. 지속적인 미세 플리커
            while (true)
            {
                float factor = Random.Range(0.7f, 1.2f);
                for (int i = 0; i < targetLights.Count; i++)
                {
                    if (targetLights[i] != null && i < _originalIntensities.Count)
                    {
                        targetLights[i].intensity = _originalIntensities[i] * factor;
                    }
                }
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            }
        }

        private void SetAllLights(Color c)
        {
            for (int i = 0; i < targetLights.Count; i++)
            {
                if (targetLights[i] != null)
                {
                    targetLights[i].enabled = true;
                    targetLights[i].color = c;
                }
            }
        }

        private void SetLightsEnabled(bool state)
        {
            foreach (var l in targetLights)
            {
                if (l != null) l.enabled = state;
            }
        }

        private void RestoreLights()
        {
            for (int i = 0; i < targetLights.Count; i++)
            {
                if (targetLights[i] != null && i < _originalColors.Count && i < _originalIntensities.Count)
                {
                    targetLights[i].enabled = true;
                    targetLights[i].color = _originalColors[i];
                    targetLights[i].intensity = _originalIntensities[i];
                }
            }
        }
    }
}
