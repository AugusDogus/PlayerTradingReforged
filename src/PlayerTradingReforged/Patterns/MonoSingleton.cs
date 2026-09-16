using System;
using UnityEngine;

namespace PlayerTradingReforged.Patterns;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T? _instance;
    public static T? Current => _instance != null ? _instance : null;
    public static T Instance => Current ?? throw new InvalidOperationException(typeof(T).Name + " has not initialized.");

    private void Awake()
    {
        if (Current != null) { Destroy(this); return; }
        if (this is T instance) { _instance = instance; Init(); }
    }
    protected virtual void Init() { }
}
