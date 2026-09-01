using System.Collections.Generic;
using UnityEngine;

public class GlobalSnowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputeShader snowComputeShader;
    [SerializeField] private RenderTexture globalSnowTexture;

    [Header("Config")]
    [SerializeField] private int resolution = 2048;
    [SerializeField] private float worldSize = 100f;

    [Header("Snow Regeneration")]
    [SerializeField] private bool enableRegeneration = true;
    [SerializeField] private float fillRate = 0.02f;
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Snow Mesh")]
    [SerializeField] private Transform snowMeshParent;
    [SerializeField] private float meshBoundingExpansionAmount;

    private int _fillBlackKernel;
    private int _fillSnowKernel;

    private int _snowImageId;
    private int _fillRateId;
    private int _threadGroups;
    private float _timer;

    private void Awake()
    {
        ExpandBoundingBoxOfSnowPlanes();
        InitializeRenderTexture();
        SetGlobalShaderVariables();

        _fillSnowKernel = snowComputeShader.FindKernel("FillSnow");
        _snowImageId = Shader.PropertyToID("snowImage");
        _fillRateId = Shader.PropertyToID("fillRate");

        _threadGroups = Mathf.CeilToInt(resolution / 8f);
    }

    private void Update()
    {
        if (!enableRegeneration) return;

        _timer += Time.deltaTime;

        if (_timer >= updateInterval)
        {
            _timer = 0f;
            FillSnowLayer();
        }
    }

    private void ExpandBoundingBoxOfSnowPlanes()
    {
        MeshFilter[] meshFilters = snowMeshParent.GetComponentsInChildren<MeshFilter>();
        HashSet<Mesh> processedMeshes = new HashSet<Mesh>();

        foreach (MeshFilter filter in meshFilters)
        {
            Mesh sharedMesh = filter.sharedMesh;

            if (sharedMesh != null && !processedMeshes.Contains(sharedMesh))
            {
                Bounds bounds = sharedMesh.bounds;
                bounds.Expand(meshBoundingExpansionAmount);
                sharedMesh.bounds = bounds;

                processedMeshes.Add(sharedMesh);
            }
        }
    }

    private void InitializeRenderTexture()
    {
        if (globalSnowTexture == null)
        {
            globalSnowTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            globalSnowTexture.enableRandomWrite = true;

            globalSnowTexture.wrapMode = TextureWrapMode.Clamp;
            globalSnowTexture.Create();
        }

        _fillBlackKernel = snowComputeShader.FindKernel("FillBlack");
        snowComputeShader.SetTexture(_fillBlackKernel, "snowImage", globalSnowTexture);
        snowComputeShader.Dispatch(_fillBlackKernel, resolution / 8, resolution / 8, 1);
    }

    private void SetGlobalShaderVariables()
    {
        Shader.SetGlobalTexture("_PathTexture", globalSnowTexture);
        Shader.SetGlobalFloat("_SnowWorldSize", worldSize);
    }

    private void FillSnowLayer()
    {
        if (globalSnowTexture == null) return;

        snowComputeShader.SetTexture(_fillSnowKernel, _snowImageId, globalSnowTexture);
        snowComputeShader.SetFloat(_fillRateId, fillRate);

        snowComputeShader.Dispatch(_fillSnowKernel, _threadGroups, _threadGroups, 1);
    }

    public RenderTexture GetGlobalTexture()
    {
        return globalSnowTexture;
    }

    public float GetWorldSize()
    {
        return worldSize;
    }

}
