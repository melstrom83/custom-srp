using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

enum Pass
{
    Copy,
    BloomHorizontal,
    BloomVertical,
    BloomCombine,
    BloomPrefilter,
    BloomPrefilterFireflies,
    BloomScatter,
    ToneMappingNone,
    ToneMappingACES,
    ToneMappingNeutral,
    ToneMappingReinhard
}

public partial class PostFXStack
{
    const string bufferName = "PostFX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };

    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;

    bool useHDR;

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");

    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(var i = 0; i < maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    partial void ApplySceneViewState();

#if UNITY_EDITOR
    partial void ApplySceneViewState()
    {
        if(camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
        {
            settings = null;
        }
    }
#endif

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,
        bool useHDR)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;

        ApplySceneViewState();
    }

    public bool IsActive => settings != null;

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,
            MeshTopology.Triangles, 3);
    }

    public void Render(int sourceId)
    {
        //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

        if(DoBloom(sourceId))
        {
            DoToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool DoBloom(int sourceId)
    {
        var bloom = settings.Bloom;
        var width = camera.pixelWidth / 2;
        var height = camera.pixelHeight / 2;
        var format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2.0f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        var fromId = bloomPrefilterId;
        var toId = bloomPyramidId + 1;

        if (bloom.maxIterations == 0
            || bloom.intensity <= 0.0f
            || width < bloom.downscaleLimit * 2
            || height < bloom.downscaleLimit * 2)
        {
            return false;
        }

        buffer.BeginSample("Bloom");

        int i;
        for(i = 0; i < bloom.maxIterations; ++i)
        {
            if(width < bloom.downscaleLimit || height < bloom.downscaleLimit)
            {
                break;
            }

            var midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        

        buffer.SetGlobalFloat(bloomBicubicUpsamplingId,
            bloom.bicubicUpsampling ? 1.0f : 0.0f);

        float finalIntensity;
        Pass combinePass;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = Pass.BloomCombine;
            buffer.SetGlobalFloat(bloomIntensityId, 1.0f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }

        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for (i -= 1; i > 0; --i)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT(bloomResultId, camera.pixelHeight, camera.pixelWidth, 0,
            FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, combinePass);
        buffer.ReleaseTemporaryRT(fromId);
        

        buffer.EndSample("Bloom");

        return true;
    }

    void DoToneMapping(int sourceId)
    {
        var mode = settings.ToneMapping.mode;
        var pass = Pass.ToneMappingNone + (int)mode;
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, pass);
    }
}
