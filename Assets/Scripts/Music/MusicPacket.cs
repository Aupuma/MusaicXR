using System.Collections.Generic;

[System.Serializable]
public class MusicPacket
{
    public string clipID; // The ID of the clip
    public Dictionary<string, float> parameters; // Parameters like "x", "y" with values between 0 and 1
}