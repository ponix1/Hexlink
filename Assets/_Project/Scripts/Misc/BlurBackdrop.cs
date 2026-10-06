using UnityEngine;

// Blurred backdrop for the Transparent design: tiny enabled cameras (the
// PuzzlePreview-proven pattern - URP renders targetTexture cameras
// automatically, never call Camera.Render manually) capture the scene into
// heavily downsampled render textures. Frosted surfaces display the capture
// through plain RawImages. The cameras are parented under Camera.main so they
// follow it with no per-frame code.
public static class BlurBackdrop
{
    // Screen size divided by this = capture resolution; the tiny texture
    // bilinear-upscaled gives a cheap blur with no post-processing pass.
    private const int Downsample = 24;
    private const int StrongDownsample = 48;

    // Standard and extra-strong blur captures, plus the cameras rendering them.
    private static RenderTexture texture;
    private static RenderTexture strongTexture;
    private static Camera standardCamera;
    private static Camera strongCamera;

    // Get (lazily creating) the backdrop texture; "strong" is the blurrier one.
    public static RenderTexture GetTexture(bool strong)
    {
        EnsureCreated();
        return strong ? strongTexture : texture;
    }

    // Tear everything down when leaving the Transparent design.
    public static void Shutdown()
    {
        if (texture == null) return;

        ReleaseTexture(ref texture);
        ReleaseTexture(ref strongTexture);

        if (standardCamera != null) Object.Destroy(standardCamera.gameObject);
        if (strongCamera != null) Object.Destroy(strongCamera.gameObject);
        standardCamera = null;
        strongCamera = null;
    }

    // Scene changed - re-parent to the new main camera.
    public static void Refresh()
    {
        if (texture == null) return;
        AttachToMainCamera(standardCamera);
        AttachToMainCamera(strongCamera);
    }

    private static void ReleaseTexture(ref RenderTexture renderTexture)
    {
        if (renderTexture == null) return;
        renderTexture.Release();
        Object.Destroy(renderTexture);
        renderTexture = null;
    }

    // Lazy init on first use.
    private static void EnsureCreated()
    {
        if (texture != null) return;

        texture = CreateTexture(Downsample);
        strongTexture = CreateTexture(StrongDownsample);
        standardCamera = CreateCamera(texture);
        strongCamera = CreateCamera(strongTexture);
    }

    // Downsampled RT; the 4px floor keeps it valid on any window size.
    private static RenderTexture CreateTexture(int downsample)
    {
        RenderTexture renderTexture = new RenderTexture(
            Mathf.Max(4, Screen.width / downsample),
            Mathf.Max(4, Screen.height / downsample),
            16)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        renderTexture.Create();
        return renderTexture;
    }

    // Hidden camera rendering straight into the target RT; URP renders it
    // automatically because it has a targetTexture.
    private static Camera CreateCamera(RenderTexture target)
    {
        GameObject cameraGO = new GameObject("BlurBackdropCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.targetTexture = target;
        camera.depth = -100f;
        camera.depthTextureMode = DepthTextureMode.None;
        AttachToMainCamera(camera);
        return camera;
    }

    // Copy the main camera's pose, projection and culling so the capture
    // matches what the player sees; parenting makes it follow for free.
    private static void AttachToMainCamera(Camera camera)
    {
        Camera main = Camera.main;
        if (main == null || camera == null) return;

        camera.transform.SetParent(main.transform, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.cullingMask = main.cullingMask;
        camera.clearFlags = main.clearFlags;
        camera.backgroundColor = main.backgroundColor;
        camera.fieldOfView = main.fieldOfView;
        camera.orthographic = main.orthographic;
        camera.orthographicSize = main.orthographicSize;
        camera.nearClipPlane = main.nearClipPlane;
        camera.farClipPlane = main.farClipPlane;
    }
}
