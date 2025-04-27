using System;
using UnityEngine;

/// <summary>
/// Base class for ScriptableObject-based settings that need to notify
/// listeners of value changes in the Unity Editor.
/// </summary>
public class UpdatableData : ScriptableObject
{
    /// <summary>
    /// Event triggered when values are updated in the editor.
    /// Subscribe to this event to respond to settings changes.
    /// </summary>
    public event Action OnValuesUpdated;

    /// <summary>
    /// Controls whether changes automatically trigger updates.
    /// When true, changes in the Inspector will immediately notify listeners.
    /// </summary>
    public bool autoUpdate;

    #if UNITY_EDITOR
    /// <summary>
    /// Called by Unity when values are modified in the Inspector.
    /// Queues update notification if auto-update is enabled.
    /// Editor-only.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    /// <summary>
    /// Notifies listeners of value changes and removes update handler.
    /// Uses single-shot pattern to prevent multiple notifications.
    /// Editor-only.
    /// </summary>
    public void NotifyOfUpdatedValues()
    {
        // Remove handler first to prevent potential multiple notifications
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;

        // Notify listeners if any are subscribed
        OnValuesUpdated?.Invoke();
    }
    #endif
}
