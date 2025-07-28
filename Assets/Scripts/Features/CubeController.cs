using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    public GameObject cubePrefab; // Предполагается, что это маленький кубик, составляющий большой куб
    public float rigidDelay = 5f;
    private GameObject[,,] cubes;

    // Здесь мы создаем наш большой куб из маленьких кубиков
    void Start()
    {
        cubes = new GameObject[10, 10, 10];

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                for (int k = 0; k < 10; k++)
                {
                    cubes[i, j, k] = Instantiate(cubePrefab, new Vector3(i, j, k), Quaternion.identity);
                    cubes[i, j, k].AddComponent<Rigidbody>();
                    cubes[i, j, k].GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                hit.collider.GetComponent<Rigidbody>().isKinematic = false;
                StartCoroutine(MakeKinematic(hit.collider.gameObject));
            }
        }
    }

    IEnumerator MakeKinematic(GameObject hitCube)
    {
        yield return new WaitForSeconds(rigidDelay);
        hitCube.GetComponent<Rigidbody>().isKinematic = true;
    }
}
