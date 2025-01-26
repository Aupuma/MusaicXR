using System.Collections;
using Fusion.Addons.LineDrawing;
using Fusion.XR.Shared;
using UnityEngine;

namespace Fusion.Addons.MXPenIntegration {
    public class NetworkMXPenWithColorCycling : NetworkMXPen
    {
        protected ColorCyclingLineDrawer colorCyclingDrawer;
        private bool wasBackButtonPressed = false;  // Track previous state
        
        [Header("Vacuum Settings")]
        [SerializeField] private Renderer vacuumRenderer;
        [SerializeField] private float vacuumRadius = 0.1f;
        [SerializeField] private float vacuumDistance = 1.0f;
        [SerializeField] private float vacuumDuration = 0.5f;
        [SerializeField] private AnimationCurve vacuumScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private AnimationCurve vacuumMovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine currentVacuumCoroutine;
        private NetworkLineDrawing currentVacuumTarget;

        protected override void Awake()
        {
            base.Awake();
            colorCyclingDrawer = GetComponentInChildren<ColorCyclingLineDrawer>();
            networkLineDrawer = colorCyclingDrawer;
            
            // Ensure vacuum renderer starts disabled
            if (vacuumRenderer != null)
            {
                vacuumRenderer.enabled = false;
            }
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

        void Update(){
            // Handle front button press for vacuum effect
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

        private void PerformSphereCast()
        {
            RaycastHit hit;
            if (UnityEngine.Physics.SphereCast(transform.position, vacuumRadius, transform.forward, out hit, vacuumDistance))
            {
                var drawingHandle = hit.collider.GetComponent<DrawingHandle>();
                if (drawingHandle != null)
                {
                    var networkLineDrawing = drawingHandle.GetComponentInParent<NetworkLineDrawing>();
                    if (networkLineDrawing != null && networkLineDrawing != currentVacuumTarget)
                    {
                        // Only start new vacuum if we're not already vacuuming this target
                        if (Object.HasStateAuthority)
                        {
                            if (currentVacuumCoroutine != null)
                            {
                                StopCoroutine(currentVacuumCoroutine);
                            }
                            currentVacuumTarget = networkLineDrawing;
                            currentVacuumCoroutine = StartCoroutine(VacuumAndDestroy(networkLineDrawing));
                        }
                    }
                }
            }
        }

        private IEnumerator VacuumAndDestroy(NetworkLineDrawing drawing)
        {
            if (drawing == null || drawing.Object == null) yield break;

            Vector3 startScale = drawing.transform.localScale;
            Vector3 startPosition = drawing.transform.position;
            float elapsed = 0.0f;

            while (elapsed < vacuumDuration)
            {
                if (drawing == null || drawing.Object == null) yield break;

                float t = elapsed / vacuumDuration;
                
                // Get the current tip position for end position
                Vector3 endPosition = transform.position;
                
                // Apply curves for smooth animation
                float scaleFactor = vacuumScaleCurve.Evaluate(t);
                float movementFactor = vacuumMovementCurve.Evaluate(t);

                drawing.transform.localScale = startScale * scaleFactor;
                drawing.transform.position = Vector3.Lerp(startPosition, endPosition, movementFactor);
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (drawing != null && drawing.Object != null && Object.HasStateAuthority)
            {
                Runner.Despawn(drawing.Object);
            }

            currentVacuumTarget = null;
            currentVacuumCoroutine = null;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            
            // Clean up coroutine if active when despawned
            if (currentVacuumCoroutine != null)
            {
                StopCoroutine(currentVacuumCoroutine);
                currentVacuumCoroutine = null;
            }
        }
    }
}