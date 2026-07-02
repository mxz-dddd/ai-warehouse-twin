using System;
using System.Collections.Generic;
using AIWarehouseTwin.World;
using UnityEngine;

namespace AIWarehouseTwin.Agent
{
    public sealed class AgentSpawner : MonoBehaviour
    {
        private readonly List<ActorController> spawnedActors = new List<ActorController>();

        [SerializeField]
        private Transform actorsRoot;

        [SerializeField]
        private WarehousePalette palette;

        public Transform ActorsRoot => EnsureActorsRoot();

        public IReadOnlyList<ActorController> SpawnedActors => spawnedActors;

        public string LastWarning { get; private set; } = string.Empty;

        public void SetPalette(WarehousePalette nextPalette)
        {
            palette = nextPalette;
        }

        public ActorController SpawnActor(string actorId, string actorType)
        {
            LastWarning = string.Empty;

            var normalizedType = NormalizeActorType(actorType);
            switch (normalizedType)
            {
                case "worker":
                    return CreateWorker(actorId);
                case "forklift":
                    return CreateForklift(actorId);
                default:
                    LastWarning = $"Unknown actor type '{actorType}' for actor '{actorId}'.";
                    Debug.LogWarning(LastWarning);
                    return null;
            }
        }

        public void ClearActors()
        {
            spawnedActors.Clear();

            if (actorsRoot == null)
            {
                return;
            }

            for (var i = actorsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyObject(actorsRoot.GetChild(i).gameObject);
            }
        }

        private WorkerActor CreateWorker(string actorId)
        {
            var actor = CreateActorObject<WorkerActor>("Worker", actorId);
            actor.SetPalette(palette);
            actor.EnsureVisuals();
            return actor;
        }

        private ForkliftActor CreateForklift(string actorId)
        {
            var actor = CreateActorObject<ForkliftActor>("Forklift", actorId);
            actor.SetPalette(palette);
            actor.EnsureVisuals();
            return actor;
        }

        private T CreateActorObject<T>(string prefix, string actorId)
            where T : ActorController
        {
            var go = new GameObject(StableActorName(prefix, actorId));
            go.transform.SetParent(EnsureActorsRoot(), false);

            var actor = go.AddComponent<T>();
            actor.actorId = actorId ?? string.Empty;
            spawnedActors.Add(actor);
            return actor;
        }

        private Transform EnsureActorsRoot()
        {
            if (actorsRoot != null)
            {
                return actorsRoot;
            }

            var root = new GameObject("ActorsRoot");
            root.transform.SetParent(transform, false);
            actorsRoot = root.transform;
            return actorsRoot;
        }

        private static string StableActorName(string prefix, string actorId)
        {
            return $"{prefix}_{(string.IsNullOrWhiteSpace(actorId) ? "unknown" : actorId)}";
        }

        private static string NormalizeActorType(string actorType)
        {
            return (actorType ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
