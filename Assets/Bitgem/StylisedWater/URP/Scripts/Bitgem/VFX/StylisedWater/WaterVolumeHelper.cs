using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bitgem.VFX.StylisedWater
{
    public class WaterVolumeHelper : MonoBehaviour
    {
        #region Private static fields
        private static WaterVolumeHelper instance = null;
        #endregion

        #region Public fields
        public WaterVolumeBase WaterVolume = null;
        #endregion

        #region Public static properties
        public static WaterVolumeHelper Instance { get { return instance; } }
        #endregion

        #region Public methods
        public float? GetHeight(Vector3 _position)
        {
            // Ensure a water volume
            if (!WaterVolume)
            {
                Debug.LogWarning("WaterVolume is not assigned in WaterVolumeHelper.");
                return 0f;
            }

            // Ensure a material
            var renderer = WaterVolume.gameObject.GetComponent<MeshRenderer>();
            if (!renderer || !renderer.sharedMaterial)
            {
                Debug.LogWarning("MeshRenderer or material missing on WaterVolume.");
                return 0f;
            }

            // Convert world position to local space
            Vector3 localPosition = WaterVolume.transform.InverseTransformPoint(_position);

            // Try getting base height with world position
            var waterHeight = WaterVolume.GetHeight(_position);
            if (!waterHeight.HasValue)
            {
                // Fallback: Try adjusted position (local to world)
                Vector3 adjustedPosition = WaterVolume.transform.TransformPoint(localPosition);
                waterHeight = WaterVolume.GetHeight(adjustedPosition);
                if (!waterHeight.HasValue)
                {
                    // Fallback to water's y-position as a last resort
                    waterHeight = WaterVolume.transform.position.y;
                }
            }

            // Calculate wave offset using local position
            var _WaveFrequency = renderer.sharedMaterial.GetFloat("_WaveFrequency");
            var _WaveScale = renderer.sharedMaterial.GetFloat("_WaveScale");
            var _WaveSpeed = renderer.sharedMaterial.GetFloat("_WaveSpeed");
            var time = Time.time * _WaveSpeed;
            var shaderOffset = (Mathf.Sin(localPosition.x * _WaveFrequency + time) + 
                               Mathf.Cos(localPosition.z * _WaveFrequency + time)) * _WaveScale;

            return waterHeight.Value + shaderOffset;
        }
        #endregion

        #region MonoBehaviour events
        private void Awake()
        {
            instance = this;
        }
        #endregion
    }
}