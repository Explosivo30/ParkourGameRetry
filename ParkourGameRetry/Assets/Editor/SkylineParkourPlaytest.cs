using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Comprueba el circuito guardado con el jugador real en Play Mode, en una ejecución batch.</summary>
public class SkylineParkourPlaytest
{
    const string Pending = "SkylineParkourPlaytest.Pending";
    readonly List<string> report = new List<string>();
    PlayerStateMachine player;
    SkylineParkourCourse course;
    CheckpointManager checkpoints;
    Keyboard keyboard;
    IEnumerator routine;
    int lastFrame = -1;
    double deadline;

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(SkylineParkourSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true);
        Install();
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Install()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    static void OnPlayMode(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false)) return;
        SessionState.SetBool(Pending,false);
        EditorApplication.delayCall += () => new SkylineParkourPlaytest().Start();
    }
    void Start()
    {
        Application.runInBackground=true;
        Time.captureDeltaTime=1f/60f;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>();
        player=UnityEngine.Object.FindFirstObjectByType<PlayerStateMachine>();
        course=UnityEngine.Object.FindFirstObjectByType<SkylineParkourCourse>();
        checkpoints=UnityEngine.Object.FindFirstObjectByType<CheckpointManager>();
        routine=Verify();
        deadline=EditorApplication.timeSinceStartup+120;
        EditorApplication.update+=Tick;
        Debug.Log("SKYLINE PLAYTEST STARTED" );
    }
    void Tick()
    {
        if(routine==null||lastFrame==Time.frameCount) return;
        lastFrame=Time.frameCount;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Playtest exceeded its time limit.");
            if(!routine.MoveNext()) Finish(0);
        }
        catch(Exception ex)
        {
            report.Add("FAIL "+ex);
            Debug.LogException(ex);
            Finish(1);
        }
    }
    void Finish(int code)
    {
        EditorApplication.update-=Tick;
        routine=null;
        File.WriteAllLines("Logs/SkylinePlaytest.txt",report);
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        Debug.Log("SKYLINE PLAYTEST: "+(code==0?"PASSED":"FAILED"));
        EditorApplication.Exit(code);
    }
    void Keys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        InputSystem.Update();
    }
    void Assert(bool value,string description)
    {
        if(!value) throw new Exception(description+" Player: "+player.transform.position+" State: "+player.GetCurrentState()?.GetType().Name);
        report.Add("PASS "+description);
        Debug.Log("SKYLINE PASS: "+description);
    }
    void ResetAt(int index)
    {
        Keys();
        checkpoints.ActivateCheckpoint(checkpoints.allCheckpoints[index]);
        checkpoints.RespawnPlayer(player);
    }
    IEnumerator Verify()
    {
        for(int i=0;i<10;i++) yield return null;
        Assert(player.Grounded,"Spawn grounded; player and input initialize");
        Assert(Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,player.transform.position)<2,"Camera follows player");
        Keys(Key.W);
        for(int i=0;i<35;i++) yield return null;
        Assert(player.transform.position.z>1,"W moves forward in the saved scene");
        float initialY=player.transform.position.y, highest=initialY;
        Keys(Key.W,Key.Space);
        for(int i=0;i<30;i++){ highest=Mathf.Max(highest,player.transform.position.y); yield return null; }
        Assert(highest>initialY+1,"Space produces an actual jump");
        ResetAt(1);
        for(int i=0;i<8;i++) yield return null;
        Keys(Key.W);
        for(int i=0;i<180&&player.transform.position.x<10.5f;i++) yield return null;
        Keys(Key.W,Key.Space);
        bool traversed=false;
        for(int i=0;i<150;i++)
        {
            traversed |= player.GetCurrentState() is PlayerWallClimbState || player.GetCurrentState() is PlayerLedgeGrabState;
            if(player.transform.position.x>13.1f&&player.transform.position.y>4.7f) break;
            yield return null;
        }
        Assert(traversed&&player.transform.position.x>13.1f&&player.transform.position.y>4.7f,"Holding Space climbs the first 2.8 m terrace");
        ResetAt(2);
        for(int i=0;i<8;i++) yield return null;
        Keys(Key.W);
        for(int i=0;i<130&&player.transform.position.z>43.4f;i++) yield return null;
        Keys(Key.W,Key.LeftCtrl);
        bool slid=false;
        for(int i=0;i<100;i++)
        {
            slid |= player.GetCurrentState() is PlayerSlideState;
            if(player.transform.position.z<37.5f) break;
            yield return null;
        }
        Assert(slid&&player.transform.position.z<37.5f,"Slide crosses the low tunnel");
        ResetAt(3);
        for(int i=0;i<8;i++) yield return null;
        // Colocación en el carril lateral; el cruce se realiza mediante input y física reales.
        var wallSection=GameObject.Find("04 / PARED").transform;
        player.CC.enabled=false;
        player.transform.position=wallSection.TransformPoint(new Vector3(2.3f,1.1f,4));
        player.CC.enabled=true;
        Physics.SyncTransforms();
        Keys(Key.W);
        for(int i=0;i<120&&wallSection.InverseTransformPoint(player.transform.position).z<7.6f;i++) yield return null;
        Keys(Key.W,Key.Space);
        bool ranOnWall=false;
        for(int i=0;i<160;i++)
        {
            ranOnWall |= player.GetCurrentState() is PlayerWallRunState;
            if(wallSection.InverseTransformPoint(player.transform.position).z>19) break;
            yield return null;
        }
        Assert(ranOnWall&&wallSection.InverseTransformPoint(player.transform.position).z>19,"Wall-run crosses to the lower landing");
        ResetAt(4);
        for(int i=0;i<8;i++) yield return null;
        Vector3 spawn=checkpoints.currentCheckpoint.spawnPoint.position;
        Keys();
        player.CC.enabled=false;
        player.transform.position=new Vector3(200,-15,200);
        player.CC.enabled=true;
        for(int i=0;i<8;i++) yield return null;
        Assert(Vector3.Distance(player.transform.position,spawn)<.5f,"Falling returns to the active checkpoint");
        player.CC.enabled=false;
        player.transform.position=course.finish.position;
        player.CC.enabled=true;
        for(int i=0;i<4;i++) yield return null;
        Assert(course.Finished,"Reaching the finish stops the timer");
        course.Retry();
        for(int i=0;i<5;i++) yield return null;
        Assert(!course.Finished&&checkpoints.currentCheckpoint.sectorIndex==0,"Retry after finish starts a new lap");
    }
}