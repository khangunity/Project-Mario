using UnityEngine;
using System;
using System.Collections.Generic;


public class UnityMainThreadDispatcher :
    MonoBehaviour
{
    private static readonly Queue<Action>
        actions =
        new Queue<Action>();


    private static UnityMainThreadDispatcher
        instance;


    void Awake()
    {
        instance = this;
    }


    void Update()
    {
        lock (actions)
        {
            while (
                actions.Count > 0
            )
            {
                Action action =
                    actions.Dequeue();


                action?.Invoke();
            }
        }
    }


    public static void Enqueue(
        Action action
    )
    {
        lock (actions)
        {
            actions.Enqueue(
                action
            );
        }
    }
}