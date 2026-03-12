using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine : MonoBehaviour
{
    protected State currentState;
    protected Dictionary<Type, State> states = new Dictionary<Type, State>();

   

    private void Update()
    {
        currentState?.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        currentState?.FixedTick(Time.fixedDeltaTime);
    }

    public void AddState(State state)
    {
        states.Add(state.GetType(), state);
    }

    public void SwitchState(Type newStateType)
    {
        if (currentState != null && currentState.GetType() == newStateType) return;
        currentState?.Exit();

        if (states.TryGetValue(newStateType, out State newState))
        {
            currentState = newState;
            currentState.Enter();
        }
        else
        {
            Debug.LogError(
                $"Estado '{newStateType.FullName}' no encontrado en '{gameObject.name}'. " +
                $"¿Olvidaste AddState() en Awake/Start?"
            );
        }
    }

    public State GetCurrentState()
    {
        return currentState;
    }
}
