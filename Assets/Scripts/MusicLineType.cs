using UnityEngine;

[CreateAssetMenu(fileName = "MusicLineType", menuName = "Music/Line Type")]
public class MusicLineType : ScriptableObject
{
    public Color color; // Match with ColorCyclingLineDrawer colors
    public AudioClip audioLoop;
    [Range(0f, 2f)] public float baseVolume = 1f;
    [Range(0f, 3f)] public float maxVolume = 2f;
    [Range(0f, 1f)] public float baseReverb = 0f;
    [Range(0f, 1f)] public float maxReverb = 1f;
    [Range(0.5f, 2f)] public float basePitch = 1f;
    [Range(0.5f, 2f)] public float maxPitch = 1.5f;
}