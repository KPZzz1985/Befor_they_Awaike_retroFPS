using UnityEngine;

public class VirtualMuscle : MonoBehaviour
{
    public CharacterJoint joint;
    public float strength;
    public Vector3 defaultRotation;
    public float animationTime;
    public float DieDuration; // ����� ���������

    private Quaternion initialRotation;
    private float timer;
    private bool isReversed = false;
    private float dieTimer = 0; // ������ ��� ������������ ���������

    void Start()
    {
        if (joint == null)
        {
            joint = GetComponent<CharacterJoint>();
            Debug.Log("[VirtualMuscle] Joint ������� �� GameObject.");
        }

        initialRotation = joint.transform.localRotation;
        Debug.Log($"[VirtualMuscle] ������� ����������: {initialRotation}");
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= animationTime)
        {
            isReversed = !isReversed;
            timer = 0;
            Debug.Log($"[VirtualMuscle] �������� ����������: {isReversed}");
        }

        ApplyMuscleForce();

        // ���������, �������� �� ���������
        if (dieTimer >= DieDuration)
        {
            strength -= Time.deltaTime; // ���������� ��������� ����
            if (strength <= 0)
            {
                Debug.Log("[VirtualMuscle] ������ ��������.");
                this.enabled = false; // ��������� ������
            }
        }
        else
        {
            dieTimer += Time.deltaTime;
        }
    }

    void ApplyMuscleForce()
    {
        Vector3 rotation = isReversed ? -defaultRotation : defaultRotation;
        Quaternion targetRotation = Quaternion.Euler(rotation) * initialRotation;

        joint.transform.localRotation = Quaternion.Slerp(joint.transform.localRotation, targetRotation, Time.deltaTime * strength);
        Debug.Log($"[VirtualMuscle] ���� ���������. ������� ��: {targetRotation}");
    }
}
