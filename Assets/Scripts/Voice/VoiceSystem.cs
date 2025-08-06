using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoiceSystem : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the EnemyHealth component (optional; auto-assigned if null)")]
    private EnemyHealth enemyHealth;
    [SerializeField]
    [Tooltip("Reference to the Damageable component for head part to stop voice when destroyed")]
    private Damageable headDamageable;

    [SerializeField]
    private VoiceLibrary voiceLibrary;

    private AudioSource audioSource;

    // Queue for pending voice events
    private Queue<VoiceEventType> _queue = new Queue<VoiceEventType>();

    private bool _isPlaying = false;
    private VoiceEventType _currentEvent;

    // Last event times for cooldown management
    private Dictionary<VoiceEventType, float> _lastEventTimes = new Dictionary<VoiceEventType, float>();

    // Last played clip index per event type, to avoid immediate repeats
    private Dictionary<VoiceEventType, int> _lastPlayedIndex = new Dictionary<VoiceEventType, int>();

    private void Awake()
    {
        // Ensure an AudioSource is present and enabled
        audioSource = GetComponent<AudioSource>();
        // Do not recreate or force-enable AudioSource, preserve editor settings
        // Assign EnemyHealth if not set
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnCriticalDamageChanged += OnCriticalDamageChanged;
            enemyHealth.OnDeath += OnEnemyDeath;
        }
        // Subscribe to head Damageable if assigned
        if (headDamageable != null)
            headDamageable.OnPartDestroyed += OnDamageableDestroyed;
    }
    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnCriticalDamageChanged -= OnCriticalDamageChanged;
            enemyHealth.OnDeath -= OnEnemyDeath;
        }
        if (headDamageable != null)
            headDamageable.OnPartDestroyed -= OnDamageableDestroyed;
    }

    /// <summary>
    /// Triggers a voice event; will play immediately if possible or enqueue.
    /// </summary>
    public void TriggerEvent(VoiceEventType type)
    {
        // Skip based on overall say chance for this event
        float sayChance = voiceLibrary.GetSayChance(type);
        if (Random.value > sayChance)
            return;
        float cooldown = voiceLibrary.GetCooldown(type);
        _lastEventTimes.TryGetValue(type, out var lastTime);

        if (Time.time - lastTime < cooldown)
            return;

        _lastEventTimes[type] = Time.time;

        if (!_isPlaying || voiceLibrary.GetPriority(type) >= voiceLibrary.GetPriority(_currentEvent))
        {
            Play(type);
        }
        else if (!_queue.Contains(type))
        {
            _queue.Enqueue(type);
        }
    }

    private void Play(VoiceEventType type)
    {
        _currentEvent = type;
        _isPlaying = true;

        var clip = GetRandomClip(type);
        if (clip == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        Invoke(nameof(OnClipFinished), clip.length);
    }

    private void OnClipFinished()
    {
        _isPlaying = false;
        if (_queue.Count > 0)
        {
            Play(_queue.Dequeue());
        }
    }
    private void OnDisable()
    {
        // Stop and clear voice when component is disabled
        StopAllVoice();
    }
    /// <summary>
    /// Called when critical damage state changes. Stops all voice if entering critical.
    /// </summary>
    private void OnCriticalDamageChanged(bool isCritical)
    {
        if (isCritical)
            StopAllVoice();
    }
    /// <summary>
    /// Called when the linked Damageable (e.g., head) is destroyed. Stops all voice.
    /// </summary>
    private void OnDamageableDestroyed()
    {
        StopAllVoice();
    }
    /// <summary>
    /// Stops current and queued voice playback.
    /// </summary>
    private void StopAllVoice()
    {
        // Stop any ongoing playback
        if (audioSource != null)
            audioSource.Stop();
        _queue.Clear();
        _isPlaying = false;
        CancelInvoke(nameof(OnClipFinished));
    }

    /// <summary>
    /// Returns a random clip for the event, avoiding immediate repeats.
    /// </summary>
    private AudioClip GetRandomClip(VoiceEventType type)
    {
        var clips = voiceLibrary.GetClips(type);
        if (clips == null || clips.Count == 0)
            return null;

        if (clips.Count == 1)
        {
            _lastPlayedIndex[type] = 0;
            return clips[0];
        }

        _lastPlayedIndex.TryGetValue(type, out var lastIndex);
        int newIndex;
        do
        {
            newIndex = Random.Range(0, clips.Count);
        } while (newIndex == lastIndex);

        _lastPlayedIndex[type] = newIndex;
        return clips[newIndex];
    }

    /// <summary>
    /// Called when the enemy dies (final death event).
    /// </summary>
    private void OnEnemyDeath(EnemyHealth eh)
    {
        StopAllVoice();
        enabled = false;
    }

    private void Update()
    {
        // Stop voice if enemy is dead, in special death, or head part destroyed
        bool shouldStop = false;
        if (enemyHealth != null && (enemyHealth.isHandlingDeath || enemyHealth.isDead))
        {
            shouldStop = true;
        }
        // Head destruction handled by event subscription, no need to poll here
        if (shouldStop)
        {
            StopAllVoice();
            // preserve AudioSource component; only stop playback
            enabled = false;
        }
    }
}