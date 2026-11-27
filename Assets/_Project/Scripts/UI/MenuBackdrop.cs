using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WG.Core;
using WG.World;

namespace WG.UI
{
    /// <summary>Фон главного меню. Задача 03 подменяет реализацию на настоящий мир игры через MainMenuController.BackgroundProvider.</summary>
    public interface IMenuBackground
    {
        void Build(Transform root);
    }

    /// <summary>
    /// Временный фон Phase 1: закат, холмы, танк MBT_Main и медленный облёт камеры.
    /// Заменяется настоящим миром в задаче 03.
    /// </summary>
    public class SimpleMenuBackground : IMenuBackground
    {
        public void Build(Transform root)
        {
            SceneAtmosphere.Setup(root, sunPitch: 8f, sunYaw: -60f, sunColor: new Color(1f, 0.62f, 0.38f), intensity: 1.6f,
                fogColor: new Color(0.62f, 0.5f, 0.45f), fogDensity: 0.004f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(60, 1, 60);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.19f, 0.24f, 0.12f), 0.05f);
            UnityEngine.Object.Destroy(ground.GetComponent<Collider>());

            var rng = new System.Random(7);
            var hillMat = MaterialLibrary.Lit(new Color(0.16f, 0.21f, 0.11f), 0.05f);
            var rockMat = MaterialLibrary.Lit(new Color(0.32f, 0.31f, 0.3f), 0.1f);
            for (int i = 0; i < 18; i++)
            {
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Hill";
                UnityEngine.Object.Destroy(hill.GetComponent<Collider>());
                hill.transform.SetParent(root, false);
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = 90f + (float)rng.NextDouble() * 160f;
                float s = 40f + (float)rng.NextDouble() * 90f;
                hill.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, -s * 0.32f, Mathf.Sin(ang) * dist);
                hill.transform.localScale = new Vector3(s, s * (0.35f + (float)rng.NextDouble() * 0.5f), s);
                hill.GetComponent<Renderer>().sharedMaterial = dist > 180 ? rockMat : hillMat;
            }

            var tank = ModelRegistry.Spawn("MBT_Main", Vector3.zero, Quaternion.Euler(0, 210, 0), root);
            if (tank == null)
            {
                tank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tank.transform.SetParent(root, false);
                tank.transform.localScale = new Vector3(3.6f, 2.2f, 7f);
                tank.transform.localPosition = new Vector3(0, 1.1f, 0);
                tank.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(new Color(0.3f, 0.32f, 0.2f));
            }

            var camGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(root, false);
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 38f;
            cam.farClipPlane = 1500f;
            cam.clearFlags = CameraClearFlags.Skybox;
            SceneAtmosphere.ConfigureCamera(cam);
            var orbit = camGo.AddComponent<OrbitCamera>();
            orbit.Target = new Vector3(0, 1.4f, 0);
            orbit.Distance = 15f;
            orbit.Height = 3.2f;
            orbit.Speed = 2.2f;
            orbit.Angle = 140f;
            orbit.LookOffset = new Vector3(-3.5f, 0, 0); // объект правее центра — меню слева
        }
    }

    /// <summary>Медленный облёт точки.</summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Vector3 Target, LookOffset;
        public float Distance = 15f, Height = 3f, Speed = 3f, Angle;

        void LateUpdate()
        {
            Angle += Speed * Time.unscaledDeltaTime;
            float a = Angle * Mathf.Deg2Rad;
            var pos = Target + new Vector3(Mathf.Sin(a) * Distance, Height + Mathf.Sin(Time.unscaledTime * 0.2f) * 0.4f, Mathf.Cos(a) * Distance);
            transform.position = pos;
            var look = Quaternion.LookRotation(Target - pos);
            transform.rotation = look;
            transform.position += transform.right * -LookOffset.x;
        }
    }

    /// <summary>Свет, небо, туман, постобработка — общие для меню и игры.</summary>
    public static class SceneAtmosphere
    {
        public static Light Sun { get; private set; }
        static Volume volume;

        public static void Setup(Transform root, float sunPitch, float sunYaw, Color sunColor, float intensity, Color fogColor, float fogDensity)
        {
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            var sunGo = new GameObject("Sun", typeof(Light));
            sunGo.transform.SetParent(root, false);
            Sun = sunGo.GetComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.color = sunColor;
            Sun.intensity = intensity;
            Sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0);
            RenderSettings.sun = Sun;

            var volGo = new GameObject("PostFX", typeof(Volume));
            volGo.transform.SetParent(root, false);
            volume = volGo.GetComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(1.0f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.saturation.Override(8f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            volume.profile = profile;
            ApplySettings(default);
            EventBus.Subscribe<SettingsChanged>(ApplySettings);
            volGo.AddComponent<UnsubscribeOnDestroy>().Action = () => EventBus.Unsubscribe<SettingsChanged>(ApplySettings);
            DynamicGI.UpdateEnvironment();
        }

        public static void ConfigureCamera(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = SettingsManager.Current.postProcessing;
            data.antialiasing = AntialiasingMode.None;
        }

        static void ApplySettings(SettingsChanged _)
        {
            if (volume != null) volume.enabled = SettingsManager.Current.postProcessing;
            foreach (var cam in Camera.allCameras)
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = SettingsManager.Current.postProcessing;
        }
    }

    public class UnsubscribeOnDestroy : MonoBehaviour
    {
        public Action Action;
        void OnDestroy() => Action?.Invoke();
    }
}
