#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpeedStars.EditorTools
{
    /// <summary>
    /// Input System's Simulator plugin disables native pointers for its entire lifetime,
    /// including while a separate Game view is focused. Hand those same devices back
    /// to Game and restore the plugin's ownership when returning to Simulator.
    /// Editor-only: device builds keep their normal input routing.
    /// </summary>
    [InitializeOnLoad]
    public static class SimulatorInputFocusGuard
    {
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly HashSet<InputDevice> RestoredPointers=new HashSet<InputDevice>();
        static readonly HashSet<InputDevice> SuspendedTouchscreens=new HashSet<InputDevice>();
        static double _nextCheck;
        static bool _quitting;

        static SimulatorInputFocusGuard()
        {
            EditorApplication.update+=Tick;
            EditorApplication.quitting+=()=>_quitting=true;
            AssemblyReloadEvents.beforeAssemblyReload+=RestoreSimulator;
        }

        static object Read(object value,string field) => value?.GetType().GetField(field,Fields)?.GetValue(value);

        static void Tick()
        {
            if(_quitting || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            if(EditorApplication.timeSinceStartup<_nextCheck)return;
            _nextCheck=EditorApplication.timeSinceStartup+.2;
            var focused=EditorWindow.focusedWindow;
            if(focused)SynchronizeForView(focused.GetType().Name);
        }

        /// <summary>Also used to verify the two editor view routes without changing the player's settings.</summary>
        public static void SynchronizeForView(string viewName)
        {
            if(_quitting)return;
            if(viewName=="SimulatorWindow") { RestoreSimulator();return; }
            if(viewName!="GameView")return;

            foreach(var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if(window.GetType().Name!="SimulatorWindow")continue;
                var controller=Read(Read(window,"m_Main"),"m_PluginController");
                if(!(Read(controller,"plugins") is IEnumerable plugins))continue;
                foreach(var plugin in plugins)
                {
                    if(plugin.GetType().FullName!="UnityEngine.InputSystem.Editor.InputSystemPlugin")continue;
                    if(Read(plugin,"SimulatorTouchscreen") is InputDevice touch && touch.added && touch.enabled)
                    { InputSystem.DisableDevice(touch);SuspendedTouchscreens.Add(touch); }
                    // Restore only devices disabled by this plugin, never an unrelated disabled device.
                    if(!(Read(plugin,"m_DisabledDevices") is IEnumerable disabled))continue;
                    foreach(var item in disabled)
                        if(item is InputDevice device && device.added && !device.enabled)
                        { InputSystem.EnableDevice(device);RestoredPointers.Add(device); }
                }
            }
        }

        static void RestoreSimulator()
        {
            if(_quitting)return;
            foreach(var device in RestoredPointers)
                if(device.added && device.enabled)InputSystem.DisableDevice(device);
            foreach(var device in SuspendedTouchscreens)
                if(device.added && !device.enabled)InputSystem.EnableDevice(device);
            RestoredPointers.Clear();SuspendedTouchscreens.Clear();
        }
    }
}
#endif
