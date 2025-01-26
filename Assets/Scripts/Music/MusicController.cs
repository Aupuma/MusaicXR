using UnityEngine;
using System.Collections.Generic;

public enum EffectType
{
    Volume,
    Pitch,
    SpatialBlend,
    DistortionLevel,
    EchoDelay,
    ReverbLevel
}

[System.Serializable]
public class ParamMapping
{
    public string paramName; // Parameter name (e.g., "x", "y")
    public EffectType effectType; // The audio aspect to map to (volume, distortion, etc.)
    public float minValue = 0f; // Minimum value for the audio parameter
    public float maxValue = 1f; // Maximum value for the audio parameter
}

public class MusicController : MonoBehaviour
{
    public List<ParamMapping> paramMappings; // Global parameter mappings (editable in the editor)
    private Dictionary<string, GameObject> clipObjects = new Dictionary<string, GameObject>(); // Clip GameObjects by ID

    private LiveAudioAnalyzer audioAnalyzer;

    void Awake()
    {
        audioAnalyzer = GetComponent<LiveAudioAnalyzer>();
    }

    private void Start()
    {
        // Initialize clipObjects dictionary by finding all child objects with ClipIdentifier
        foreach (Transform child in transform)
        {
            var identifier = child.GetComponent<ClipIdentifier>();
            if (identifier != null)
            {
                clipObjects[identifier.clipID] = child.gameObject;
            }
        }

        audioAnalyzer.OnSliceAnalyzed += ProcessMusicPacket;
    }

    public void ProcessMusicPacket(List<MusicPacket> musicPacket)
    {
        HashSet<string> activeClipIDs = new HashSet<string>();

        // Update parameters for active clips
        foreach (var clipData in musicPacket)
        {
            if (clipObjects.TryGetValue(clipData.clipID, out var clipObject))
            {
                UpdateClipParameters(clipObject, clipData.parameters);
                activeClipIDs.Add(clipData.clipID);
            }
            else
            {
                Debug.LogWarning($"Clip ID {clipData.clipID} not found!");
            }
        }

        // Mute inactive clips
        foreach (var kvp in clipObjects)
        {
            if (!activeClipIDs.Contains(kvp.Key))
            {
                MuteClip(kvp.Value);
            }
        }
    }

    private void UpdateClipParameters(GameObject clipObject, Dictionary<string, float> parameters)
    {
        var audioSource = clipObject.GetComponent<AudioSource>();
        if (audioSource == null) return;

        foreach (var mapping in paramMappings)
        {
            if (parameters.TryGetValue(mapping.paramName, out var paramValue))
            {
                float mappedValue = Mathf.Lerp(mapping.minValue, mapping.maxValue, paramValue);

                // Update the appropriate audio property
                switch (mapping.effectType)
                {
                    case EffectType.Volume:
                        audioSource.volume = mappedValue;
                        break;
                    case EffectType.Pitch:
                        audioSource.pitch = mappedValue;
                        break;
                    case EffectType.SpatialBlend:
                        audioSource.spatialBlend = mappedValue;
                        break;
                    case EffectType.DistortionLevel:
                        var distortion = clipObject.GetComponent<AudioDistortionFilter>();
                        if (distortion != null) distortion.distortionLevel = mappedValue;
                        break;
                    case EffectType.EchoDelay:
                        var echo = audioSource.GetComponent<AudioEchoFilter>();
                        if (echo != null) echo.delay = mappedValue;
                        break;
                    case EffectType.ReverbLevel:
                        var reverb = audioSource.GetComponent<AudioReverbFilter>();
                        if (reverb != null) reverb.reverbLevel = mappedValue;
                        break;
                    default:
                        Debug.LogWarning($"EffectType {mapping.effectType} not handled!");
                        break;
                }
            }
        }
    }

    private void MuteClip(GameObject clipObject)
    {
        var audioSource = clipObject.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = 0f; // Mute by setting volume to zero
        }
    }
    
    void OnDestroy() {
        audioAnalyzer.OnSliceAnalyzed -= ProcessMusicPacket;
    }
}
