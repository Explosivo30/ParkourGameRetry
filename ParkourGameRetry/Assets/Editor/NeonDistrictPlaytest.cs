using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Input-driven acceptance checks and a bounded 1080p editor benchmark.</summary>
public class NeonDistrictPlaytest
{
    const string Pending="NeonDistrictPlaytest.Pending",TestRecord="DistritoNeon.AcceptanceTest.Best";
    readonly List<string> report=new();
    PlayerStateMachine player; NeonDistrictCourse course;
    Keyboard keyboard; Mouse mouse; Gamepad gamepad;
    IEnumerator routine;int lastFrame=-1;double deadline;
    float distance;Vector3 lastPosition;bool trackDistance;
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(NeonDistrictSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true); Install(); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Install() { EditorApplication.playModeStateChanged-=OnPlayMode;EditorApplication.playModeStateChanged+=OnPlayMode; }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,false);EditorApplication.delayCall+=()=>new NeonDistrictPlaytest().Start();
    }
    void Start()
    {
        Application.runInBackground=true;Time.captureDeltaTime=1f/60f;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();gamepad=InputSystem.AddDevice<Gamepad>();
        player=UnityEngine.Object.FindFirstObjectByType<PlayerStateMachine>();course=UnityEngine.Object.FindFirstObjectByType<NeonDistrictCourse>();
        course.recordKey=TestRecord;PlayerPrefs.DeleteKey(TestRecord);
        routine=Array.IndexOf(Environment.GetCommandLineArgs(),"-neonShortcuts")>=0?Shortcuts():Verify();deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update+=Tick;
    }
    void Tick()
    {
        if(routine==null||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test time limit.");
            if(trackDistance){distance+=Vector3.Distance(player.transform.position,lastPosition);lastPosition=player.transform.position;}
            if(!routine.MoveNext())Finish(0);
        }
        catch(Exception e){report.Add("FAIL "+e+" Position "+player.transform.position+" State "+player.GetCurrentState()?.GetType().Name);Debug.LogException(e);Finish(1);}
    }
    void Finish(int code)
    {
        EditorApplication.update-=Tick;routine=null;File.WriteAllLines("Logs/NeonPlaytest.txt",report);
        PlayerPrefs.DeleteKey(TestRecord);PlayerPrefs.Save();
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(gamepad!=null)InputSystem.RemoveDevice(gamepad);
        Debug.Log("NEON PLAYTEST "+(code==0?"PASSED":"FAILED"));EditorApplication.Exit(code);
    }
    void Check(bool pass,string text)
    { if(!pass)throw new Exception(text);report.Add("PASS "+text);Debug.Log("NEON PASS "+text); }
    void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    IEnumerator Drive(float x,float z,Key extra=Key.None,int limit=720)
    {
        if(extra==Key.None)Keys(Key.W);else Keys(Key.W,extra);int recoveries=course.Recoveries;Vector3 previousPosition=player.transform.position;
        for(int i=0;i<limit;i++)
        {
                        if(Vector3.Distance(previousPosition,player.transform.position)>8)throw new Exception("Unexpected recovery during continuous traversal.");
            previousPosition=player.transform.position;
            Vector3 delta=Vector3.ProjectOnPlane(new Vector3(x,0,z)-player.transform.position,Vector3.up);
            if(delta.magnitude<.9f||course.Mode==NeonDistrictMode.Results)yield break;player.transform.rotation=Quaternion.LookRotation(delta);
            if(course.Recoveries!=recoveries)throw new Exception("Fell on route toward "+x+", "+z);
            yield return null;
        }
        throw new Exception("Cannot reach "+x+", "+z);
    }
    IEnumerator Steps(params IEnumerator[] steps){foreach(var step in steps)while(step.MoveNext())yield return step.Current;}
    void Place(Vector3 surface,float yaw=0)
    {
        var spawn=new GameObject("Test spawn").transform;spawn.position=surface+Vector3.up*1.13f;spawn.rotation=Quaternion.Euler(0,yaw,0);
        player.ResetForCircuit(spawn);UnityEngine.Object.DestroyImmediate(spawn.gameObject);
    }
    IEnumerator Verify()
    {
        for(int i=0;i<15;i++)yield return null;
        Check(player.Grounded&&course.Mode==NeonDistrictMode.Exploration&&course.Elapsed==0,"Grounded exploration spawn; clock remains zero");
        Place(new Vector3(24,13.8f,-66));Keys(Key.E);for(int i=0;i<5;i++)yield return null;Keys();
        Check(course.Mode==NeonDistrictMode.Exploration&&course.NextCheckpoint==0,"Finish and interaction far from market cannot start a race");
        course.CancelRace();for(int i=0;i<5;i++)yield return null;
        Keys(Key.E);yield return null;Keys();
        Check(course.Mode==NeonDistrictMode.Countdown&&course.Elapsed==0&&!player.enabled,"E at terminal starts a frozen three-second countdown");
        for(int i=0;i<190;i++)yield return null;
        Check(course.Mode==NeonDistrictMode.Racing&&player.enabled,"Countdown releases the real player controller");
        Place(new Vector3(24,13.8f,-66));for(int i=0;i<5;i++)yield return null;
        Check(course.Mode==NeonDistrictMode.Racing&&course.NextCheckpoint==0,"Finish rejects skipped checkpoints");
        course.CancelRace();course.BeginRace();for(int i=0;i<190;i++)yield return null;
        distance=0;lastPosition=player.transform.position;trackDistance=true;
        var route=Steps(
            Drive(-58,-77),Drive(-58,-67,Key.Space),Drive(-58,-60),Drive(-24,-60),Drive(-24,-43),Drive(-24,-30),Drive(-24,-18,Key.Space),
            Drive(-24,-10),Drive(-24,1,Key.LeftCtrl),Drive(-24,12),Drive(-35,12),Drive(-49,12,Key.Space),Drive(-59,12),Drive(-59,38),Drive(-59,50,Key.Space));
        while(route.MoveNext())yield return route.Current;
        Check(course.NextCheckpoint==2,"Market ascent, skate roofs, slide and three running jumps completed");
        route=Steps(Drive(-59,60),Drive(-59,69,Key.Space),Drive(-59,72),Drive(-59,83,Key.Space),Drive(24,83));
        while(route.MoveNext())yield return route.Current;
        Check(course.NextCheckpoint==3&&player.transform.position.y>13,"Both Capa terraces climbed and central avenue crossed");
        route=Steps(Drive(59,83),Drive(59,77),Drive(70,77),Drive(70,63),Drive(59,63),Drive(59,52),Drive(24,52),Drive(24,20));
        while(route.MoveNext())yield return route.Current;
        Check(course.NextCheckpoint==4,"Safe Canal bridge needs no wall-run");
        route=Steps(Drive(30,15),Drive(30,10),Drive(30,1,Key.Space),Drive(30,.7f),Drive(30,-13,Key.Space),Drive(24,-24),Drive(59,-24),Drive(59,-50));
        while(route.MoveNext())yield return route.Current;
        Check(course.NextCheckpoint==5,"Main Spectre platforms need no grapple");
        route=Steps(Drive(59,-83),Drive(45,-83),Drive(31,-83),Drive(24,-83),Drive(24,-66));
        while(route.MoveNext())yield return route.Current;
        Keys();trackDistance=false;for(int i=0;i<6;i++)yield return null;
        Check(course.Mode==NeonDistrictMode.Results&&course.NextCheckpoint==6&&course.Recoveries==0,"Continuous complete lap, every checkpoint, zero recoveries");
        float lap=course.Elapsed;report.Add("MEASURE main lap "+NeonDistrictCourse.FormatTime(lap)+" / distance "+distance.ToString("F1")+" m");
        Check(PlayerPrefs.HasKey(TestRecord)&&Mathf.Approximately(PlayerPrefs.GetFloat(TestRecord),lap),"Best time persisted under an isolated district key");
        for(int i=0;i<25;i++)yield return null;
        Check(Mathf.Approximately(lap,course.Elapsed)&&!player.enabled,"Result clock frozen");
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name=="REPETIR CARRERA [R]","Result buttons have gamepad focus");CaptureResult();
        GameObject.Find("SEGUIR EXPLORANDO").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();for(int i=0;i<5;i++)yield return null;
        Check(course.Mode==NeonDistrictMode.Exploration&&course.CurrentZone==5&&course.Elapsed==0&&player.enabled,"Results button resumes exploration on the terrace");
        route=Steps(Drive(24,-83),Drive(-35,-83),Drive(-35,-35),Drive(-24,-35),Drive(-24,-57),Drive(-24,-96),Drive(-58,-96),Drive(-58,-86));
        while(route.MoveNext())yield return route.Current;
        Check(course.CurrentZone==0&&course.Elapsed==0,"Terrace-to-market loop traversable with real controller and zero exploration timer");
        course.BeginRace();Check(Mathf.Approximately(course.BestTime,lap),"Persisted best reloads for repeat races");
        for(int i=0;i<190;i++)yield return null;
                Place(course.gates[0].volume.transform.position-Vector3.up*2.5f);
        for(int i=0;i<5;i++)yield return null;
        Check(course.NextCheckpoint==1,"First checkpoint records a new recovery position");
        float before=course.Elapsed;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));yield return null;yield return null;Keys();for(int i=0;i<10;i++)yield return null;
        Check(course.Recoveries==1&&course.Elapsed>before&&Vector3.Distance(player.transform.position,course.gates[0].recoverySpawn.position)<.5f,"R recovers without resetting time");
        player.CC.enabled=false;player.transform.position=Vector3.down*10;player.CC.enabled=true;for(int i=0;i<10;i++)yield return null;
        Check(course.Recoveries==2&&player.Grounded,"Falling recovers to last race checkpoint");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.T));yield return null;yield return null;Keys();for(int i=0;i<5;i++)yield return null;
        Check(course.Mode==NeonDistrictMode.Exploration&&course.AtTerminal&&course.Elapsed==0,"T cancels to exploration at the market");
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.North));InputSystem.Update();yield return null;
        InputSystem.QueueStateEvent(gamepad,new GamepadState());InputSystem.Update();
        Check(course.Mode==NeonDistrictMode.Countdown,"Existing gamepad Interact starts the countdown");course.CancelRace();
        var shortcuts=Shortcuts();while(shortcuts.MoveNext())yield return shortcuts.Current;
    }
    IEnumerator Shortcuts()
    {
        course.CancelRace();Keys();for(int i=0;i<10;i++)yield return null;
        Place(new Vector3(56.7f,13.2f,82),180);var step=Drive(56.7f,76.5f);while(step.MoveNext())yield return step.Current;
        Keys(Key.W,Key.Space);bool wall=false;
        for(int i=0;i<170;i++)
        {
            if(i%15==0)report.Add("WALL "+i+" "+player.transform.position+" "+player.GetCurrentState()?.GetType().Name+" grounded="+player.Grounded+" wall="+player.CheckWallRight(out _)+" input="+player.Controls.MovementValue);
            player.transform.rotation=Quaternion.Euler(0,180,0);wall|=player.GetCurrentState() is PlayerWallRunState;
            if(player.transform.position.z<64)break;yield return null;
        }
        Check(wall&&player.transform.position.z<64&&player.transform.position.y>10,"Optional wall-run reaches the far roof (entered="+wall+")");
        Keys();Place(new Vector3(24,11.8f,12),180);Vector3 anchor=GameObject.Find("Neon grapple anchor").transform.position;
        for(int i=0;i<8;i++)
        {
            Vector3 aim=anchor-Camera.main.transform.position;player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim,Vector3.up));
            player.GetComponentInChildren<CinemachinePanTilt>().TiltAxis.Value=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;yield return null;
        }
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));InputSystem.Update();bool grapple=false;
        for(int i=0;i<220;i++)
        { grapple|=player.GetCurrentState() is PlayerGrappleState;if(grapple&&player.transform.position.z< -7&&player.Grounded)break;yield return null; }
        InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();
        Check(grapple&&player.transform.position.z< -6&&player.transform.position.y>11,"Optional grapple reaches Spectre landing");
        var ramps=new[]{(new Vector3(-32.8f,0,30),new Vector3(-58,8,30)),(new Vector3(-25,0,68),new Vector3(-59,10.6f,68)),
            (new Vector3(87,0,52),new Vector3(61,11.8f,52)),(new Vector3(87,0,-32),new Vector3(61,12.3f,-32)),(new Vector3(87,0,-73),new Vector3(59,12.3f,-73))};
        foreach(var pair in ramps)
        {
            Place(pair.Item1);step=Drive(pair.Item2.x,pair.Item2.z);while(step.MoveNext())yield return step.Current;
            Check(player.transform.position.y>pair.Item2.y+.7f,"Public street ramp reaches "+pair.Item2);
            step=Drive(pair.Item1.x,pair.Item1.z);while(step.MoveNext())yield return step.Current;
            Check(player.transform.position.y<3,"Public ramp returns to street");
        }
        // Visit all six districts continuously in exploration, using both directions of every access.
        course.CancelRace();
        var tour=Steps(Drive(-61.5f,-86),Drive(-61.5f,-60),Drive(0,-60),Drive(0,34),Drive(-32.8f,34),Drive(-32.8f,30),Drive(-58,30),Drive(-32.8f,30),
            Drive(-32.8f,38),Drive(-25,38),Drive(-25,68),Drive(-59,68),Drive(-25,68),Drive(-25,34),
            Drive(0,34),Drive(87,34),Drive(87,52),Drive(61,52),Drive(87,52),
            Drive(87,-32),Drive(61,-32),Drive(87,-32),Drive(87,-73),Drive(59,-73),
            Drive(59,-83),Drive(45,-83),Drive(31,-83),Drive(24,-83),Drive(-35,-83),Drive(-35,-35),Drive(-24,-35),
            Drive(-24,-57),Drive(-24,-96),Drive(-58,-96),Drive(-58,-86));
        var visited=new HashSet<int>();
        while(tour.MoveNext()){visited.Add(course.CurrentZone);yield return tour.Current;}
        Check(visited.Count==6&&course.CurrentZone==0&&course.Elapsed==0,"Continuous exploration tour visits all six blocks and returns to market (visited "+string.Join(",",visited)+")");
        Keys();Place(new Vector3(-24,22,-18));
        for(int i=0;i<180;i++)yield return null;
        Check(Vector3.Distance(player.transform.position,course.zones[1].safeSpawn.position)<1&&course.Recoveries==0&&course.Elapsed==0,"High exploration fall uses the district safe point without a timer");
        course.CancelRace();var perf=Benchmark();while(perf.MoveNext())yield return perf.Current;
    }
    static void CaptureResult()
    {
        var canvas=GameObject.Find("Distrito Neon / Results").GetComponent<Canvas>();var camera=Camera.main;
        var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);var old=camera.targetTexture;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;camera.targetTexture=rt;
        Canvas.ForceUpdateCanvases();camera.Render();camera.Render();camera.Render();
        var prev=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("Logs/NeonPreviews/08_Results.png",image.EncodeToPNG());
        RenderTexture.active=prev;camera.targetTexture=old;canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    IEnumerator Benchmark()
    {
        Keys();Time.captureDeltaTime=0;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        var camera=Camera.main;var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
        camera.targetTexture=rt;camera.enabled=false;
        report.Add("PERF "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.processorType);
        report.Add("PERF Editor 1920x1080 offscreen render plus synchronous GPU readback; no frame cap; includes editor overhead.");
        foreach(Vector3 p in new[]{new Vector3(-58,0,-86),new Vector3(24,11.8f,20),new Vector3(24,13.8f,-71)})
        {
            Place(p,p.x>0?180:0);
            for(int i=0;i<60;i++){RenderAndSync(camera,rt,pixel);yield return null;}
            var samples=new List<double>();double prev=Time.realtimeSinceStartupAsDouble;
            for(int i=0;i<180;i++)
            {
                RenderAndSync(camera,rt,pixel);yield return null;
                double now=Time.realtimeSinceStartupAsDouble;samples.Add((now-prev)*1000);prev=now;
            }
            samples.Sort();double sum=0;foreach(double n in samples)sum+=n;
            report.Add("PERF "+p+" average "+(1000/(sum/samples.Count)).ToString("F1")+" fps, p50 "+samples[90].ToString("F2")+"ms, p95 "+samples[171].ToString("F2")+"ms");
            Check(rt.IsCreated()&&rt.width==1920&&rt.height==1080,"1080p camera target rendered and read back at "+p);
        }
        camera.targetTexture=null;camera.enabled=true;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixel);
    }
    static void RenderAndSync(Camera camera,RenderTexture target,Texture2D pixel)
    {
        camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
        pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);pixel.Apply(false);RenderTexture.active=old;
    }
}