using System;
using System.Collections.Generic;
using UnityEngine;

namespace WG.World
{
    /// <summary>Реестр моделей: id (имя FBX) → модель. Заполняется в редакторе (WARGAME/Refresh Model Registry).</summary>
    [CreateAssetMenu(menuName = "WARGAME/Model Registry")]
    public class ModelRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string category;
            public GameObject model;
        }

        public List<Entry> entries = new();

        static ModelRegistry instance;
        static Dictionary<string, Entry> map;

        public static ModelRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ModelRegistry>("ModelRegistry");
                    map = new Dictionary<string, Entry>();
                    if (instance != null)
                        foreach (var e in instance.entries)
                            if (e != null && e.model != null && !string.IsNullOrEmpty(e.id)) map[e.id] = e;
                }
                return instance;
            }
        }

        public static GameObject Get(string id)
        {
            _ = Instance;
            return map != null && map.TryGetValue(id, out var e) ? e.model : null;
        }

        /// <summary>Создать экземпляр модели. null — если модели нет (вызывающий код делает запасную геометрию).</summary>
        public static GameObject Spawn(string id, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            var m = Get(id);
            if (m == null) return null;
            var go = Instantiate(m, pos, rot, parent);
            go.name = id;
            return go;
        }
    }

    /// <summary>Материалы для объектов, создаваемых кодом (шаблоны из Resources/Materials, клонируются с цветом).</summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        public static Material Template(string name)
        {
            if (cache.TryGetValue("tpl:" + name, out var m)) return m;
            m = Resources.Load<Material>("Materials/" + name);
            if (m == null)
            {
                Debug.LogWarning($"[WARGAME] Нет материала Resources/Materials/{name} — запустите WARGAME/Setup Project.");
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader) { name = name };
            }
            cache["tpl:" + name] = m;
            return m;
        }

        /// <summary>Непрозрачный Lit-материал заданного цвета (кэшируется по цвету).</summary>
        public static Material Lit(Color c, float smoothness = 0.25f)
        {
            string key = $"lit:{ColorUtility.ToHtmlStringRGBA(c)}:{smoothness:0.00}";
            if (cache.TryGetValue(key, out var m)) return m;
            m = new Material(Template("Lit_Base")) { name = key, enableInstancing = true };
            m.SetColor(BaseColor, c);
            m.SetFloat(Smoothness, smoothness);
            cache[key] = m;
            return m;
        }
    }
}
