using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AIWarehouseTwin.VFX
{
    public sealed class PickCompleteVFX : MonoBehaviour
    {
        public const int DefaultBurstCount = 30;
        public const float DefaultDuration = 0.3f;
        public const float DefaultStartLifetime = 0.3f;

        private const string ParticleSystemTypeName =
            "UnityEngine.ParticleSystem, UnityEngine.ParticleSystemModule";

        private static readonly Color DefaultHighlightColor = new Color(1f, 0.8784314f, 0.4f, 1f);

        [SerializeField]
        private int burstCount = DefaultBurstCount;

        [SerializeField]
        private float duration = DefaultDuration;

        [SerializeField]
        private float startLifetime = DefaultStartLifetime;

        [SerializeField]
        private Color startColor = DefaultHighlightColor;

        [SerializeField]
        private bool destroyWhenStopped;

        private Component particleSystemInstance;

        public int BurstCount => burstCount;

        public float Duration => duration;

        public float StartLifetime => startLifetime;

        public Color StartColor => startColor;

        public bool DestroyWhenStopped
        {
            get => destroyWhenStopped;
            set => destroyWhenStopped = value;
        }

        public bool HasParticleSystem => particleSystemInstance != null || FindParticleSystem() != null;

        public Component ParticleSystem => EnsureParticleSystem();

        public bool IsPlaying => GetParticleSystemBool("isPlaying");

        public Component EnsureParticleSystem()
        {
            if (particleSystemInstance == null)
            {
                particleSystemInstance = FindParticleSystem();
            }

            if (particleSystemInstance == null)
            {
                var type = ResolveParticleSystemType();
                if (type == null)
                {
                    return null;
                }

                particleSystemInstance = gameObject.AddComponent(type);
            }

            Configure(particleSystemInstance);
            return particleSystemInstance;
        }

        public void Play()
        {
            var system = EnsureParticleSystem();
            if (system == null)
            {
                return;
            }

            InvokeParticleSystemMethod(system, "Stop");
            InvokeParticleSystemMethod(system, "Play");
        }

        public void PlayAt(Vector3 position)
        {
            transform.position = position;
            Play();
        }

        public void Stop()
        {
            if (particleSystemInstance == null)
            {
                particleSystemInstance = FindParticleSystem();
            }

            if (particleSystemInstance != null)
            {
                InvokeParticleSystemMethod(particleSystemInstance, "Stop");
            }
        }

        private void Awake()
        {
            EnsureParticleSystem();
        }

        private void Update()
        {
            if (!destroyWhenStopped || particleSystemInstance == null || IsAlive())
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        private Component FindParticleSystem()
        {
            var type = ResolveParticleSystemType();
            return type == null ? null : GetComponent(type);
        }

        private static Type ResolveParticleSystemType()
        {
            return Type.GetType(ParticleSystemTypeName);
        }

        private void Configure(Component system)
        {
            if (system == null)
            {
                return;
            }

            ConfigureMainModule(system);
            ConfigureEmissionModule(system);
            ConfigureShapeModule(system);
        }

        private void ConfigureMainModule(Component system)
        {
            var main = GetModule(system, "main");
            SetModuleProperty(main, "duration", Mathf.Max(0.01f, duration));
            SetModuleProperty(main, "startLifetime", CreateMinMaxCurve(Mathf.Max(0.01f, startLifetime)));
            SetModuleProperty(main, "startSpeed", CreateMinMaxCurve(1.2f));
            SetModuleProperty(main, "startSize", CreateMinMaxCurve(0.12f));
            SetModuleProperty(main, "startColor", CreateMinMaxGradient(startColor));
            SetModuleProperty(main, "loop", false);
            SetModuleProperty(main, "playOnAwake", false);
        }

        private void ConfigureEmissionModule(Component system)
        {
            var emission = GetModule(system, "emission");
            SetModuleProperty(emission, "enabled", true);
            SetModuleProperty(emission, "rateOverTime", CreateMinMaxCurve(0f));

            var burstType = system.GetType().GetNestedType("Burst");
            if (burstType == null)
            {
                return;
            }

            var constructor = burstType.GetConstructor(new[] { typeof(float), typeof(short) });
            if (constructor == null)
            {
                return;
            }

            var burst = constructor.Invoke(new object[] { 0f, (short)Mathf.Max(0, burstCount) });
            var bursts = Array.CreateInstance(burstType, 1);
            bursts.SetValue(burst, 0);

            var setBursts = emission.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(method =>
                {
                    if (method.Name != "SetBursts")
                    {
                        return false;
                    }

                    var parameters = method.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.IsArray;
                });
            setBursts?.Invoke(emission, new object[] { bursts });
        }

        private static void ConfigureShapeModule(Component system)
        {
            var shape = GetModule(system, "shape");
            SetModuleProperty(shape, "enabled", true);
            var shapeTypeProperty = shape.GetType().GetProperty("shapeType");
            if (shapeTypeProperty != null)
            {
                var sphere = Enum.Parse(shapeTypeProperty.PropertyType, "Sphere");
                shapeTypeProperty.SetValue(shape, sphere);
            }

            SetModuleProperty(shape, "radius", 0.25f);
        }

        private static object GetModule(Component system, string propertyName)
        {
            return system.GetType().GetProperty(propertyName)?.GetValue(system);
        }

        private static void SetModuleProperty(object module, string propertyName, object value)
        {
            module?.GetType().GetProperty(propertyName)?.SetValue(module, value);
        }

        private static object CreateMinMaxCurve(float value)
        {
            var type = ResolveParticleSystemType()?.GetNestedType("MinMaxCurve");
            return type?.GetConstructor(new[] { typeof(float) })?.Invoke(new object[] { value });
        }

        private static object CreateMinMaxGradient(Color color)
        {
            var type = ResolveParticleSystemType()?.GetNestedType("MinMaxGradient");
            return type?.GetConstructor(new[] { typeof(Color) })?.Invoke(new object[] { color });
        }

        private bool IsAlive()
        {
            var method = particleSystemInstance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.Name == "IsAlive" &&
                    candidate.GetParameters().Length == 0);
            return method != null && (bool)method.Invoke(particleSystemInstance, Array.Empty<object>());
        }

        private bool GetParticleSystemBool(string propertyName)
        {
            if (particleSystemInstance == null)
            {
                particleSystemInstance = FindParticleSystem();
            }

            var property = particleSystemInstance?.GetType().GetProperty(propertyName);
            return property != null && (bool)property.GetValue(particleSystemInstance);
        }

        private static void InvokeParticleSystemMethod(Component system, string methodName)
        {
            var method = system.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == 0);
            method?.Invoke(system, Array.Empty<object>());
        }
    }
}
