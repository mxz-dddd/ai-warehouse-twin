using AIWarehouseTwin.Agent;
using AIWarehouseTwin.VFX;
using AIWarehouseTwin.World;
using NUnit.Framework;
using UnityEngine;

namespace AIWarehouseTwin.Tests
{
    public sealed class WorkerActorTests
    {
        private GameObject gameObject;
        private GameObject vfxGameObject;
        private WarehousePalette palette;

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }

            if (palette != null)
            {
                Object.DestroyImmediate(palette);
            }

            if (vfxGameObject != null)
            {
                Object.DestroyImmediate(vfxGameObject);
            }
        }

        [Test]
        public void EnsureVisuals_creates_capsule_body_and_sphere_head()
        {
            var worker = CreateWorker();

            worker.EnsureVisuals();

            Assert.That(worker.Body, Is.Not.Null);
            Assert.That(worker.Head, Is.Not.Null);
            Assert.That(worker.Body.name, Is.EqualTo("WorkerBody"));
            Assert.That(worker.Head.name, Is.EqualTo("WorkerHead"));
            Assert.That(worker.Body.GetComponent<CapsuleCollider>(), Is.Not.Null);
            Assert.That(worker.Head.GetComponent<SphereCollider>(), Is.Not.Null);
        }

        [Test]
        public void EnsureVisuals_uses_palette_worker_color_when_provided()
        {
            var worker = CreateWorker();
            palette = ScriptableObject.CreateInstance<WarehousePalette>();
            palette.ApplyDefaultColors();

            worker.SetPalette(palette);
            worker.EnsureVisuals();

            var bodyColor = worker.Body.GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(bodyColor.r, Is.EqualTo(palette.worker.r).Within(0.001f));
            Assert.That(bodyColor.g, Is.EqualTo(palette.worker.g).Within(0.001f));
            Assert.That(bodyColor.b, Is.EqualTo(palette.worker.b).Within(0.001f));
        }

        [Test]
        public void Moving_state_updates_bob_offset_without_changing_route_position()
        {
            var worker = CreateWorker();
            worker.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 2f, Vector3.zero, new Vector3(2f, 0f, 0f), ActorState.Moving),
            });

            worker.Tick(0.25f);
            var firstBob = worker.BobOffset;
            var positionAfterFirstTick = worker.transform.position;

            worker.Tick(0.5f);

            Assert.That(Mathf.Abs(worker.BobOffset - firstBob), Is.GreaterThan(0.0001f));
            Assert.That(positionAfterFirstTick.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(worker.transform.position.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Non_moving_state_resets_bob_offset()
        {
            var worker = CreateWorker();
            worker.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 1f, Vector3.zero, Vector3.right, ActorState.Moving),
                new ActorRouteSegment(1f, 2f, Vector3.right, Vector3.right, ActorState.Picking),
            });

            worker.Tick(0.25f);
            Assert.That(Mathf.Abs(worker.BobOffset), Is.GreaterThan(0f));

            worker.Tick(1.5f);

            Assert.That(worker.BobOffset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Picking_state_triggers_pick_complete_vfx_once()
        {
            var worker = CreateWorker();
            var calls = 0;
            worker.PickVfxFactory = position =>
            {
                calls++;
                return CreateVfx();
            };
            worker.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 1f, Vector3.zero, Vector3.right, ActorState.Picking),
            });

            worker.Tick(0.25f);
            worker.Tick(0.5f);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Picking_state_can_trigger_again_after_leaving()
        {
            var worker = CreateWorker();
            var calls = 0;
            worker.PickVfxFactory = position =>
            {
                calls++;
                return CreateVfx();
            };
            worker.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 1f, Vector3.zero, Vector3.right, ActorState.Picking),
                new ActorRouteSegment(1f, 2f, Vector3.right, Vector3.right, ActorState.Moving),
                new ActorRouteSegment(2f, 3f, Vector3.right, Vector3.right * 2f, ActorState.Picking),
            });

            worker.Tick(0.5f);
            worker.Tick(1.5f);
            worker.Tick(2.5f);

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void Picking_without_vfx_binding_is_safe()
        {
            var worker = CreateWorker();
            worker.LoadRoute(new[]
            {
                new ActorRouteSegment(0f, 1f, Vector3.zero, Vector3.right, ActorState.Picking),
            });

            Assert.DoesNotThrow(() => worker.Tick(0.5f));
        }

        private WorkerActor CreateWorker()
        {
            gameObject = new GameObject("WorkerActorTests");
            return gameObject.AddComponent<WorkerActor>();
        }

        private PickCompleteVFX CreateVfx()
        {
            if (vfxGameObject != null)
            {
                Object.DestroyImmediate(vfxGameObject);
            }

            vfxGameObject = new GameObject("PickCompleteVFXTests");
            return vfxGameObject.AddComponent<PickCompleteVFX>();
        }
    }
}
