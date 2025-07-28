using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AffineTransformRenderer : MonoBehaviour
{
    public float scale = 0.1f;
    private Camera _camera;
    private Matrix4x4 originalProjectionMatrix;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        originalProjectionMatrix = _camera.projectionMatrix;
    }

    private void OnPreCull()
    {
        Matrix4x4 affineMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1 / scale, 1 / scale, 1));
        _camera.projectionMatrix = originalProjectionMatrix * affineMatrix;
    }

    private void OnPostRender()
    {
        _camera.projectionMatrix = originalProjectionMatrix;
    }
}