using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mimesis_Mod_Menu.Core.Features
{
    public class FullbrightManager : FeatureManager
    {
        private bool isApplied;
        private float originalAmbientIntensity = -1f;
        private Color originalAmbientColor = Color.white;
        private List<Light> modifiedLights = new List<Light>();
        private const float FULLBRIGHT_INTENSITY = 1.5f;

        public override void Update()
        {
            if (IsEnabled)
            {
                if (!isApplied)
                    Apply();
                MaintainBrightness();
            }
            else if (isApplied)
            {
                Restore();
            }
        }

        private void Apply()
        {
            try
            {
                StoreOriginalSettings();
                ApplyBrightness();
                isApplied = true;
                LogMessage("Enabled");
            }
            catch (Exception ex)
            {
                LogError(nameof(Apply), ex);
            }
        }

        private void StoreOriginalSettings()
        {
            if (originalAmbientIntensity < 0f)
            {
                originalAmbientColor = RenderSettings.ambientLight;
                originalAmbientIntensity = RenderSettings.ambientIntensity;
            }
        }

        private void ApplyBrightness()
        {
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientIntensity = FULLBRIGHT_INTENSITY;

            modifiedLights.Clear();
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != null && light.enabled)
                {
                    light.intensity *= FULLBRIGHT_INTENSITY;
                    modifiedLights.Add(light);
                }
            }
        }

        private void MaintainBrightness()
        {
            try
            {
                if (RenderSettings.ambientIntensity < FULLBRIGHT_INTENSITY - 0.1f)
                    RenderSettings.ambientIntensity = FULLBRIGHT_INTENSITY;

                RenderSettings.ambientLight = Color.white;
            }
            catch { }
        }

        private void Restore()
        {
            try
            {
                if (originalAmbientIntensity >= 0f)
                {
                    RenderSettings.ambientLight = originalAmbientColor;
                    RenderSettings.ambientIntensity = originalAmbientIntensity;
                }

                foreach (Light light in modifiedLights)
                {
                    if (light != null)
                        light.intensity /= FULLBRIGHT_INTENSITY;
                }

                modifiedLights.Clear();
                isApplied = false;
                LogMessage("Disabled");
            }
            catch (Exception ex)
            {
                LogError(nameof(Restore), ex);
            }
        }

        public override void Cleanup()
        {
            IsEnabled = false;
            Restore();
        }
    }
}
