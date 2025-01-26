using Fusion.Addons.LineDrawing;
using Fusion.XR.Shared;
using UnityEngine;

namespace Fusion.Addons.MXPenIntegration {
    public class NetworkMXPenWithColorCycling : NetworkMXPen
    {
        protected ColorCyclingLineDrawer colorCyclingDrawer;
        private bool wasBackButtonPressed = false;
        
        [Header("Vacuum Settings")]
        [SerializeField] private Renderer vacuumRenderer;
        [SerializeField] private float vacuumRadius = 0.1f;
        [SerializeField] private float vacuumDistance = 1.0f;

        protected override void Awake()
        {
            base.Awake();
            colorCyclingDrawer = GetComponentInChildren<ColorCyclingLineDrawer>();
            networkLineDrawer = colorCyclingDrawer;
            
            if (vacuumRenderer != null)
            {
                vacuumRenderer.enabled = false;
            }
        }

        protected override bool ShouldStopCurrentVolumeDrawing()
        {
            return localHardwareStylus.CurrentState.cluster_front_value;
        }

        protected override void VolumeDrawing()
        {
            base.VolumeDrawing();

            bool isBackButtonPressed = localHardwareStylus.CurrentState.cluster_back_value;
            if (isBackButtonPressed && !wasBackButtonPressed)
            {
                colorCyclingDrawer.CycleColor();
            }
            wasBackButtonPressed = isBackButtonPressed;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            
            if (Object.HasStateAuthority)
            {
                bool isFrontButtonPressed = localHardwareStylus.CurrentState.cluster_front_value;
                if (isFrontButtonPressed)
                {
                    if (vacuumRenderer != null)
                    {
                        vacuumRenderer.enabled = true;
                    }
                    PerformSphereCast();
                }
                else
                {
                    if (vacuumRenderer != null)
                    {
                        vacuumRenderer.enabled = false;
                    }
                }
            }
        }

        private void PerformSphereCast()
        {
            if (!Object.HasStateAuthority) return;

            RaycastHit hit;
            if (UnityEngine.Physics.SphereCast(transform.position, vacuumRadius, transform.forward, out hit, vacuumDistance))
            {
                var drawingHandle = hit.collider.GetComponent<DrawingHandle>();
                if (drawingHandle != null)
                {
                    var networkLineDrawing = drawingHandle.GetComponentInParent<NetworkLineDrawing>();
                    if (networkLineDrawing != null && networkLineDrawing.Object != null)
                    {
                        Runner.Despawn(networkLineDrawing.Object);
                    }
                }
            }
        }
    }
}