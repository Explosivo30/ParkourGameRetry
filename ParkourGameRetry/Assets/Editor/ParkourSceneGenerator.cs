using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.ProBuilder;

/// <summary>
/// Master Scene Builder Editor Script for the Parkour Scene (parkourTest.unity).
/// Programmatically constructs a complete, high-performance 5-sector parkour environment
/// using ProBuilder geometry, lighting, post-processing, Cinemachine camera, player prefab placement,
/// checkpoints, ziplines, and kill zones.
/// </summary>
public static class ParkourSceneGenerator
{
    private const string ScenePath = "Assets/Scenes/parkourTest.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/-----Player.prefab";
    private const string PostProcessProfilePath = "Assets/Settings/SampleSceneProfile.asset";
    private const string MaterialDir = "Assets/Materials/Parkour";

    private const int LayerDefault = 0;
    private const int LayerGround = 3;
    private const int LayerWallRun = 6;
    private const int LayerGrapple = 7;

    // Thematic Materials
    private static Material s_MatSector1Base;
    private static Material s_MatSector1Accent;
    private static Material s_MatSector1Vault;

    private static Material s_MatSector2Base;
    private static Material s_MatSector2WallRun;
    private static Material s_MatSector2Accent;

    private static Material s_MatSector3Base;
    private static Material s_MatSector3Grapple;
    private static Material s_MatSector3Accent;

    private static Material s_MatSector4Base;
    private static Material s_MatSector4WallRun;
    private static Material s_MatSector4Slide;
    private static Material s_MatSector4Zipline;

    private static Material s_MatSector5Base;
    private static Material s_MatSector5Accent;
    private static Material s_MatSector5Gold;
    private static Material s_MatSector5Monolith;

    private static Material s_MatCpActive;
    private static Material s_MatCpInactive;

    [MenuItem("Parkour/Generate Parkour Scene (parkourTest)")]
    public static void GenerateSceneMenu()
    {
        GenerateParkourScene(true);
    }

    public static void GenerateSceneBatch()
    {
        GenerateParkourScene(false);
    }

    /// <summary>
    /// Master generation pipeline.
    /// </summary>
    public static void GenerateParkourScene(bool interactive = true)
    {
        Debug.Log("[ParkourSceneGenerator] Beginning parkourTest scene generation...");

        // Ensure materials directory exists and load/create all materials
        EnsureMaterials();

        // Step 1: Create a new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Step 2: Environment, Lighting, Post-Processing, and Camera
        SetupEnvironmentAndLighting();
        SetupGlobalPostProcessVolume();
        SetupMainCamera();

        // Step 3: Player Prefab Placement
        GameObject playerInstance = SetupPlayerPrefab();

        // Step 4: Root containers for sectors
        GameObject envRoot = new GameObject("--- ENVIRONMENT GEOMETRY ---");
        GameObject s1Root = new GameObject("--- SECTOR 1: BASICS ---");
        GameObject s2Root = new GameObject("--- SECTOR 2: VERTICALITY ---");
        GameObject s3Root = new GameObject("--- SECTOR 3: AERIAL AGILITY ---");
        GameObject s4Root = new GameObject("--- SECTOR 4: SPEED & MOMENTUM ---");
        GameObject s5Root = new GameObject("--- SECTOR 5: MASTERY GAUNTLET ---");
        GameObject cpRoot = new GameObject("--- CHECKPOINTS ---");
        GameObject hzRoot = new GameObject("--- HAZARD & KILL ZONES ---");

        s1Root.transform.SetParent(envRoot.transform);
        s2Root.transform.SetParent(envRoot.transform);
        s3Root.transform.SetParent(envRoot.transform);
        s4Root.transform.SetParent(envRoot.transform);
        s5Root.transform.SetParent(envRoot.transform);

        // Step 5: Build ProBuilder geometry for all 5 Sectors
        BuildSector1(s1Root.transform);
        BuildSector2(s2Root.transform);
        BuildSector3(s3Root.transform);
        BuildSector4(s4Root.transform);
        BuildSector5(s5Root.transform);

        // Step 6: Checkpoints & CheckpointManager
        SetupCheckpoints(cpRoot.transform);

        // Step 7: Kill Zones & Hazards
        SetupKillZones(hzRoot.transform);

        // Ensure target directory exists and save the scene
        string sceneDirectory = Path.GetDirectoryName(ScenePath);
        if (!Directory.Exists(sceneDirectory) && !string.IsNullOrEmpty(sceneDirectory))
        {
            Directory.CreateDirectory(sceneDirectory);
        }

        bool saveOk = EditorSceneManager.SaveScene(scene, ScenePath);
        if (saveOk)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ParkourSceneGenerator] SUCCESS: parkourTest scene successfully created and saved to '{ScenePath}'.");
            if (interactive)
            {
                EditorUtility.DisplayDialog("Parkour Scene Generator", "parkourTest scene generated successfully!", "OK");
            }
        }
        else
        {
            Debug.LogError($"[ParkourSceneGenerator] FAILED to save scene to '{ScenePath}'.");
        }
    }

    #region Materials Setup

    private static void EnsureMaterials()
    {
        if (!Directory.Exists(MaterialDir))
        {
            Directory.CreateDirectory(MaterialDir);
        }

        // Sector 1: Clean Prototyping White / Orange Trim
        s_MatSector1Base = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector1_Base.mat", new Color(0.92f, 0.93f, 0.95f), Color.black, 0.4f, 0.05f);
        s_MatSector1Accent = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector1_Accent.mat", new Color(1.0f, 0.48f, 0.05f), new Color(1.0f, 0.35f, 0.0f) * 0.8f, 0.6f, 0.2f);
        s_MatSector1Vault = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector1_Vault.mat", new Color(1.0f, 0.60f, 0.10f), new Color(1.0f, 0.45f, 0.0f) * 1.2f, 0.7f, 0.3f);

        // Sector 2: Industrial Slate Blue / Cyan Trim
        s_MatSector2Base = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector2_Base.mat", new Color(0.22f, 0.28f, 0.36f), Color.black, 0.5f, 0.2f);
        s_MatSector2WallRun = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector2_WallRun.mat", new Color(0.12f, 0.65f, 0.95f), new Color(0.1f, 0.75f, 1.0f) * 2.0f, 0.8f, 0.4f);
        s_MatSector2Accent = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector2_Accent.mat", new Color(0.15f, 0.80f, 0.90f), new Color(0.1f, 0.85f, 0.95f) * 1.5f, 0.7f, 0.3f);

        // Sector 3: Midnight Obsidian / Emerald Green Emissive
        s_MatSector3Base = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector3_Base.mat", new Color(0.10f, 0.12f, 0.16f), Color.black, 0.6f, 0.3f);
        s_MatSector3Grapple = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector3_Grapple.mat", new Color(0.10f, 0.95f, 0.45f), new Color(0.10f, 1.30f, 0.55f) * 3.5f, 0.9f, 0.5f);
        s_MatSector3Accent = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector3_Accent.mat", new Color(0.12f, 0.85f, 0.48f), new Color(0.10f, 0.90f, 0.50f) * 1.8f, 0.75f, 0.25f);

        // Sector 4: Carbon Asphalt / Amber Glow
        s_MatSector4Base = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector4_Base.mat", new Color(0.15f, 0.16f, 0.18f), Color.black, 0.4f, 0.1f);
        s_MatSector4WallRun = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector4_WallRun.mat", new Color(0.95f, 0.48f, 0.05f), new Color(1.0f, 0.55f, 0.05f) * 2.2f, 0.8f, 0.3f);
        s_MatSector4Slide = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector4_Slide.mat", new Color(0.98f, 0.72f, 0.08f), new Color(1.0f, 0.80f, 0.10f) * 1.5f, 0.95f, 0.2f);
        s_MatSector4Zipline = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector4_Zipline.mat", new Color(1.0f, 0.75f, 0.10f), new Color(1.0f, 0.75f, 0.10f) * 3.0f, 0.8f, 0.6f);

        // Sector 5: Synthwave Crimson / Gold
        s_MatSector5Base = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector5_Base.mat", new Color(0.22f, 0.09f, 0.14f), Color.black, 0.5f, 0.2f);
        s_MatSector5Accent = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector5_Accent.mat", new Color(0.92f, 0.12f, 0.38f), new Color(1.0f, 0.15f, 0.42f) * 2.5f, 0.8f, 0.3f);
        s_MatSector5Gold = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector5_Gold.mat", new Color(1.0f, 0.84f, 0.10f), new Color(1.0f, 0.86f, 0.20f) * 2.5f, 0.9f, 0.85f);
        s_MatSector5Monolith = GetOrCreateMaterial($"{MaterialDir}/Mat_Sector5_Monolith.mat", new Color(0.08f, 0.08f, 0.12f), new Color(0.85f, 0.10f, 0.30f) * 0.4f, 0.7f, 0.4f);

        // Checkpoints
        s_MatCpActive = GetOrCreateMaterial($"{MaterialDir}/Mat_Checkpoint_Active.mat", new Color(0.10f, 1.0f, 0.75f), new Color(0.10f, 1.20f, 0.85f) * 3.5f, 0.85f, 0.3f);
        s_MatCpInactive = GetOrCreateMaterial($"{MaterialDir}/Mat_Checkpoint_Inactive.mat", new Color(0.35f, 0.36f, 0.40f), new Color(1.0f, 0.40f, 0.05f) * 0.5f, 0.5f, 0.2f);
    }

    private static Material GetOrCreateMaterial(string path, Color baseColor, Color emissionColor, float smoothness, float metallic)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            else if (mat.HasProperty("_Color")) mat.color = baseColor;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emissionColor.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            }

            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            else if (mat.HasProperty("_Color")) mat.color = baseColor;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emissionColor.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    #endregion

    #region Environment, Lighting, Post-Processing, and Camera

    private static void SetupEnvironmentAndLighting()
    {
        // Directional Sun
        GameObject sunObj = new GameObject("Directional Light");
        sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light sunLight = sunObj.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.96f, 0.88f);
        sunLight.intensity = 1.3f;
        sunLight.shadows = LightShadows.Soft;

        UniversalAdditionalLightData lightData = sunObj.AddComponent<UniversalAdditionalLightData>();
        if (lightData != null)
        {
            lightData.lightCookieSize = Vector2.one;
        }

        // RenderSettings Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0035f;
        RenderSettings.fogColor = new Color(0.12f, 0.15f, 0.22f);
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.sun = sunLight;
    }

    private static void SetupGlobalPostProcessVolume()
    {
        GameObject volumeObj = new GameObject("Global Post Process Volume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessProfilePath);
        if (profile != null)
        {
            volume.sharedProfile = profile;
        }
        else
        {
            Debug.LogWarning($"[ParkourSceneGenerator] Could not find VolumeProfile at '{PostProcessProfilePath}'. Creating fallback profile.");
            VolumeProfile runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = runtimeProfile;
        }
    }

    private static void SetupMainCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 2.5f, -3f);
        cameraObj.transform.rotation = Quaternion.identity;

        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 85f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        cameraObj.AddComponent<AudioListener>();

        // Universal Additional Camera Data
        UniversalAdditionalCameraData camData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
        }

        // CinemachineBrain
        CinemachineBrain brain = cameraObj.AddComponent<CinemachineBrain>();
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
    }

    private static GameObject SetupPlayerPrefab()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError($"[ParkourSceneGenerator] Player prefab not found at '{PlayerPrefabPath}'.");
            return null;
        }

        GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerInstance.name = "-----Player";
        playerInstance.transform.position = new Vector3(0f, 1.5f, 0f);
        playerInstance.transform.rotation = Quaternion.identity;

        return playerInstance;
    }

    #endregion

    #region ProBuilder Mesh Helper

    private static GameObject CreateProBuilderCube(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation, Material mat, int layer = LayerGround)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
        GameObject go = pbMesh.gameObject;
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);

        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = size;
        go.layer = layer;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        MeshCollider mc = go.GetComponent<MeshCollider>();
        if (mc == null) mc = go.AddComponent<MeshCollider>();
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mc.sharedMesh = mf.sharedMesh;
        }

        return go;
    }

    private static GameObject CreateProBuilderStairs(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation, Material mat, int layer = LayerGround)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Stair);
        GameObject go = pbMesh.gameObject;
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);

        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = size;
        go.layer = layer;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        MeshCollider mc = go.GetComponent<MeshCollider>();
        if (mc == null) mc = go.AddComponent<MeshCollider>();
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mc.sharedMesh = mf.sharedMesh;
        }

        return go;
    }

    private static GameObject CreateGrappleAnchorSphere(string name, Transform parent, Vector3 position, float diameter, Material mat)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        if (parent != null) sphere.transform.SetParent(parent, false);

        sphere.transform.position = position;
        sphere.transform.localScale = new Vector3(diameter, diameter, diameter);
        sphere.layer = LayerGrapple;

        MeshRenderer mr = sphere.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
        }

        // Ensure collider is intact
        SphereCollider sc = sphere.GetComponent<SphereCollider>();
        if (sc == null) sc = sphere.AddComponent<SphereCollider>();
        sc.isTrigger = false;

        return sphere;
    }

    #endregion

    #region Sector 1: Basics

    private static void BuildSector1(Transform parent)
    {
        GameObject sectorRoot = new GameObject("Sector1_Basics_Geometry");
        sectorRoot.transform.SetParent(parent, false);

        // 1. Start Platform: 10m x 1m x 15m (Layer 3 Ground)
        // Positioned centered at (0, 0, 7.5), top surface at Y = 0.5m
        CreateProBuilderCube("S1_StartPlatform", sectorRoot.transform,
            new Vector3(0f, 0f, 7.5f), new Vector3(10f, 1f, 15f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // Accent borders on start platform
        CreateProBuilderCube("S1_Start_BorderL", sectorRoot.transform,
            new Vector3(-4.8f, 0.6f, 7.5f), new Vector3(0.4f, 0.3f, 15f), Quaternion.identity, s_MatSector1Accent, LayerGround);
        CreateProBuilderCube("S1_Start_BorderR", sectorRoot.transform,
            new Vector3(4.8f, 0.6f, 7.5f), new Vector3(0.4f, 0.3f, 15f), Quaternion.identity, s_MatSector1Accent, LayerGround);

        // 2. Vault Obstacles: 3 distinct vault blocks (h=0.8m, 1.0m, 1.2m, depth=0.8m) on Layer 3 Ground
        // Base platform top is at Y = 0.5m
        // Vault 1: h = 0.8m -> center Y = 0.5 + 0.4 = 0.9m
        CreateProBuilderCube("S1_Vault_0.8m", sectorRoot.transform,
            new Vector3(0f, 0.9f, 5.0f), new Vector3(4f, 0.8f, 0.8f), Quaternion.identity, s_MatSector1Vault, LayerGround);

        // Vault 2: h = 1.0m -> center Y = 0.5 + 0.5 = 1.0m
        CreateProBuilderCube("S1_Vault_1.0m", sectorRoot.transform,
            new Vector3(0f, 1.0f, 9.5f), new Vector3(4f, 1.0f, 0.8f), Quaternion.identity, s_MatSector1Vault, LayerGround);

        // Vault 3: h = 1.2m -> center Y = 0.5 + 0.6 = 1.1m
        CreateProBuilderCube("S1_Vault_1.2m", sectorRoot.transform,
            new Vector3(0f, 1.1f, 14.0f), new Vector3(4f, 1.2f, 0.8f), Quaternion.identity, s_MatSector1Vault, LayerGround);

        // 3. Jump Gaps: 4.5m, 5.5m, 6.5m across elevated platforms
        // Start platform ends at Z = 15m.
        // Gap 1: 4.5m -> Jump Platform 1 starts at Z = 19.5m, length = 10m (ends Z = 29.5m), top Y = 0.5m
        CreateProBuilderCube("S1_JumpPlatform_1", sectorRoot.transform,
            new Vector3(0f, 0f, 24.5f), new Vector3(6f, 1f, 10f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // Gap 2: 5.5m -> Jump Platform 2 starts at Z = 35.0m, length = 10m (ends Z = 45.0m), top Y = 1.0m
        CreateProBuilderCube("S1_JumpPlatform_2", sectorRoot.transform,
            new Vector3(0f, 0.5f, 40.0f), new Vector3(6f, 1f, 10f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // Gap 3: 6.5m -> Jump Platform 3 starts at Z = 51.5m, length = 12m (ends Z = 63.5m), top Y = 1.5m
        CreateProBuilderCube("S1_JumpPlatform_3", sectorRoot.transform,
            new Vector3(0f, 1.0f, 57.5f), new Vector3(6f, 1f, 12f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // 4. Slide Tunnel: 10° downward ramp (14m long) with low overhead ceiling (1.75m clearance)
        // Starts at Z = 63.5m, Y = 1.5m
        float rampAngle = 10f;
        float rampLen = 14f;
        float rampRad = rampAngle * Mathf.Deg2Rad;
        float deltaZ = rampLen * Mathf.Cos(rampRad); // ~13.787m
        float deltaY = -rampLen * Mathf.Sin(rampRad); // ~-2.43m

        Vector3 rampCenter = new Vector3(0f, 1.0f + deltaY * 0.5f, 63.5f + deltaZ * 0.5f);
        Quaternion rampRot = Quaternion.Euler(rampAngle, 0f, 0f);

        CreateProBuilderCube("S1_SlideRamp_Floor", sectorRoot.transform,
            rampCenter, new Vector3(5f, 0.6f, rampLen), rampRot, s_MatSector1Accent, LayerGround);

        // Low ceiling roof panel: placed 1.75m clearance above ramp floor
        Vector3 ceilingCenter = rampCenter + (rampRot * Vector3.up) * (0.3f + 1.75f + 0.2f);
        CreateProBuilderCube("S1_SlideRamp_Ceiling", sectorRoot.transform,
            ceilingCenter, new Vector3(5f, 0.4f, rampLen), rampRot, s_MatSector1Base, LayerGround);

        // Slide tunnel side walls
        Vector3 wallLCenter = rampCenter + (rampRot * Vector3.left) * 2.3f + (rampRot * Vector3.up) * 1.075f;
        Vector3 wallRCenter = rampCenter + (rampRot * Vector3.right) * 2.3f + (rampRot * Vector3.up) * 1.075f;
        CreateProBuilderCube("S1_SlideRamp_WallL", sectorRoot.transform,
            wallLCenter, new Vector3(0.4f, 2.35f, rampLen), rampRot, s_MatSector1Base, LayerGround);
        CreateProBuilderCube("S1_SlideRamp_WallR", sectorRoot.transform,
            wallRCenter, new Vector3(0.4f, 2.35f, rampLen), rampRot, s_MatSector1Base, LayerGround);

        // Slide Exit landing platform at Z = 77.5m ... 88.0m, top Y = -0.65m
        CreateProBuilderCube("S1_SlideLandingPlatform", sectorRoot.transform,
            new Vector3(0f, -1.15f, 82.75f), new Vector3(6f, 1f, 10.5f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // 5. Ascent to High Elevated Ledge for Landing Roll Drop
        // Grand stairs rising from Y = -0.65m to Y = 8.0m, spanning Z = 88.0m ... 102.0m
        CreateProBuilderStairs("S1_AscentStairs", sectorRoot.transform,
            new Vector3(0f, 3.175f, 95.0f), new Vector3(5f, 8.65f, 14f), Quaternion.identity, s_MatSector1Base, LayerGround);

        // High Drop Platform: Z = 102.0m ... 112.0m, top surface at Y = 8.0m
        CreateProBuilderCube("S1_DropLedgePlatform", sectorRoot.transform,
            new Vector3(0f, 7.5f, 107.0f), new Vector3(6f, 1f, 10f), Quaternion.identity, s_MatSector1Accent, LayerGround);

        // 6. Landing Roll Drop: 5.5m vertical drop (Y = 8.0m -> Y = 2.5m) onto large landing platform (8m x 16m)
        // Large Landing Platform: Z = 114.0m ... 130.0m, top surface at Y = 2.5m, centered at (0, 2.0, 122.0)
        CreateProBuilderCube("S1_LandingRollPad", sectorRoot.transform,
            new Vector3(0f, 2.0f, 122.0f), new Vector3(8f, 1f, 16f), Quaternion.identity, s_MatSector1Base, LayerGround);
    }

    #endregion

    #region Sector 2: Verticality

    private static void BuildSector2(Transform parent)
    {
        GameObject sectorRoot = new GameObject("Sector2_Verticality_Geometry");
        sectorRoot.transform.SetParent(parent, false);

        // 1. Sector 2 Entry / Staging Platform: Z = 130.0m ... 140.0m, top surface at Y = 2.5m
        CreateProBuilderCube("S2_EntryPlatform", sectorRoot.transform,
            new Vector3(0f, 2.0f, 135.0f), new Vector3(8f, 1f, 10f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // 2. Grand Staircase & Ascent Towers up to +25m elevation
        // Staircase 1: Rising from Y = 2.5m to Y = 10.0m, spanning Z = 140.0m ... 154.0m
        CreateProBuilderStairs("S2_AscentStairs_1", sectorRoot.transform,
            new Vector3(0f, 5.75f, 147.0f), new Vector3(6f, 7.5f, 14f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // Tower Platform 1: Z = 154.0m ... 164.0m, top Y = 10.0m
        CreateProBuilderCube("S2_TowerPlatform_1", sectorRoot.transform,
            new Vector3(0f, 9.5f, 159.0f), new Vector3(8f, 1f, 10f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // Staircase 2: Rising from Y = 10.0m to Y = 18.0m, spanning Z = 164.0m ... 178.0m
        CreateProBuilderStairs("S2_AscentStairs_2", sectorRoot.transform,
            new Vector3(0f, 13.5f, 171.0f), new Vector3(6f, 8.0f, 14f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // Wall Run Launch Platform: Z = 178.0m ... 188.0m, top Y = 18.0m
        CreateProBuilderCube("S2_WallRun_LaunchPad", sectorRoot.transform,
            new Vector3(0f, 17.5f, 183.0f), new Vector3(7f, 1f, 10f), Quaternion.identity, s_MatSector2Accent, LayerGround);

        // 3. Right Wall Run wall: 14m x 6m x 0.6m on Layer 6 (WallRun)
        // Positioned at X = +3.5m, spanning Z = 189.0m ... 203.0m, Y = 18.0m ... 24.0m
        CreateProBuilderCube("S2_RightWallRun_Panel", sectorRoot.transform,
            new Vector3(3.5f, 21.0f, 196.0f), new Vector3(0.6f, 6.0f, 14.0f), Quaternion.identity, s_MatSector2WallRun, LayerWallRun);

        // Transfer Gap: 5.5m gap spanning Z = 203.0m ... 208.5m over void

        // 4. Left Wall Run wall: 16m x 6m x 0.6m on Layer 6 (WallRun)
        // Positioned at X = -3.5m, spanning Z = 208.5m ... 224.5m, Y = 18.0m ... 24.0m
        CreateProBuilderCube("S2_LeftWallRun_Panel", sectorRoot.transform,
            new Vector3(-3.5f, 21.0f, 216.5f), new Vector3(0.6f, 6.0f, 16.0f), Quaternion.identity, s_MatSector2WallRun, LayerWallRun);

        // Catch Platform after Wall Runs: Z = 225.5m ... 235.5m, top Y = 18.0m
        CreateProBuilderCube("S2_WallRun_CatchPad", sectorRoot.transform,
            new Vector3(0f, 17.5f, 230.5f), new Vector3(7f, 1f, 10f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // 5. Ledge Grab & Climb: Vertical wall with ledge top 2.2m above platform feet, requiring mantle pull-up
        // Approach platform: Z = 235.5m ... 242.0m, top Y = 18.0m
        CreateProBuilderCube("S2_LedgeApproachPad", sectorRoot.transform,
            new Vector3(0f, 17.5f, 238.75f), new Vector3(6f, 1f, 6.5f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // Ledge wall: width 6m, height 2.2m, thickness 1.2m, center Y = 18.0 + 1.1 = 19.1m, top Y = 20.2m
        CreateProBuilderCube("S2_LedgeClimb_Wall", sectorRoot.transform,
            new Vector3(0f, 19.1f, 242.6f), new Vector3(6f, 2.2f, 1.2f), Quaternion.identity, s_MatSector2Accent, LayerGround);

        // Upper platform behind ledge: Z = 243.2m ... 254.0m, top Y = 20.2m
        CreateProBuilderCube("S2_LedgeUpperPad", sectorRoot.transform,
            new Vector3(0f, 19.7f, 248.6f), new Vector3(6f, 1f, 10.8f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // 6. Final ascent to Apex Tower at +25m elevation
        // Staircase 3: Rising from Y = 20.2m to Y = 25.0m, spanning Z = 254.0m ... 266.0m
        CreateProBuilderStairs("S2_ApexStairs", sectorRoot.transform,
            new Vector3(0f, 22.1f, 260.0f), new Vector3(6f, 4.8f, 12f), Quaternion.identity, s_MatSector2Base, LayerGround);

        // Apex Tower Platform (+25m elevation): Z = 266.0m ... 278.0m, top Y = 25.0m
        CreateProBuilderCube("S2_ApexTowerPlatform", sectorRoot.transform,
            new Vector3(0f, 24.5f, 272.0f), new Vector3(10f, 1f, 12f), Quaternion.identity, s_MatSector2Base, LayerGround);
    }

    #endregion

    #region Sector 3: Aerial Agility

    private static void BuildSector3(Transform parent)
    {
        GameObject sectorRoot = new GameObject("Sector3_Aerial_Geometry");
        sectorRoot.transform.SetParent(parent, false);

        // 1. Air Dash Gap: 11m jump + air dash across void
        // Apex platform ends at Z = 278.0m, top Y = 25.0m
        // Void gap spanning Z = 278.0m ... 289.0m (11m gap)
        // Floating Island 1: Z = 289.0m ... 298.0m, top Y = 25.0m
        CreateProBuilderCube("S3_FloatingIsland_1", sectorRoot.transform,
            new Vector3(0f, 24.5f, 293.5f), new Vector3(6f, 1f, 9f), Quaternion.identity, s_MatSector3Base, LayerGround);

        // 2. Grappling Hook Anchors: 3 suspended Layer 7 (Grapple) spheres (dia 2.0m) with glowing emissive green materials
        // spanning 20m-25m grapple swings across high void, interleaved with narrow precision landing pads (4m x 4m)

        // Grapple Anchor 1: suspended at (0, 34.0, 311.0)
        CreateGrappleAnchorSphere("S3_GrappleAnchor_1", sectorRoot.transform,
            new Vector3(0f, 34.0f, 311.0f), 2.0f, s_MatSector3Grapple);

        // Narrow Precision Landing Pad 1: 4m x 1m x 4m, centered at (0, 24.5, 324.0), top Y = 25.0m
        CreateProBuilderCube("S3_PrecisionPad_1", sectorRoot.transform,
            new Vector3(0f, 24.5f, 324.0f), new Vector3(4f, 1f, 4f), Quaternion.identity, s_MatSector3Accent, LayerGround);

        // Grapple Anchor 2: suspended at (1.5, 35.0, 341.0)
        CreateGrappleAnchorSphere("S3_GrappleAnchor_2", sectorRoot.transform,
            new Vector3(1.5f, 35.0f, 341.0f), 2.0f, s_MatSector3Grapple);

        // Narrow Precision Landing Pad 2: 4m x 1m x 4m, centered at (0, 25.0, 356.0), top Y = 25.5m
        CreateProBuilderCube("S3_PrecisionPad_2", sectorRoot.transform,
            new Vector3(0f, 25.0f, 356.0f), new Vector3(4f, 1f, 4f), Quaternion.identity, s_MatSector3Accent, LayerGround);

        // Grapple Anchor 3: suspended at (-1.5, 36.0, 373.0)
        CreateGrappleAnchorSphere("S3_GrappleAnchor_3", sectorRoot.transform,
            new Vector3(-1.5f, 36.0f, 373.0f), 2.0f, s_MatSector3Grapple);

        // Sector 3 Exit Platform: 8m x 1m x 14m, centered at (0, 26.5, 391.0), top Y = 27.0m (Z = 384.0m ... 398.0m)
        CreateProBuilderCube("S3_ExitPlatform", sectorRoot.transform,
            new Vector3(0f, 26.5f, 391.0f), new Vector3(8f, 1f, 14f), Quaternion.identity, s_MatSector3Base, LayerGround);
    }

    #endregion

    #region Sector 4: Speed & Momentum

    private static void BuildSector4(Transform parent)
    {
        GameObject sectorRoot = new GameObject("Sector4_Momentum_Geometry");
        sectorRoot.transform.SetParent(parent, false);

        // 1. Zipline 1 Launch Tower on Sector 3 exit / Sector 4 start platform (Z = 398.0m)
        // Zipline 1: 42m descending zipline wire (15° slope) with ParkourZipline trigger and LineRenderer cable
        // Start Anchor: (0, 29.5, 396.0)
        // Length = 42m, slope = 15° -> deltaZ = 42 * cos(15°) = 40.57m, deltaY = -42 * sin(15°) = -10.87m
        // End Anchor: (0, 18.63, 436.57)

        GameObject zip1Obj = new GameObject("S4_Zipline_42m");
        zip1Obj.transform.SetParent(sectorRoot.transform, false);

        GameObject zip1Start = new GameObject("Zipline_StartAnchor");
        zip1Start.transform.SetParent(zip1Obj.transform, false);
        zip1Start.transform.position = new Vector3(0f, 29.5f, 396.0f);

        GameObject zip1End = new GameObject("Zipline_EndAnchor");
        zip1End.transform.SetParent(zip1Obj.transform, false);
        zip1End.transform.position = new Vector3(0f, 18.63f, 436.57f);

        // Trigger Collider encompassing start
        BoxCollider zip1Col = zip1Obj.AddComponent<BoxCollider>();
        zip1Col.isTrigger = true;
        zip1Col.center = new Vector3(0f, 28.5f, 397.0f);
        zip1Col.size = new Vector3(3f, 3f, 3f);

        LineRenderer zip1Line = zip1Obj.AddComponent<LineRenderer>();
        zip1Line.useWorldSpace = true;
        zip1Line.startWidth = 0.05f;
        zip1Line.endWidth = 0.05f;
        zip1Line.positionCount = 2;
        zip1Line.SetPosition(0, zip1Start.transform.position);
        zip1Line.SetPosition(1, zip1End.transform.position);
        zip1Line.material = s_MatSector4Zipline;

        ParkourZipline ziplineScript = zip1Obj.AddComponent<ParkourZipline>();
        ziplineScript.startPoint = zip1Start.transform;
        ziplineScript.endPoint = zip1End.transform;
        ziplineScript.speed = 16f;

        // Zipline landing tower / platform: Z = 434.0m ... 444.0m, top Y = 17.0m
        CreateProBuilderCube("S4_Zipline_LandingPad", sectorRoot.transform,
            new Vector3(0f, 16.5f, 439.0f), new Vector3(7f, 1f, 10f), Quaternion.identity, s_MatSector4Base, LayerGround);

        // 2. 3-stage Chained Wall Runs: Array of 3 alternating wall-run panels (Layer 6 WallRun) with 4.5m transfer jumps
        // Stage 1 (Left Wall): 10m x 5m x 0.6m, at X = -3.0m, Z = 445.0m ... 455.0m, Y = 16.0m ... 21.0m
        CreateProBuilderCube("S4_WallRun_Stage1_Left", sectorRoot.transform,
            new Vector3(-3.0f, 18.5f, 450.0f), new Vector3(0.6f, 5.0f, 10.0f), Quaternion.identity, s_MatSector4WallRun, LayerWallRun);

        // Transfer Gap 1: 4.5m gap spanning Z = 455.0m ... 459.5m

        // Stage 2 (Right Wall): 10m x 5m x 0.6m, at X = +3.0m, Z = 459.5m ... 469.5m, Y = 15.5m ... 20.5m
        CreateProBuilderCube("S4_WallRun_Stage2_Right", sectorRoot.transform,
            new Vector3(3.0f, 18.0f, 464.5f), new Vector3(0.6f, 5.0f, 10.0f), Quaternion.identity, s_MatSector4WallRun, LayerWallRun);

        // Transfer Gap 2: 4.5m gap spanning Z = 469.5m ... 474.0m

        // Stage 3 (Left Wall): 10m x 5m x 0.6m, at X = -3.0m, Z = 474.0m ... 484.0m, Y = 15.0m ... 20.0m
        CreateProBuilderCube("S4_WallRun_Stage3_Left", sectorRoot.transform,
            new Vector3(-3.0f, 17.5f, 479.0f), new Vector3(0.6f, 5.0f, 10.0f), Quaternion.identity, s_MatSector4WallRun, LayerWallRun);

        // Catch platform: Z = 485.0m ... 495.0m, top Y = 14.5m
        CreateProBuilderCube("S4_WallRun_CatchPad", sectorRoot.transform,
            new Vector3(0f, 14.0f, 490.0f), new Vector3(7f, 1f, 10f), Quaternion.identity, s_MatSector4Base, LayerGround);

        // 3. Steep Momentum Slide: 25° steep slide chute (18m long) giving maximum slope acceleration into Flow Rank S
        // Starts at Z = 495.0m, Y = 14.5m
        float slideAngle = 25f;
        float slideLen = 18f;
        float slideRad = slideAngle * Mathf.Deg2Rad;
        float sDeltaZ = slideLen * Mathf.Cos(slideRad); // ~16.31m
        float sDeltaY = -slideLen * Mathf.Sin(slideRad); // ~-7.61m

        Vector3 slideCenter = new Vector3(0f, 14.0f + sDeltaY * 0.5f, 495.0f + sDeltaZ * 0.5f);
        Quaternion slideRot = Quaternion.Euler(slideAngle, 0f, 0f);

        CreateProBuilderCube("S4_SteepSlide_Floor", sectorRoot.transform,
            slideCenter, new Vector3(5f, 0.6f, slideLen), slideRot, s_MatSector4Slide, LayerGround);

        // Side guard rails
        Vector3 sRailLCenter = slideCenter + (slideRot * Vector3.left) * 2.3f + (slideRot * Vector3.up) * 0.4f;
        Vector3 sRailRCenter = slideCenter + (slideRot * Vector3.right) * 2.3f + (slideRot * Vector3.up) * 0.4f;
        CreateProBuilderCube("S4_SteepSlide_RailL", sectorRoot.transform,
            sRailLCenter, new Vector3(0.4f, 0.8f, slideLen), slideRot, s_MatSector4WallRun, LayerGround);
        CreateProBuilderCube("S4_SteepSlide_RailR", sectorRoot.transform,
            sRailRCenter, new Vector3(0.4f, 0.8f, slideLen), slideRot, s_MatSector4WallRun, LayerGround);

        // Slide Exit landing platform: Z = 512.0m ... 524.0m, top Y = 6.8m
        CreateProBuilderCube("S4_SlideLanding_Platform", sectorRoot.transform,
            new Vector3(0f, 6.3f, 518.0f), new Vector3(8f, 1f, 12f), Quaternion.identity, s_MatSector4Base, LayerGround);
    }

    #endregion

    #region Sector 5: Mastery Gauntlet

    private static void BuildSector5(Transform parent)
    {
        GameObject sectorRoot = new GameObject("Sector5_Gauntlet_Geometry");
        sectorRoot.transform.SetParent(parent, false);

        // Climax obstacle course chaining all 8 mechanics in continuous sequence:
        // High platform Zipline entry -> High-speed Left Wall Run -> Vault hurdle -> Long-range Grapple Swing (26m)
        // -> Air Dash over hazard gap -> Ledge Climb up monolithic tower -> 9m vertical drop requiring Landing Roll
        // -> Speed Slide under hydraulic gate -> Final Grapple Slingshot -> Grand Victory Monolith Platform with celebratory golden arch.

        // 1. Zipline 2 Entry Tower: Z = 524.0m ... 532.0m, top Y = 17.0m via ascent stairs
        CreateProBuilderStairs("S5_AscentStairs_ZipTower", sectorRoot.transform,
            new Vector3(0f, 11.4f, 528.0f), new Vector3(6f, 10.2f, 8f), Quaternion.identity, s_MatSector5Base, LayerGround);

        CreateProBuilderCube("S5_ZipLaunchPlatform", sectorRoot.transform,
            new Vector3(0f, 16.5f, 535.0f), new Vector3(6f, 1f, 6f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // Zipline 2: Start at (0, 19.5, 535.0), End at (0, 11.5, 560.0) (25m fast zipline)
        GameObject zip2Obj = new GameObject("S5_Zipline_Gauntlet");
        zip2Obj.transform.SetParent(sectorRoot.transform, false);

        GameObject zip2Start = new GameObject("Zipline2_StartAnchor");
        zip2Start.transform.SetParent(zip2Obj.transform, false);
        zip2Start.transform.position = new Vector3(0f, 19.5f, 535.0f);

        GameObject zip2End = new GameObject("Zipline2_EndAnchor");
        zip2End.transform.SetParent(zip2Obj.transform, false);
        zip2End.transform.position = new Vector3(0f, 11.5f, 560.0f);

        BoxCollider zip2Col = zip2Obj.AddComponent<BoxCollider>();
        zip2Col.isTrigger = true;
        zip2Col.center = new Vector3(0f, 18.5f, 536.0f);
        zip2Col.size = new Vector3(3f, 3f, 3f);

        LineRenderer zip2Line = zip2Obj.AddComponent<LineRenderer>();
        zip2Line.useWorldSpace = true;
        zip2Line.startWidth = 0.05f;
        zip2Line.endWidth = 0.05f;
        zip2Line.positionCount = 2;
        zip2Line.SetPosition(0, zip2Start.transform.position);
        zip2Line.SetPosition(1, zip2End.transform.position);
        zip2Line.material = s_MatSector4Zipline;

        ParkourZipline zip2Script = zip2Obj.AddComponent<ParkourZipline>();
        zip2Script.startPoint = zip2Start.transform;
        zip2Script.endPoint = zip2End.transform;
        zip2Script.speed = 18f;

        // 2. High-Speed Left Wall Run: 14m x 6m x 0.6m on Layer 6 (WallRun)
        // Positioned at X = -3.5m, Z = 562.0m ... 576.0m, Y = 10.0m ... 16.0m
        CreateProBuilderCube("S5_HighSpeed_LeftWallRun", sectorRoot.transform,
            new Vector3(-3.5f, 13.0f, 569.0f), new Vector3(0.6f, 6.0f, 14.0f), Quaternion.identity, s_MatSector5Accent, LayerWallRun);

        // 3. Vault Hurdle: Platform at Z = 577.0m ... 587.0m, top Y = 9.0m
        CreateProBuilderCube("S5_Vault_Platform", sectorRoot.transform,
            new Vector3(0f, 8.5f, 582.0f), new Vector3(6f, 1f, 10f), Quaternion.identity, s_MatSector5Base, LayerGround);

        // Vault Obstacle (h = 1.0m, depth = 0.8m) centered at (0, 9.5, 582.0) on Layer 3 Ground
        CreateProBuilderCube("S5_Vault_Hurdle", sectorRoot.transform,
            new Vector3(0f, 9.5f, 582.0f), new Vector3(4f, 1.0f, 0.8f), Quaternion.identity, s_MatSector1Vault, LayerGround);

        // 4. Long-Range Grapple Swing (26m across void):
        // Void gap spanning Z = 587.0m ... 615.0m
        // Grapple Anchor 4: suspended at (0, 23.0, 601.0) on Layer 7 Grapple
        CreateGrappleAnchorSphere("S5_GrappleAnchor_Swing", sectorRoot.transform,
            new Vector3(0f, 23.0f, 601.0f), 2.0f, s_MatSector3Grapple);

        // 5. Air Dash over Hazard Gap:
        // Intermediate landing pad at Z = 615.0m ... 621.0m, top Y = 9.0m
        CreateProBuilderCube("S5_AirDash_LaunchPad", sectorRoot.transform,
            new Vector3(0f, 8.5f, 618.0f), new Vector3(5f, 1f, 6f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // 11m hazard gap spanning Z = 621.0m ... 632.0m to Monolith Base Platform
        // Monolith Base Platform: Z = 632.0m ... 640.0m, top Y = 9.0m
        CreateProBuilderCube("S5_Monolith_BasePad", sectorRoot.transform,
            new Vector3(0f, 8.5f, 636.0f), new Vector3(6f, 1f, 8f), Quaternion.identity, s_MatSector5Monolith, LayerGround);

        // 6. Ledge Climb up Monolithic Tower:
        // Vertical wall with ledge top 2.2m above platform (Y = 9.0 + 2.2 = 11.2m)
        CreateProBuilderCube("S5_Monolith_LedgeClimbWall_1", sectorRoot.transform,
            new Vector3(0f, 10.1f, 640.6f), new Vector3(6f, 2.2f, 1.2f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // Intermediate ledge tier: Z = 641.2m ... 647.0m, top Y = 11.2m
        CreateProBuilderCube("S5_Monolith_Tier1_Pad", sectorRoot.transform,
            new Vector3(0f, 10.7f, 644.1f), new Vector3(6f, 1f, 5.8f), Quaternion.identity, s_MatSector5Monolith, LayerGround);

        // Ledge climb 2 up to Y = 13.4m (2.2m above tier 1)
        CreateProBuilderCube("S5_Monolith_LedgeClimbWall_2", sectorRoot.transform,
            new Vector3(0f, 12.3f, 647.6f), new Vector3(6f, 2.2f, 1.2f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // High Monolith Overhang Deck: Z = 648.2m ... 660.0m, rising to top Y = 18.0m via steps
        CreateProBuilderStairs("S5_Monolith_UpperStairs", sectorRoot.transform,
            new Vector3(0f, 15.2f, 654.1f), new Vector3(6f, 4.6f, 11.8f), Quaternion.identity, s_MatSector5Monolith, LayerGround);

        CreateProBuilderCube("S5_Monolith_HighDropLedge", sectorRoot.transform,
            new Vector3(0f, 17.5f, 664.0f), new Vector3(6f, 1f, 8f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // 7. 9m Vertical Drop requiring Landing Roll:
        // Drop from Y = 18.0m down to Y = 9.0m (fall velocity ~ 13.3 m/s)
        // Lower Landing Deck: 8m x 16m, Z = 667.0m ... 683.0m, top Y = 9.0m
        CreateProBuilderCube("S5_HighDrop_LandingRollPad", sectorRoot.transform,
            new Vector3(0f, 8.5f, 675.0f), new Vector3(8f, 1f, 16f), Quaternion.identity, s_MatSector5Base, LayerGround);

        // 8. Speed Slide under Hydraulic Gate:
        // Floor runs Z = 683.0m ... 695.0m, top Y = 9.0m
        CreateProBuilderCube("S5_HydraulicGate_Floor", sectorRoot.transform,
            new Vector3(0f, 8.5f, 689.0f), new Vector3(5f, 1f, 12f), Quaternion.identity, s_MatSector4Slide, LayerGround);

        // Low Overhead Hydraulic Gate barrier at Z = 689.0m (clearance 1.75m, gate bottom Y = 10.75m, gate height 3.0m)
        CreateProBuilderCube("S5_HydraulicGate_OverheadBeam", sectorRoot.transform,
            new Vector3(0f, 12.25f, 689.0f), new Vector3(5f, 3.0f, 1.0f), Quaternion.identity, s_MatSector5Accent, LayerGround);

        // 9. Final Grapple Slingshot:
        // At Z = 695.0m, launch into final abyss
        // Final Grapple Anchor 5: suspended at (0, 26.0, 712.0) on Layer 7 Grapple (dia 2.0m, Gold emissive)
        CreateGrappleAnchorSphere("S5_FinalGrapple_Slingshot", sectorRoot.transform,
            new Vector3(0f, 26.0f, 712.0f), 2.0f, s_MatSector5Gold);

        // 10. Grand Victory Monolith Platform with Celebratory Golden Arch:
        // Platform: 16m x 2m x 24m, centered at (0, 14.0, 736.0), top surface at Y = 15.0m (Z = 724.0m ... 748.0m)
        CreateProBuilderCube("S5_VictoryPlatform", sectorRoot.transform,
            new Vector3(0f, 14.0f, 736.0f), new Vector3(16f, 2f, 24f), Quaternion.identity, s_MatSector5Monolith, LayerGround);

        // Celebratory Golden Arch & Columns
        // Left Column
        CreateProBuilderCube("S5_VictoryArch_ColumnL", sectorRoot.transform,
            new Vector3(-6.0f, 19.0f, 736.0f), new Vector3(1.5f, 8.0f, 1.5f), Quaternion.identity, s_MatSector5Gold, LayerGround);
        // Right Column
        CreateProBuilderCube("S5_VictoryArch_ColumnR", sectorRoot.transform,
            new Vector3(6.0f, 19.0f, 736.0f), new Vector3(1.5f, 8.0f, 1.5f), Quaternion.identity, s_MatSector5Gold, LayerGround);
        // Golden Crossbeam Arch
        CreateProBuilderCube("S5_VictoryArch_TopBeam", sectorRoot.transform,
            new Vector3(0f, 23.5f, 736.0f), new Vector3(13.5f, 1.5f, 1.5f), Quaternion.identity, s_MatSector5Gold, LayerGround);

        // Monolith Monument Pillar behind stage
        CreateProBuilderCube("S5_VictoryMonolith_Spire", sectorRoot.transform,
            new Vector3(0f, 22.0f, 746.0f), new Vector3(3f, 14.0f, 3f), Quaternion.identity, s_MatSector5Gold, LayerGround);
    }

    #endregion

    #region Checkpoints & CheckpointManager

    private static void SetupCheckpoints(Transform parent)
    {
        // Create CheckpointManager Singleton GameObject
        GameObject managerObj = new GameObject("CheckpointManager");
        managerObj.transform.SetParent(parent, false);
        CheckpointManager cpManager = managerObj.AddComponent<CheckpointManager>();
        cpManager.allCheckpoints = new List<Checkpoint>();

        // 6 Sequential Checkpoints:
        // 1. CP_01_Start (Sector 1 Start) at (0, 0.5, 2.0)
        Checkpoint cp1 = CreateCheckpointInstance("CP_01_Start", parent, 0, "Sector 1: Basics",
            new Vector3(0f, 0.5f, 2.0f), Quaternion.identity);

        // 2. CP_02_Verticality (Sector 2 Entry) at (0, 2.5, 133.0)
        Checkpoint cp2 = CreateCheckpointInstance("CP_02_Verticality", parent, 1, "Sector 2: Verticality",
            new Vector3(0f, 2.5f, 133.0f), Quaternion.identity);

        // 3. CP_03_Aerial (Sector 3 Entry) at (0, 25.0, 274.0)
        Checkpoint cp3 = CreateCheckpointInstance("CP_03_Aerial", parent, 2, "Sector 3: Aerial Agility",
            new Vector3(0f, 25.0f, 274.0f), Quaternion.identity);

        // 4. CP_04_Momentum (Sector 4 Entry) at (0, 27.0, 390.0)
        Checkpoint cp4 = CreateCheckpointInstance("CP_04_Momentum", parent, 3, "Sector 4: Speed & Momentum",
            new Vector3(0f, 27.0f, 390.0f), Quaternion.identity);

        // 5. CP_05_Gauntlet (Sector 5 Entry) at (0, 6.8, 520.0)
        Checkpoint cp5 = CreateCheckpointInstance("CP_05_Gauntlet", parent, 4, "Sector 5: Mastery Gauntlet",
            new Vector3(0f, 6.8f, 520.0f), Quaternion.identity);

        // 6. CP_06_Victory (Victory Platform) at (0, 15.0, 730.0)
        Checkpoint cp6 = CreateCheckpointInstance("CP_06_Victory", parent, 5, "Victory Monolith",
            new Vector3(0f, 15.0f, 730.0f), Quaternion.identity);

        cpManager.allCheckpoints.Add(cp1);
        cpManager.allCheckpoints.Add(cp2);
        cpManager.allCheckpoints.Add(cp3);
        cpManager.allCheckpoints.Add(cp4);
        cpManager.allCheckpoints.Add(cp5);
        cpManager.allCheckpoints.Add(cp6);

        cpManager.defaultSpawnPoint = cp1.spawnPoint;
        cpManager.currentCheckpoint = cp1;
        cp1.SetActiveState(true);
    }

    private static Checkpoint CreateCheckpointInstance(string name, Transform parent, int sectorIdx, string sectorName, Vector3 position, Quaternion rotation)
    {
        GameObject cpObj = new GameObject(name);
        cpObj.transform.SetParent(parent, false);
        cpObj.transform.position = position;
        cpObj.transform.rotation = rotation;
        cpObj.layer = LayerDefault;

        // Trigger Collider
        BoxCollider triggerCol = cpObj.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.center = new Vector3(0f, 1.5f, 0f);
        triggerCol.size = new Vector3(4f, 3.5f, 4f);

        // Spawn Point child
        GameObject spawnObj = new GameObject("SpawnPoint");
        spawnObj.transform.SetParent(cpObj.transform, false);
        spawnObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        spawnObj.transform.localRotation = Quaternion.identity;

        // Beacon visual pillar
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "BeaconVisual";
        beacon.transform.SetParent(cpObj.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        beacon.transform.localScale = new Vector3(0.35f, 1.25f, 0.35f);
        beacon.layer = LayerDefault;

        // Remove collider from visual beacon
        Collider beaconCol = beacon.GetComponent<Collider>();
        if (beaconCol != null) UnityEngine.Object.DestroyImmediate(beaconCol);

        MeshRenderer mr = beacon.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = s_MatCpInactive;
        }

        Checkpoint cp = cpObj.AddComponent<Checkpoint>();
        cp.sectorIndex = sectorIdx;
        cp.sectorName = sectorName;
        cp.spawnPoint = spawnObj.transform;
        cp.beaconRenderer = mr;
        cp.activeMaterial = s_MatCpActive;
        cp.inactiveMaterial = s_MatCpInactive;
        cp.isActivated = false;

        return cp;
    }

    #endregion

    #region Hazards & Kill Zones

    private static void SetupKillZones(Transform parent)
    {
        // Global Void KillZone trigger at Y = -25m (600m x 10m x 800m)
        GameObject globalVoid = new GameObject("Global Void KillZone");
        globalVoid.transform.SetParent(parent, false);
        globalVoid.transform.position = new Vector3(0f, -25.0f, 375.0f);
        globalVoid.layer = LayerDefault;

        BoxCollider globalVoidCol = globalVoid.AddComponent<BoxCollider>();
        globalVoidCol.isTrigger = true;
        globalVoidCol.size = new Vector3(600f, 10f, 800f);

        KillZone globalKz = globalVoid.AddComponent<KillZone>();
        globalKz.zoneName = "Global Void Abyss";

        // Sector-specific pit hazard triggers
        CreateSectorKillZone("S1_JumpGap_Hazard", parent, new Vector3(0f, -5.0f, 38.0f), new Vector3(30f, 8f, 50f), "Sector 1 Jump Pit");
        CreateSectorKillZone("S2_WallRun_Hazard", parent, new Vector3(0f, 5.0f, 206.0f), new Vector3(30f, 8f, 50f), "Sector 2 Wall Run Void");
        CreateSectorKillZone("S3_Aerial_Hazard", parent, new Vector3(0f, 10.0f, 335.0f), new Vector3(40f, 8f, 120f), "Sector 3 Aerial Abyss");
        CreateSectorKillZone("S4_Momentum_Hazard", parent, new Vector3(0f, 0.0f, 465.0f), new Vector3(30f, 8f, 60f), "Sector 4 Wall Transfer Pit");
        CreateSectorKillZone("S5_Gauntlet_Hazard", parent, new Vector3(0f, -2.0f, 600.0f), new Vector3(40f, 8f, 140f), "Sector 5 Gauntlet Void");
    }

    private static void CreateSectorKillZone(string name, Transform parent, Vector3 position, Vector3 size, string zoneName)
    {
        GameObject hzObj = new GameObject(name);
        hzObj.transform.SetParent(parent, false);
        hzObj.transform.position = position;
        hzObj.layer = LayerDefault;

        BoxCollider col = hzObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        KillZone kz = hzObj.AddComponent<KillZone>();
        kz.zoneName = zoneName;
    }

    #endregion
}
