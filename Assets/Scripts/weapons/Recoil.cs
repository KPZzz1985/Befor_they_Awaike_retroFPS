using UnityEngine;

public class Recoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float recoilRange = 0.5f; // �������� ������
    public float recoilSpeed = 10f; // �������� ����������� ������
    public float recoilDuration = 0.1f; // ����������������� ������
    public AnimationCurve recoilCurve; // ������, �������� ���� ������

    [Header("Object Settings")]
    public Transform recoilTransform; // ������, ������� ������������ �� ����� ������

    [Header("Input Settings")]
    public KeyCode fireButton = KeyCode.Mouse0; // ������ ��������

    private bool canFire = true; // ����, ������������, ����� �� ������� ������
    private float recoilTime; // ����� ��������� � ������ ������
    private Quaternion originalRotation; // �������� ���������� �������

    private void Start()
    {
        originalRotation = recoilTransform.localRotation; // ��������� �������� ����������
    }

    private void FixedUpdate()
    {
        // ���������� ������ ������� � �������� ����������
        recoilTransform.localRotation = Quaternion.Lerp(recoilTransform.localRotation, originalRotation, Time.deltaTime * recoilSpeed);

        // �������� ����� �� ����������������� ������
        recoilTime -= Time.deltaTime;

        // ���� ����� ������ ����� 0, �� ����� ������� ������ �����
        if (recoilTime <= 0)
        {
            canFire = true;
        }
    }

    private void Update()
    {
        // ���� ��� ���������� ������, �� �������
        if (!canFire)
        {
            return;
        }

        // ���� ������ ������ ��������, �� �������� ������
        if (Input.GetKeyDown(fireButton))
        {
            // ������� ��������� ���������� �� ���������
            float recoilAmount = recoilCurve.Evaluate(Random.value) * recoilRange;
            Quaternion recoilRotation = Quaternion.AngleAxis(recoilAmount, transform.up);

            // ��������� ���������� � ���������� �������
            Quaternion targetRotation = recoilTransform.localRotation * recoilRotation;
            recoilTransform.localRotation = Quaternion.RotateTowards(recoilTransform.localRotation, targetRotation, 180f);

            // ������������� ����� ������ � ����, ��� ������ ������� ������ �����
            recoilTime = recoilDuration;
            canFire = false;
        }
    }
}