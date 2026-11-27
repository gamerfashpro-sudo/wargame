using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace WG.Core
{
    public enum GameAction
    {
        CamForward, CamBack, CamLeft, CamRight, CamRotLeft, CamRotRight,
        CamTiltUp, CamTiltDown, CamFocus, CamFast, Pause, Speed1, Speed2, Speed3
    }

    [Serializable]
    public class KeyBindingEntry { public GameAction action; public Key key; }

    /// <summary>Переназначаемые клавиши. Хранятся в GameSettings.keys.</summary>
    public static class KeyBindings
    {
        public static readonly Dictionary<GameAction, Key> Defaults = new()
        {
            [GameAction.CamForward] = Key.W, [GameAction.CamBack] = Key.S,
            [GameAction.CamLeft] = Key.A, [GameAction.CamRight] = Key.D,
            [GameAction.CamRotLeft] = Key.Q, [GameAction.CamRotRight] = Key.E,
            [GameAction.CamTiltUp] = Key.R, [GameAction.CamTiltDown] = Key.T,
            [GameAction.CamFocus] = Key.F, [GameAction.CamFast] = Key.LeftShift,
            [GameAction.Pause] = Key.Space, [GameAction.Speed1] = Key.Digit1,
            [GameAction.Speed2] = Key.Digit2, [GameAction.Speed3] = Key.Digit3,
        };

        static readonly Dictionary<GameAction, Key> current = new(Defaults);

        public static Key Get(GameAction a) => current.TryGetValue(a, out var k) ? k : Defaults[a];

        /// <summary>Назначить клавишу. Если она занята другим действием — действия меняются клавишами.</summary>
        public static void Set(GameAction a, Key k)
        {
            var old = Get(a);
            foreach (var other in new List<GameAction>(current.Keys))
                if (other != a && current[other] == k) current[other] = old;
            current[a] = k;
        }

        public static bool IsDown(GameAction a)
        {
            var kb = Keyboard.current;
            return kb != null && kb[Get(a)].isPressed;
        }

        public static bool WasPressed(GameAction a)
        {
            var kb = Keyboard.current;
            return kb != null && kb[Get(a)].wasPressedThisFrame;
        }

        public static void Load(List<KeyBindingEntry> list)
        {
            current.Clear();
            foreach (var kv in Defaults) current[kv.Key] = kv.Value;
            if (list == null) return;
            foreach (var e in list) if (e.key != Key.None) current[e.action] = e.key;
        }

        public static List<KeyBindingEntry> Export()
        {
            var list = new List<KeyBindingEntry>();
            foreach (var kv in current) list.Add(new KeyBindingEntry { action = kv.Key, key = kv.Value });
            return list;
        }

        public static void ResetDefaults() => Load(null);

        public static string KeyName(Key k)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                var name = kb[k].displayName;
                if (!string.IsNullOrEmpty(name)) return name.ToUpperInvariant();
            }
            return k.ToString();
        }
    }
}
