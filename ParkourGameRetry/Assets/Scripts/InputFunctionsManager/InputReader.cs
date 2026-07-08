using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour,InputSystem_Actions.IPlayerActions
{



    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }

    public event Action OnJumpStarted;   
    public event Action OnJumpCanceled;
    public event Action JumpEvent;
    public event Action DodgeEvent;
    public event Action TargetEvent;

    // ---- Crouch events para Slide y Landing Roll ----
    public event Action OnCrouchStarted;
    public event Action OnCrouchCanceled;
    public bool CrouchHeld { get; private set; }

    // ---- Sprint events para Dash ----
    public event Action OnSprintStarted;
    public event Action OnSprintCanceled;
    public bool SprintHeld { get; private set; }

    public bool isTargeting = false;

    InputSystem_Actions controls;
    void Start()
    {
        
        controls = new InputSystem_Actions();
        controls.Player.SetCallbacks(this);

        controls.Player.Enable();
    }

    void OnDestroy()
    {
        controls.Player.Disable();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CrouchHeld = true;
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
        
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) OnJumpStarted?.Invoke();
        if (context.canceled) OnJumpCanceled?.Invoke();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookValue = context.ReadValue<Vector2>();
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
            OnSprintStarted?.Invoke();
        }
        if (context.canceled)
        {
            SprintHeld = false;
            OnSprintCanceled?.Invoke();
        }
    }


}
