using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    private void Start()
    {
        // Находим камеру по тэгу MainCamera
        Camera mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        // Прикрепляем к объекту на котором находится скрипт
        mainCamera.transform.SetParent(transform);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;
    }
}