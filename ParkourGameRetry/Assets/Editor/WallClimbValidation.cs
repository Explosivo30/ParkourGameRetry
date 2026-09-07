using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Pruebas con colisiones reales. Ejecutar con -executeMethod WallClimbValidation.RunBatch.</summary>
public static class WallClimbValidation
{
    static readonly List<string> results = new List<string>();
    static int failures;

    const string PendingKey = "WallClimbValidation.Pending";

    [InitializeOnLoadMethod]
    static void InstallPlayModeCallback()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void RunBatch()
    {
        SessionState.SetBool(PendingKey, true);
        InstallPlayModeCallback();
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        EditorApplication.delayCall += RunTests;
    }

    static void RunTests()
    {
        Run("Espacio real se conserva entre estados y registra la liberación", TestHeldInput);
        Run("Una pared frontal permite trepar al mantener espacio", TestFrontWall);
        Run("Sin espacio, alejándose o agachado no se inicia una trepada", TestIntent);
        Run("Una pared lateral no activa la trepada frontal", TestSideWall);
        Run("Los triggers no sirven como paredes", TestTrigger);
        Run("Soltar espacio interrumpe el ascenso", TestRelease);
        Run("El ascenso se agota y no se recarga al volver a pulsar", TestExhaustion);
        Run("Aterrizar permite volver a trepar", TestLandingReset);
        Run("El techo detiene el ascenso sin atravesarlo", TestCeiling);
        Run("Mantener espacio sube al borde sin segunda pulsación", TestMantle);
        Run("Se rechaza un borde sin espacio para la cabeza", TestBlockedLedge);
        Run("Un obstáculo ajeno a las capas trepables también bloquea", TestOtherLayerBlocker);
        Run("Un obstáculo que aparece durante el mantle interrumpe la subida", TestMovingBlocker);
        Run("Soltarse no vuelve a agarrar inmediatamente el mismo borde", TestDrop);
        Run("La altura de detección respeta el centro de la cápsula", TestOffsetCapsule);
        Run("La trepada tiene el mismo recorrido a 30, 60 y 120 fps", TestFrameRates);
        Run("La trepada enlaza con el borde y termina sobre la plataforma", TestClimbToLedge);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/WallClimbValidation.txt", results);
        Debug.Log($"WallClimbValidation: {results.Count - failures}/{results.Count} passed.");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    static void Run(string name, Action test)
    {
        try { test(); results.Add("PASS " + name); }
        catch (Exception ex) { failures++; results.Add("FAIL " + name + ": " + ex); }
        Debug.Log(results[results.Count - 1]);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Set(object obj, string name, object value)
    {
        obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    }

    static void Invoke(object obj, string method)
    {
        obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    }

    sealed class Fixture : IDisposable
    {
        public readonly GameObject player;
        public readonly PlayerStateMachine sm;
        public readonly InputReader input;
        public readonly CharacterController cc;
        readonly List<GameObject> objects = new List<GameObject>();
        public Fixture(float wallTop = 20f)
        {
            player = new GameObject("Climb test player");
            player.transform.position = new Vector3(3000f, 2f, 0f);
            player.layer = 8;
            cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            input = player.AddComponent<InputReader>();
            if (!Application.isPlaying) Invoke(input, "Awake");
            if (!Application.isPlaying) Invoke(input, "OnEnable");
            sm = player.AddComponent<PlayerStateMachine>();
            if (!Application.isPlaying) Invoke(sm, "Awake");
            Set(sm, "_groundMask", (LayerMask)1);
            Set(sm, "_vaultMask", (LayerMask)1);
            Set(sm, "_wallRunMask", (LayerMask)1);
            Set(sm, "_height", -0.4f);
            Set(sm, "_castLength", 0.25f);
            sm.AddState(new PlayerIdleState(sm));
            sm.AddState(new PlayerWallClimbState(sm));
            sm.AddState(new PlayerLedgeGrabState(sm));
            sm.SwitchState(typeof(PlayerIdleState));
            Held(true);
            Move(Vector2.up);
            if (wallTop > 0f) Box(new Vector3(0f, (wallTop - 2f) * 0.5f, 2.2f), new Vector3(5f, wallTop + 2f, 3f));
            Physics.SyncTransforms();
        }
        public GameObject Box(Vector3 position, Vector3 size, bool trigger = false, int layer = 0)
        {
            var go = new GameObject("Climb test obstacle");
            go.transform.position = position + Vector3.right * 3000f;
            go.layer = layer;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = trigger;
            objects.Add(go);
            Physics.SyncTransforms();
            return go;
        }
        public void Held(bool held) => Set(input, "<JumpHeld>k__BackingField", held);
        public void Move(Vector2 move) => Set(input, "<MovementValue>k__BackingField", move);
        public void Crouch(bool held) => Set(input, "<CrouchHeld>k__BackingField", held);
        public void Tick(float dt = 1f / 60f) => sm.GetCurrentState().Tick(dt);
        public void StartClimb()
        {
            Require(sm.CanWallClimb(), "Expected a climbable wall.");
            sm.SwitchState(typeof(PlayerWallClimbState));
        }
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(player);
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            Physics.SyncTransforms();
        }
    }

    static void TestHeldInput()
    {
        using var f = new Fixture();
        bool oldRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        var oldUpdateMode = InputSystem.settings.updateMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        var oldFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            f.Held(false);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Require(f.sm.JumpHeld && f.sm.JumpPressed, $"Space press was not received: held={f.sm.JumpHeld}, pressed={f.sm.JumpPressed}, key={keyboard.spaceKey.isPressed}, play={Application.isPlaying}.");
            f.sm.UnsubscribeInputJump();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Require(!f.sm.JumpHeld, "Space remains held after release in an unsubscribed state.");
            f.sm.SubscribeInputJump();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            f.sm.ResetVaultPhysics();
            Require(f.sm.JumpHeld, "Vault cleanup erased the physical held input.");
            Invoke(f.input, "OnDisable");
            Require(!f.sm.JumpHeld, "Disabling input must clear held state.");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = oldFocus;
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.updateMode = oldUpdateMode;
            Application.runInBackground = oldRunInBackground;
        }
    }

    static void TestFrontWall()
    {
        using var f = new Fixture();
        f.StartClimb();
        float before = f.player.transform.position.y;
        f.Tick();
        Require(f.player.transform.position.y > before, "Climb did not gain height.");
    }
    static void TestIntent()
    {
        using var f = new Fixture();
        f.Held(false);
        Require(!f.sm.CanWallClimb(), "Climbed without space.");
        f.Held(true); f.Move(Vector2.down);
        Require(!f.sm.CanWallClimb(), "Climbed while backing away.");
        f.Move(Vector2.up); f.Crouch(true);
        Require(!f.sm.CanWallClimb(), "Climbed while crouching.");
        f.Crouch(false); f.sm.SetVerticalVelocity(-10f);
        Require(!f.sm.CanWallClimb(), "Caught a wall during a fast fall.");
    }
    static void TestSideWall()
    {
        using var f = new Fixture();
        f.player.transform.rotation = Quaternion.Euler(0, 90, 0);
        Physics.SyncTransforms();
        Require(!f.sm.CanWallClimb(), "A side wall activated frontal climbing.");
    }
    static void TestTrigger()
    {
        using var f = new Fixture(0);
        f.Box(new Vector3(0, 5, 2.2f), new Vector3(5, 10, 3), true);
        Require(!f.sm.CanWallClimb(), "Trigger detected as wall.");
    }
    static void TestRelease()
    {
        using var f = new Fixture();
        f.StartClimb(); f.Tick(); f.Held(false); f.Tick();
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Release did not stop climbing.");
        Require(f.sm.GetVerticalVelocity().y <= 0f, "Release retained upward climb speed.");
    }
    static void TestExhaustion()
    {
        using var f = new Fixture();
        f.StartClimb();
        for (int i = 0; i < 100 && f.sm.GetCurrentState() is PlayerWallClimbState; i++) f.Tick();
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Climb did not expire.");
        Require(f.player.transform.position.y <= 2f + f.sm.WallClimbMaxRise + 0.01f, "Exceeded rise cap.");
        f.Held(false); f.Held(true);
        Set(f.sm, "_ledgeReattachTime", -1f);
        Require(!f.sm.CanWallClimb(), "Repeated input recharged climbing in midair.");
    }
    static void TestLandingReset()
    {
        using var f = new Fixture();
        f.sm.ConsumeWallClimb();
        f.Box(new Vector3(0, -0.5f, -2), new Vector3(8, 1, 8));
        f.cc.enabled = false;
        f.player.transform.position = new Vector3(3000, 1.1f, 0);
        f.cc.enabled = true;
        f.sm.ResetVerticalVelocity();
        Physics.SyncTransforms();
        f.sm.GroundDetection();
        Require(f.sm.Grounded, "Fixture did not detect ground.");
        f.sm.SetVerticalVelocity(5f);
        Require(f.sm.CanWallClimb(), "Landing failed to restore climbing.");
    }
    static void TestCeiling()
    {
        using var f = new Fixture();
        f.Box(new Vector3(0, 3.4f, 0), new Vector3(5, 0.2f, 5));
        f.StartClimb();
        for (int i = 0; i < 60 && f.sm.GetCurrentState() is PlayerWallClimbState; i++) f.Tick();
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Climbing continued against a ceiling.");
        Require(f.player.transform.position.y + 1f < 3.5f, "Capsule crossed the ceiling.");
    }
    static void TestMantle()
    {
        using var f = new Fixture(3.3f);
        f.sm.SetVerticalVelocity(5f);
        Require(f.sm.CanLedgeGrab(), "Held jump could not find reachable ledge while rising.");
        Vector3 target = f.sm.LedgeClimbTarget;
        f.sm.SwitchState(typeof(PlayerLedgeGrabState));
        Require(!f.sm.JumpPressed, "Entry did not consume the press.");
        for (int i = 0; i < 60 && f.sm.GetCurrentState() is PlayerLedgeGrabState; i++)
        {
            f.Tick();
            Require(f.cc.enabled, "Mantle disabled collision detection.");
        }
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Mantle did not finish.");
        Require(Vector3.Distance(f.player.transform.position, target) < 0.1f, "Mantle did not reach the platform.");
    }
    static void TestClimbToLedge()
    {
        using var f = new Fixture(5.3f);
        Require(!f.sm.CanLedgeGrab(), "High ledge should require climbing first.");
        f.StartClimb();
        bool usedLedge = false;
        for (int i = 0; i < 180 && !(f.sm.GetCurrentState() is PlayerIdleState); i++)
        {
            f.Tick();
            usedLedge |= f.sm.GetCurrentState() is PlayerLedgeGrabState;
        }
        Require(usedLedge, "Climb never transitioned into mantle.");
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Traversal did not complete.");
        Require(f.player.transform.position.y > 6.3f && f.player.transform.position.z > 1f,
            "Did not finish standing above the platform.");
    }

    static void TestBlockedLedge()
    {
        using var f = new Fixture(3.3f);
        f.Box(new Vector3(0, 5.2f, 2), new Vector3(5, 0.2f, 3));
        Require(!f.sm.CanLedgeGrab(), "Accepted ledge with insufficient headroom.");
    }
    static void TestOtherLayerBlocker()
    {
        using var f = new Fixture(3.3f);
        f.Box(new Vector3(0, 4.3f, 1.65f), new Vector3(2, 1, 1), false, 24);
        Require(!f.sm.CanLedgeGrab(), "Ignored an obstacle outside the climb mask.");
    }
    static void TestMovingBlocker()
    {
        using var f = new Fixture(3.3f);
        Require(f.sm.CanLedgeGrab(), "Missing ledge.");
        f.sm.SwitchState(typeof(PlayerLedgeGrabState));
        f.Box(new Vector3(0, 3.5f, 0), new Vector3(4, 0.2f, 1));
        for (int i = 0; i < 60 && f.sm.GetCurrentState() is PlayerLedgeGrabState; i++) f.Tick();
        Require(f.sm.GetCurrentState() is PlayerIdleState && f.cc.enabled, "Did not safely cancel blocked mantle.");
        Require(f.player.transform.position.z < 0.5f, "Passed through the new obstacle.");
    }
    static void TestDrop()
    {
        using var f = new Fixture(3.3f);
        f.Held(false);
        Require(f.sm.CanLedgeGrab(), "Missing passive ledge.");
        f.sm.SwitchState(typeof(PlayerLedgeGrabState));
        f.Crouch(true); f.Tick(); f.Crouch(false);
        Require(f.sm.GetCurrentState() is PlayerIdleState, "Crouch did not release ledge.");
        Require(!f.sm.CanLedgeGrab(), "Immediately reattached after dropping.");
    }
    static void TestOffsetCapsule()
    {
        using var f = new Fixture(3.3f);
        f.cc.center = Vector3.up;
        f.player.transform.position -= Vector3.up;
        Physics.SyncTransforms();
        Require(f.sm.CanLedgeGrab(), "Ledge detection depends on transform pivot instead of feet.");
        Require(Mathf.Abs(f.sm.LedgeClimbTarget.y - 3.57f) < 0.02f, "Landing ignored capsule center.");
    }
    static float ClimbAt(float dt)
    {
        using var f = new Fixture();
        f.StartClimb();
        for (int i = 0; i < 200 && f.sm.GetCurrentState() is PlayerWallClimbState; i++) f.Tick(dt);
        return f.player.transform.position.y;
    }
    static void TestFrameRates()
    {
        float y30 = ClimbAt(1f / 30f), y60 = ClimbAt(1f / 60f), y120 = ClimbAt(1f / 120f);
        Require(Mathf.Abs(y30 - y60) < 0.03f && Mathf.Abs(y60 - y120) < 0.03f,
            $"Frame-dependent heights: {y30}, {y60}, {y120}.");
    }
}