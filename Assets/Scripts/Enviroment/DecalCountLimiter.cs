using System.Collections.Generic;
using UnityEngine;
using Knife.DeferredDecals; // ������������ ���, ��� ����� ����� Decal

/// <summary>
/// ������-�������, ������� �� ��� � ����� ��������� ����� MaxDecals �������.
/// ��� ��������� (OnEnable) ����� Decal �������� � �������; 
/// ���� ������� �������������, ����� ������ ������ ������������.
/// </summary>
[DisallowMultipleComponent]
public class DecalCountLimiter : MonoBehaviour
{
    /// <summary>
    /// ����������� ���������� ����� ������� � �����.
    /// </summary>
    [Tooltip("������������ ����� ������������ ������������ ������� � �����")]
    public int MaxDecals = 100;

    // ������� ���� �������� ������� (FIFO)
    private static Queue<Decal> _decalQueue = new Queue<Decal>();

    // ��� ����, ����� �� ����������� Destroy (���� ������ ���������� ������� ���������),
    // ����� ������� ��������������� ���������, ����� ���������, �� ����� �� Decal ��� � �������.
    private static HashSet<Decal> _inQueue = new HashSet<Decal>();

    private Decal _myDecal;

    private void Awake()
    {
        // ������������, ��� �� ��� �� GameObject ���� ��������� Decal
        _myDecal = GetComponent<Decal>();
        if (_myDecal == null)
        {
            Debug.LogWarning($"�� ������� {gameObject.name} ��� ���������� Decal, �� ����� DecalCountLimiter. ������ ����� ��������.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // ���� � ��� ���� Decal, ������������ ��� � �������
        if (_myDecal == null) return;

        // ���� ����� ���� Decal ��� � ������� ����� (�� ������ ��������� ��� ���������� ������), ������ �� ������
        if (_inQueue.Contains(_myDecal)) return;

        // ������� � �������
        _decalQueue.Enqueue(_myDecal);
        _inQueue.Add(_myDecal);

        // ���� ��������� �����, ������� ��������� �������
        if (_decalQueue.Count > MaxDecals)
        {
            Decal oldest = _decalQueue.Dequeue();
            _inQueue.Remove(oldest);
            if (oldest != null && oldest.gameObject != null)
            {
                // ������� ��� GameObject � �������
                Destroy(oldest.gameObject);
            }
        }
    }

    private void OnDisable()
    {
        // ���� ���-�� ������ Disable/Awake ����� Destroy, ��� ����� ������ ������ �� �������
        if (_myDecal != null && _inQueue.Contains(_myDecal))
        {
            // ������� ������ � ����������� ������� ��� ����� ������
            Queue<Decal> newQueue = new Queue<Decal>(_decalQueue.Count);
            foreach (var d in _decalQueue)
            {
                if (d != _myDecal) newQueue.Enqueue(d);
            }
            _decalQueue = newQueue;
            _inQueue.Remove(_myDecal);
        }
    }

    private void OnDestroy()
    {
        // �� ������ ������ ���� ������ ��� Destroy
        if (_myDecal != null && _inQueue.Contains(_myDecal))
        {
            Queue<Decal> newQueue = new Queue<Decal>(_decalQueue.Count);
            foreach (var d in _decalQueue)
            {
                if (d != _myDecal) newQueue.Enqueue(d);
            }
            _decalQueue = newQueue;
            _inQueue.Remove(_myDecal);
        }
    }
}
