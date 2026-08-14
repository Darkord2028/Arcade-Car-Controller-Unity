using UnityEngine;

public class GlobalSnowPathDrawer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputeShader snowComputeShader;
    [SerializeField] private GlobalSnowManager snowManager;
    [SerializeField] private LayerMask snowLayerMask = ~0;

    [Header("Config")]
    [SerializeField] private float spotSize = 0.005f;

    private int _snowImageId;
    private int _resolutionId;
    private int _xPosId;
    private int _yPosId;
    private int _spotSizeId;
    private int _drawOffsetXId;
    private int _drawOffsetYId;
    private int _drawSpotKernel;

    private RenderTexture _snowRenderTexture;
    private float _worldSize;
    private Transform _myTransform;
    private Vector2Int _pixelPosition;
    private Vector3 _lastDrawPosition;

    private void Start()
    {
        _myTransform = transform;
        _lastDrawPosition = _myTransform.position;

        _snowRenderTexture = snowManager.GetGlobalTexture();
        _worldSize = snowManager.GetWorldSize();

        _drawSpotKernel = snowComputeShader.FindKernel("DrawSpot");
        _snowImageId = Shader.PropertyToID("snowImage");
        _resolutionId = Shader.PropertyToID("resolution");
        _xPosId = Shader.PropertyToID("xPos");
        _yPosId = Shader.PropertyToID("yPos");
        _spotSizeId = Shader.PropertyToID("spotSize");
        _drawOffsetXId = Shader.PropertyToID("drawOffsetX");
        _drawOffsetYId = Shader.PropertyToID("drawOffsetY");
    }

    private void FixedUpdate()
    {
        if (Vector3.SqrMagnitude(_myTransform.position - _lastDrawPosition) < 0.001f) return;

        Ray ray = new Ray(_myTransform.position + Vector3.up * 0.5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f, snowLayerMask))
        {
            float uvX = (hit.point.x / _worldSize) + 0.5f;
            float uvY = (hit.point.z / _worldSize) + 0.5f;

            if (uvX >= 0f && uvX <= 1f && uvY >= 0f && uvY <= 1f)
            {
                _pixelPosition.x = Mathf.RoundToInt(uvX * _snowRenderTexture.width);
                _pixelPosition.y = Mathf.RoundToInt(uvY * _snowRenderTexture.height);

                DrawSpot();
                _lastDrawPosition = _myTransform.position;
            }
        }
    }

    private void DrawSpot()
    {
        if (_snowRenderTexture == null) return;

        int radiusPx = Mathf.CeilToInt(spotSize * _snowRenderTexture.width);

        int minX = Mathf.Max(0, _pixelPosition.x - radiusPx);
        int minY = Mathf.Max(0, _pixelPosition.y - radiusPx);
        int maxX = Mathf.Min(_snowRenderTexture.width, _pixelPosition.x + radiusPx);
        int maxY = Mathf.Min(_snowRenderTexture.height, _pixelPosition.y + radiusPx);

        int drawWidth = maxX - minX;
        int drawHeight = maxY - minY;

        if (drawWidth <= 0 || drawHeight <= 0) return;

        snowComputeShader.SetTexture(_drawSpotKernel, _snowImageId, _snowRenderTexture);
        snowComputeShader.SetFloat(_resolutionId, _snowRenderTexture.width);
        snowComputeShader.SetFloat(_xPosId, _pixelPosition.x);
        snowComputeShader.SetFloat(_yPosId, _pixelPosition.y);
        snowComputeShader.SetFloat(_spotSizeId, spotSize);

        snowComputeShader.SetInt(_drawOffsetXId, minX);
        snowComputeShader.SetInt(_drawOffsetYId, minY);

        int groupsX = Mathf.CeilToInt(drawWidth / 8f);
        int groupsY = Mathf.CeilToInt(drawHeight / 8f);

        snowComputeShader.Dispatch(_drawSpotKernel, groupsX, groupsY, 1);
    }
}