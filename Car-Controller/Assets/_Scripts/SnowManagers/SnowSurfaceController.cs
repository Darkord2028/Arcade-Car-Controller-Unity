using UnityEngine;

public class SnowSurfaceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputeShader snowComputeShader;
    [SerializeField] private RenderTexture snowRenderTexture;
    [SerializeField] private float colorValueToAdd;

    [Header("Config")]
    [SerializeField] private int resolution = 512;

    private string _snowImageProperty = "snowImage";
    private string _colorValueToAddProperty = "colorValueToAdd";
    private string _resolutionProperty = "resolution";
    private string _xPosProperty = "xPos";
    private string _yPosProperty = "yPos";
    private string _spotSizeProperty = "spotSize";

    private string _initKernel = "Init";
    private string _fillBlackKernel = "FillBlack";

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    private void Awake()
    {
        CreateRenderTexture();
        SetRenderTextureColorBlack();
        SetMaterialTexture();
        ExtendMeshBounds();
    }

    private void CreateRenderTexture()
    {
        if (snowRenderTexture != null) return;
        snowRenderTexture = new RenderTexture(resolution, resolution, 24);
        snowRenderTexture.enableRandomWrite = true;
        snowRenderTexture.Create();
    }

    private void SetRenderTextureColorBlack()
    {
        int kernel_handle = snowComputeShader.FindKernel(_fillBlackKernel);
        snowComputeShader.SetTexture(kernel_handle, _snowImageProperty, snowRenderTexture);
        snowComputeShader.SetFloat(_colorValueToAddProperty, colorValueToAdd);
        snowComputeShader.SetFloat(_resolutionProperty, resolution);
        snowComputeShader.SetFloat(_xPosProperty, 0);
        snowComputeShader.SetFloat(_yPosProperty, 0);
        snowComputeShader.SetFloat(_spotSizeProperty, 0);
        snowComputeShader.Dispatch(kernel_handle, snowRenderTexture.width / 8, snowRenderTexture.height / 8, 1);
    }

    private void SetMaterialTexture()
    {
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _meshRenderer.material.SetTexture("_PathTexture", snowRenderTexture);
    }

    private void AddSnowLayer()
    {
        int kernel_handle = snowComputeShader.FindKernel(_initKernel);
        snowComputeShader.SetTexture(kernel_handle, _snowImageProperty, snowRenderTexture);
        snowComputeShader.SetFloat(_colorValueToAddProperty, colorValueToAdd);
        snowComputeShader.SetFloat(_resolutionProperty, resolution);
        snowComputeShader.SetFloat(_xPosProperty, 0);
        snowComputeShader.SetFloat(_yPosProperty, 0);
        snowComputeShader.SetFloat(_spotSizeProperty, 0);
        snowComputeShader.Dispatch(kernel_handle, snowRenderTexture.width / 8, snowRenderTexture.height / 8, 1);
    }

    private void ExtendMeshBounds()
    {
        _meshFilter = GetComponentInChildren<MeshFilter>();
        Bounds bounds = _meshFilter.mesh.bounds;
        bounds.extents = new Vector3(2, 0, 2);
        _meshFilter.mesh.bounds = bounds;
    }

    public RenderTexture GetRenderTexture() { return snowRenderTexture; }
}