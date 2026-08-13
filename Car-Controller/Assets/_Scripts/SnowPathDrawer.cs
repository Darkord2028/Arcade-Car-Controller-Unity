using UnityEngine;

public class SnowPathDrawer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputeShader snowComputeShader;
    [SerializeField] private RenderTexture snowRenderTexture;
    [SerializeField] private SnowSurfaceController snowSurfaceController;

    [SerializeField] private LayerMask snowLayerMask = ~0;

    [Header("Config")]
    [SerializeField] private float spotSize;

    private int _snowImageId;
    private int _colorValueToAddId;
    private int _resolutionId;
    private int _xPosId;
    private int _yPosId;
    private int _spotSizeId;
    private int _drawSpotKernel;

    private Transform _myTransform;
    private Vector2Int _position;
    private Vector3 _lastDrawPosition;

    private int _threadGroupsX;
    private int _threadGroupsY;

    private void Start()
    {
        snowRenderTexture = snowSurfaceController.GetRenderTexture();
        _myTransform = transform;

        _drawSpotKernel = snowComputeShader.FindKernel("DrawSpot");
        _snowImageId = Shader.PropertyToID("snowImage");
        _colorValueToAddId = Shader.PropertyToID("colorValueToAdd");
        _resolutionId = Shader.PropertyToID("resolution");
        _xPosId = Shader.PropertyToID("xPos");
        _yPosId = Shader.PropertyToID("yPos");
        _spotSizeId = Shader.PropertyToID("spotSize");

        if (snowRenderTexture != null)
        {
            _threadGroupsX = Mathf.CeilToInt(snowRenderTexture.width / 8f);
            _threadGroupsY = Mathf.CeilToInt(snowRenderTexture.height / 8f);
        }

        _lastDrawPosition = _myTransform.position;
    }

    private void FixedUpdate()
    {
        if (Vector3.SqrMagnitude(_myTransform.position - _lastDrawPosition) < 0.001f) return;

        Ray ray = new Ray(_myTransform.position + Vector3.up * 0.5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f, snowLayerMask))
        {
            if (hit.collider.transform.IsChildOf(snowSurfaceController.transform) || hit.collider.transform == snowSurfaceController.transform)
            {
                Vector2 uv = hit.textureCoord;

                _position.x = Mathf.RoundToInt(uv.x * snowRenderTexture.width);
                _position.y = Mathf.RoundToInt(uv.y * snowRenderTexture.height);

                DrawSpot();

                _lastDrawPosition = _myTransform.position;
            }
        }
    }

    private void DrawSpot()
    {
        if (snowRenderTexture == null || snowComputeShader == null) return;

        snowComputeShader.SetTexture(_drawSpotKernel, _snowImageId, snowRenderTexture);
        snowComputeShader.SetFloat(_colorValueToAddId, 0);
        snowComputeShader.SetFloat(_resolutionId, snowRenderTexture.width);
        snowComputeShader.SetFloat(_xPosId, _position.x);
        snowComputeShader.SetFloat(_yPosId, _position.y);
        snowComputeShader.SetFloat(_spotSizeId, spotSize);

        snowComputeShader.Dispatch(_drawSpotKernel, _threadGroupsX, _threadGroupsY, 1);
    }
}