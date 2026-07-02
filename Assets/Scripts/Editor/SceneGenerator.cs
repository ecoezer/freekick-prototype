using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Oyun sahnesini sıfırdan kurar: saha, kale (file + direkler), kaleci, baraj,
/// top, kamera, ışık, oyun sistemleri ve tüm referans bağlantıları.
/// Materyaller Assets/Materials altına ASSET olarak kaydedilir
/// (sahneye gömülü kaçak materyal = build'de pembe obje sorunu yaşanmaz).
/// </summary>
public class SceneGenerator
{
    private const float GoalZ = 35f;

    public static void GenerateScene()
    {
        Debug.Log("[SceneGenerator] Starting scene generation...");
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Materyal asset'leri ─────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Texture2D grassTex = MakeGrassTexture();

        Material grassLight = MakeMaterial("GrassLight", new Color(0.95f, 1f, 0.92f));
        grassLight.mainTexture = grassTex;
        grassLight.mainTextureScale = new Vector2(26, 26);
        grassLight.SetFloat("_Glossiness", 0.08f);

        Material grassDark  = MakeMaterial("GrassDark",  new Color(0.78f, 0.85f, 0.75f));
        grassDark.mainTexture = grassTex;
        grassDark.mainTextureScale = new Vector2(26, 1.2f);
        grassDark.SetFloat("_Glossiness", 0.08f);
        Material lineMat    = MakeMaterial("PitchLine",  new Color(0.96f, 0.96f, 0.96f));
        Material postMat    = MakeMaterial("GoalPost",   Color.white);
        Material keeperMat  = MakeMaterial("Keeper",     new Color(1f, 0.5f, 0.05f));
        Material shortsMat  = MakeMaterial("Shorts",     new Color(0.12f, 0.12f, 0.14f));
        Material wallMat    = MakeMaterial("WallPlayer", new Color(0.75f, 0.12f, 0.15f));
        Material skinMat    = MakeMaterial("Skin",       new Color(0.9f, 0.72f, 0.58f));
        Material boardMat   = MakeMaterial("AdBoard",    new Color(0.12f, 0.22f, 0.55f));
        boardMat.EnableKeyword("_EMISSION");
        boardMat.SetColor("_EmissionColor", new Color(0.05f, 0.1f, 0.3f));
        Material standMat   = MakeMaterial("Stand",      new Color(0.13f, 0.15f, 0.22f));

        // Gerçek file görünümü: alfa-cutout ızgara dokusu.
        Material netMat = new Material(Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse"));
        netMat.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        netMat.mainTexture = MakeNetTexture();
        netMat.mainTextureScale = new Vector2(14, 6);
        netMat.SetFloat("_Cutoff", 0.5f);
        AssetDatabase.CreateAsset(netMat, "Assets/Materials/Net.mat");

        Material aimLineMat = new Material(Shader.Find("Sprites/Default"));
        AssetDatabase.CreateAsset(aimLineMat, "Assets/Materials/AimLine.mat");

        Material ballMat = MakeMaterial("Ball", Color.white);
        ballMat.mainTexture = MakeBallTexture();

        var ballPhysicsMat = new PhysicsMaterial("BallPhysics")
        {
            bounciness      = 0.55f,
            dynamicFriction = 0.4f,
            staticFriction  = 0.4f,
            bounceCombine   = PhysicsMaterialCombine.Maximum
        };
        AssetDatabase.CreateAsset(ballPhysicsMat, "Assets/Materials/BallPhysics.physicMaterial");

        // ── Işık ve atmosfer ────────────────────────────────
        GameObject lightObj = new GameObject("Sun");
        Light light = lightObj.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.35f;
        light.color     = new Color(1f, 0.956f, 0.89f);
        light.shadows   = LightShadows.Soft;
        light.shadowStrength = 0.85f;
        lightObj.transform.rotation = Quaternion.Euler(38, -42, 0);

        // Prosedürel gökyüzü (gerçekçi atmosfer + güneş diski).
        Material skyMat = new Material(Shader.Find("Skybox/Procedural"));
        skyMat.SetFloat("_SunSize", 0.035f);
        skyMat.SetFloat("_SunSizeConvergence", 5f);
        skyMat.SetFloat("_AtmosphereThickness", 1.05f);
        skyMat.SetColor("_SkyTint", new Color(0.5f, 0.62f, 0.75f));
        skyMat.SetColor("_GroundColor", new Color(0.37f, 0.43f, 0.32f));
        skyMat.SetFloat("_Exposure", 1.25f);
        AssetDatabase.CreateAsset(skyMat, "Assets/Materials/Sky.mat");

        RenderSettings.skybox          = skyMat;
        RenderSettings.sun             = light;
        RenderSettings.ambientMode     = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.2f;

        RenderSettings.fog             = true;
        RenderSettings.fogMode         = FogMode.Linear;
        RenderSettings.fogColor        = new Color(0.72f, 0.79f, 0.88f);
        RenderSettings.fogStartDistance = 95f;
        RenderSettings.fogEndDistance   = 300f;

        // ── Saha ────────────────────────────────────────────
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Pitch"; // BallPhysicsController zemin tespitinde bu ismi kullanır.
        ground.transform.localScale = new Vector3(130, 1, 130);
        ground.transform.position   = new Vector3(0, -0.5f, 5);
        ground.GetComponent<MeshRenderer>().sharedMaterial = grassLight;

        // Çim şeritleri (görsel, collider'sız).
        for (int i = -5; i <= 5; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "GrassStripe";
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            stripe.transform.localScale = new Vector3(130, 0.012f, 6f);
            stripe.transform.position   = new Vector3(0, 0.006f, i * 12f + 2f);
            stripe.GetComponent<MeshRenderer>().sharedMaterial = grassDark;
        }

        // Saha çizgileri.
        MakeLine(lineMat, new Vector3(0, 0, GoalZ),            new Vector3(80,    0.012f, 0.12f)); // Kale çizgisi
        MakeLine(lineMat, new Vector3(0, 0, GoalZ - 16.5f),    new Vector3(40.32f,0.012f, 0.12f)); // Ceza sahası ön
        MakeLine(lineMat, new Vector3( 20.16f, 0, GoalZ-8.25f),new Vector3(0.12f, 0.012f, 16.5f)); // Ceza sahası yan
        MakeLine(lineMat, new Vector3(-20.16f, 0, GoalZ-8.25f),new Vector3(0.12f, 0.012f, 16.5f));
        MakeLine(lineMat, new Vector3(0, 0, GoalZ - 5.5f),     new Vector3(18.32f,0.012f, 0.12f)); // Kale sahası ön
        MakeLine(lineMat, new Vector3( 9.16f, 0, GoalZ-2.75f), new Vector3(0.12f, 0.012f, 5.5f));  // Kale sahası yan
        MakeLine(lineMat, new Vector3(-9.16f, 0, GoalZ-2.75f), new Vector3(0.12f, 0.012f, 5.5f));
        MakeLine(lineMat, new Vector3(0, 0, GoalZ - 11f),      new Vector3(0.25f, 0.012f, 0.25f)); // Penaltı noktası

        // Reklam panoları + tribün silüeti.
        for (int i = -1; i <= 1; i++)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "AdBoard";
            board.transform.position   = new Vector3(i * 9f, 0.45f, GoalZ + 3.5f);
            board.transform.localScale = new Vector3(8.6f, 0.9f, 0.12f);
            board.GetComponent<MeshRenderer>().sharedMaterial = boardMat;
        }

        // Tribün silüeti: arkada kademeli iki blok (fog içinde yumuşak görünür).
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Stand";
        stand.transform.position   = new Vector3(0, 4.5f, GoalZ + 26f);
        stand.transform.localScale = new Vector3(130, 9, 2);
        stand.GetComponent<MeshRenderer>().sharedMaterial = standMat;

        GameObject standUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standUpper.name = "StandUpper";
        standUpper.transform.position   = new Vector3(0, 10f, GoalZ + 32f);
        standUpper.transform.localScale = new Vector3(130, 10, 2);
        standUpper.GetComponent<MeshRenderer>().sharedMaterial = standMat;

        // ── Kale ────────────────────────────────────────────
        GameObject goal = new GameObject("Goal");
        goal.transform.position = new Vector3(0, 0, GoalZ);
        // forward = +z (saha -z tarafında). GameManager -forward'ı saha yönü olarak kullanır.

        MakePost(goal.transform, "LeftPost",  new Vector3(-3.66f, 1.22f, 0), new Vector3(0.12f, 1.22f, 0.12f), Quaternion.identity,           postMat);
        MakePost(goal.transform, "RightPost", new Vector3( 3.66f, 1.22f, 0), new Vector3(0.12f, 1.22f, 0.12f), Quaternion.identity,           postMat);
        MakePost(goal.transform, "Crossbar",  new Vector3(0, 2.5f, 0),       new Vector3(0.12f, 3.72f, 0.12f), Quaternion.Euler(0, 0, 90),    postMat);

        // File (yarı saydam, collider'lı — top kalede kalır).
        MakeNetPanel(goal.transform, "NetBack",  new Vector3(0, 1.22f, 1.7f),   new Vector3(7.44f, 2.44f, 0.06f), netMat);
        MakeNetPanel(goal.transform, "NetTop",   new Vector3(0, 2.47f, 0.85f),  new Vector3(7.44f, 0.06f, 1.7f),  netMat);
        MakeNetPanel(goal.transform, "NetLeft",  new Vector3(-3.72f, 1.22f, 0.85f), new Vector3(0.06f, 2.44f, 1.7f), netMat);
        MakeNetPanel(goal.transform, "NetRight", new Vector3( 3.72f, 1.22f, 0.85f), new Vector3(0.06f, 2.44f, 1.7f), netMat);

        // Gol algılama trigger'ı (kale ağzının hemen arkası).
        GameObject goalTrigger = new GameObject("GoalTrigger");
        goalTrigger.transform.SetParent(goal.transform);
        goalTrigger.transform.localPosition = new Vector3(0, 1.17f, 0.8f);
        BoxCollider triggerBox = goalTrigger.AddComponent<BoxCollider>();
        triggerBox.size      = new Vector3(7.2f, 2.3f, 1.3f);
        triggerBox.isTrigger = true;
        GoalDetector goalDetector = goalTrigger.AddComponent<GoalDetector>();

        // ── Top ─────────────────────────────────────────────
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position   = new Vector3(0, 0.11f, GoalZ - 22f);
        ball.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        ball.GetComponent<MeshRenderer>().sharedMaterial = ballMat;
        ball.GetComponent<SphereCollider>().sharedMaterial = ballPhysicsMat;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.45f;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        TrailRenderer trail = ball.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.12f;
        trail.endWidth   = 0.01f;
        trail.sharedMaterial = aimLineMat;
        trail.startColor = new Color(1f, 1f, 1f, 0.7f);
        trail.endColor   = new Color(1f, 1f, 1f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        BallLauncher          launcher          = ball.AddComponent<BallLauncher>();
        BallStateTracker      tracker           = ball.AddComponent<BallStateTracker>();
        BallResetter          resetter          = ball.AddComponent<BallResetter>();
        BallPhysicsController physicsController = ball.AddComponent<BallPhysicsController>();
        BallVisualFeedback    visualFeedback    = ball.AddComponent<BallVisualFeedback>();
        var hitStop   = ball.AddComponent<FreekickGame.GameFeel.HitStopManager>();
        var telemetry = ball.AddComponent<FreekickGame.Telemetry.TelemetryManager>();

        // ── Kaleci ──────────────────────────────────────────
        GameObject keeper = new GameObject("Goalkeeper");
        keeper.transform.position = new Vector3(0, 0, GoalZ - 0.7f);

        Rigidbody keeperRb = keeper.AddComponent<Rigidbody>();
        keeperRb.isKinematic = true;

        CapsuleCollider keeperCol = keeper.AddComponent<CapsuleCollider>();
        keeperCol.center = new Vector3(0, 0.95f, 0);
        keeperCol.radius = 0.35f;
        keeperCol.height = 1.9f;

        MakeVisualCapsule(keeper.transform, "Body",   new Vector3(0, 1.15f, 0), new Vector3(0.58f, 0.62f, 0.42f), keeperMat);
        MakeVisualCube(keeper.transform,    "Shorts", new Vector3(0, 0.62f, 0), new Vector3(0.52f, 0.42f, 0.4f),  shortsMat);
        MakeVisualCube(keeper.transform,    "LegL",   new Vector3(-0.14f, 0.22f, 0), new Vector3(0.17f, 0.45f, 0.2f), shortsMat);
        MakeVisualCube(keeper.transform,    "LegR",   new Vector3( 0.14f, 0.22f, 0), new Vector3(0.17f, 0.45f, 0.2f), shortsMat);
        MakeVisualSphere(keeper.transform,  "Head",   new Vector3(0, 1.78f, 0), 0.3f, skinMat);

        GoalkeeperController keeperController = keeper.AddComponent<GoalkeeperController>();

        // ── Baraj ───────────────────────────────────────────
        GameObject wallRoot = new GameObject("Wall");
        DefensiveWall wall = wallRoot.AddComponent<DefensiveWall>();

        for (int i = 0; i < 4; i++)
        {
            GameObject player = new GameObject("WallPlayer_" + i);
            player.transform.SetParent(wallRoot.transform);
            player.transform.localPosition = new Vector3(i * 0.55f, 0, 0);

            Rigidbody prb = player.AddComponent<Rigidbody>();
            prb.isKinematic = true;

            CapsuleCollider pcol = player.AddComponent<CapsuleCollider>();
            pcol.center = new Vector3(0, 0.9f, 0);
            pcol.radius = 0.26f;
            pcol.height = 1.8f;

            player.AddComponent<WallPlayer>();

            MakeVisualCapsule(player.transform, "Body",   new Vector3(0, 1.08f, 0), new Vector3(0.52f, 0.58f, 0.38f), wallMat);
            MakeVisualCube(player.transform,    "Shorts", new Vector3(0, 0.58f, 0), new Vector3(0.48f, 0.38f, 0.36f), shortsMat);
            MakeVisualCube(player.transform,    "LegL",   new Vector3(-0.12f, 0.2f, 0), new Vector3(0.16f, 0.42f, 0.18f), shortsMat);
            MakeVisualCube(player.transform,    "LegR",   new Vector3( 0.12f, 0.2f, 0), new Vector3(0.16f, 0.42f, 0.18f), shortsMat);
            MakeVisualSphere(player.transform,  "Head",   new Vector3(0, 1.66f, 0), 0.28f, skinMat);
        }

        // ── Oyun sistemleri ─────────────────────────────────
        GameObject systems = new GameObject("GameSystems");
        AimTargetProvider aimProvider = systems.AddComponent<AimTargetProvider>();
        ShotInput      input      = systems.AddComponent<ShotInput>();
        ShotController controller = systems.AddComponent<ShotController>();
        MatchReferee   referee    = systems.AddComponent<MatchReferee>();
        GameManager    gameManager= systems.AddComponent<GameManager>();
        GameHUD        hud        = systems.AddComponent<GameHUD>();

        // Yay önizleme.
        GameObject arcObj = new GameObject("AimArc");
        LineRenderer arcLine = arcObj.AddComponent<LineRenderer>();
        arcLine.startWidth = 0.07f;
        arcLine.endWidth   = 0.03f;
        arcLine.sharedMaterial = aimLineMat;
        arcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AimVisualController aimVisual = arcObj.AddComponent<AimVisualController>();

        // Nişangâh halkası.
        GameObject reticleObj = new GameObject("AimReticle");
        LineRenderer reticleLine = reticleObj.AddComponent<LineRenderer>();
        reticleLine.startWidth = 0.045f;
        reticleLine.endWidth   = 0.045f;
        reticleLine.sharedMaterial = aimLineMat;
        reticleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AimReticle reticle = reticleObj.AddComponent<AimReticle>();

        // ── Kamera ──────────────────────────────────────────
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 58f;
        cam.allowHDR    = true;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 2.8f, GoalZ - 28f);
        FreekickCameraController camController = camObj.AddComponent<FreekickCameraController>();

        // ── Post-processing (Bloom + ACES + Vignette) ───────
        var ppProfile = ScriptableObject.CreateInstance<PostProcessProfile>();

        var bloom = ppProfile.AddSettings<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(1.4f);
        bloom.threshold.Override(1.05f);
        bloom.softKnee.Override(0.4f);

        var grading = ppProfile.AddSettings<ColorGrading>();
        grading.enabled.Override(true);
        grading.tonemapper.Override(Tonemapper.ACES);
        grading.postExposure.Override(0.45f);
        grading.saturation.Override(12f);
        grading.contrast.Override(14f);

        var vignette = ppProfile.AddSettings<Vignette>();
        vignette.enabled.Override(true);
        vignette.intensity.Override(0.24f);
        vignette.smoothness.Override(0.4f);

        AssetDatabase.CreateAsset(ppProfile, "Assets/Materials/PostFX.asset");

        GameObject ppVolumeGO = new GameObject("PostProcessVolume");
        ppVolumeGO.layer = 1; // Builtin TransparentFX katmanı — TagManager düzenlemesi gerekmez.
        PostProcessVolume ppVolume = ppVolumeGO.AddComponent<PostProcessVolume>();
        ppVolume.isGlobal      = true;
        ppVolume.sharedProfile = ppProfile;

        PostProcessLayer ppLayer = camObj.AddComponent<PostProcessLayer>();
        ppLayer.volumeTrigger    = camObj.transform;
        ppLayer.volumeLayer      = 1 << 1;
        ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        var ppResources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(
            "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
        if (ppResources != null) ppLayer.Init(ppResources);
        else Debug.LogError("[SceneGenerator] PostProcessResources bulunamadı!");

        // ── Referans bağlantıları (SerializedObject) ────────
        Wire(physicsController, ("ballLauncher", launcher));
        Wire(tracker,           ("ballLauncher", launcher));
        Wire(resetter,          ("ballStateTracker", tracker), ("ballLauncher", launcher), ("ballPhysicsController", physicsController));
        Wire(visualFeedback,    ("shotInput", input));
        Wire(hitStop,           ("ballLauncher", launcher));
        Wire(telemetry,         ("shotInput", input), ("ballLauncher", launcher), ("ballResetter", resetter));

        Wire(aimProvider, ("goalCenter", goal.transform));
        Wire(input,       ("ballTransform", ball.transform), ("directionProviderObject", aimProvider));
        Wire(controller,  ("shotInput", input), ("ballLauncher", launcher), ("ballResetter", resetter));

        Wire(aimVisual, ("shotInput", input), ("ballTransform", ball.transform), ("ballLauncher", launcher),
                        ("ballPhysics", physicsController), ("targetProviderObject", aimProvider));
        Wire(reticle,   ("targetProviderObject", aimProvider), ("ballLauncher", launcher),
                        ("ballResetter", resetter), ("shotInput", input));

        Wire(keeperController, ("ballLauncher", launcher), ("ballResetter", resetter),
                               ("ballRigidbody", rb), ("goalCenter", goal.transform));
        Wire(wall, ("ballLauncher", launcher), ("goalCenter", goal.transform));

        Wire(referee, ("ballLauncher", launcher), ("ballStateTracker", tracker), ("ballResetter", resetter),
                      ("goalDetector", goalDetector), ("goalkeeper", keeperController), ("wall", wall));
        Wire(gameManager, ("referee", referee), ("ballResetter", resetter), ("wall", wall), ("goalCenter", goal.transform));
        Wire(hud, ("shotInput", input), ("referee", referee), ("gameManager", gameManager));

        Wire(camController, ("ballTransform", ball.transform), ("ballLauncher", launcher),
                            ("ballResetter", resetter), ("shotInput", input), ("goalTransform", goal.transform));

        AssetDatabase.SaveAssets();

        // ── Sahneyi kaydet ──────────────────────────────────
        string scenePath = "Assets/Scenes/Main.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, scenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        Debug.Log("[SceneGenerator] Scene generation completed and saved to " + scenePath);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static Material MakeMaterial(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, $"Assets/Materials/{name}.mat");
        return mat;
    }

    /// <summary>Pentagon-benekli, dikişli top dokusu üretir (dönüş görünür olsun).</summary>
    private static Texture2D MakeBallTexture()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);

        Color black = new Color(0.07f, 0.07f, 0.08f);

        Vector2[] spots =
        {
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),
            new Vector2(0.5f,  0.55f), new Vector2(0.2f,  0.75f),
            new Vector2(0.8f,  0.78f), new Vector2(0.95f, 0.5f), new Vector2(0.05f, 0.5f)
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)size, y / (float)size);

                // Panel benekleri + ince dikiş halkası + hafif kirlenme.
                float minDist = 1f;
                foreach (Vector2 s in spots)
                    minDist = Mathf.Min(minDist, Vector2.Distance(uv, s));

                Color c;
                if (minDist < 0.075f)       c = black;                               // Panel
                else if (minDist < 0.085f)  c = new Color(0.55f, 0.55f, 0.55f);      // Dikiş
                else
                {
                    float dirt = Mathf.PerlinNoise(uv.x * 9f, uv.y * 9f) * 0.08f;
                    float v = 0.98f - dirt;
                    c = new Color(v, v, v);
                }
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        AssetDatabase.CreateAsset(tex, "Assets/Materials/BallTexture.asset");
        return tex;
    }

    /// <summary>Çok oktavlı Perlin gürültüsüyle doğal çim dokusu üretir.</summary>
    private static Texture2D MakeGrassTexture()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;

        Color darkGreen  = new Color(0.16f, 0.42f, 0.14f);
        Color midGreen   = new Color(0.24f, 0.55f, 0.19f);
        Color lightGreen = new Color(0.34f, 0.66f, 0.26f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x / (float)size, fy = y / (float)size;

                // Oktavlar: geniş lekeler + orta detay + ince "çim sapı" grenli doku.
                float n = Mathf.PerlinNoise(fx * 4f,  fy * 4f)  * 0.45f
                        + Mathf.PerlinNoise(fx * 16f, fy * 16f) * 0.35f
                        + Mathf.PerlinNoise(fx * 48f, fy * 48f) * 0.20f;

                Color c = n < 0.5f
                    ? Color.Lerp(darkGreen, midGreen, n * 2f)
                    : Color.Lerp(midGreen, lightGreen, (n - 0.5f) * 2f);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        AssetDatabase.CreateAsset(tex, "Assets/Materials/GrassTexture.asset");
        return tex;
    }

    /// <summary>Alfa-cutout file ızgarası dokusu üretir.</summary>
    private static Texture2D MakeNetTexture()
    {
        const int size = 64;
        const int cell = 16;
        const int lineW = 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;

        Color rope  = new Color(0.92f, 0.92f, 0.92f, 1f);
        Color empty = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onLine = (x % cell) < lineW || (y % cell) < lineW;
                tex.SetPixel(x, y, onLine ? rope : empty);
            }
        }

        tex.Apply();
        AssetDatabase.CreateAsset(tex, "Assets/Materials/NetTexture.asset");
        return tex;
    }

    private static void MakeLine(Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "PitchLine";
        Object.DestroyImmediate(line.GetComponent<Collider>());
        line.transform.position   = pos + Vector3.up * 0.018f;
        line.transform.localScale = scale;
        line.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void MakePost(Transform parent, string name, Vector3 localPos, Vector3 scale, Quaternion rot, Material mat)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = name;
        post.transform.SetParent(parent);
        post.transform.localPosition = localPos;
        post.transform.localRotation = rot;
        post.transform.localScale    = scale;
        post.GetComponent<MeshRenderer>().sharedMaterial = mat;
        post.AddComponent<WoodworkNotifier>();
    }

    private static void MakeNetPanel(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent);
        panel.transform.localPosition = localPos;
        panel.transform.localScale    = scale;
        panel.GetComponent<MeshRenderer>().sharedMaterial = mat;
        panel.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void MakeVisualCapsule(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void MakeVisualCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void MakeVisualSphere(Transform parent, string name, Vector3 localPos, float diameter, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * diameter;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void Wire(Component target, params (string field, Object value)[] refs)
    {
        SerializedObject so = new SerializedObject(target);
        foreach ((string field, Object value) in refs)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[SceneGenerator] '{target.GetType().Name}' üzerinde '{field}' alanı yok!");
                continue;
            }
            prop.objectReferenceValue = value;
        }
        so.ApplyModifiedProperties();
    }
}
