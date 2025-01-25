using Fusion.Addons.LineDrawing;
using Fusion.XR.Shared;
using UnityEngine;

namespace Fusion.Addons.MXPenIntegration {
    public class NetworkMXPenWithColorCycling : NetworkMXPen
    {
        protected ColorCyclingLineDrawer colorCyclingDrawer;
        private bool wasBackButtonPressed = false;  // Track previous state

        protected override void Awake()
        {
            base.Awake();
            colorCyclingDrawer = GetComponentInChildren<ColorCyclingLineDrawer>();
            networkLineDrawer = colorCyclingDrawer;
        }

        protected override bool ShouldStopCurrentVolumeDrawing()
        {
            // Only use front button to stop drawing
            return localHardwareStylus.CurrentState.cluster_front_value;
        }

        protected override void VolumeDrawing()
        {
            base.VolumeDrawing();

            // Handle color cycling only on button press (not hold)
            bool isBackButtonPressed = localHardwareStylus.CurrentState.cluster_back_value;
            if (isBackButtonPressed && !wasBackButtonPressed)
            {
                colorCyclingDrawer.CycleColor();
            }
            wasBackButtonPressed = isBackButtonPressed;
        }
    }
}