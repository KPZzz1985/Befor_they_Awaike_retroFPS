using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DecalPrefabData
{
    public GameObject prefab;
    public float minScale = 0.1f;
    public float maxScale = 1.0f;

    public DecalPrefabData(GameObject prefab, float minScale, float maxScale)
    {
        this.prefab = prefab;
        this.minScale = minScale;
        this.maxScale = maxScale;
    }
}

public class SimpleDecalContainer : MonoBehaviour
{
    // ��������� ������ �������� ������� � ����������� ������������ �������
    public List<DecalPrefabData> decalDatas = new List<DecalPrefabData>();

    public GameObject CreateRandomDecal(Vector3 position, Quaternion rotation)
    {
        if (decalDatas.Count == 0)
        {
            Debug.LogWarning("Decal datas list is empty.");
            return null;
        }

        // �������� ��������� ������ �� ����������
        int index = UnityEngine.Random.Range(0, decalDatas.Count);
        DecalPrefabData decalData = decalDatas[index];
        if (decalData.prefab != null)
        {
            // ������ ������ � �������� �������� � ���������
            GameObject decal = Instantiate(decalData.prefab, position, rotation);

            // ������������ �������� ������ ��������� ��� Y
            float randomYRotation = UnityEngine.Random.Range(0f, 360f);
            decal.transform.Rotate(0f, randomYRotation, 0f, Space.Self);

            // ������������ ��������
            float scale = UnityEngine.Random.Range(decalData.minScale, decalData.maxScale);
            decal.transform.localScale = new Vector3(scale, scale, scale);

            return decal;
        }

        return null;
    }
}
