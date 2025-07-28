using System.Collections.Generic;
using UnityEngine;
using Knife.DeferredDecals; // пространство имён, где лежит класс Decal

/// <summary>
/// Скрипт-лимитер, который не даёт в сцене появиться более MaxDecals декалям.
/// При появлении (OnEnable) новый Decal попадает в очередь; 
/// если очередь переполняется, самый старый декаль уничтожается.
/// </summary>
[DisallowMultipleComponent]
public class DecalCountLimiter : MonoBehaviour
{
    /// <summary>
    /// Максимально допустимое число декалей в сцене.
    /// </summary>
    [Tooltip("Максимальное число одновременно отображаемых декалей в сцене")]
    public int MaxDecals = 100;

    // Очередь всех активных декалей (FIFO)
    private static Queue<Decal> _decalQueue = new Queue<Decal>();

    // Для того, чтобы не дублировать Destroy (если декаль уничтожили другими способами),
    // будем хранить вспомогательную коллекцию, чтобы проверять, не лежит ли Decal уже в очереди.
    private static HashSet<Decal> _inQueue = new HashSet<Decal>();

    private Decal _myDecal;

    private void Awake()
    {
        // Предполагаем, что на том же GameObject есть компонент Decal
        _myDecal = GetComponent<Decal>();
        if (_myDecal == null)
        {
            Debug.LogWarning($"На объекте {gameObject.name} нет компонента Decal, но висит DecalCountLimiter. Скрипт будет отключён.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Если у нас есть Decal, регистрируем его в очередь
        if (_myDecal == null) return;

        // Если вдруг этот Decal уже в очередь попал (не должно случиться при нормальной работе), ничего не делаем
        if (_inQueue.Contains(_myDecal)) return;

        // Положим в очередь
        _decalQueue.Enqueue(_myDecal);
        _inQueue.Add(_myDecal);

        // Если превысили лимит, удаляем старейший элемент
        if (_decalQueue.Count > MaxDecals)
        {
            Decal oldest = _decalQueue.Dequeue();
            _inQueue.Remove(oldest);
            if (oldest != null && oldest.gameObject != null)
            {
                // Удаляем сам GameObject с декалью
                Destroy(oldest.gameObject);
            }
        }
    }

    private void OnDisable()
    {
        // Если кто-то вызвал Disable/Awake перед Destroy, нам нужно убрать декаль из очереди
        if (_myDecal != null && _inQueue.Contains(_myDecal))
        {
            // Простой способ — пересобрать очередь без этого декаля
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
        // На всякий случай тоже чистим при Destroy
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
