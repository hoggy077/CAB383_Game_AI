using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class W2_EventTrigger : MonoBehaviour
{

    public enum TriggerState
    {
        Patrol,
        Hide,
        Attack
    }



    #region Workshop
    #region Step 3
    //void OnTriggerEnter(Collider other)
    //{
    //    if(other.tag == "Player")
    //    {
    //        Debug.Log("Yeet");
    //    }
    //}
    #endregion


    public TriggerState enter;
    public TriggerState exit;

    public Enemy[] Enems;



    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            switch (enter)
            {
                case TriggerState.Patrol:
                    ChangeEnemyStates(0);
                    break;
                case TriggerState.Hide:
                    ChangeEnemyStates(1);
                    break;
                case TriggerState.Attack:
                    ChangeEnemyStates(2);
                    break;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            switch (exit)
            {
                case TriggerState.Patrol:
                    ChangeEnemyStates(0);
                    break;
                case TriggerState.Hide:
                    ChangeEnemyStates(1);
                    break;
                case TriggerState.Attack:
                    ChangeEnemyStates(2);
                    break;
            }
        }
    }



    void ChangeEnemyStates(int State)
    {
        //foreach (Enemy enemy in Enems)
        //{
        //    enemy.newState = State;
        //}
    }
    #endregion

    #region My Implementation
    //Turns out this doesn't work in inspector. This issue was resolved following 2019.3

    //public TriggerState Enter;
    //public TriggerState Exit;

    //public UnityEvent<int> OnEnter;
    //public UnityEvent<int> OnExit;

    //void OnTriggerEnter(Collider other) => chngState(true);
    //void OnTriggerExit(Collider other) => chngState();

    //void chngState(bool isEnter = false)
    //{
    //    if (isEnter)
    //        OnEnter.Invoke((int)Enter);
    //    else
    //        OnExit.Invoke((int)Exit);
    //}

    #endregion
}
