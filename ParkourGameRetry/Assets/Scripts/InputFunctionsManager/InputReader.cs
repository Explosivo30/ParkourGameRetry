using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour,InputSystem_Actions.IPlayerActions
{



    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    public bool LookComesFromPointer { get; private set; }
    public bool JumpHeld { get; private set; }

    public event Action OnJumpStarted;   
    public event Action OnJumpCanceled;
    public event Action JumpEvent;
    public event Action DodgeEvent;
    public event Action TargetEvent;
    public event Action OnGrapplePressed;
    public event Action OnInteractPressed;

    // ---- Crouch events para Slide y Landing Roll ----
    public event Action OnCrouchStarted;
    public event Action OnCrouchCanceled;
    public bool CrouchHeld { get; private set; }
    public bool CrouchPressedThisFrame { get; private set; }
    public float LastCrouchPressedTime { get; private set; } = -999f;

    // ---- Sprint events para Dash ----
    public event Action OnSprintStarted;
    public event Action OnSprintCanceled;
    public bool SprintHeld { get; private set; }
    public bool SprintPressedThisFrame { get; private set; }

    public bool isTargeting = false;

    InputSystem_Actions controls;

    void Awake()
    {
        controls = new InputSystem_Actions();
        controls.Player.SetCallbacks(this);
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls?.Player.Disable();
        MovementValue = Vector2.zero;
        LookValue = Vector2.zero;
        JumpHeld = false;
        CrouchHeld = false;
        CrouchPressedThisFrame = false;
        LastCrouchPressedTime = -999f;
        SprintHeld = false;
        SprintPressedThisFrame = false;
    }

    void LateUpdate()
    {
        // Los estados leen estos flancos en Update. Limpiarlos aquí impide
        // reactivar dash o slide por el mero hecho de mantener el botón.
        CrouchPressedThisFrame = false;
        SprintPressedThisFrame = false;
    }

    void OnDestroy()
    {
        if (controls == null) return;
        controls.Player.SetCallbacks(null);
        controls.Dispose();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
            OnGrapplePressed?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CrouchHeld = true;
            CrouchPressedThisFrame = true;
            LastCrouchPressedTime = Time.unscaledTime;
            OnCrouchStarted?.Invoke();
        }
        if (context.canceled)
        {
            CrouchHeld = false;
            OnCrouchCanceled?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) OnInteractPressed?.Invoke();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            JumpHeld = true;
            OnJumpStarted?.Invoke();
        }
        if (context.canceled)
        {
            JumpHeld = false;
            OnJumpCanceled?.Invoke();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookValue = context.ReadValue<Vector2>();
        LookComesFromPointer = context.control?.device is Pointer;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
       MovementValue = context.ReadValue<Vector2>();
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SprintHeld = true;
            SprintPressedThisFrame = true;
            OnSprintStarted?.Invoke();
        }
        if (context.canceled)
        {
            SprintHeld = false;
            OnSprintCanceled?.Invoke();
        }
    }


}
