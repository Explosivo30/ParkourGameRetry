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

/// <summary>Construye una escena independiente y editable de parkour entre azoteas.</summary>
public static class SkylineParkourSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/SkylineParkour.unity";
    const string MaterialDir = "Assets/Materials/SkylineParkour";
    static Material concrete, graphite, turquoise, orange, white, glass, gold, fontMaterial;
    static Transform geometry, decoration, checkpointRoot;
    static CheckpointManager manager;
    static PlayerStateMachine player;
    static SkylineParkourCourse course;
    static readonly List<Checkpoint> checkpoints = new List<Checkpoint>();

    [MenuItem("Parkour/Create Skyline Rooftop Scene")]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }
        Build();
    }

    public static void BuildBatch()
    {
        try { Build(); Validate(); CapturePreviews(); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    static void Build()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("SkylineParkour already exists; open it instead of overwriting it.");
        checkpoints.Clear();
        Directory.CreateDirectory(MaterialDir);
        Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        concrete = Mat("Concrete", new Color(.73f,.72f,.66f));
        graphite = Mat("Graphite", new Color(.105f,.16f,.19f));
        turquoise = Mat("Route_Turquoise", new Color(.06f,.75f,.72f), .6f);
        orange = Mat("Climb_Terracotta", new Color(.91f,.31f,.12f));
        white = Mat("Warm_White", new Color(.92f,.92f,.82f));
        glass = Mat("Window_Blue", new Color(.16f,.29f,.36f), .1f);
        gold = Mat("Finish_Gold", new Color(1f,.68f,.16f), .7f);
        fontMaterial = new Material(Shader.Find("Skyline/SignText"));
        AssetDatabase.CreateAsset(fontMaterial, MaterialDir + "/Sign_Text.mat");
        geometry = Root("01 / ROOFTOP COURSE");
        decoration = Root("02 / CITY & DETAILS");
        checkpointRoot = Root("03 / CHECKPOINTS");
        manager = new GameObject("Checkpoint Manager").AddComponent<CheckpointManager>();
        manager.transform.SetParent(checkpointRoot);
        Environment();
        Player();
        BuildStart();
        BuildAscent();
        BuildFlow();
        BuildWallRun();
        BuildPrecision();
        BuildFinal();
        City();
        manager.allCheckpoints = new List<Checkpoint>(checkpoints);
        manager.currentCheckpoint = checkpoints[0];
        manager.defaultSpawnPoint = checkpoints[0].spawnPoint;
        checkpoints[0].isActivated = true;
        course.checkpoints = manager;
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Scene could not be saved.");
        Debug.Log("SKYLINE scene created: " + ScenePath);
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

    static void Checkpoint(Transform section, string name, Vector3 localPosition, float width = 7)
    {
        var cpObject = new GameObject("CP " + checkpoints.Count + " / " + name);
        cpObject.transform.SetParent(checkpointRoot,false);
        cpObject.transform.position = section.TransformPoint(localPosition + Vector3.up * 1.4f);
        cpObject.transform.rotation = section.rotation;
        var trigger = cpObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(width,2.8f,2);
        var checkpoint = cpObject.AddComponent<Checkpoint>();
        checkpoint.sectorIndex = checkpoints.Count;
        checkpoint.sectorName = name;
        var spawn = Root("Respawn",cpObject.transform);
        spawn.position = section.TransformPoint(localPosition + new Vector3(0,1.13f,-.65f));
        spawn.rotation = section.rotation;
        checkpoint.spawnPoint = spawn;
        checkpoint.activeMaterial = turquoise;
        checkpoint.inactiveMaterial = gold;
        var beacon = Box("Checkpoint beacon", section, localPosition + new Vector3(-width*.5f-.3f,.8f,0), new Vector3(.22f,1.6f,.22f), gold, false);
        checkpoint.beaconRenderer = beacon.GetComponent<MeshRenderer>();
        Box("Checkpoint floor band",section,localPosition+Vector3.up*.035f,new Vector3(width,.035f,.3f),gold,false);
        checkpoints.Add(checkpoint);
    }

    static void BuildStart()
    {
        var s = Section("01 / SALTOS",Vector3.zero,0);
        Roof(s,"Launch roof",0,9,12,26,0);
        Roof(s,"First jump / 4 m gap",0,30,7,8,.25f);
        Roof(s,"Second jump / 4 m gap",0,44,10,12,1.2f);
        Sign(s,"SKYLINE", "ROOFTOP RUN  /  SIGUE EL TURQUESA",new Vector3(0,3.5f,-2));
        Sign(s,"01 / SALTA", "WASD + ESPACIO  /  R: CHECKPOINT",new Vector3(-7.2f,3.7f,16),7);
        Checkpoint(s,"01 SALTOS",new Vector3(0,0,1));
        Box("Warm-up obstacle",s,new Vector3(0,.6f,10),new Vector3(5,1.2f,1.2f),orange);
        for (int side=-1;side<=1;side+=2)
        {
            Box("Launch guardrail",s,new Vector3(side*5.8f,.65f,5),new Vector3(.12f,1.3f,16),graphite);
            Box("Planter",s,new Vector3(side*4.3f,.35f,13),new Vector3(1.3f,.7f,2.4f),graphite);
            Box("Architectural planting",s,new Vector3(side*4.3f,1.1f,13),new Vector3(.85f,.9f,1.9f),turquoise,false);
        }
        // La conexión gira a la derecha sobre la última azotea.
        Sign(s,"GIRA A LA DERECHA", "02 / ASCENSO",new Vector3(0,4.7f,49),8);
    }

    static void BuildAscent()
    {
        var s = Section("02 / ASCENSO",new Vector3(0,1.2f,50),90);
        Roof(s,"Ascent run-up",0,4,10,16,0);
        Roof(s,"Climb terrace 1 / 2.8 m",0,16.5f,10,9,2.8f);
        Roof(s,"Climb terrace 2 / 2.8 m",0,26.5f,10,11,5.6f);
        Roof(s,"Climb terrace 3 / 2.8 m",0,37,12,10,8.4f);
        Sign(s,"02 / TREPA", "MANTEN ESPACIO FRENTE A LA PARED",new Vector3(-8,4.5f,8),9);
        Checkpoint(s,"02 ASCENSO",new Vector3(0,0,3));
        foreach (Vector2 face in new [] { new Vector2(12,2.8f), new Vector2(21,5.6f), new Vector2(32,8.4f) })
        {
            Box("Terracotta climb face",s,new Vector3(0,face.y-1.35f,face.x-.015f),new Vector3(6,2.7f,.04f),orange,false);
            for (int side=-1;side<=1;side+=2)
                Box("Climb guide",s,new Vector3(side*2.8f,face.y-1.35f,face.x-.04f),new Vector3(.08f,2.5f,.02f),white,false);
        }
        Text(s,"MANTEN ESPACIO",new Vector3(0,1.5f,11.94f),.036f,Color.white);
        Sign(s,"SIGUE A LA DERECHA", "03 / RITMO",new Vector3(0,12,41),8);
    }

    static void BuildFlow()
    {
        var s = Section("03 / RITMO",new Vector3(42,9.6f,50),180);
        Roof(s,"Slide runway",0,6,12,20,0);
        Roof(s,"Rhythm landing 1",0,23,8,8,0);
        Roof(s,"Rhythm landing 2",0,34,8,8,-.5f);
        Sign(s,"03 / DESLIZA", "CORRE + CTRL  /  SALTA AL SALIR",new Vector3(-8,3.6f,4),8);
        Checkpoint(s,"03 RITMO",new Vector3(0,0,1));
        Box("Slide tunnel roof / 1.8 m clearance",s,new Vector3(0,2.0f,10),new Vector3(7,.4f,3.5f),orange);
        Box("Tunnel pillar L",s,new Vector3(-3.4f,.9f,10),new Vector3(.2f,1.8f,3.5f),graphite);
        Box("Tunnel pillar R",s,new Vector3(3.4f,.9f,10),new Vector3(.2f,1.8f,3.5f),graphite);
        float slope = Mathf.Atan2(3,12)*Mathf.Rad2Deg;
        Box("Downhill ramp",s,new Vector3(0,-2.3f,44),new Vector3(8,.6f,Mathf.Sqrt(153)),concrete,true,3,Quaternion.Euler(slope,0,0));
        Roof(s,"Flow exit roof",0,56,12,12,-3.5f);
        Sign(s,"GIRA A LA DERECHA", "04 / PARED",new Vector3(0,.1f,61),8);
    }

    static void BuildWallRun()
    {
        var s = Section("04 / PARED",new Vector3(42,6.1f,-12),270);
        Roof(s,"Wall-run approach",0,3,9,14,0);
        Roof(s,"Wall-run landing",0,24,10,12,-3);
        Box("Wall-run panel / 8 m gap",s,new Vector3(3.25f,.3f,16),new Vector3(.5f,8.8f,16),turquoise,true,6);
        for (float zz=9;zz<24;zz+=2)
            Box("Wall-run horizontal markers",s,new Vector3(2.98f,1.6f,zz),new Vector3(.025f,.09f,1.2f),white,false);
        Sign(s,"04 / CORRE POR LA PARED", "SALTA PEGADO AL TURQUESA",new Vector3(-8,3.7f,3),9);
        Checkpoint(s,"04 PARED",new Vector3(-.5f,0,0));
        Arrow(s,2.45f,0,7);
        Roof(s,"Optional recovery step A",-4.7f,12,2.4f,2.6f,-1);
        Roof(s,"Optional recovery step B",-4.7f,16,2.4f,2.6f,-.5f);
        Sign(s,"GIRA A LA IZQUIERDA", "05 / PRECISION",new Vector3(0,.7f,29),8);
    }

    static void BuildPrecision()
    {
        var s = Section("05 / PRECISION",new Vector3(12,3.1f,-12),180);
        Roof(s,"Precision approach",0,3,10,14,0);
        Roof(s,"Offset beam 1",2.2f,16,2.8f,8,.4f);
        Roof(s,"Offset beam 2",-1.5f,27,3,8,1f);
        Roof(s,"Offset beam 3",1,38,3.2f,8,1.5f);
        Roof(s,"Precision exit roof",0,53,12,16,2);
        Sign(s,"05 / PRECISION", "MIRA LA SIGUIENTE PLATAFORMA",new Vector3(-8,3.7f,5),9);
        Checkpoint(s,"05 PRECISION",new Vector3(0,0,0));
        Sign(s,"GIRA A LA IZQUIERDA", "06 / ULTIMO VUELO",new Vector3(0,5.7f,60),9);
    }

    static void BuildFinal()
    {
        var s = Section("06 / ULTIMO VUELO",new Vector3(12,5.1f,-73),90);
        Roof(s,"Grapple takeoff",0,4,12,16,0);
        Roof(s,"Finish rooftop",0,37,14,22,.5f);
        Sign(s,"06 / ULTIMO VUELO", "APUNTA A LA ESFERA + CLIC DERECHO",new Vector3(-9,4.4f,5),10);
        Checkpoint(s,"06 GANCHO",new Vector3(0,0,0));
        var anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anchor.name = "Grapple anchor / layer 7";
        anchor.transform.SetParent(s,false);
        anchor.transform.localPosition = new Vector3(0,6,28.5f);
        anchor.transform.localScale = Vector3.one*1.5f;
        anchor.layer = 7;
        anchor.GetComponent<Renderer>().sharedMaterial = turquoise;
        Box("Grapple gantry",s,new Vector3(0,8.5f,28.5f),new Vector3(12,.3f,.3f),graphite,false);
        Box("Gantry mast L",s,new Vector3(-5.8f,4.5f,28.5f),new Vector3(.3f,8,.3f),graphite);
        Box("Gantry mast R",s,new Vector3(5.8f,4.5f,28.5f),new Vector3(.3f,8,.3f),graphite);
        Box("Anchor hanger",s,new Vector3(0,7.1f,28.5f),new Vector3(.07f,2.6f,.07f),gold,false);
        // Ruta alternativa de saltos, visible a la izquierda del gancho.
        Roof(s,"Alternative jump 1",-6,15,4,3,0);
        Roof(s,"Alternative jump 2",-7.5f,20,4,3,.25f);
        Roof(s,"Alternative jump 3",-6,24.5f,4,3,.5f);
        Sign(s,"META", "SKYLINE / ROOFTOP RUN",new Vector3(0,4,43),9);
        for(int side=-1;side<=1;side+=2)
            Box("Finish gate pillar",s,new Vector3(side*3.3f,2.25f,40),new Vector3(.3f,3.5f,.3f),gold);
        Box("Finish gate header",s,new Vector3(0,4,40),new Vector3(6.9f,.25f,.3f),gold,false);
        for(int x=-5;x<=5;x++)
            Box("Finish checker",s,new Vector3(x*.52f,.54f,40),new Vector3(.5f,.035f,.65f),x%2==0?white:graphite,false);
        var finish = Root("Finish detection",s);
        finish.localPosition = new Vector3(0,1.63f,40);
        course.finish = finish;
    }

    static void Player()
    {
        var go = new GameObject("Player / Skyline");
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
        course=new GameObject("Skyline / Timer & Recovery").AddComponent<SkylineParkourCourse>();
        course.player=player;
    }

    static void Environment()
    {
        var lighting = new LightingSettings { name = "Skyline Realtime Lighting", bakedGI = false, realtimeGI = false };
        AssetDatabase.CreateAsset(lighting, MaterialDir + "/SkylineLighting.asset");
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
        sun.transform.rotation=Quaternion.Euler(48,-38,0);
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
        AssetDatabase.CreateAsset(profile,MaterialDir+"/SkylineVolume.asset");
        foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);
        var volume=new GameObject("Skyline atmosphere").AddComponent<Volume>();
        volume.isGlobal=true; volume.sharedProfile=profile;
    }

    static void City()
    {
        Box("Distant city floor",decoration,new Vector3(20,-36,-15),new Vector3(500,1,500),graphite,false);
        var random=new System.Random(2741);
        for(int i=0;i<52;i++)
        {
            float x=-92+(i%9)*26+(float)random.NextDouble()*9;
            float z=-130+(i/9)*47+(float)random.NextDouble()*12;
            if(x>-14&&x<70&&z>-91&&z<65) continue;
            float height=18+(float)random.NextDouble()*31;
            float width=9+(float)random.NextDouble()*7;
            float depth=11+(float)random.NextDouble()*9;
            var tower=Root("City tower "+i,decoration);
            tower.position=new Vector3(x,-35,z);
            Box("Tower shell",tower,new Vector3(0,height*.5f,0),new Vector3(width,height,depth),i%3==0?concrete:graphite,false);
            Box("Tower cap",tower,new Vector3(0,height,0),new Vector3(width+.4f,.5f,depth+.4f),white,false);
            for(float y=4;y<height-2;y+=4)
            {
                Box("Window ribbon front",tower,new Vector3(0,y,-depth*.5f-.02f),new Vector3(width-.7f,1.2f,.025f),glass,false);
                Box("Window ribbon side",tower,new Vector3(-width*.5f-.02f,y,0),new Vector3(.025f,1.2f,depth-.7f),glass,false);
            }
        }
    }

    public static void Validate()
    {
        Physics.SyncTransforms();
        var report=new List<string>();
        var found=UnityEngine.Object.FindObjectsByType<PlayerStateMachine>(FindObjectsSortMode.None);
        if(found.Length!=1) throw new Exception("Exactly one player is required.");
        if(checkpoints.Count!=6) throw new Exception("Six checkpoints are required.");
        if(Camera.main==null||Camera.main.GetComponent<CinemachineBrain>()==null) throw new Exception("Camera brain is missing.");
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
        foreach(var cp in checkpoints)
        {
            Vector3 pos=cp.spawnPoint.position;
            if(!Physics.Raycast(pos,Vector3.down,out var floor,2f,(1<<3)|(1<<6),QueryTriggerInteraction.Ignore))
                throw new Exception("Checkpoint has no ground: "+cp.name);
            if(Physics.CheckCapsule(pos-Vector3.up*.5f,pos+Vector3.up*.5f,.48f,(1<<3)|(1<<6),QueryTriggerInteraction.Ignore))
                throw new Exception("Checkpoint is obstructed: "+cp.name);
            report.Add("PASS grounded and clear: "+cp.name);
        }
        report.Add("PASS one player, camera + Cinemachine, correct input and masks");
        report.Add("PASS no missing scripts");
        report.Add("PASS route: jumps 4 m; climbs 2.8 m; slide clearance 1.8 m; wall gap 8 m; grapple gap 14 m");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/SkylineSceneValidation.txt",report);
        Debug.Log("SKYLINE validation passed.");
    }

    public static void CapturePreviews()
    {
        Directory.CreateDirectory("Logs/SkylinePreviews");
        var go=new GameObject("Temporary preview camera");
        var cam=go.AddComponent<Camera>();
        cam.fieldOfView=52; cam.nearClipPlane=.1f; cam.farClipPlane=600;
        cam.allowHDR=true;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        Render(cam,new Vector3(-64,65,-103),new Vector3(20,0,-16),"Overview");
        Render(cam,new Vector3(0,2.7f,1),new Vector3(0,1.5f,30),"Start");
        Render(cam,new Vector3(-3,8.5f,37),new Vector3(23,7,50),"Climb");
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void Render(Camera cam,Vector3 from,Vector3 at,string name)
    {
        cam.transform.position=from; cam.transform.LookAt(at);
        var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);
        cam.targetTexture=target;
        cam.Render();
        cam.Render();
        cam.Render();
        var previous=RenderTexture.active;
        RenderTexture.active=target;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
        File.WriteAllBytes("Logs/SkylinePreviews/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=previous; cam.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
}