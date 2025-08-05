using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Voice/Voice Library", fileName = "VoiceLibrary")]
public class VoiceLibrary : ScriptableObject
{
    [System.Serializable]
    public class VoiceEventData
    {
        public VoiceEventType eventType;
        public List<AudioClip> clips;
        [Tooltip("Priority of this voice event")]
        public int priority = 0;
        [Tooltip("Cooldown in seconds before this event can play again")]
        public float cooldown = 0f;
        [Tooltip("Chance (0-1) to speak this event")]
        [Range(0f, 1f)]
        public float sayChance = 1f;
    }

    [SerializeField]
    private List<VoiceEventData> events = new List<VoiceEventData>();

    public List<AudioClip> GetClips(VoiceEventType type)
    {
        var entry = events.Find(e => e.eventType == type);
        return entry != null ? entry.clips : new List<AudioClip>();
    }

    public int GetPriority(VoiceEventType type)
    {
        var entry = events.Find(e => e.eventType == type);
        return entry != null ? entry.priority : 0;
    }

    public float GetCooldown(VoiceEventType type)
    {
        var entry = events.Find(e => e.eventType == type);
        return entry != null ? entry.cooldown : 0f;
    }
    /// <summary>
    /// Returns the chance that a voice event will trigger.
    /// </summary>
    public float GetSayChance(VoiceEventType type)
    {
        var entry = events.Find(e => e.eventType == type);
        return entry != null ? entry.sayChance : 1f;
    }
}