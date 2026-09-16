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

/// <summary>Runs a complete input-driven lap and checks timing, recovery and both shortcuts.</summary>
public class DistritoLibrePlaytest
{
    const string Pending="DistritoLibrePlaytest.Pending";
    readonly List<string> report=new List<string>();
    PlayerStateMachine player;
    DistritoLibreCourse course;
    Keyboard keyboard;
    Mouse mouse;
    IEnumerator routine;
    int lastFrame=-1;
    double deadline;
    bool oldRecordExists;
    float oldRecord;
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(DistritoLibreSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true); Install(); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Install()
    {
        EditorApplication.playModeStateChanged-=OnPlayMode;
        EditorApplication.playModeStateChanged+=OnPlayMode;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false)) return;
        SessionState.SetBool(Pending,false);
        EditorApplication.delayCall+=()=>new DistritoLibrePlaytest().Start();
    }
    void Start()
    {
        Application.runInBackground=true; Time.captureDeltaTime=1f/60f;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
        player=UnityEngine.Object.FindFirstObjectByType<PlayerStateMachine>();
        course=UnityEngine.Object.FindFirstObjectByType<DistritoLibreCourse>();
        oldRecordExists=PlayerPrefs.HasKey(DistritoLibreCourse.RecordKey);
        oldRecord=PlayerPrefs.GetFloat(DistritoLibreCourse.RecordKey);
        course.saveBestTime=false;
        routine=Verify(); deadline=EditorApplication.timeSinceStartup+240;
        EditorApplication.update+=Tick;
    }
    void Tick()
    {
        if(routine==null || lastFrame==Time.frameCount) return;
        lastFrame=Time.frameCount;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Playtest time limit.");
            if(!routine.MoveNext()) Finish(0);
        }
        catch(Exception ex) { report.Add("FAIL "+ex+" Position "+player.transform.position+" State "+player.GetCurrentState()?.GetType().Name); Debug.LogException(ex); Finish(1); }
    }
    void Finish(int code)
    {
        EditorApplication.update-=Tick; routine=null;
        File.WriteAllLines("Logs/DistritoLibrePlaytest.txt",report);
        if(oldRecordExists) PlayerPrefs.SetFloat(DistritoLibreCourse.RecordKey,oldRecord); else PlayerPrefs.DeleteKey(DistritoLibreCourse.RecordKey);
        PlayerPrefs.Save();
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        if(mouse!=null) InputSystem.RemoveDevice(mouse);
        Debug.Log("DISTRITO LIBRE PLAYTEST: "+(code==0?"PASSED":"FAILED"));
        EditorApplication.Exit(code);
    }
    void Check(bool pass,string text)
    {
        if(!pass) throw new Exception(text);
        report.Add("PASS "+text); Debug.Log("DISTRITO PASS: "+text);
    }
    static void CaptureResult()
    {
        var canvas=GameObject.Find("Distrito Libre / Results").GetComponent<Canvas>();
        var camera=Camera.main;
        var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);
        var oldTarget=camera.targetTexture;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;
        canvas.worldCamera=camera; canvas.planeDistance=.5f;
        camera.targetTexture=target;
        Canvas.ForceUpdateCanvases(); camera.Render(); camera.Render(); camera.Render();
        var previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
        File.WriteAllBytes("Logs/DistritoLibrePreviews/Result.png",image.EncodeToPNG());
        RenderTexture.active=previous; camera.targetTexture=oldTarget;
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
    void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); InputSystem.Update(); }
    Transform Section(string name)=>GameObject.Find(name).transform;
    IEnumerator Drive(Transform section,float x,float z,Key extra=Key.None,int limit=420)
    {
        var target=section.TransformPoint(new Vector3(x,0,z));
        if(extra==Key.None) Keys(Key.W); else Keys(Key.W,extra);
        int recoveries=course.Recoveries;
        for(int i=0;i<limit;i++)
        {
            Vector3 delta=Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up);
            if(delta.magnitude<1.15f) yield break;
            player.transform.rotation=Quaternion.LookRotation(delta);
            if(course.Recoveries!=recoveries) throw new Exception("Fell during drive to "+section.name+" / "+x+", "+z);
            yield return null;
        }
        throw new Exception("Could not reach "+section.name+" / "+x+", "+z);
    }
    IEnumerator Perform(IEnumerator child)
    {
        while(child.MoveNext()) yield return child.Current;
    }
    IEnumerator Wait(int frames) { Keys(); for(int i=0;i<frames;i++) yield return null; }
    void Place(Transform section,Vector3 point)
    {
        var spawn=new GameObject("Temporary test spawn").transform;
        spawn.position=section.TransformPoint(point); spawn.rotation=section.rotation;
        player.ResetForCircuit(spawn); UnityEngine.Object.DestroyImmediate(spawn.gameObject);
    }
    IEnumerator Verify()
    {
        for(int i=0;i<12;i++) yield return null;
        Check(player.Grounded && !course.Started,"Grounded spawn; clock waits at zero");
        Check(Camera.main!=null && Vector3.Distance(Camera.main.transform.position,player.transform.position)<2,"First-person camera follows player");
        // Touching the finish out of order must not complete or start the race.
        Place(Section("06 / LA TERRAZA COMUN"),new Vector3(0,3.13f,64));
        for(int i=0;i<4;i++) yield return null;
        Check(!course.Started && !course.Finished && course.NextGate==0,"Finish rejects skipped checkpoints");
        course.RestartLap();
        for(int i=0;i<5;i++) yield return null;
        Transform s=Section("01 / BAJO LA RED");
        var route=new List<IEnumerator>
        {
            Drive(s,0,16),Drive(s,0,25,Key.Space),Drive(s,0,30),Drive(s,0,42,Key.Space),Drive(s,0,51),Drive(s,0,61,Key.Space)
        };
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Check(course.Started && course.Elapsed>3,"Crossing start begins the clock; first roof jumps traversed");
        s=Section("02 / RODANTES");
        route=new List<IEnumerator>{Drive(s,0,10),Drive(s,0,18,Key.LeftCtrl),Drive(s,0,20),Drive(s,0,30,Key.Space),Drive(s,0,39),Drive(s,0,49,Key.Space),Drive(s,0,69)};
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Check(course.NextGate==2,"Skate district, low duct and running jumps completed");
        s=Section("03 / CAPA");
        route=new List<IEnumerator>{Drive(s,0,13.5f),Drive(s,0,25,Key.Space),Drive(s,0,29.5f),Drive(s,0,42,Key.Space),Drive(s,0,57)};
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Check(course.NextGate==3 && player.transform.position.y>6,"Both 2.6 m mural terraces climbed");
        s=Section("04 / CANAL ABIERTO");
        route=new List<IEnumerator>{Drive(s,0,10),Drive(s,-11,13),Drive(s,-11,29),Drive(s,0,30),Drive(s,0,68)};
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Check(course.NextGate==4,"Safe bridge route crosses the wall-run gap");
        s=Section("05 / HORIZONTE");
        route=new List<IEnumerator>{Drive(s,-6,12),Drive(s,-6,20.5f),Drive(s,-6,29.5f,Key.Space),Drive(s,-6,29.8f),Drive(s,-6,44,Key.Space),Drive(s,0,56),Drive(s,0,69,Key.Space),Drive(s,0,73),Drive(s,0,85,Key.Space)};
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Check(course.NextGate==5,"Safe aerial stepping roofs need no grapple");
        s=Section("06 / LA TERRAZA COMUN");
        route=new List<IEnumerator>{Drive(s,0,18),Drive(s,0,31,Key.Space),Drive(s,0,59),Drive(s,0,63)};
        foreach(var step in route) { var e=Perform(step); while(e.MoveNext()) yield return e.Current; }
        Keys(); for(int i=0;i<6;i++) yield return null;
        Check(course.Finished && course.NextGate==7 && course.Recoveries==0,"Complete continuous lap reaches the finish with every checkpoint");
        report.Add("MEASURE automated main-route lap: "+DistritoLibreCourse.FormatTime(course.Elapsed));
        float finished=course.Elapsed;
        for(int i=0;i<25;i++) yield return null;
        Check(Mathf.Approximately(finished,course.Elapsed) && !player.enabled,"Result time is frozen and readable");
        CaptureResult();
        for(int i=0;i<8;i++) yield return null;
        GameObject.Find("Distrito Libre / Results").GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        for(int i=0;i<5;i++) yield return null;
        Check(!course.Started && course.Elapsed==0 && course.NextGate==0 && course.Recoveries==0 && player.StyleScore==0 && player.enabled,"New lap restores player, style, checkpoints and zero timer");
        var start=Perform(Drive(Section("01 / BAJO LA RED"),0,10)); while(start.MoveNext()) yield return start.Current;
        float beforeFall=course.Elapsed;
        Keys(); player.CC.enabled=false; player.transform.position=Vector3.down*15; player.CC.enabled=true;
        for(int i=0;i<10;i++) yield return null;
        Check(course.Recoveries==1 && player.Grounded && course.Elapsed>beforeFall,"Fall returns to checkpoint while time continues");
        s=Section("04 / CANAL ABIERTO"); Place(s,new Vector3(2.3f,1.13f,8));
        var approach=Perform(Drive(s,2.3f,13.5f)); while(approach.MoveNext()) yield return approach.Current;
        Keys(Key.W,Key.Space); bool wall=false;
        for(int i=0;i<150;i++)
        {
            player.transform.rotation=s.rotation;
            wall|=player.GetCurrentState() is PlayerWallRunState;
            if(s.InverseTransformPoint(player.transform.position).z>27) break;
            yield return null;
        }
        Check(wall && s.InverseTransformPoint(player.transform.position).z>27,"Optional wall-run shortcut reaches its landing");
        Keys(); s=Section("05 / HORIZONTE"); Place(s,new Vector3(0,1.13f,19));
        Vector3 anchor=GameObject.Find("Grapple shortcut anchor").transform.position;
        for(int i=0;i<5;i++)
        {
            Vector3 aim=anchor-Camera.main.transform.position;
            player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim,Vector3.up));
            player.GetComponentInChildren<CinemachinePanTilt>().TiltAxis.Value=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;
            yield return null;
        }
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right)); InputSystem.Update();
        bool grapple=false;
        for(int i=0;i<180;i++)
        {
            grapple|=player.GetCurrentState() is PlayerGrappleState;
            if(grapple && s.InverseTransformPoint(player.transform.position).z>37 && player.Grounded) break;
            yield return null;
        }
        InputSystem.QueueStateEvent(mouse,new MouseState()); InputSystem.Update();
        Check(grapple && s.InverseTransformPoint(player.transform.position).z>36,"Optional grapple reaches the far rooftop");
        Check(DistritoLibreCourse.FormatTime(65.25f)=="01:05.25" || DistritoLibreCourse.FormatTime(65.25f)=="01:05,25","Result formats minutes and hundredths");
    }
}