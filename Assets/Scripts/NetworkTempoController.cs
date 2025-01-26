using Fusion;
using UnityEngine;

public class NetworkTempoController : NetworkBehaviour
{
    [Networked] private float NetworkedAngle { get; set; }
    public float rotationSpeed = 30f; // degrees per second
    public float sliceAngle = 45f;
    
    // Event that local listeners can subscribe to
    public System.Action<float, float> OnSliceUpdated; // (currentAngle, sliceWidth)

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            NetworkedAngle += rotationSpeed * Runner.DeltaTime;
            NetworkedAngle = Mathf.Repeat(NetworkedAngle, 360f);
        }
    }

    public override void Render()
    {
        // Notify listeners of current slice position
        OnSliceUpdated?.Invoke(NetworkedAngle, sliceAngle);
    }
}