using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum NeonDistrictMode { Exploration, Countdown, Racing, Results }

/// <summary>Free exploration and a voluntary race; isolated from the existing circuits.</summary>
public class NeonDistrictCourse : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        public string name;
        public string hint;
        public BoxCollider area;
        public Transform safeSpawn;
    }
    [Serializable]
    public class Gate
    {
        public string name;
        public BoxCollider volume;
        public Transform recoverySpawn;
        public GameObject marker;
    }
    public PlayerStateMachine player;
    public Transform terminal;
    public Transform raceStart;
    public Zone[] zones;
    public Gate[] gates;
    public Vector2 districtSize = new Vector2(180,210);
    public string recordKey = "DistritoNeon.Race.v1.Best";
    public NeonDistrictMode Mode { get; private set; } = NeonDistrictMode.Exploration;
    public int NextCheckpoint { get; private set; }
    public int CurrentZone { get; private set; }
    public int Recoveries { get; private set; }
    public float BestTime { get; private set; }
    public ParkourStyleRank PeakRank { get; private set; }
    public bool NewRecord { get; private set; }
    public float Elapsed => Mode == NeonDistrictMode.Racing ? (float)(Time.timeAsDouble - startedAt)
        : Mode == NeonDistrictMode.Results ? resultTime : 0f;
    public float CountdownRemaining => Mode == NeonDistrictMode.Countdown ? Mathf.Max(0,(float)(countdownUntil-Time.timeAsDouble)) : 0f;
    public bool AtTerminal => player != null && terminal != null && Vector3.Distance(player.transform.position,terminal.position)<3.5f;
    double startedAt, countdownUntil;
    float resultTime, messageUntil;
    string message;
    bool ownsFreeze, reachedRoofs, trackingFall;
    float explorationFallPeak;
    CursorLockMode previousCursor;
    bool previousVisible;
    Canvas resultCanvas;
    GUIStyle small, body, heading, countdownStyle;
    readonly Color mint = new Color(.35f,1f,.79f);

    void Start()
    {
        if(player==null || terminal==null || raceStart==null || zones==null || zones.Length!=6 || gates==null || gates.Length!=6)
        { Debug.LogError("Distrito Neon needs a player, terminal, start, six zones and six gates.",this); enabled=false; return; }
        previousCursor=Cursor.lockState; previousVisible=Cursor.visible;
        BestTime=PlayerPrefs.GetFloat(recordKey,0f);
        player.Controls.OnInteractPressed+=Interact;
        UpdateMarkers(); LockCursor();
    }
    void Interact() { if(Mode==NeonDistrictMode.Exploration && AtTerminal) BeginRace(); }
    void Update()
    {
        var keys=Keyboard.current;
        if(keys!=null)
        {
            if(keys.tKey.wasPressedThisFrame) CancelRace();
            else if(keys.rKey.wasPressedThisFrame) { if(Mode==NeonDistrictMode.Results) BeginRace(); else Recover(); }
            if(keys.escapeKey.wasPressedThisFrame) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }
        if(Mode!=NeonDistrictMode.Results && Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
        if(Mode==NeonDistrictMode.Countdown)
        {
            if(Time.timeAsDouble>=countdownUntil) { startedAt=Time.timeAsDouble; Mode=NeonDistrictMode.Racing; Unfreeze(); UpdateMarkers(); }
            return;
        }
        if(Mode==NeonDistrictMode.Results) return;
                Vector3 p=player.transform.position;
        if(Mode==NeonDistrictMode.Exploration)
        {
            if(player.Grounded) { trackingFall=false; explorationFallPeak=p.y; }
            else
            {
                if(!trackingFall) { trackingFall=true; explorationFallPeak=p.y; }
                explorationFallPeak=Mathf.Max(explorationFallPeak,p.y);
                if(explorationFallPeak-p.y>6f) { Recover(); return; }
            }
        }
        if(Mode==NeonDistrictMode.Racing && p.y>8) reachedRoofs=true;
        if(Mode==NeonDistrictMode.Racing && reachedRoofs && p.y<4) { Recover(); return; }
        if(p.y < -6f || Mathf.Abs(p.x)>districtSize.x*.5f+3 || Mathf.Abs(p.z)>districtSize.y*.5f+3) { Recover(); return; }
        for(int i=0;i<zones.Length;i++) if(Inside(zones[i].area,p)) { CurrentZone=i; break; }
        if(Mode!=NeonDistrictMode.Racing) return;
        if(player.StyleRank>PeakRank) PeakRank=player.StyleRank;
        if(NextCheckpoint<gates.Length && Inside(gates[NextCheckpoint].volume,p))
        {
            message=gates[NextCheckpoint].name+"  /  "+FormatTime(Elapsed);
            messageUntil=Time.unscaledTime+2.5f;
            NextCheckpoint++;
            if(NextCheckpoint==gates.Length) CompleteRace();
            UpdateMarkers();
        }
    }
    public bool BeginRace()
    {
        if(Mode!=NeonDistrictMode.Exploration && Mode!=NeonDistrictMode.Results) return false;
        ClearResults(); BestTime=PlayerPrefs.GetFloat(recordKey,0f);
        reachedRoofs=false; NextCheckpoint=0; Recoveries=0; PeakRank=ParkourStyleRank.D; NewRecord=false; resultTime=0f; CurrentZone=0;
        player.ResetForCircuit(raceStart);
        Mode=NeonDistrictMode.Countdown; countdownUntil=Time.timeAsDouble+3;
        Freeze(); LockCursor(); UpdateMarkers(); return true;
    }
    void CompleteRace()
    {
        resultTime=Elapsed; Mode=NeonDistrictMode.Results;
        NewRecord=BestTime<=0 || resultTime<BestTime;
        if(NewRecord) { BestTime=resultTime; PlayerPrefs.SetFloat(recordKey,BestTime); PlayerPrefs.Save(); }
        Freeze(); Cursor.lockState=CursorLockMode.None; Cursor.visible=true; ShowResults();
    }
    public void Recover()
    {
        if(Mode==NeonDistrictMode.Countdown || Mode==NeonDistrictMode.Results) return;
        Transform spawn=zones[CurrentZone].safeSpawn;
        if(Mode==NeonDistrictMode.Racing) { Recoveries++; spawn=NextCheckpoint==0?raceStart:gates[NextCheckpoint-1].recoverySpawn; }
        player.ResetForCircuit(spawn);
        reachedRoofs=NextCheckpoint>0; trackingFall=false;
        message=Mode==NeonDistrictMode.Racing?"CHECKPOINT / El tiempo sigue corriendo":"DE VUELTA EN "+zones[CurrentZone].name;
        messageUntil=Time.unscaledTime+2.5f; LockCursor();
    }
    public void CancelRace()
    {
        EnterExploration(zones[0].safeSpawn,0);
    }
    public void ContinueExploring()
    {
        if(Mode!=NeonDistrictMode.Results) return;
        EnterExploration(zones[5].safeSpawn,5);
    }
    void EnterExploration(Transform spawn,int zone)
    {
        ClearResults(); Mode=NeonDistrictMode.Exploration;
        NextCheckpoint=0; Recoveries=0; resultTime=0; messageUntil=0; CurrentZone=zone;
        ownsFreeze=false; trackingFall=false; player.ResetForCircuit(spawn); UpdateMarkers(); LockCursor();
    }
    void Freeze() { ownsFreeze=true; player.Controls.enabled=false; player.enabled=false; }
    void Unfreeze() { ownsFreeze=false; player.enabled=true; player.Controls.enabled=true; }
    void UpdateMarkers()
    {
        for(int i=0;i<gates.Length;i++) if(gates[i].marker!=null)
            gates[i].marker.SetActive((Mode==NeonDistrictMode.Racing || Mode==NeonDistrictMode.Countdown) && i==NextCheckpoint);
    }
    static bool Inside(BoxCollider volume,Vector3 point)
    {
        Vector3 p=volume.transform.InverseTransformPoint(point)-volume.center, h=volume.size*.5f;
        return Mathf.Abs(p.x)<=h.x && Mathf.Abs(p.y)<=h.y && Mathf.Abs(p.z)<=h.z;
    }
    void LockCursor() { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
    void ClearResults() { if(resultCanvas!=null) { resultCanvas.gameObject.SetActive(false); Destroy(resultCanvas.gameObject); resultCanvas=null; } }
    void OnDisable()
    {
        if(ownsFreeze && player!=null) Unfreeze();
        Cursor.lockState=previousCursor; Cursor.visible=previousVisible;
    }
    void OnDestroy() { if(player!=null && player.Controls!=null) player.Controls.OnInteractPressed-=Interact; }
    public static string FormatTime(float t)=>$"{Mathf.FloorToInt(t/60):00}:{t%60:00.00}";

    void ShowResults()
    {
        var go=new GameObject("Distrito Neon / Results",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.transform.SetParent(transform,false);
        resultCanvas=go.GetComponent<Canvas>(); resultCanvas.renderMode=RenderMode.ScreenSpaceOverlay; resultCanvas.sortingOrder=100;
        var scale=go.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1440,900); scale.matchWidthOrHeight=.5f;
        if(EventSystem.current==null)
        {
            var events=new GameObject("Neon UI input",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(transform,false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var panel=new GameObject("Finish card",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(go.transform,false);
        var r=panel.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=new Vector2(620,470);
        panel.GetComponent<Image>().color=new Color(.012f,.025f,.043f,.98f);
        RectTransform stripe=UiRect("Neon stripe",r,new Rect(0,0,620,5)); stripe.gameObject.AddComponent<Image>().color=mint;
        Label(r,"ENCONTRASTE TU LINEA",new Rect(36,28,548,38),28,mint,true);
        Label(r,"DISTRITO NEON / LA TERRAZA COMUN",new Rect(36,75,548,28),16,Color.white);
        Label(r,FormatTime(resultTime),new Rect(24,110,572,92),68,Color.white,true,TextAnchor.MiddleCenter);
        Label(r,NewRecord?"NUEVA MEJOR MARCA":"MEJOR MARCA  "+FormatTime(BestTime),new Rect(36,220,548,35),25,mint,true);
        Label(r,$"Recuperaciones: {Recoveries}       Estilo maximo: {PeakRank}",new Rect(36,269,548,30),18,Color.white);
        Label(r,"Abajo nos separan. Arriba encontramos nuestro camino.",new Rect(36,310,548,30),15,new Color(.7f,.78f,.82f));
        var repeat=Button(r,"REPETIR CARRERA [R]",new Rect(36,373,548,38),()=>BeginRace());
        Button(r,"SEGUIR EXPLORANDO",new Rect(36,420,548,34),ContinueExploring);
        EventSystem.current.SetSelectedGameObject(repeat.gameObject);
    }
    UnityEngine.UI.Button Button(Transform parent,string name,Rect rect,UnityEngine.Events.UnityAction action)
    {
        var rt=UiRect(name,parent,rect); var fill=rt.gameObject.AddComponent<Image>(); fill.color=mint;
        var button=rt.gameObject.AddComponent<Button>(); button.targetGraphic=fill; button.onClick.AddListener(action);
        var colors=button.colors;colors.selectedColor=new Color(1,.82f,.47f);button.colors=colors;
        Label(rt,name,new Rect(0,0,rect.width,rect.height),17,new Color(.015f,.04f,.05f),true,TextAnchor.MiddleCenter);
        return button;
    }
    static RectTransform UiRect(string name,Transform parent,Rect rect)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(rect.x,-rect.y); rt.sizeDelta=rect.size; return rt;
    }
    static void Label(Transform parent,string text,Rect rect,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.UpperLeft)
    {
        var label=UiRect(text,parent,rect).gameObject.AddComponent<Text>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text=text; label.fontSize=size; label.color=color; label.alignment=alignment; label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal; label.raycastTarget=false;
    }
    void Styles()
    {
        if(small!=null)return;
        small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true}; small.normal.textColor=new Color(.74f,.82f,.87f);
        body=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};
        heading=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold}; heading.normal.textColor=mint;
        countdownStyle=new GUIStyle(heading){fontSize=90,alignment=TextAnchor.MiddleCenter};
    }
    static void Panel(Rect r) { GUI.color=new Color(.01f,.025f,.04f,.9f); GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white; }
    void OnGUI()
    {
        if(player==null || zones==null || zones.Length==0) return;
        Styles(); var matrix=GUI.matrix; var color=GUI.color;
        float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f); GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=Screen.width/scale,height=Screen.height/scale;
        Panel(new Rect(24,24,330,107)); GUI.Label(new Rect(42,32,300,34),"DISTRITO NEON",heading);
        GUI.Label(new Rect(42,75,300,32),Mode==NeonDistrictMode.Exploration?"EXPLORACION LIBRE":Mode==NeonDistrictMode.Countdown?"PREPARATE":FormatTime(Elapsed),heading);
        if(Mode==NeonDistrictMode.Racing)
        {
            GUI.Label(new Rect(width*.5f-230,28,460,34),"SIGUIENTE: "+gates[NextCheckpoint].name,body);
            GUI.Label(new Rect(42,107,300,24),$"CHECKPOINTS {Mathf.Min(NextCheckpoint,5)} / 5",small);
        }
        Panel(new Rect(24,height-122,600,98));
        GUI.Label(new Rect(40,height-116,564,28),zones[CurrentZone].name,body);
        GUI.Label(new Rect(40,height-83,564,35),zones[CurrentZone].hint,small);
        GUI.Label(new Rect(40,height-47,564,24),"R recuperar     T volver al mercado     Esc cursor",small);
        if(Mode==NeonDistrictMode.Exploration && AtTerminal)
        {
            Panel(new Rect(width*.5f-250,height*.69f,500,88));
            GUI.Label(new Rect(width*.5f-230,height*.69f+10,460,32),"E / INTERACTUAR : INICIAR CARRERA",body);
            GUI.Label(new Rect(width*.5f-230,height*.69f+46,460,28),BestTime>0?"MEJOR MARCA  "+FormatTime(BestTime):"Explora primero o encuentra tu mejor tiempo.",small);
        }
        if(Mode==NeonDistrictMode.Countdown)
            GUI.Label(new Rect(width*.5f-140,height*.38f,280,140),Mathf.CeilToInt(CountdownRemaining).ToString(),countdownStyle);
        else if(Mode!=NeonDistrictMode.Results && Time.unscaledTime<messageUntil)
            GUI.Label(new Rect(width*.5f-260,height*.22f,520,50),message,body);
        GUI.matrix=matrix; GUI.color=color;
    }
}