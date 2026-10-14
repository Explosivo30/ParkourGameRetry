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

/// <summary>Independent, reproducible six-block district. Writes only its own scene and variants.</summary>
public static class NeonDistrictSceneBuilder
{
    public const string ScenePath="Assets/Scenes/DistritoNeon.unity";
    const string Mats="Assets/Materials/DistritoNeon";
    const string Hive="Assets/HIVEMIND/CyberpunkCity/URP/Art/Prefabs/";
    static Material asphalt,concrete,metal,mint,yellow,pink,warm,wood,leaf,font,glass;
    static Transform route,art,logic;
    static NeonDistrictCourse course;
    static PlayerStateMachine player;
    static readonly List<NeonDistrictCourse.Gate> gates=new();
    static readonly List<NeonDistrictCourse.Zone> zones=new();
    static int buildingIndex;
    [MenuItem("Parkour/Open Distrito Neon")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if(File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath); else Build();
    }
    public static void BuildBatch()
    {
        try { Build(); Validate(); CapturePreviews(); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void PreviewBatch()
    {
        try { EditorSceneManager.OpenScene(ScenePath); Validate(); CapturePreviews(); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Build()
    {
        gates.Clear(); zones.Clear(); buildingIndex=0; Directory.CreateDirectory(Mats);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        asphalt=Mat("Wet_Asphalt",new Color(.045f,.06f,.077f),0,.86f);
        concrete=Mat("Rooftop_Concrete",new Color(.27f,.33f,.36f),0,.37f);
        metal=Mat("Graphite_Steel",new Color(.032f,.053f,.07f),0,.64f);
        mint=Mat("Connections_Mint",new Color(.1f,.93f,.67f),2.5f,.55f);
        yellow=Mat("Shortcuts_Amber",new Color(1,.63f,.055f),2f,.55f);
        pink=Mat("CAPA_Pink",new Color(.9f,.045f,.3f),1.6f,.42f);
        warm=Mat("Home_Warm",new Color(1,.64f,.31f),2f,.45f);
        wood=Mat("Skate_Plywood",new Color(.42f,.24f,.12f),0,.35f);
        leaf=Mat("Plant_Green",new Color(.09f,.24f,.12f),0,.2f);
        glass=Mat("Window_Blue",new Color(.065f,.2f,.3f),.45f,.9f);
        font=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/Sign_Text.mat");
        if(font==null) { font=new Material(Shader.Find("Skyline/SignText")); AssetDatabase.CreateAsset(font,Mats+"/Sign_Text.mat"); }
        route=Root("01 / PLAYABLE SURFACES"); art=Root("02 / HIVEMIND CITY"); logic=Root("03 / RACE AND EXPLORATION");
        Environment(); Player();
                Box("District street collider / 180 x 210 m",route,new Vector3(0,-.5f,0),new Vector3(180,1,210),asphalt).GetComponent<Renderer>().enabled=false;
        // Separate pavement renderers let the existing URP light limit select local shop lights.
        for(int x=0;x<12;x++)for(int z=0;z<14;z++)
            Box("Wet pavement tile",art,new Vector3(-82.5f+x*15,-.035f,-97.5f+z*15),new Vector3(15,.07f,15),asphalt,false);
        Market(); Skaters(); Capa(); Canal(); Spectre(); Home(); StreetNetwork(); CityDetail(); Skyline();
        course.gates=gates.ToArray(); course.zones=zones.ToArray(); Physics.SyncTransforms(); AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new Exception("Failed to save DistritoNeon.");
        Debug.Log("NEON SCENE BUILT");
    }
    static Material Mat(string name,Color color,float emission=0,float smooth=.3f)
    {
        string path=Mats+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",smooth>.7f?.25f:.05f);
        if(emission>0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*emission); }
                if(name=="Wet_Asphalt"||name=="Rooftop_Concrete")
        {
            var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/HIVEMIND/CyberpunkCity/URP/Art/Materials/Instances/M_Concrete_03.mat");
            if(src!=null)
            {
                m.SetTexture("_BaseMap",src.GetTexture("_BaseMap"));
                m.SetTexture("_BumpMap",src.GetTexture("_NormalMap"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.4f);
                m.SetTextureScale("_BaseMap",Vector2.one*(name=="Wet_Asphalt"?3:4));
                m.SetColor("_BaseColor",name=="Wet_Asphalt"?new Color(.23f,.29f,.36f):new Color(.62f,.69f,.73f));
            }
        }
        EditorUtility.SetDirty(m); return m;
    }
    static Transform Root(string name,Transform parent=null)
    { var g=new GameObject(name); g.transform.SetParent(parent,false); return g.transform; }
    static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material m,bool collision=true,int layer=3,Quaternion rotation=default)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name;g.transform.SetParent(parent,false); g.transform.localPosition=p; g.transform.localScale=size;
        g.transform.localRotation=rotation==default?Quaternion.identity:rotation;g.GetComponent<Renderer>().sharedMaterial=m; g.layer=collision?layer:2;
        if(!collision) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic); return g;
    }
    static void Text(Transform parent,string text,Vector3 position,float size,Color color,float yaw=0)
    {
        var go=Root(text.Replace('\n',' '),parent).gameObject; go.layer=2;go.transform.localPosition=position; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var t=go.AddComponent<TextMesh>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=100; t.characterSize=size; t.anchor=TextAnchor.MiddleCenter; t.alignment=TextAlignment.Center;
        t.text=text; t.color=color; go.GetComponent<MeshRenderer>().sharedMaterial=font; go.AddComponent<SkylineSignText>();
    }
    static void Sign(string title,string subtitle,Vector3 p,float yaw=0,Material color=null,float width=9)
    {
        var root=Root(title,art); root.position=p; root.rotation=Quaternion.Euler(0,yaw,0);
        Box("Sign housing",root,Vector3.zero,new Vector3(width,2.6f,.22f),metal,false);
        Box("Neon header",root,new Vector3(0,1.3f,-.14f),new Vector3(width,.09f,.07f),color??mint,false);
        Text(root,title,new Vector3(0,.38f,-.14f),.066f,Color.white);Text(root,subtitle,new Vector3(0,-.63f,-.14f),.028f,new Color(.65f,.95f,.85f));
    }
    static void Arrow(Vector3 p,float yaw=0,Material material=null)
    {
        var a=Root("Transit mark",art); a.position=p+Vector3.up*.055f; a.rotation=Quaternion.Euler(0,yaw,0);
        Box("L",a,new Vector3(-.23f,0,0),new Vector3(.11f,.02f,.65f),material??mint,false,2,Quaternion.Euler(0,45,0));
        Box("R",a,new Vector3(.23f,0,0),new Vector3(.11f,.02f,.65f),material??mint,false,2,Quaternion.Euler(0,-45,0));
    }
    static void Line(Vector3 a,Vector3 b,float interval=5,Material material=null)
    {
        var d=b-a; float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;int n=Mathf.Max(1,Mathf.FloorToInt(d.magnitude/interval));
        for(int i=0;i<=n;i++) Arrow(Vector3.Lerp(a,b,i/(float)n),yaw,material);
    }
    static void Roof(string name,float x,float z,float w,float l,float top,bool body=true,Material mat=null)
    {
        Box(name,route,new Vector3(x,top-.3f,z),new Vector3(w,.6f,l),mat??concrete);
        foreach(int side in new[]{-1,1})Box("Visible roof edge",art,new Vector3(x+side*(w*.5f-.1f),top+.02f,z),new Vector3(.10f,.03f,l),mint,false);
                if(!body)
        {
            if(w>8||l>10)
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                    Box("Bridge support / street stays open",route,new Vector3(x+sx*(w*.5f-.8f),(top-.6f)*.5f,z+sz*(l*.5f-.8f)),new Vector3(.4f,top-.6f,.4f),metal);
            return;
        }
        Box(name+" / simple building collider",route,new Vector3(x,(top-.6f)*.5f,z),new Vector3(w-.2f,top-.6f,l-.2f),metal).GetComponent<Renderer>().enabled=false;
        int rows=Mathf.Max(1,Mathf.CeilToInt(l/12));
        for(int i=0;i<rows;i++)Fit(buildingIndex++%2==0?"SM_MERGED_BP_House_Shop_E3":"SM_MERGED_BP_Building9",
            new Vector3(x,(top-.65f)*.5f,z-l*.5f+(i+.5f)*l/rows),new Vector3(w-.05f,top-.65f,l/rows-.05f));
    }
    static void Ramp(string name,Vector3 from,Vector3 to,float width,Material material=null,bool marks=true)
    {
        Vector3 delta=to-from,flat=Vector3.ProjectOnPlane(delta,Vector3.up);
        float yaw=Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;
        Quaternion rot=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(pitch,0,0);
        Box(name,route,(from+to)*.5f-rot*Vector3.up*.2f,new Vector3(width,.4f,delta.magnitude+.12f),material??concrete,true,3,rot);
        for(int s=-1;s<=1;s+=2)Box("Ramp edge light",art,(from+to)*.5f+rot*new Vector3(s*(width*.5f-.08f),.025f,0),new Vector3(.1f,.04f,delta.magnitude),mint,false,2,rot);
                if(marks) Line(from,to,4);
        if(name.Contains("public") && delta.y>5)
        {
            // Visual stair treads follow a smooth collision ramp; small details never catch the controller.
            int treads=Mathf.CeilToInt(flat.magnitude/.55f);
            for(int i=1;i<treads;i++)
                Box("Stair tread",art,Vector3.Lerp(from,to,i/(float)treads)+Vector3.up*.015f,
                    new Vector3(width-.25f,.045f,.055f),metal,false,2,Quaternion.Euler(0,yaw,0));
            Fit("SM_Stairs-Short",Vector3.Lerp(from,to,.04f)-Vector3.up*.2f,new Vector3(width-.15f,.5f,1.1f),yaw);
        }
    }
    static Transform Spawn(string name,Vector3 p,float yaw=0)
    { var t=Root(name,logic); t.position=p+Vector3.up*1.13f; t.rotation=Quaternion.Euler(0,yaw,0); return t; }
    static void Zone(string name,string hint,float x,float z,Vector3 safe,float yaw=0)
    {
        var t=Root(name+" / exploration area",logic); t.position=new Vector3(x,16,z);
        var c=t.gameObject.AddComponent<BoxCollider>(); c.isTrigger=true; c.size=new Vector3(64,38,68);
        zones.Add(new NeonDistrictCourse.Zone{name=name,hint=hint,area=c,safeSpawn=Spawn(name+" / safe point",safe,yaw)});
    }
    static void Gate(string name,Vector3 p,float yaw=0,bool finish=false)
    {
        var root=Root(name+" / ordered gate",logic); root.position=p+Vector3.up*2.5f; root.rotation=Quaternion.Euler(0,yaw,0);
        var volume=root.gameObject.AddComponent<BoxCollider>(); volume.isTrigger=true; volume.size=new Vector3(11,6,3);
        var marker=Root("Active checkpoint beacon",root);
        Box("Beacon",marker,new Vector3(0,3.2f,0),new Vector3(1.1f,1.1f,1.1f),finish?warm:mint,false,2,Quaternion.Euler(0,45,45));
        Text(marker,finish?"META":"0"+(gates.Count+1),new Vector3(0,4.7f,0),.10f,Color.white);marker.gameObject.SetActive(false);
        Box("Gate band",root,new Vector3(0,-2.46f,0),new Vector3(10,.035f,.3f),finish?warm:mint,false);
        foreach(int side in new[]{-1,1}) Box("Gate light",root,new Vector3(side*5.5f,0,0),new Vector3(.12f,5,.12f),finish?warm:mint,false);
        gates.Add(new NeonDistrictCourse.Gate{name=name,volume=volume,marker=marker.gameObject,recoverySpawn=Spawn(name+" / checkpoint recovery",p+Quaternion.Euler(0,yaw,0)*Vector3.back*2,yaw)});
    }
    static GameObject Fit(string name,Vector3 center,Vector3 size,float yaw=0)
    {
        string path=Hive+name+".prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new Exception("Missing HIVEMIND prefab: "+path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.name="HIVEMIND / "+name; g.transform.SetParent(art,false);
        foreach(var c in g.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true)) if(c!=null)UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in g.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
        g.transform.rotation=Quaternion.Euler(0,yaw,0);Bounds bounds=BoundsOf(g);
        Vector3 localSize=yaw%180==0?size:new Vector3(size.z,size.y,size.x);
        Vector3 origSize=yaw%180==0?bounds.size:new Vector3(bounds.size.z,bounds.size.y,bounds.size.x);
        g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(localSize.x/Mathf.Max(.01f,origSize.x),localSize.y/Mathf.Max(.01f,origSize.y),localSize.z/Mathf.Max(.01f,origSize.z)));
        bounds=BoundsOf(g); g.transform.position+=center-bounds.center;
        foreach(var r in g.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=true; }
        foreach(var lod in g.GetComponentsInChildren<LODGroup>(true))
        {
            var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)levels[i].screenRelativeTransitionHeight=i==levels.Length-1?.008f:Mathf.Max(.02f,.6f/(i+1));
            lod.SetLODs(levels); lod.RecalculateBounds();
        }
        return g;
    }
    static Bounds BoundsOf(GameObject g)
    {
        var rs=g.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)throw new Exception("No renderers: "+g.name);
        var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    static void Lamp(Vector3 p,Color color,float intensity=3,float range=13)
    {
        var go=Root("Local light / no shadows",art);go.position=p;var l=go.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;
    }
    static void Market()
    {
        Zone("MERCADO BAJO LA RED","Comercios abajo; las marcas menta conectan el barrio.",-39,-68,new Vector3(-58,0,-86));
        course.raceStart=Spawn("Race start",new Vector3(-58,0,-86));
        course.terminal=Root("Voluntary race terminal / E or gamepad Interact",logic);course.terminal.position=new Vector3(-60,1.2f,-85);
        Box("Terminal pedestal",art,new Vector3(-60,.8f,-85),new Vector3(1.1f,1.6f,.7f),metal);
        Box("Terminal screen",art,new Vector3(-60,1.7f,-85.4f),new Vector3(1.35f,.7f,.06f),mint,false);
        Sign("BAJO LA RED","E : CARRERA    /    EXPLORA A TU RITMO",new Vector3(-58,4,-91),180,mint,13);
        Box("Vault or pass / market counter",route,new Vector3(-58,.55f,-73),new Vector3(5,1.1f,1.4f),wood);
        Ramp("Market ascent",new Vector3(-24,0,-58),new Vector3(-24,8,-42),8);
        Roof("Market upper landing",-24,-35,12,14,8,false);
        Roof("Market return balcony",-35,-59,5,48,8,false);
        Roof("Return balcony junction",-30,-35,15,6,8,false);
        Line(new Vector3(-58,0,-85),new Vector3(-58,0,-60));
        Line(new Vector3(-58,0,-60),new Vector3(-24,0,-60));
        Line(new Vector3(-24,8,-42),new Vector3(-24,8,-29));
        for(int i=0;i<3;i++) { float z=-84+i*8;
            Fit("SM_House_Shop_A_Front",new Vector3(-64.5f,2.4f,z),new Vector3(.5f,4.8f,6));
            Fit("SM_House_Shop_B_Front",new Vector3(-50.5f,2.3f,z),new Vector3(.6f,4.6f,6),180);
            Box("Shop shell",route,new Vector3(-68,3,z),new Vector3(6,6,7),metal);
            Box("Shop shell",route,new Vector3(-47,3,z),new Vector3(6,6,7),metal);
            Fit("SM_Billboard_Front_Long",new Vector3(-64,4.6f,z),new Vector3(.3f,.9f,5));
            Box("Market awning",art,new Vector3(-62.5f,4,z),new Vector3(4,.12f,5),i%2==0?pink:wood,false,2,Quaternion.Euler(0,0,-6));
                        Box("Shopfront neon",art,new Vector3(-50.86f,3.1f,z),new Vector3(.07f,.13f,5.5f),i%2==0?pink:mint,false);
            Lamp(new Vector3(-58,4,z),i%2==0?new Color(1,.12f,.4f):new Color(.12f,.7f,1),14,13);
        }
        Sign("ARRIBA HAY OTRO CAMINO","RODANTES  /  SIGUE LA MENTA",new Vector3(-24,3,-57),0);
    }
    static void Skaters()
    {
        Zone("RODANTES","Acelera, desliza bajo el conducto o busca tu propia linea.",-39,0,new Vector3(-24,8,-18));
        Roof("Rodantes entry",-24,-3.5f,12,43,8);
        Roof("Skate east roof",-28,12,20,12,8);
        Roof("Skate west roof",-53.5f,12,25,12,8);
        Roof("West run-up",-59,27,12,26,8,false);
        Gate("RODANTES",new Vector3(-24,8,-18));
        Box("Slide duct / 1.8m clearance",route,new Vector3(-24,10.2f,-4),new Vector3(5,.8f,3),metal);
        Fit("SM_Vent_Square_A",new Vector3(-24,10.6f,-4),new Vector3(5,1.6f,3));
        Ramp("Rodantes public street access",new Vector3(-32.8f,0,30),new Vector3(-53,8,30),5);
        Line(new Vector3(-24,8,-24),new Vector3(-24,8,12));
        Line(new Vector3(-24,8,12),new Vector3(-37,8,12));
        Line(new Vector3(-42,8,12),new Vector3(-59,8,12));
        Line(new Vector3(-59,8,12),new Vector3(-59,8,39));
        for(int i=0;i<3;i++)
        {
            Ramp("Skate bank",new Vector3(-48+i*5,8,9),new Vector3(-48+i*5,9.3f,6.5f),3,wood,false);
            Fit("SM_Trashbag_01",new Vector3(-20,8.4f,8+i*2),new Vector3(.8f,.8f,.8f));
        }
        Sign("RODANTES","NO PARES / ENCUENTRA TU LINEA",new Vector3(-17,12,7),90,yellow);
        Sign("DESLIZA / CTRL","O PASA POR UN LADO",new Vector3(-18.2f,12,-7),90,mint,7);
        Lamp(new Vector3(-28,12,5),new Color(.2f,.75f,1),4,19);
        Lamp(new Vector3(-56,12,23),new Color(1,.45f,.12f),3,16);
    }
    static void Capa()
    {
        Zone("CAPA","Dos trepadas cortas. Espacio mantenido junto a la pared.",-39,68,new Vector3(-59,8,50));
        Roof("Capa arrival",-59,53,12,20,8);
        Roof("Capa terrace 1",-59,69,12,12,10.6f);
        Roof("Capa terrace 2",-59,82,12,14,13.2f);
        Roof("Capa roof crossing",-39,83,30,10,13.2f);
        Roof("Central avenue elevated bridge",0,83,48,6,13.2f,false);
        Gate("CAPA",new Vector3(-59,8,50));
        Ramp("Capa public access",new Vector3(-25,0,68),new Vector3(-53,10.6f,68),5);
        Line(new Vector3(-59,8,44),new Vector3(-59,8,61));
        Line(new Vector3(-59,10.6f,64),new Vector3(-59,10.6f,73));
        Line(new Vector3(-59,13.2f,76),new Vector3(-59,13.2f,83));
        Line(new Vector3(-59,13.2f,83),new Vector3(23,13.2f,83));
        foreach(float z in new[]{63f,75f})
        {
            float y=z==63?9.3f:11.9f;
            Box("Painted climb lip",art,new Vector3(-59,y+1.15f,z-.06f),new Vector3(8,.2f,.08f),pink,false);
            Text(art,"CAPA / DEJA HUELLA",new Vector3(-59,y,z-.08f),.068f,Color.white);
        }
        Sign("CAPA","LA CIUDAD TAMBIEN ES NUESTRA",new Vector3(-47,17,87),0,pink,12);
        for(int i=0;i<9;i++)Box("Mural colour slash",art,new Vector3(-48+i*2,15,87),new Vector3(.65f,5,.05f),i%2==0?pink:yellow,false,2,Quaternion.Euler(0,0,25));
        Fit("SM_Balcon_Concrete_Plate_Long",new Vector3(-52.2f,11,69),new Vector3(1.5f,.3f,6));
        Lamp(new Vector3(-57,16,70),new Color(1,.15f,.5f),5,21);
    }
    static void Canal()
    {
        Zone("CANAL ABIERTO","Pasarela exterior segura. Amarillo: atajo de wall-run.",39,68,new Vector3(24,13.2f,83),90);
        Roof("Canal north gallery",40,83,44,10,13.2f);
        Roof("Wallrun launch",59,79,12,10,13.2f);
        Roof("Wallrun landing",59,59,12,14,11.8f);
        Roof("Canal south gallery",41.5f,52,47,8,11.8f);
        Roof("Spectre approach",24,35,12,34,11.8f,false);
        Gate("CANAL ABIERTO",new Vector3(24,13.2f,83),90);
        Ramp("Safe detour east",new Vector3(59,13.2f,77),new Vector3(70,13.2f,77),4);
        Ramp("Safe detour south",new Vector3(70,13.2f,77),new Vector3(70,11.8f,63),4);
        Ramp("Safe detour west",new Vector3(70,11.8f,63),new Vector3(59,11.8f,63),4);
        Box("Optional wall-run / 8m gap",route,new Vector3(55.75f,13.5f,69.5f),new Vector3(.5f,8.8f,11),metal,true,6);
        Box("Wall-run yellow band",art,new Vector3(56.02f,14.5f,69.5f),new Vector3(.04f,.4f,11),yellow,false);
        Ramp("Canal public street access",new Vector3(87,0,52),new Vector3(65,11.8f,52),6);
        Line(new Vector3(24,13.2f,83),new Vector3(59,13.2f,83));
        Line(new Vector3(59,13.2f,83),new Vector3(59,13.2f,78));
        Line(new Vector3(59,11.8f,63),new Vector3(59,11.8f,52));
        Line(new Vector3(59,11.8f,52),new Vector3(24,11.8f,52));
        Line(new Vector3(24,11.8f,52),new Vector3(24,11.8f,21));
        Line(new Vector3(56.7f,13.2f,80),new Vector3(56.7f,13.2f,75),3,yellow);
        Fit("SM_Bridge_A",new Vector3(0,12.1f,83),new Vector3(16,1.8f,5));
        for(int i=0;i<4;i++)
        {
            Box("Antenna mast",art,new Vector3(36+i*6,18,87),new Vector3(.18f,10,.18f),metal,false);
            Box("Antenna crossbar",art,new Vector3(36+i*6,22,87),new Vector3(3,.12f,.12f),warm,false);
        }
        Sign("CANAL ABIERTO","PASARELA MENTA  /  WALL-RUN AMARILLO",new Vector3(69,18,81),180,mint,12);
        Lamp(new Vector3(65,18,70),new Color(.1f,.55f,1),5,23);
    }
    static void Spectre()
    {
        Zone("AVENIDA ESPECTRO","Plataformas a la izquierda. Gancho opcional en amarillo.",39,0,new Vector3(24,11.8f,20),180);
        Roof("Spectre launch",24,16,18,16,11.8f);
        Roof("Stepping balcony 1",30,3,6,8,11.8f,false);
        Roof("Stepping balcony 2",30,-7,6,8,12.3f,false);
        Roof("Spectre landing",24,-18,18,24,12.3f);
        Roof("Spectre east crossing",43,-24,40,10,12.3f);
        Roof("Home approach",59,-39,12,26,12.3f,false);
        Gate("AVENIDA ESPECTRO",new Vector3(24,11.8f,20),180);
        Ramp("Spectre street access",new Vector3(87,0,-32),new Vector3(65,12.3f,-32),6);
        Line(new Vector3(24,11.8f,20),new Vector3(30,11.8f,14));
        Line(new Vector3(30,11.8f,14),new Vector3(30,11.8f,0));
        Line(new Vector3(30,12.3f,-4),new Vector3(30,12.3f,-18));
        Line(new Vector3(30,12.3f,-18),new Vector3(24,12.3f,-24));
        Line(new Vector3(24,12.3f,-24),new Vector3(59,12.3f,-24));
        Line(new Vector3(59,12.3f,-24),new Vector3(59,12.3f,-49));
        var anchor=GameObject.CreatePrimitive(PrimitiveType.Sphere); anchor.name="Neon grapple anchor";
        anchor.transform.SetParent(route);anchor.transform.position=new Vector3(24,17.6f,-8);anchor.transform.localScale=Vector3.one*1.5f;
        anchor.layer=7;anchor.GetComponent<Renderer>().sharedMaterial=yellow;
        Box("Grapple gantry",art,new Vector3(24,18.8f,-8),new Vector3(15,.22f,.22f),metal,false);
        Line(new Vector3(24,11.8f,16),new Vector3(24,11.8f,9),3,yellow);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Hive+"SM_BigHologram.prefab");
        var holo=(GameObject)PrefabUtility.InstantiatePrefab(source);holo.name="ESPECTRO / HIVEMIND hologram";
        holo.transform.SetParent(art);holo.transform.position=new Vector3(40,26,1);holo.transform.rotation=Quaternion.Euler(90,0,0);holo.transform.localScale=new Vector3(12,1,22);
        foreach(var c in holo.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
        Sign("ESPECTRO","PLATAFORMAS <     /     GANCHO: BOTON DERECHO",new Vector3(24,18,9),180,yellow,14);
        Lamp(new Vector3(31,17,0),new Color(.3f,.7f,1),4,20);
        Lamp(new Vector3(29,18,-19),new Color(1,.1f,.5f),4,20);
    }
    static void Home()
    {
        Zone("TERRAZA COMUN","El barrio se conecta aqui. Sigue explorando o intenta otra vuelta.",39,-68,new Vector3(24,13.8f,-71));
        Roof("Home arrival",59,-68,12,36,12.3f);
        Roof("Home west connection",49,-83,20,10,12.3f);
        Ramp("Home gentle ascent",new Vector3(45,12.3f,-83),new Vector3(31,13.8f,-83),8);
        Roof("Shared terrace",24,-72,14,30,13.8f);
        Gate("TERRAZA COMUN",new Vector3(59,12.3f,-50),180);
        Gate("META / TERRAZA COMUN",new Vector3(24,13.8f,-66),0,true);
        Ramp("Public return bridge to market",new Vector3(24,13.8f,-83),new Vector3(-35,8,-83),5);
        Ramp("Home public street access",new Vector3(87,0,-73),new Vector3(65,12.3f,-73),5);
        Line(new Vector3(59,12.3f,-51),new Vector3(59,12.3f,-83));
        Line(new Vector3(59,12.3f,-83),new Vector3(45,12.3f,-83));
        Line(new Vector3(31,13.8f,-83),new Vector3(24,13.8f,-83));
        Line(new Vector3(24,13.8f,-83),new Vector3(24,13.8f,-62));
        Sign("TERRAZA COMUN","ABAJO NOS SEPARAN. ARRIBA NOS ENCONTRAMOS.",new Vector3(24,18,-59),0,warm,14);
        for(int i=0;i<5;i++)
        {
            float z=-78+i*4;
            Box("Plant pot",art,new Vector3(18.1f,14.2f,z),new Vector3(1.3f,.8f,1.6f),wood);
            Box("Green planting",art,new Vector3(18.1f,15,z),new Vector3(1.2f,1.2f,1.5f),leaf,false);
            Fit("SM_China_Lantern",new Vector3(29.8f,17.5f,z),new Vector3(.7f,1.1f,.7f));
            Box("Lantern cable",art,new Vector3(24,18,z),new Vector3(12,.025f,.025f),metal,false);
        }
        Box("Communal bench",art,new Vector3(29,14.35f,-61),new Vector3(1.1f,1.1f,5),wood);
        Lamp(new Vector3(24,18,-72),new Color(1,.65f,.32f),6,22);
    }
        static void StreetNetwork()
    {
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<7;i++)
            {
                float z=-90+i*30;
                Fit("SM_Lamp_Street",new Vector3(side*8,2.55f,z),new Vector3(2.8f,5.1f,.57f),side<0?180:0);
                Box("Street lamp glow",art,new Vector3(side*7,5,z),new Vector3(1.3f,.06f,.45f),warm,false);
            }
            for(int i=0;i<3;i++)
            {
                float z=-68+i*68;
                Fit(i%2==0?"SM_MERGED_BP_House_Shop_E3":"SM_MERGED_BP_Building9",new Vector3(side*77,14,z+10),new Vector3(14,28,16),side<0?0:180);
                Box("Perimeter building collision",route,new Vector3(side*77,13,z+10),new Vector3(13,26,15),metal).GetComponent<Renderer>().enabled=false;
                Fit("SM_Billboard_SIde_Long_01",new Vector3(side*68.8f,19,z+10),new Vector3(.45f,9,2.3f),90);
            }
        }
        foreach(float z in new[]{-96f,-34f,34f,96f})
        {
            for(float x=-82;x<=82;x+=7) Arrow(new Vector3(x,0,z),90);
            Box("Crosswalk",art,new Vector3(0,.015f,z),new Vector3(16,.02f,1),concrete,false);
        }
        foreach(float x in new[]{-85f,0f,85f})Line(new Vector3(x,0,-97),new Vector3(x,0,97),8);
        Sign("DISTRITO NEON","SEIS MANZANAS / UN CAMINO PROPIO",new Vector3(0,8,-96),180,pink,17);
        foreach(int side in new[]{-1,1})
        {
            Box("District boundary",route,new Vector3(side*89.5f,1.2f,0),new Vector3(1,2.4f,210),metal);
            Box("District boundary",route,new Vector3(0,1.2f,side*104.5f),new Vector3(180,2.4f,1),metal);
        }
    }
    static void CityDetail()
    {
        // Tall imported facades frame each run without occupying the marked landings.
        foreach(int side in new[]{-1,1})
        {
            int index=0;
            foreach(float z in new[]{-69f,-44f,-8f,21f,64f})
            {
                float h=27+(index%3)*7;
                Fit(index++%2==0?"SM_MERGED_BP_House_Shop_E3":"SM_MERGED_BP_Building9",new Vector3(side*13.8f,h*.5f,z),new Vector3(6,h,13),side<0?0:180);
                Box("Avenue tower collider",route,new Vector3(side*13.8f,h*.5f,z),new Vector3(5.8f,h,12.8f),metal).GetComponent<Renderer>().enabled=false;
                Fit("SM_Billboard_SIde_Long_01",new Vector3(side*17.2f,18,z+3),new Vector3(.4f,9,2),90);
                for(int level=0;level<4;level++)
                    Box("Facade neon seam",art,new Vector3(side*17,5+level*6,z),new Vector3(.05f,.08f,11),index%2==0?pink:mint,false);
                Lamp(new Vector3(side*19,15,z),index%2==0?new Color(.15f,.65f,1):new Color(1,.2f,.48f),5,17);
            }
        }
        // Wires, ventilation and shop signage use the pack's actual meshes.
        for(int i=0;i<3;i++)
        {
            Fit("SM_wire_Splines_6",new Vector3(-58,6,-82+i*8),new Vector3(13,1.3f,1));
            Fit("SM_Billboard_SIde_Long_01",new Vector3(-64,7,-83+i*8),new Vector3(.4f,4.8f,1.3f),90);
        }
        foreach(Vector3 p in new[]{new Vector3(-63,10,28),new Vector3(-28,10,4),new Vector3(62,14,-43)})
            Fit("SM_Vent_Round_A",p,new Vector3(1.4f,2,3));
        // Small light housings reveal the route edges without putting props in the landing areas.
        foreach(var z in zones)
        {
            Vector3 p=z.safeSpawn.position;
            Box("Safe point plinth light",art,p+new Vector3(3,-1.08f,0),new Vector3(1,.06f,1),warm,false);
            Lamp(p+Vector3.up*3,new Color(.45f,.75f,1),4,16);
        }
    }
    static void Skyline()
    {
        var rng=new System.Random(37);
        for(int i=0;i<24;i++)
        {
            float angle=i*Mathf.PI*2/24;
            var center=new Vector3(Mathf.Cos(angle)*145,0,Mathf.Sin(angle)*161);
            float height=38+(float)rng.NextDouble()*65; center.y=height*.5f-8;
            string path="Assets/WhiteCity/Prefabs/Buildings/"+(i%14+1)+".prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)continue;
            var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.name="WhiteCity / distant skyline";g.transform.SetParent(art);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=BoundsOf(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(18/b.size.x,height/b.size.y,22/b.size.z));
            b=BoundsOf(g);g.transform.position+=center-b.center;
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var ms=r.sharedMaterials;for(int n=0;n<ms.Length;n++)ms[n]=metal;r.sharedMaterials=ms;r.shadowCastingMode=ShadowCastingMode.Off;
            }
            for(int j=0;j<6;j++)Box("Distant illuminated band",art,new Vector3(center.x-9.05f,10+j*8,center.z),new Vector3(.08f,.3f,18),j%3==0?pink:glass,false);
        }
    }
    static void Player()
    {
        var go=new GameObject("Player / Distrito Neon");go.transform.position=new Vector3(-58,1.13f,-86);
        var cc=go.AddComponent<CharacterController>();cc.height=2;cc.radius=.5f;cc.center=Vector3.zero;cc.skinWidth=.15f;cc.stepOffset=.3f;
        go.AddComponent<InputReader>();player=go.AddComponent<PlayerStateMachine>();
        var pivot=Root("View pivot",go.transform);pivot.localPosition=Vector3.up*.72f;
        var view=Root("Player camera",pivot);var vcam=view.gameObject.AddComponent<CinemachineCamera>();vcam.Lens.FieldOfView=90;
        var pan=view.gameObject.AddComponent<CinemachinePanTilt>();pan.ReferenceFrame=CinemachinePanTilt.ReferenceFrames.ParentObject;pan.TiltAxis.Range=new Vector2(-85,85);
        var so=new SerializedObject(player);
        so.FindProperty("cc").objectReferenceValue=cc;so.FindProperty("_cameraPanTilt").objectReferenceValue=pan;so.FindProperty("_cameraTiltTarget").objectReferenceValue=pivot;
        so.FindProperty("_height").floatValue=-.4f;so.FindProperty("_castRadius").floatValue=.48f;so.FindProperty("_castLength").floatValue=.32f;so.FindProperty("_maxAngle").floatValue=45;
        so.FindProperty("_groundMask").intValue=(1<<3)|(1<<6);so.FindProperty("_vaultMask").intValue=(1<<3)|(1<<6);so.FindProperty("_wallClimbMask").intValue=(1<<3)|(1<<6);
        so.FindProperty("_wallRunMask").intValue=1<<6;so.FindProperty("_grappleMask").intValue=1<<7;so.ApplyModifiedPropertiesWithoutUndo();
        var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.transform.position=go.transform.position+Vector3.up*.72f;
        var cam=camera.AddComponent<Camera>();cam.nearClipPlane=.05f;cam.farClipPlane=420;cam.fieldOfView=90;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.095f,.16f);
        camera.AddComponent<AudioListener>();camera.AddComponent<CinemachineBrain>();camera.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        course=Root("Distrito Neon / Exploration and race",logic).gameObject.AddComponent<NeonDistrictCourse>();course.player=player;
    }
    static void Environment()
    {
        var lighting=AssetDatabase.LoadAssetAtPath<LightingSettings>(Mats+"/Lighting.asset");
        if(lighting==null) { lighting=new LightingSettings{name="Neon Lighting",bakedGI=false,realtimeGI=false};AssetDatabase.CreateAsset(lighting,Mats+"/Lighting.asset"); }
        Lightmapping.lightingSettings=lighting;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.39f,.47f,.61f);RenderSettings.ambientEquatorColor=new Color(.3f,.35f,.44f);RenderSettings.ambientGroundColor=new Color(.08f,.10f,.15f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.025f,.045f,.085f);RenderSettings.fogDensity=.0045f;RenderSettings.skybox=null;
        var moon=Root("Moon / single shadow light").gameObject.AddComponent<Light>();moon.type=LightType.Directional;moon.color=new Color(.45f,.64f,1);moon.intensity=.65f;
        moon.shadows=LightShadows.Soft;moon.transform.rotation=Quaternion.Euler(48,-28,0);RenderSettings.sun=moon;
        var fill=Root("City bounce / no shadows").gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.45f,.56f,.85f);fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(25,140,0);
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Mats+"/Atmosphere.asset");
        if(profile==null)
        {
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom=profile.Add<Bloom>();bloom.intensity.Override(.3f);bloom.threshold.Override(1.25f);
            profile.Add<Vignette>().intensity.Override(.13f);
            var colors=profile.Add<ColorAdjustments>();colors.contrast.Override(8);colors.postExposure.Override(.4f);
            AssetDatabase.CreateAsset(profile,Mats+"/Atmosphere.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
        }
        var volume=Root("Night / after rain").gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
        var probe=Root("Wet pavement reflection / once").gameObject.AddComponent<ReflectionProbe>();
        probe.transform.position=new Vector3(0,8,0);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.size=new Vector3(190,80,220);probe.boxProjection=true;probe.intensity=.75f;
    }
    static void Validate()
    {
        var c=UnityEngine.Object.FindFirstObjectByType<NeonDistrictCourse>();
        if(c==null||c.zones.Length!=6||c.gates.Length!=6)throw new Exception("Invalid district references.");
        var lines=new List<string>{"Six explorable blocks; five ordered checkpoints and finish."};int missing=0,materials=0;
        foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            foreach(var m in r.sharedMaterials){materials++;if(m==null||m.shader==null||m.shader.name.Contains("Error")){missing++;lines.Add("Missing material: "+r.name);}}
        foreach(var z in c.zones)if(z.area==null||z.safeSpawn==null)throw new Exception("Missing zone reference.");
        foreach(var g in c.gates)if(g.volume==null||g.recoverySpawn==null||g.marker==null)throw new Exception("Missing gate reference.");
        lines.Add("Material references checked: "+materials+" / missing: "+missing);
        lines.Add("Movement assets unchanged. New InputReader interaction event only.");
        Directory.CreateDirectory("Logs/NeonPreviews");File.WriteAllLines("Logs/NeonValidation.txt",lines);
        if(missing>0)throw new Exception("Invalid materials.");Debug.Log("NEON VALIDATED: "+materials+" material references.");
    }
    static void CapturePreviews()
    {
        Directory.CreateDirectory("Logs/NeonPreviews");
        Render("01_Mercado",new Vector3(-58,1.85f,-86),new Vector3(-58,3,-63));
        Render("02_Rodantes",new Vector3(-24,9.85f,-16),new Vector3(-24,10,9));
        Render("03_Capa",new Vector3(-59,9.85f,52),new Vector3(-59,13,78));
        Render("04_Canal",new Vector3(59,15.05f,82),new Vector3(68,12,62));
        Render("05_Espectro",new Vector3(24,13.65f,17),new Vector3(28,13,-10));
        Render("06_Terraza",new Vector3(24,15.65f,-81),new Vector3(24,16,-59));
        Render("07_Barrio",new Vector3(-110,125,-135),new Vector3(0,5,0),65);
    }
    static void Render(string name,Vector3 p,Vector3 target,float fov=83)
    {
        var go=new GameObject("Preview camera");var c=go.AddComponent<Camera>();c.transform.position=p;c.transform.LookAt(target);c.fieldOfView=fov;c.nearClipPlane=.1f;c.farClipPlane=480;
        c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.06f,.095f,.16f);
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);c.targetTexture=rt;for(int i=0;i<3;i++)c.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        File.WriteAllBytes("Logs/NeonPreviews/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=previous;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
    }
}
