using System;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Editable city circuit. Existing scenes and controller tuning remain independent.</summary>
public static class DistritoLibreSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/DistritoLibre.unity";
    const string MaterialDir = "Assets/Materials/DistritoLibre";
    static Material concrete, graphite, turquoise, orange, white, glass, gold, fontMaterial, pink, wood, green;
    static Transform geometry, decoration, checkpointRoot;
    static PlayerStateMachine player;
    static DistritoLibreCourse course;
    static readonly List<DistritoLibreCourse.Gate> gates = new List<DistritoLibreCourse.Gate>();

    [MenuItem("Parkour/Open or Create Distrito Libre")]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        Build(); Validate();
    }

    public static void BuildBatch()
    {
        try { Build(); Validate(); CapturePreviews(); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    public static void PreviewBatch()
    {
        try { EditorSceneManager.OpenScene(ScenePath); Validate(); CapturePreviews(); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    static void Build()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("DistritoLibre already exists. Open it instead of overwriting it.");
        gates.Clear();
        Directory.CreateDirectory(MaterialDir);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        concrete = Mat("Weathered_Concrete", new Color(.66f,.63f,.54f));
        graphite = Mat("Midnight_Metal", new Color(.10f,.15f,.18f));
        turquoise = Mat("Route_Mint", new Color(.12f,.84f,.64f), .35f);
        orange = Mat("Clay_Climb", new Color(.90f,.29f,.12f));
        white = Mat("Chalk", new Color(.91f,.89f,.78f));
        glass = Mat("Window_Petrol", new Color(.10f,.26f,.30f));
        gold = Mat("Shortcut_Yellow", new Color(1f,.71f,.08f), .25f);
        pink = Mat("CAPA_Magenta", new Color(.78f,.10f,.39f));
        wood = Mat("Skate_Plywood", new Color(.57f,.34f,.16f));
        green = Mat("Terrace_Planting", new Color(.22f,.37f,.18f));
        fontMaterial = new Material(Shader.Find("Skyline/SignText"));
        AssetDatabase.CreateAsset(fontMaterial, MaterialDir + "/Sign_Text.mat");
        geometry = Root("01 / CIRCUIT - 420 m");
        decoration = Root("02 / CITY & COMMUNITIES");
        checkpointRoot = Root("03 / ORDERED GATES");
        Environment(); Player();
        BuildControl(); BuildSkaters(); BuildWriters(); BuildWallLink(); BuildAerial(); BuildHome();
        course.gates = gates.ToArray();
        City();
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save scene.");
        Debug.Log("DISTRITO LIBRE CREATED: " + ScenePath);
    }
    static Material Mat(string name, Color color, float emission = 0)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new Exception("URP Lit is required.");
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", name.Contains("Window") ? .7f : .22f);
        if (emission > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        AssetDatabase.CreateAsset(material, MaterialDir + "/" + name + ".mat");
        return material;
    }

    static Transform Root(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Section(string name, Vector3 origin, float yaw)
    {
        var section = Root(name, geometry);
        section.position = origin;
        section.rotation = Quaternion.Euler(0, yaw, 0);
        return section;
    }

    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material,
        bool collision = true, int layer = 3, Quaternion rotation = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.layer = collision ? layer : 2;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static void Roof(Transform parent, string name, float x, float z, float width, float length, float top)
    {
        Box(name, parent, new Vector3(x, top - .45f, z), new Vector3(width,.9f,length), concrete);
        Box(name + " / facade", parent, new Vector3(x, top - 10f, z), new Vector3(width-.5f,18f,length-.5f), graphite, false);
        Box(name + " / lower rim", parent, new Vector3(x, top - 1.2f, z), new Vector3(width+.12f,.3f,length+.12f), white, false);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Turquoise edge", parent, new Vector3(x+side*(width*.5f-.12f),top+.015f,z), new Vector3(.13f,.04f,length-.4f), turquoise, false);
            for (float zz = z-length*.5f+1.5f; zz < z+length*.5f-1; zz+=3.5f)
                Box("Facade window", parent, new Vector3(x+side*(width*.5f-.23f),top-3.8f,zz), new Vector3(.03f,3.6f,1.65f), glass, false);
        }
        for (float zz = z-length*.5f+2; zz < z+length*.5f-1; zz+=4.5f) Arrow(parent, x, top, zz);
    }

    static void Arrow(Transform parent, float x, float y, float z, Material material = null)
    {
        material = material != null ? material : turquoise;
        Box("Route chevron L", parent, new Vector3(x-.23f,y+.045f,z), new Vector3(.12f,.025f,.65f), material, false, 2, Quaternion.Euler(0,45,0));
        Box("Route chevron R", parent, new Vector3(x+.23f,y+.045f,z), new Vector3(.12f,.025f,.65f), material, false, 2, Quaternion.Euler(0,-45,0));
    }

    static void Text(Transform parent, string text, Vector3 position, float size, Color color)
    {
        var go = new GameObject(text.Replace('\n',' '));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.layer = 2;
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.fontSize = 100;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.text = text;
        mesh.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = fontMaterial;
        go.AddComponent<SkylineSignText>();
    }

    static void Sign(Transform section, string title, string hint, Vector3 position, float width = 8)
    {
        var sign = Root(title, section);
        sign.localPosition = position;
        Box("Sign panel",sign,Vector3.zero,new Vector3(width,2.1f,.16f),graphite,false);
        Box("Sign stripe",sign,new Vector3(0,1.05f,-.1f),new Vector3(width,.08f,.06f),turquoise,false);
        Text(sign,title,new Vector3(0,.38f,-.095f),.066f,Color.white);
        Text(sign,hint,new Vector3(0,-.47f,-.095f),.033f,new Color(.44f,.94f,.86f));
        Box("Sign support L", sign, new Vector3(-width*.46f,-1.7f,.1f), new Vector3(.12f,1.7f,.14f), graphite, false);
        Box("Sign support R", sign, new Vector3(width*.46f,-1.7f,.1f), new Vector3(.12f,1.7f,.14f), graphite, false);
    }

    static void Player()
    {
        var go = new GameObject("Player / Distrito Libre");
        go.transform.position = new Vector3(0,1.13f,0);
        var cc = go.AddComponent<CharacterController>();
        cc.height=2; cc.radius=.5f; cc.center=Vector3.zero; cc.skinWidth=.15f; cc.stepOffset=.3f;
        go.AddComponent<InputReader>();
        player = go.AddComponent<PlayerStateMachine>();
        var pivot = Root("View pivot",go.transform);
        pivot.localPosition = Vector3.up*.72f;
        var view = Root("Player camera",pivot);
        var virtualCamera = view.gameObject.AddComponent<CinemachineCamera>();
        virtualCamera.Lens.FieldOfView=90;
        var panTilt = view.gameObject.AddComponent<CinemachinePanTilt>();
        panTilt.ReferenceFrame=CinemachinePanTilt.ReferenceFrames.ParentObject;
        panTilt.TiltAxis.Range=new Vector2(-85,85);
        var so = new SerializedObject(player);
        so.FindProperty("cc").objectReferenceValue=cc;
        so.FindProperty("_cameraPanTilt").objectReferenceValue=panTilt;
        so.FindProperty("_cameraTiltTarget").objectReferenceValue=pivot;
        so.FindProperty("_height").floatValue=-.4f;
        so.FindProperty("_castRadius").floatValue=.48f;
        so.FindProperty("_castLength").floatValue=.32f;
        so.FindProperty("_maxAngle").floatValue=45;
        so.FindProperty("_groundMask").intValue=(1<<3)|(1<<6);
        so.FindProperty("_vaultMask").intValue=(1<<3)|(1<<6);
        so.FindProperty("_wallRunMask").intValue=1<<6;
        so.FindProperty("_wallClimbMask").intValue=(1<<3)|(1<<6);
        so.FindProperty("_grappleMask").intValue=1<<7;
        so.ApplyModifiedPropertiesWithoutUndo();
        var camGo = new GameObject("Main Camera");
        camGo.tag="MainCamera";
        camGo.transform.position=go.transform.position+Vector3.up*.72f;
        var cam=camGo.AddComponent<Camera>();
        cam.nearClipPlane=.05f; cam.farClipPlane=600; cam.fieldOfView=90;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CinemachineBrain>();
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        course=new GameObject("Distrito Libre / Circuit").AddComponent<DistritoLibreCourse>();
        course.player=player;
    }

    static void Environment()
    {
        var lighting = new LightingSettings { name = "DistritoLibre Realtime Lighting", bakedGI = false, realtimeGI = false };
        AssetDatabase.CreateAsset(lighting, MaterialDir + "/DistritoLibreLighting.asset");
        Lightmapping.lightingSettings = lighting;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.5f,.64f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.52f,.55f,.52f);
        RenderSettings.ambientGroundColor=new Color(.24f,.25f,.27f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
        RenderSettings.fogColor=new Color(.54f,.65f,.69f); RenderSettings.fogDensity=.003f;
        var sun=new GameObject("Late afternoon sun").AddComponent<Light>();
        sun.type=LightType.Directional; sun.intensity=1.8f;
        sun.color=new Color(1f,.85f,.68f);
        sun.transform.rotation=Quaternion.Euler(38,-42,0);
        sun.shadows=LightShadows.Soft;
        RenderSettings.sun=sun;
        var sky=new Material(Shader.Find("Skybox/Procedural"));
        sky.SetColor("_SkyTint",new Color(.43f,.58f,.69f));
        sky.SetColor("_GroundColor",new Color(.35f,.38f,.39f));
        sky.SetFloat("_Exposure",1.15f);
        AssetDatabase.CreateAsset(sky,MaterialDir+"/Sky.asset");
        RenderSettings.skybox=sky;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
        profile.Add<Bloom>().intensity.Override(.22f);
        profile.Add<Vignette>().intensity.Override(.14f);
        var colors=profile.Add<ColorAdjustments>();
        colors.contrast.Override(8); colors.saturation.Override(-5);
        AssetDatabase.CreateAsset(profile,MaterialDir+"/DistritoLibreVolume.asset");
        foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);
        var volume=new GameObject("DistritoLibre atmosphere").AddComponent<Volume>();
        volume.isGlobal=true; volume.sharedProfile=profile;
    }

    static void Gate(Transform section, string title, string hint, Vector3 point, float width = 10f, bool finish = false)
    {
        var root = Root((finish ? "FINISH" : "GATE " + gates.Count) + " / " + title, checkpointRoot);
        root.position = section.TransformPoint(point + Vector3.up * 2.5f);
        root.rotation = section.rotation;
        var volume = root.gameObject.AddComponent<BoxCollider>();
        volume.isTrigger = true;
        volume.size = new Vector3(width, 7f, 3f);
        var spawn = Root("Recovery spawn", root);
        spawn.position = section.TransformPoint(point + new Vector3(0, 1.13f, -3f));
        spawn.rotation = section.rotation;
        gates.Add(new DistritoLibreCourse.Gate { title = title, hint = hint, volume = volume, spawn = spawn });
        Material mat = finish ? gold : turquoise;
        Box("Gate band", section, point + Vector3.up * .045f, new Vector3(width,.04f,.45f), mat, false);
        foreach(int side in new[] {-1,1})
        {
            Box("Gate post", section, point + new Vector3(side * (width*.5f+.2f),2.1f,0),new Vector3(.18f,4.2f,.18f),graphite);
            Box("Gate beacon",section,point + new Vector3(side * (width*.5f+.2f),2.8f,-.12f),new Vector3(.14f,1.1f,.06f),mat,false);
        }
        Box("Gate header",section,point+Vector3.up*4.2f,new Vector3(width+.6f,.15f,.2f),mat,false);
        Text(section,finish ? "LLEGASTE A CASA" : gates.Count == 1 ? "SALIDA / DISTRITO LIBRE" : "0"+(gates.Count-1)+" / "+title,
            point + new Vector3(0,4.65f,0),.045f,Color.white);
    }

    static void BuildControl()
    {
        var s=Section("01 / BAJO LA RED",Vector3.zero,0);
        Roof(s,"Transit depot roof",0,14,14,36,0);
        Roof(s,"First crossing / 3 m",0,44,10,18,0);
        Roof(s,"Junction roof",0,61,14,10,0);
        Gate(s,"BAJO LA RED","WASD + raton. Espacio: salta; mantenlo frente a una pared para subir.",new Vector3(0,0,5));
        Box("Vent to vault or pass beside",s,new Vector3(0,.55f,19),new Vector3(5,1.1f,1.2f),orange);
        Sign(s,"SAL DE LA CUADRICULA","SIGUE LAS MARCAS MENTA",new Vector3(-9,3.3f,12),8);
        Sign(s,"SALTA / ESPACIO","DOS HUECOS DE 3 METROS",new Vector3(-8,3.6f,29),8);
        Sign(s,"RODANTES  >","GIRA A LA DERECHA",new Vector3(0,3.4f,65),8);
        for(int i=0;i<6;i++)
        {
            Box("Transit louvres",s,new Vector3(6.35f,1.1f,7+i*3.5f),new Vector3(.22f,2.2f,2.9f),graphite);
            Box("Controlled district lamp",s,new Vector3(6.16f,1.8f,7+i*3.5f),new Vector3(.04f,.13f,2.4f),orange,false);
        }
        var fence=Root("Below / Civic checkpoint",decoration);
        fence.position=new Vector3(-14,-18,16);
        Box("Checkpoint canopy",fence,new Vector3(0,4,0),new Vector3(14,.5f,6),graphite,false);
        Sign(fence,"CIVIC / SECTOR 08","ACCESO REGISTRADO",new Vector3(0,2.4f,-3.2f),10);
        for(int i=-5;i<=5;i+=2) Box("Access barrier",fence,new Vector3(i,1,-1),new Vector3(.4f,2,.4f),orange,false);
    }

    static void BuildSkaters()
    {
        var s=Section("02 / RODANTES",new Vector3(0,0,62),90);
        Roof(s,"Skate social roof",0,9,16,26,0);
        Roof(s,"Plywood landing",0,33,12,16,.5f);
        Roof(s,"Long line",0,54,14,20,.5f);
        Roof(s,"Writers junction",0,70,16,12,.5f);
        Gate(s,"RODANTES","Corre + Ctrl para deslizar bajo el conducto. Puedes rodearlo por la derecha.",new Vector3(0,0,4),12);
        Box("Low service duct / 1.8 m clearance",s,new Vector3(-1,2,14),new Vector3(7,.4f,3.5f),wood);
        foreach(int side in new[]{-1,1}) Box("Duct support",s,new Vector3(-1+side*3.5f,.9f,14),new Vector3(.18f,1.8f,3.5f),graphite);
        Text(s,"BAJA Y FLUYE",new Vector3(-1,2.04f,12.2f),.039f,Color.white);
        // Skate furniture stays beside the playable route.
        Box("Skate bank left",s,new Vector3(-6.1f,.7f,12),new Vector3(2.5f,.3f,6),wood,true,3,Quaternion.Euler(-14,0,0));
        Box("Grind rail",s,new Vector3(5.8f,1f,8),new Vector3(.12f,.12f,5),graphite);
        Bench(s,new Vector3(5.7f,.5f,51));
        Skateboard(s,new Vector3(-5.2f,.7f,47));
        Skateboard(s,new Vector3(-5.1f,.7f,49));
        Mural(s,new Vector3(-7.1f,2.5f,56),-90,"RODANTES","NO PARES. ENCUENTRA TU LINEA.",turquoise);
        Sign(s,"<  CAPA","IZQUIERDA / DISTRITO DE MURALES",new Vector3(0,4,74),10);
        for(float z=27;z<=37;z+=2) Box("Plywood seams",s,new Vector3(-4,.53f,z),new Vector3(2,.025f,.045f),wood,false);
    }

    static void BuildWriters()
    {
        var s=Section("03 / CAPA",new Vector3(72,.5f,62),0);
        Roof(s,"Workshop approach",0,6,16,20,0);
        Roof(s,"First mural terrace",0,24,16,16,2.6f);
        Roof(s,"Second mural terrace",0,40,16,16,5.2f);
        Roof(s,"Writers lookout",0,55,16,14,5.2f);
        Gate(s,"CAPA","Manten Espacio contra las paredes naranjas. Aqui dejamos nuestra huella.",new Vector3(0,0,4),12);
        foreach(var face in new[]{new Vector2(16,2.6f),new Vector2(32,5.2f)})
        {
            Box("Solid climb face",s,new Vector3(0,face.y-1.3f,face.x+.25f),new Vector3(16,2.6f,.5f),orange);
            for(int x=-5;x<=5;x+=2) Box("Hand-painted climb stripe",s,new Vector3(x,face.y-1.2f,face.x-.025f),new Vector3(.12f,2,.03f),white,false);
            Text(s,"SUBE / MANTEN ESPACIO",new Vector3(0,face.y-.55f,face.x-.05f),.037f,Color.white);
        }
        Mural(s,new Vector3(-7.7f,5.4f,25),-90,"CAPA","EL MURO ES DE QUIEN LO PINTA.",pink);
        Mural(s,new Vector3(7.7f,8.3f,43),90,"SIN PERMISO","LA CIUDAD TAMBIEN ES NUESTRA.",orange);
        for(int i=0;i<8;i++)
        {
            Box("Paint crate",s,new Vector3(-6.6f+i*.42f,5.45f,52),new Vector3(.3f,.5f,.3f),i%2==0?pink:gold,false);
        }
        Bench(s,new Vector3(5.7f,5.2f,54));
        Sign(s,"CANAL ABIERTO  >","DERECHA / DOS FORMAS DE CRUZAR",new Vector3(0,8.5f,61),11);
    }

    static void BuildWallLink()
    {
        var s=Section("04 / CANAL ABIERTO",new Vector3(72,5.7f,122),90);
        Roof(s,"Link takeoff",0,6,12,20,0);
        Roof(s,"Link landing",0,38,12,28,-1.4f);
        Roof(s,"Radio roof",0,62,14,20,-1.4f);
        Gate(s,"CANAL ABIERTO","Menta: pasarela izquierda. Amarillo: salta junto al muro para tomar el atajo.",new Vector3(0,0,4),10);
        Box("Wall-run shortcut / 8 m gap",s,new Vector3(3.25f,.3f,20),new Vector3(.5f,8.8f,18),turquoise,true,6);
        for(float z=13;z<29;z+=2) Box("Wall-run direction",s,new Vector3(2.98f,1.6f,z),new Vector3(.025f,.10f,1.2f),gold,false);
        // Longer continuous route with a gentle descent; no wall-run required.
        Box("Safe route outbound",s,new Vector3(-7,-.22f,13),new Vector3(12,.44f,4),concrete);
        Box("Safe route descending bridge",s,new Vector3(-11,-.9f,21),new Vector3(4,.4f,16.1f),concrete,true,3,Quaternion.Euler(5,0,0));
        Box("Safe route return",s,new Vector3(-7,-1.62f,29),new Vector3(12,.44f,4),concrete);
        for(float z=14;z<=28;z+=2.5f) Arrow(s,-11,-(z-13)*.0875f,z);
        foreach(float z in new[]{12f,14f,28f,30f}) Box("Walkway outer lip",s,new Vector3(-8,z<20?.13f:-1.27f,z),new Vector3(9,.25f,.15f),graphite);
        Sign(s,"<  PASARELA","RUTA PRINCIPAL / SIN SALTO",new Vector3(-7,3,10),8);
        Sign(s,"ATAJO / WALL-RUN","SALTA PEGADO AL MURO",new Vector3(5.6f,4.8f,10),8);
        Mural(s,new Vector3(-5.8f,1.5f,49),-90,"CANAL ABIERTO","LA ALTURA SE COMPARTE.",turquoise);
        Box("Radio mast",s,new Vector3(5.5f,6,59),new Vector3(.4f,14,.4f),graphite,false);
        for(int i=0;i<4;i++) Box("Antenna bars",s,new Vector3(5.5f,8+i,59),new Vector3(3.5f-i*.6f,.08f,.12f),white,false);
        Sign(s,"HORIZONTE  >","GIRA A LA DERECHA",new Vector3(0,2.2f,71),9);
    }

    static void BuildAerial()
    {
        var s=Section("05 / HORIZONTE",new Vector3(142,4.3f,122),180);
        Roof(s,"Aerial approach",0,9,14,26,0);
        Roof(s,"Cable landing",0,47,16,22,.5f);
        Roof(s,"Garden stepping roof",0,68,12,14,.5f);
        Roof(s,"Final junction",0,84,16,12,1f);
        MarkAerialFork(s);
        Gate(s,"HORIZONTE","Menta: plataformas de la izquierda. Atajo: apunta al anclaje y pulsa clic derecho.",new Vector3(0,0,4),12);
        Roof(s,"Safe jump 1",-6,27,6,8,0);
        Roof(s,"Safe jump 2",-6,37,6,8,.5f);

        var anchor=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anchor.name="Grapple shortcut anchor"; anchor.transform.SetParent(s,false);
        anchor.transform.localPosition=new Vector3(0,5.8f,38.5f);
        anchor.transform.localScale=Vector3.one*1.5f; anchor.layer=7;
        anchor.GetComponent<Renderer>().sharedMaterial=gold;
        Box("Cable gantry",s,new Vector3(0,8,38.5f),new Vector3(14,.25f,.25f),graphite,false);
        foreach(int side in new[]{-1,1}) Box("Gantry mast",s,new Vector3(side*6.8f,4.2f,38.5f),new Vector3(.25f,7.4f,.25f),graphite);
        Box("Anchor suspension",s,new Vector3(0,7,38.5f),new Vector3(.08f,2,.08f),white,false);
        Sign(s,"<  SALTOS","RUTA PRINCIPAL",new Vector3(-7.5f,3.5f,18),8);
        Sign(s,"ATAJO / GANCHO","ANCLAJE AMARILLO + CLIC DERECHO",new Vector3(7.5f,4.5f,18),10);
        Planter(s,new Vector3(5.2f,.5f,51));
        Planter(s,new Vector3(-4.5f,.5f,69));
        Sign(s,"LA TERRAZA  >","ULTIMO TRAMO / GIRA A LA DERECHA",new Vector3(0,4.5f,89),11);
    }

    static void MarkAerialFork(Transform section)
    {
        var remove=new List<GameObject>();
        foreach(Transform t in section)
            if(t.name.StartsWith("Route chevron") && t.localPosition.z>=10 && t.localPosition.z<=22) remove.Add(t.gameObject);
        foreach(var go in remove) UnityEngine.Object.DestroyImmediate(go);
        Arrow(section,-3,0,11); Arrow(section,-6,0,16); Arrow(section,-6,0,20.5f);
        Arrow(section,0,0,16,gold); Arrow(section,0,0,20.5f,gold);
    }
    static void BuildHome()
    {
        var s=Section("06 / LA TERRAZA COMUN",new Vector3(142,5.3f,32),270);
        Roof(s,"Home stretch",0,8,14,24,0);
        Roof(s,"Shared workshop roof",0,32,14,18,.5f);
        float angle=-Mathf.Atan2(1.5f,14)*Mathf.Rad2Deg;
        Box("Final ascent ramp",s,new Vector3(0,1.04f,48),new Vector3(10,.4f,Mathf.Sqrt(198.25f)),concrete,true,3,Quaternion.Euler(angle,0,0));
        Roof(s,"Community terrace",0,65,22,20,2);
        Gate(s,"LA TERRAZA COMUN","Un ultimo salto. La meta esta bajo las banderolas.",new Vector3(0,0,4),12);
        Gate(s,"META / LA TERRAZA","Una ciudad. Muchas formas de ser libre.",new Vector3(0,2,64),12,true);
        for(int x=-5;x<=5;x++) Box("Finish checker",s,new Vector3(x*.65f,2.05f,64),new Vector3(.63f,.025f,.75f),x%2==0?white:graphite,false);
        for(int i=0;i<3;i++)
        {
            Bench(s,new Vector3(-8,2,59+i*5));
            Planter(s,new Vector3(9,2,59+i*5));
        }
        Mural(s,new Vector3(0,5.1f,74),0,"PERTENECER","ABAJO NOS SEPARAN. ARRIBA NOS ENCONTRAMOS.",pink);
        for(int side=-1;side<=1;side+=2) Box("Awning post",s,new Vector3(side*9,4.5f,68),new Vector3(.15f,5,.15f),wood);
        Box("Bunting cord",s,new Vector3(0,7,68),new Vector3(18,.035f,.035f),white,false);
        for(int i=-8;i<=8;i++) Box("Community pennant",s,new Vector3(i,6.65f,68),new Vector3(.58f,.65f,.035f),i%3==0?pink:i%3==1?gold:turquoise,false,2,Quaternion.Euler(0,0,i%2==0?8:-8));
        Sign(s,"NINGUNA AZOTEA ES UNA ISLA","RODANTES / CAPA / CANAL ABIERTO",new Vector3(-8.5f,5.5f,38),11);
    }

    static void Bench(Transform s,Vector3 p)
    {
        Box("Community bench",s,p+Vector3.up*.55f,new Vector3(1.1f,.18f,3),wood);
        foreach(float z in new[]{-1f,1f}) Box("Bench legs",s,p+new Vector3(0,.22f,z),new Vector3(.7f,.44f,.12f),graphite);
    }
    static void Planter(Transform s,Vector3 p)
    {
        Box("Reclaimed planter",s,p+Vector3.up*.35f,new Vector3(1.4f,.7f,2),wood);
        Box("Terrace planting",s,p+Vector3.up*1.05f,new Vector3(1.1f,.9f,1.7f),green,false);
    }
    static void Skateboard(Transform s,Vector3 p)
    {
        Box("Skate deck",s,p,new Vector3(.85f,.07f,.25f),pink,false,2,Quaternion.Euler(0,18,0));
        foreach(float x in new[]{-.27f,.27f}) foreach(float z in new[]{-.13f,.13f})
        {
            var wheel=GameObject.CreatePrimitive(PrimitiveType.Sphere); wheel.name="Skate wheel";
            wheel.transform.SetParent(s,false); wheel.transform.localPosition=p+new Vector3(x,-.08f,z);
            wheel.transform.localScale=Vector3.one*.12f; wheel.GetComponent<Renderer>().sharedMaterial=white;
            UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>()); wheel.layer=2;
        }
    }
    static void Mural(Transform s,Vector3 p,float yaw,string word,string phrase,Material accent)
    {
        var wall=Root("Mural / "+word,s); wall.localPosition=p; wall.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Painted wall",wall,Vector3.zero,new Vector3(11,4,.22f),graphite);
        for(int i=0;i<6;i++) Box("Layered paint stroke",wall,new Vector3(-4+i*1.6f,.15f,-.135f),new Vector3(1.15f,3.1f,.025f),i%2==0?accent:wood,false,2,Quaternion.Euler(0,0,-18));
        Text(wall,word,new Vector3(0,.30f,-.17f),.125f,Color.white);
        Text(wall,phrase,new Vector3(0,-1.37f,-.17f),.027f,Color.white);
        for(int i=0;i<5;i++) Box("Paint drip",wall,new Vector3(-4+i*1.8f,-1.7f,-.14f),new Vector3(.06f,.8f,.02f),accent,false);
    }

    static bool NearRoute(float x,float z)
    {
        return (Mathf.Abs(x)<14 && z>-12 && z<78)
            || (Mathf.Abs(z-62)<14 && x>-12 && x<85)
            || (Mathf.Abs(x-72)<15 && z>50 && z<137)
            || (Mathf.Abs(z-122)<16 && x>55 && x<158)
            || (Mathf.Abs(x-142)<17 && z>18 && z<137)
            || (Mathf.Abs(z-32)<17 && x>60 && x<158);
    }
    static void City()
    {
        Box("Street grid",decoration,new Vector3(70,-30,55),new Vector3(600,1,550),graphite,false);
        var rng=new System.Random(2012);
        int index=0;
        for(float x=-60;x<=220;x+=27) for(float z=-60;z<=195;z+=29)
        {
            if(NearRoute(x,z)) continue;
            float h=17+(float)rng.NextDouble()*31;
            float w=12+(float)rng.NextDouble()*5;
            var t=Root("City block "+index++,decoration); t.position=new Vector3(x,-30,z);
            Box("Masonry",t,new Vector3(0,h*.5f,0),new Vector3(w,h,17),index%3==0?concrete:graphite,false);
            Box("Roof cornice",t,new Vector3(0,h,0),new Vector3(w+.5f,.5f,17.5f),white,false);
            for(float y=3;y<h-2;y+=3.5f)
            {
                Box("Front glazing",t,new Vector3(0,y,-8.53f),new Vector3(w-1,1.25f,.03f),glass,false);
                Box("Side glazing",t,new Vector3(-w*.5f-.02f,y,0),new Vector3(.03f,1.25f,16),glass,false);
            }
            if(index%4==0) Box("Roof utility",t,new Vector3(0,h+1,0),new Vector3(4,2,5),wood,false);
        }
        for(int i=-3;i<=7;i++) Box("Street light ribbon",decoration,new Vector3(70,-29.45f,i*29+14),new Vector3(360,.03f,.12f),gold,false);
        var tower=Root("CIVIC / distant control tower",decoration); tower.position=new Vector3(-42,-30,113);
        Box("Control tower",tower,new Vector3(0,42,0),new Vector3(17,84,17),graphite,false);
        for(int i=0;i<6;i++) Box("Corporate signal",tower,new Vector3(0,30+i*8,-8.6f),new Vector3(14,.3f,.1f),orange,false);
        Text(tower,"C I V I C",new Vector3(0,70,-8.65f),.15f,Color.white);
    }

    public static void Validate()
    {
        course=UnityEngine.Object.FindFirstObjectByType<DistritoLibreCourse>();
        if(course==null || course.gates.Length!=7) throw new Exception("Expected start, five checkpoints and finish.");
        if(UnityEngine.Object.FindObjectsByType<PlayerStateMachine>(FindObjectsSortMode.None).Length!=1) throw new Exception("Expected one player.");
        if(Camera.main==null || Camera.main.GetComponent<CinemachineBrain>()==null) throw new Exception("Camera not wired.");
        Physics.SyncTransforms();
        var report=new List<string>();
        foreach(var g in course.gates)
        {
            var p=g.spawn.position;
            if(!Physics.Raycast(p,Vector3.down,2f,(1<<3)|(1<<6),QueryTriggerInteraction.Ignore)) throw new Exception("No ground under "+g.title);
            if(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.48f,(1<<3)|(1<<6),QueryTriggerInteraction.Ignore)) throw new Exception("Blocked spawn "+g.title);
            report.Add("PASS clear, grounded gate: "+g.title);
        }
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
        report.Add("PASS one player, camera, seven ordered gates and no missing scripts");
        Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/DistritoLibreSceneValidation.txt",report);
        Debug.Log("DISTRITO LIBRE STRUCTURE PASSED");
    }

    public static void CapturePreviews()
    {
        Directory.CreateDirectory("Logs/DistritoLibrePreviews");
        var go=new GameObject("Temporary preview camera"); var cam=go.AddComponent<Camera>();
        cam.fieldOfView=62; cam.nearClipPlane=.1f; cam.farClipPlane=700;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        Render(cam,new Vector3(-47,110,-68),new Vector3(65,0,65),"Overview");
        Render(cam,new Vector3(0,2.3f,0),new Vector3(0,2.1f,40),"Start");
        Render(cam,new Vector3(18,4.3f,64),new Vector3(60,2,62),"Rodantes");
        Render(cam,new Vector3(71,5.9f,81),new Vector3(72,6,105),"Capa");
        Render(cam,new Vector3(93,10,31),new Vector3(69,8,32),"Finish");
        UnityEngine.Object.DestroyImmediate(go);
    }
    static void Render(Camera cam,Vector3 from,Vector3 at,string name)
    {
        cam.transform.position=from; cam.transform.LookAt(at);
        var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32); cam.targetTexture=target;
        cam.Render(); cam.Render(); cam.Render();
        var previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
        File.WriteAllBytes("Logs/DistritoLibrePreviews/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=previous; cam.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
}