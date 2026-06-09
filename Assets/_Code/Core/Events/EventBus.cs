using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awen.Core {
  public static class EventBus {
    private static readonly Dictionary<GameEventType, List<Delegate>> _handlers = new();

    public static void Subscribe(GameEventType type, EventDelegate handler) => Register(type, handler);

    public static void Subscribe<T>(GameEventType type, EventDelegate<T> handler) =>
        Register(type, handler);

    public static void Unsubscribe(GameEventType type, EventDelegate handler) => Remove(type, handler);

    public static void Unsubscribe<T>(GameEventType type, EventDelegate<T> handler) =>
        Remove(type, handler);

    public static void Emit(GameEventType type) {
      if (!_handlers.TryGetValue(type, out var list)) {
        return;
      }
      foreach (var h in list.ToArray()) {
        try {
          ((EventDelegate)h).Invoke();
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }
    }

    public static void Emit<T>(GameEventType type, T arg) {
      if (!_handlers.TryGetValue(type, out var list)) {
        return;
      }
      foreach (var h in list.ToArray()) {
        try {
          ((EventDelegate<T>)h).Invoke(arg);
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }
    }

    private static void Register(GameEventType type, Delegate handler) {
      if (!_handlers.TryGetValue(type, out var list)) {
        list = new List<Delegate>();
        _handlers[type] = list;
      }
      list.Add(handler);
    }

    private static void Remove(GameEventType type, Delegate handler) {
      if (_handlers.TryGetValue(type, out var list)) {
        list.Remove(handler);
      }
    }
  }
}
