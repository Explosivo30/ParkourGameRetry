using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Batch Play Mode checks using the active controller and real capsule collisions.</summary>
public static class GroundTurningValidation
{
    const string Pending = "GroundTurningValidation.Pending";
    static readonly List<string> report = new List<string>();
    static int failures;

    public static void RunBatch()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
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
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        EditorApplication.delayCall += RunTests;
    }

    static void RunTests()
    {
        Run("90 degree turns substantially reduce drift at 30, 60 and 120 FPS", TestDrift);
        Run("Left and right corners retain support on a two metre platform", TestNarrowPlatform);
        Run("Reversals do not create a sideways arc", TestReverse);
        Run("Straight running, starting and released-input braking stay unchanged", TestStraight);
        Run("Airborne steering retains the original momentum", TestAir);
        Run("Repeated turns do not create extra speed", TestSpeedCap);
        Run("Turning is stable across frame rates", TestFrameRates);
        Run("Paused movement does not move or alter velocity", TestPaused);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/GroundTurningValidation.txt", report);
        Debug.Log($"GROUND TURNING: {(failures == 0 ? "PASSED" : "FAILED")}");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    static void Run(string name, Action test)
    {
        try { test(); report.Add("PASS " + name); }
        catch (Exception ex) { failures++; report.Add("FAIL " + name + ": " + ex); }
        Debug.Log(report[report.Count - 1]);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    sealed class Fixture : IDisposable
    {
        readonly GameObject floor;
        readonly GameObject root;
        public readonly PlayerStateMachine player;
        public readonly CharacterController cc;
        public readonly Vector3 origin = new Vector3(0f, 1.1f, 0f);

        public Fixture(float response = 24f, float width = 100f, bool grounded = true)
        {
            floor = new GameObject("Turning test platform");
            floor.transform.position = Vector3.down * .5f;
            floor.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, width);
            root = new GameObject("Turning test player");
            root.layer = 8;
            root.transform.position = grounded ? origin : origin + Vector3.up * 10f;
            cc = root.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = .5f;
            player = root.AddComponent<PlayerStateMachine>();
            player.enabled = false; // Drive only the actual locomotion method, with a controlled frame time.
            Set(player, "_groundMask", (LayerMask)1);
            Set(player, "_height", -.4f);
            Set(player, "_castLength", .32f);
            Set(player, "_groundTurnResponsiveness", response);
            Physics.SyncTransforms();
            player.GroundDetection();
            Require(player.Grounded == grounded, "Invalid ground fixture.");
        }

        public void Step(Vector3 wish, float dt)
        {
            player.GroundDetection();
            player.PlayerHorizontalMovement(wish, dt);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(floor);
        }
    }

    static Vector3 Corner(int fps, float response, float speed, out float drift)
    {
        using var f = new Fixture(response);
        f.player.CurrentHorizontalVelocity = Vector3.forward * speed;
        for (int i = 0; i < fps / 2; i++) f.Step(Vector3.right, 1f / fps);
        drift = f.player.transform.position.z - f.origin.z;
        return f.player.CurrentHorizontalVelocity;
    }

    static void TestDrift()
    {
        foreach (int fps in new[] { 30, 60, 120 })
        {
            Corner(fps, 0f, 12f, out float before);
            Vector3 velocity = Corner(fps, 24f, 12f, out float after);
            Require(after < .5f && after < before * .15f, $"{fps} FPS: drift {before:F3} -> {after:F3} m.");
            Require(Vector3.Angle(velocity, Vector3.right) < 1f, "Velocity did not align with the corner.");
            report.Add($"MEASURE {fps} FPS, starting at 12 m/s, 0.5 s after a 90 degree turn: drift {before:F3} -> {after:F3} m.");
        }
    }

    static void TestNarrowPlatform()
    {
        foreach (float side in new[] { -1f, 1f })
        {
            using var f = new Fixture(width: 2f);
            f.player.CurrentHorizontalVelocity = Vector3.forward * 12f;
            for (int i = 0; i < 60; i++)
            {
                f.Step(Vector3.right * side, 1f / 60f);
                f.cc.Move(Vector3.down * (2f / 60f));
                f.player.GroundDetection();
                Require(f.player.Grounded, "Corner carried the capsule off the platform.");
            }
            Require(Mathf.Abs(f.player.transform.position.x) > 5f, "Turn stalled forward progress.");
        }
    }

    static void TestReverse()
    {
        using var f = new Fixture();
        f.player.CurrentHorizontalVelocity = Vector3.forward * 10f;
        for (int i = 0; i < 30; i++) f.Step(Vector3.back, 1f / 60f);
        Require(f.player.CurrentHorizontalVelocity.z < -2f, "Reversal did not respond.");
        Require(Mathf.Abs(f.player.transform.position.x) < .001f, "Reversal introduced sideways motion.");
    }

    static Vector3 Simulate(float response, Vector3 initial, Vector3 wish, bool grounded)
    {
        using var f = new Fixture(response, grounded: grounded);
        f.player.CurrentHorizontalVelocity = initial;
        for (int i = 0; i < 30; i++) f.Step(wish, 1f / 60f);
        return f.player.CurrentHorizontalVelocity;
    }

    static void TestStraight()
    {
        foreach (float speed in new[] { 0f, 10f, 20f })
        foreach (Vector3 wish in new[] { Vector3.forward, Vector3.zero })
        {
            Vector3 initial = Vector3.forward * speed;
            Require(Vector3.Distance(Simulate(0f, initial, wish, true), Simulate(24f, initial, wish, true)) < .001f,
                "Ground grip changed straight-line acceleration, momentum retention or braking.");
        }
    }

    static void TestAir()
    {
        Vector3 initial = Vector3.forward * 12f;
        Vector3 original = Simulate(0f, initial, Vector3.right, false);
        Vector3 updated = Simulate(24f, initial, Vector3.right, false);
        Require(Vector3.Distance(original, updated) < .001f && updated.z > 10f, "Ground grip affected airborne momentum.");
    }

    static void TestSpeedCap()
    {
        using var f = new Fixture();
        f.player.CurrentHorizontalVelocity = Vector3.forward * 20f;
        for (int i = 0; i < 180; i++)
        {
            float previousSpeed = f.player.CurrentHorizontalVelocity.magnitude;
            f.Step(Quaternion.Euler(0f, i * 17f, 0f) * Vector3.forward, 1f / 60f);
            Require(f.player.CurrentHorizontalVelocity.magnitude <= Mathf.Max(previousSpeed, f.player.TargetVelocity) + .001f,
                "Turning manufactured extra speed.");
        }
    }

    static void TestFrameRates()
    {
        Vector3 slow = Corner(30, 24f, 12f, out float drift30);
        Vector3 fast = Corner(120, 24f, 12f, out float drift120);
        Require(Mathf.Abs(drift30 - drift120) < .15f && Vector3.Distance(slow, fast) < .75f,
            $"Frame-rate variance too large: {drift30:F3}/{drift120:F3} m, velocities {slow}/{fast}.");
    }

    static void TestPaused()
    {
        using var f = new Fixture();
        Vector3 initial = Vector3.forward * 12f;
        f.player.CurrentHorizontalVelocity = initial;
        f.Step(Vector3.right, 0f);
        Require(f.player.CurrentHorizontalVelocity == initial && f.player.transform.position == f.origin,
            "Zero time changed movement.");
    }
}
