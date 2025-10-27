using System.Collections.Generic;
using UnityEngine;
using System;

public class MainThreadBridge : MonoBehaviour
{
    public static MainThreadBridge Instance;
    private readonly Queue<Action> _actions = new();

    void Awake() => Instance = this;

    void Update()
    {
        // Execute all queued actions on the main thread
        while (_actions.Count > 0)
        {
            var a = _actions.Dequeue();
            a.Invoke();
        }
    }

    public void RunOnMainThread(Action action)
    {
        lock (_actions)
            _actions.Enqueue(action);
    }
}