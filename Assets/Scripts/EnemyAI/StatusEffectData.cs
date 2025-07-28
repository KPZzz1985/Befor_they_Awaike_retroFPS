using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffect/Data")]
public class StatusEffectData : ScriptableObject
{
    public DamageType type;
    public float duration;
    public float tickInterval;
    public float tickDamage;

    [Header("Animator parameters")]
    public string enterParam;
    public string exitParam;
    public string deathTrigger;

    [Header("Effect behaviour")]
    public bool isEffectAnimation;      // Footnote: when true, we’ll disable movement & shooting
}
