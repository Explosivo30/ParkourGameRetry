using UnityEngine;

public enum ParkourActionType
{
    None,
    Dash,
    Slide,
    Vault,
    WallRun,
    LandingRoll,
    LedgeGrab,
    Grapple,
    Zipline
}

public enum ParkourStyleRank
{
    D,
    C,
    B,
    A,
    S,
    SS
}

[DisallowMultipleComponent]
public class ParkourStyleHUD : MonoBehaviour
{
    private PlayerStateMachine player;
    private GUIStyle rankStyle;
    private GUIStyle infoStyle;

    public void Bind(PlayerStateMachine playerStateMachine)
    {
        player = playerStateMachine;
    }

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerStateMachine>();
    }

    private void OnGUI()
    {
        if (player == null) return;
        EnsureStyles();

        const float width = 220f;
        const float height = 112f;
        Rect panel = new Rect(Screen.width - width - 24f, 24f, width, height);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.025f, 0.04f, 0.82f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        Color rankColor = GetRankColor(player.StyleRank);
        GUI.color = rankColor;
        GUI.Label(new Rect(panel.x + 12f, panel.y + 4f, 70f, 56f), player.StyleRank.ToString(), rankStyle);

        GUI.color = Color.white;
        Vector3 verticalVelocity = player.Grounded
            ? Vector3.zero
            : player.GetVerticalVelocity();
        float totalSpeed = (player.CurrentHorizontalVelocity + verticalVelocity).magnitude;
        float fallSpeed = player.Grounded ? 0f : Mathf.Max(0f, player.GetVerticalSpeed());

        string info = "FLOW " + player.StyleScore.ToString("0");
        info += System.Environment.NewLine;
        info += totalSpeed.ToString("0.0") + " m/s";
        if (fallSpeed > 0.1f)
        {
            info += System.Environment.NewLine;
            info += "FALL " + fallSpeed.ToString("0.0");
            if (fallSpeed >= player.RollMinFallSpeed && player.LandingRollInputBuffered)
                info += "  ROLL READY";
        }
        GUI.Label(new Rect(panel.x + 82f, panel.y + 10f, 130f, 64f), info, infoStyle);

        Rect barBackground = new Rect(panel.x + 12f, panel.y + 89f, panel.width - 24f, 9f);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.DrawTexture(barBackground, Texture2D.whiteTexture);

        GUI.color = rankColor;
        Rect barFill = barBackground;
        barFill.width *= player.StyleRankProgress;
        GUI.DrawTexture(barFill, Texture2D.whiteTexture);

        DrawGrappleCrosshair();

        GUI.color = previousColor;
    }

    private void DrawGrappleCrosshair()
    {
        Color crosshairColor = player.GrappleActive
            ? new Color(0.15f, 1f, 0.85f, 1f)
            : player.GrappleTargetAvailable
                ? new Color(0.35f, 1f, 0.4f, 0.95f)
                : new Color(1f, 1f, 1f, 0.45f);

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        GUI.color = crosshairColor;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 11f, 2f, 7f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 1f, centerY + 4f, 2f, 7f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 11f, centerY - 1f, 7f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX + 4f, centerY - 1f, 7f, 2f), Texture2D.whiteTexture);
    }

    private void EnsureStyles()
    {
        if (rankStyle != null) return;

        rankStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold
        };

        infoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
    }

    private static Color GetRankColor(ParkourStyleRank rank)
    {
        return rank switch
        {
            ParkourStyleRank.C => new Color(0.35f, 0.8f, 1f),
            ParkourStyleRank.B => new Color(0.35f, 1f, 0.55f),
            ParkourStyleRank.A => new Color(1f, 0.85f, 0.25f),
            ParkourStyleRank.S => new Color(1f, 0.35f, 0.2f),
            ParkourStyleRank.SS => new Color(0.9f, 0.25f, 1f),
            _ => new Color(0.82f, 0.82f, 0.82f)
        };
    }
}
