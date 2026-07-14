using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class GrappleTestArena : MonoBehaviour
{
    [SerializeField] bool lockCursorOnStart = true;

    readonly List<Material> runtimeMaterials = new List<Material>();
    Transform generatedRoot;

    private void Awake()
    {
        InitializeSceneContent();
    }

    private void InitializeSceneContent()
    {
        EnsureGameplayCamera();
        EnsureDirectionalLight();
        bool createdArena = BuildArena();

        if (Application.isPlaying && !createdArena)
            ApplyRuntimeMaterials();
    }

    private void Start()
    {
        if (!lockCursorOnStart) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
                Destroy(material);
        }
    }

    private void EnsureGameplayCamera()
    {
        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
            gameplayCamera = Object.FindFirstObjectByType<Camera>();

        if (gameplayCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4f, -30f);
            gameplayCamera = cameraObject.AddComponent<Camera>();
            gameplayCamera.fieldOfView = 90f;
            gameplayCamera.nearClipPlane = 0.1f;
            gameplayCamera.farClipPlane = 500f;
            gameplayCamera.backgroundColor = new Color(0.025f, 0.04f, 0.075f);
            cameraObject.AddComponent<AudioListener>();
        }
        else
        {
            gameplayCamera.gameObject.tag = "MainCamera";
            if (gameplayCamera.GetComponent<AudioListener>() == null)
                gameplayCamera.gameObject.AddComponent<AudioListener>();
        }

        if (gameplayCamera.GetComponent<CinemachineBrain>() == null)
            gameplayCamera.gameObject.AddComponent<CinemachineBrain>();
    }

    private static void EnsureDirectionalLight()
    {
        if (Object.FindFirstObjectByType<Light>() != null) return;

        GameObject lightObject = new GameObject("Arena Sun");
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        lightComponent.intensity = 1.25f;
        lightComponent.color = new Color(0.82f, 0.9f, 1f);
    }

    private bool BuildArena()
    {
        Transform existingRoot = transform.Find("Generated Grapple Arena");
        if (existingRoot != null)
        {
            generatedRoot = existingRoot;
            return false;
        }

        generatedRoot = new GameObject("Generated Grapple Arena").transform;
        generatedRoot.SetParent(transform, false);

        int groundLayer = LayerOrDefault("Ground");
        int wallRunLayer = LayerOrDefault("WallRun");
        int grappleLayer = LayerOrDefault("Grapple");

        Material groundMaterial = CreateMaterial("Arena Ground", new Color(0.055f, 0.075f, 0.11f));
        Material platformMaterial = CreateMaterial("Arena Platforms", new Color(0.12f, 0.19f, 0.28f));
        Material wallMaterial = CreateMaterial("Wall Run", new Color(0.15f, 0.34f, 0.48f));
        Material grappleMaterial = CreateMaterial("Grapple Targets", new Color(0.05f, 1f, 0.68f), true);
        Material accentMaterial = CreateMaterial("Arena Accent", new Color(1f, 0.24f, 0.12f), true);

        CreatePrimitive(
            PrimitiveType.Cube,
            "Safety Floor",
            new Vector3(0f, -0.5f, 10f),
            new Vector3(70f, 1f, 90f),
            groundLayer,
            groundMaterial
        );

        CreatePrimitive(PrimitiveType.Cube, "Start Platform", new Vector3(0f, 0.5f, -24f), new Vector3(10f, 1f, 9f), groundLayer, platformMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Momentum Ramp", new Vector3(0f, 0.65f, -13f), new Vector3(6f, 1.3f, 8f), groundLayer, platformMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Center Platform", new Vector3(0f, 2f, 1f), new Vector3(9f, 1f, 7f), groundLayer, platformMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Right Platform", new Vector3(12f, 4.5f, 13f), new Vector3(8f, 1f, 8f), groundLayer, platformMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Left Platform", new Vector3(-12f, 7.5f, 27f), new Vector3(8f, 1f, 8f), groundLayer, platformMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Final Platform", new Vector3(0f, 10.5f, 42f), new Vector3(11f, 1f, 10f), groundLayer, platformMaterial);

        CreatePrimitive(PrimitiveType.Cube, "Wall Run Right", new Vector3(7f, 3f, 6f), new Vector3(0.6f, 6f, 15f), wallRunLayer, wallMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Wall Run Left", new Vector3(-7f, 4f, 20f), new Vector3(0.6f, 8f, 17f), wallRunLayer, wallMaterial);

        CreateAnchor("Anchor 01", new Vector3(0f, 7f, -4f), 5.8f, grappleLayer, groundLayer, grappleMaterial, platformMaterial);
        CreateAnchor("Anchor 02", new Vector3(12f, 11f, 12f), 9.8f, grappleLayer, groundLayer, grappleMaterial, platformMaterial);
        CreateAnchor("Anchor 03", new Vector3(-12f, 15f, 26f), 13.8f, grappleLayer, groundLayer, grappleMaterial, platformMaterial);
        CreateAnchor("Anchor 04", new Vector3(0f, 20f, 41f), 18.8f, grappleLayer, groundLayer, grappleMaterial, platformMaterial);

        CreatePrimitive(PrimitiveType.Cube, "Vault Block A", new Vector3(-2f, 0.6f, -8f), new Vector3(2.5f, 1.2f, 1f), groundLayer, accentMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Vault Block B", new Vector3(2.5f, 0.9f, 20f), new Vector3(3f, 1.8f, 1.2f), groundLayer, accentMaterial);

        CreateLabel("RIGHT CLICK  -  GRAPPLE", new Vector3(0f, 3f, -19f), 0.23f, new Color(0.2f, 1f, 0.75f));
        CreateLabel("GREEN CROSSHAIR = VALID TARGET", new Vector3(0f, 2.25f, -18.9f), 0.115f, Color.white);
        return true;
    }

    private void ApplyRuntimeMaterials()
    {
        if (generatedRoot == null) return;

        Material groundMaterial = CreateMaterial("Arena Ground", new Color(0.055f, 0.075f, 0.11f));
        Material platformMaterial = CreateMaterial("Arena Platforms", new Color(0.12f, 0.19f, 0.28f));
        Material wallMaterial = CreateMaterial("Wall Run", new Color(0.15f, 0.34f, 0.48f));
        Material grappleMaterial = CreateMaterial("Grapple Targets", new Color(0.05f, 1f, 0.68f), true);
        Material accentMaterial = CreateMaterial("Arena Accent", new Color(1f, 0.24f, 0.12f), true);

        foreach (Renderer arenaRenderer in generatedRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (arenaRenderer.GetComponent<TextMesh>() != null) continue;

            string objectName = arenaRenderer.gameObject.name;
            if (objectName.Contains("[GRAPPLE]"))
                arenaRenderer.sharedMaterial = grappleMaterial;
            else if (objectName.Contains("Wall Run"))
                arenaRenderer.sharedMaterial = wallMaterial;
            else if (objectName.Contains("Vault"))
                arenaRenderer.sharedMaterial = accentMaterial;
            else if (objectName.Contains("Safety Floor"))
                arenaRenderer.sharedMaterial = groundMaterial;
            else
                arenaRenderer.sharedMaterial = platformMaterial;
        }
    }

    private void CreateAnchor(
        string anchorName,
        Vector3 anchorPosition,
        float supportHeight,
        int grappleLayer,
        int groundLayer,
        Material grappleMaterial,
        Material supportMaterial
    )
    {
        CreatePrimitive(
            PrimitiveType.Cube,
            anchorName + " Support",
            new Vector3(anchorPosition.x, supportHeight * 0.5f, anchorPosition.z),
            new Vector3(1.2f, supportHeight, 1.2f),
            groundLayer,
            supportMaterial
        );

        CreatePrimitive(
            PrimitiveType.Sphere,
            anchorName + " [GRAPPLE]",
            anchorPosition,
            Vector3.one * 2.2f,
            grappleLayer,
            grappleMaterial
        );
    }

    private GameObject CreatePrimitive(
        PrimitiveType primitiveType,
        string objectName,
        Vector3 position,
        Vector3 scale,
        int layer,
        Material material
    )
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.layer = layer;
        primitive.transform.SetParent(generatedRoot, false);
        primitive.transform.position = position;
        primitive.transform.localScale = scale;

        Renderer objectRenderer = primitive.GetComponent<Renderer>();
        if (objectRenderer != null && material != null)
            objectRenderer.sharedMaterial = material;

        return primitive;
    }

    private void CreateLabel(string labelText, Vector3 position, float characterSize, Color color)
    {
        GameObject labelObject = new GameObject(labelText);
        labelObject.transform.SetParent(generatedRoot, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = labelText;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = characterSize;
        label.color = color;
    }

    private Material CreateMaterial(string materialName, Color color, bool emissive = false)
    {
        if (!Application.isPlaying) return null;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Material material = new Material(shader) { name = materialName, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.25f);
        }

        runtimeMaterials.Add(material);
        return material;
    }

    private static int LayerOrDefault(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : 0;
    }

}
