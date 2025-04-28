using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bitgem.VFX.StylisedWater
{
    public class WateverVolumeFloater : MonoBehaviour
    {
        #region Public fields
        public WaterVolumeHelper WaterVolumeHelper = null;
        [Tooltip("Height offset above the water surface (in meters).")]
        public float heightOffset = 0.2f; // Default to 0.2 meters above water
        #endregion

        #region MonoBehaviour events
        void Update()
        {
            var instance = WaterVolumeHelper ? WaterVolumeHelper : WaterVolumeHelper.Instance;
            if (!instance)
            {
                Debug.LogWarning("WaterVolumeHelper instance not found.");
                return;
            }

            var height = instance.GetHeight(transform.position);
            if (!height.HasValue)
            {
                Debug.LogWarning($"GetHeight returned null for position {transform.position}. Keeping current y-position.");
                return;
            }

            // Apply height offset to break the water surface
            transform.position = new Vector3(transform.position.x, height.Value + heightOffset, transform.position.z);
        }
        #endregion
    }
}