using UnityEngine;
using System.Collections.Generic;

public class MusicTest : MonoBehaviour
{
    private MusicController musicController;

    private float loopDuration = 16f; // 16-second loop
    private float elapsedTime;

    public string[] clipIDs = { "Track1", "Track9", "Track11" };

    private void Start()
    {
        // Find the MusicController component on the same object
        musicController = GetComponent<MusicController>();
        if (musicController == null)
        {
            Debug.LogError("MusicController not found on the object!");
        }
    }

    private void Update()
    {
        if (musicController == null) return;

        // Update elapsed time and loop it within the duration
        elapsedTime += Time.deltaTime;
        float normalizedTime = (elapsedTime % loopDuration) / loopDuration; // Value from 0 to 1

        // Generate a music packet with parameters for each clip
        var musicPacket = new List<MusicPacket>();

        for (int i = 0; i < clipIDs.Length; i++)
        {
            // Use a sine wave and add an offset based on the clip index
            float x = Mathf.Sin(2 * Mathf.PI * normalizedTime + i * Mathf.PI / 3) * 0.5f + 0.5f; // Range [0, 1]
            float y = Mathf.Cos(2 * Mathf.PI * normalizedTime + i * Mathf.PI / 3) * 0.5f + 0.5f; // Range [0, 1]

            // Create a packet entry for the current clip
            musicPacket.Add(new MusicPacket
            {
                clipID = clipIDs[i],
                parameters = new Dictionary<string, float>
                {
                    { "x", x }, // Example: x could map to volume
                    { "y", y }  // Example: y could map to distortion
                }
            });
        }

        // Update the MusicController with the generated packet
        musicController.ProcessMusicPacket(musicPacket);
    }
}