using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FSMv2
{
    //FSMv1 (2020 attempt) Used a state map with a conditional test per state
    //Good for multiple active states since it meant multiple states could be active if they failed to test any newer states
    //Terrible for individual states since it meant each state took on additional code to check for valid transitions or valid running

    Dictionary<string, FSM_State> States = new Dictionary<string, FSM_State>();
    Dictionary<string, object> SharedVariables = new Dictionary<string, object>();
    private FSM_State CurrentState = null;
    private string DefaultState = string.Empty;


    public string getCurrentState() => CurrentState != null ? CurrentState.getName() : "Null";

    public void addState(FSM_State newState)
    {
        States.Add(newState.getName(), newState);
        if (DefaultState != string.Empty && newState.getName() == DefaultState)
        {
            CurrentState = newState;
            CurrentState.OnEntry();
        }
    }

    public void removeState(string stateName)
    {
        States.Remove(stateName);
        if (DefaultState == stateName)
        {
            KeyValuePair<string, FSM_State> newDefault = States.ElementAt(0);
            CurrentState = newDefault.Value;
            DefaultState = newDefault.Key;
        }
    }


    public void updateVariable(string name, object Value) { SharedVariables[name] = Value; }
    public object getVariable(string name) => SharedVariables[name];
    public void updateVariables(params KeyValuePair<string, object>[] entries)
    {
        foreach(KeyValuePair<string, object> entry in entries)
            updateVariable(entry.Key, entry.Value);
    }

    public void setDefaultState(string DefaultName)
    {
        DefaultState = DefaultName;
        if (CurrentState == null && States.Keys.Contains(DefaultName))
        {
            CurrentState = States[DefaultName];
            CurrentState.OnEntry();
        }
    }


    public void changeState(FSM_State newState)
    {
        CurrentState.OnExit();
        CurrentState = newState;
        CurrentState.OnEntry();
        CurrentState.OnUpdate(); //Performs an update in the same update as the transition
    }
    public void changeState(string newState)
    {
        CurrentState.OnExit();
        CurrentState = States[newState];
        CurrentState.OnEntry();
        CurrentState.OnUpdate(); //Performs an update in the same update as the transition
    }


    public void Update()
    {
        if (CurrentState != null)
            CurrentState.OnUpdate();
    }
}

public abstract class FSM_State
{
    private string Name = string.Empty;
    private protected FSMv2 Manager;

    public FSM_State(string StateName, FSMv2 FSM_Manager)
    {
        Name = StateName;
        Manager = FSM_Manager;
    }

    public string getName() => Name;

    public abstract void OnEntry();
    public abstract void OnExit();
    public abstract void OnUpdate();
}