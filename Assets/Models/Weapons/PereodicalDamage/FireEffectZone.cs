using UnityEngine;

//Footnote: this component applies a periodic-fire effect to all colliders entering its trigger zone.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FireEffectZone : MonoBehaviour
{
    [Tooltip("Data asset that describes the fire status effect (duration, tick, animation etc.)")]
    public StatusEffectData fireEffectData;

    [Tooltip("Which layers will receive the fire effect (e.g. Enemy)")]
    public LayerMask affectedLayers;

    private void Reset()
    {
        //Footnote: ����������������� ��������� � Rigidbody � ���������
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Footnote: ���������, ��� ���� ������� ������ � �����
        if (((1 << other.gameObject.layer) & affectedLayers) == 0)
            return;

        //Footnote: ���� �� ����� ����� StatusEffectHandler
        var handler = other.GetComponentInParent<StatusEffectHandler>();
        if (handler != null)
        {
            handler.ApplyEffect(fireEffectData);
        }
    }
}
