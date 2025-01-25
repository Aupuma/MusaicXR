using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace Fusion.Addons.LineDrawing 
{
    public class ColorCyclingLineDrawer : NetworkLineDrawer 
    {
        [Header("Color Settings")]
        [SerializeField] 
        private List<Color> availableColors = new List<Color> {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.magenta,
            Color.cyan
        };

        [Header("Visual Indicator")]
        [SerializeField]
        private MeshRenderer colorIndicatorSphere;

        [Networked]
        private int CurrentColorIndex { get; set; }

        protected override void Awake()
        {
            base.Awake();
            
            // If no color indicator is assigned, try to find or create one
            if (colorIndicatorSphere == null)
            {
                // Try to find an existing indicator first
                colorIndicatorSphere = tip.GetComponentInChildren<MeshRenderer>();
                
                // If none exists, create one
                if (colorIndicatorSphere == null)
                {
                    CreateColorIndicator();
                }
            }
        }

        private void CreateColorIndicator()
        {
            // Create a small sphere as a child of the tip
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "ColorIndicator";
            indicator.transform.SetParent(tip);
            
            // Position it slightly offset from the tip
            indicator.transform.localPosition = new Vector3(0, 0.02f, 0); // Adjust these values as needed
            indicator.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f); // Adjust size as needed
            
            // Get the renderer component
            colorIndicatorSphere = indicator.GetComponent<MeshRenderer>();
            
            // Remove the collider as we don't need physics
            if (indicator.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }
        }

        public void CycleColor()
        {
            if (Object.HasStateAuthority)
            {
                CurrentColorIndex = (CurrentColorIndex + 1) % availableColors.Count;
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            color = availableColors[CurrentColorIndex];
            
            // Update the indicator sphere color
            if (colorIndicatorSphere != null)
            {
                // Create an emissive material using URP lit shader
                Material indicatorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                indicatorMaterial.SetColor("_BaseColor", color);
                indicatorMaterial.SetColor("_EmissionColor", color * 0.5f);
                indicatorMaterial.EnableKeyword("_EMISSION");
                colorIndicatorSphere.material = indicatorMaterial;
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            UpdateColor();
        }

        public override void Render()
        {
            base.Render();
            // Ensure color is synchronized across network
            if (!Object.HasStateAuthority)
            {
                UpdateColor();
            }
        }
    }
}