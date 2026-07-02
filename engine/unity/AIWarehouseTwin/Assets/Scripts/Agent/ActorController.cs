using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIWarehouseTwin.Agent
{
    public enum ActorState
    {
        Idle,
        Moving,
        Picking,
        Carrying,
    }

    public readonly struct ActorRouteSegment
    {
        public ActorRouteSegment(
            float startTime,
            float endTime,
            Vector3 from,
            Vector3 to,
            ActorState state)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.from = from;
            this.to = to;
            this.state = state;
        }

        public readonly float startTime;

        public readonly float endTime;

        public readonly Vector3 from;

        public readonly Vector3 to;

        public readonly ActorState state;
    }

    public abstract class ActorController : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;

        private readonly List<ActorRouteSegment> route = new List<ActorRouteSegment>();
        private ActorState currentState = ActorState.Idle;
        private bool hasState;

        public string actorId;

        public ActorState CurrentState => hasState ? currentState : ActorState.Idle;

        public IReadOnlyList<ActorRouteSegment> Route => route;

        public void LoadRoute(IReadOnlyList<ActorRouteSegment> nextRoute)
        {
            route.Clear();
            hasState = false;
            currentState = ActorState.Idle;

            if (nextRoute == null)
            {
                return;
            }

            route.AddRange(nextRoute);
            route.Sort(CompareSegments);
        }

        public void Tick(float simTime)
        {
            if (route.Count == 0)
            {
                OnTicked(simTime);
                return;
            }

            var segment = ResolveSegment(simTime, out var sampleTime);
            transform.position = Vector3.Lerp(
                segment.from,
                segment.to,
                InterpolationAlpha(segment, sampleTime));
            UpdateFacing(segment);
            SetState(segment.state);
            OnTicked(simTime);
        }

        protected abstract void OnStateChanged(ActorState next);

        protected virtual void OnTicked(float simTime)
        {
        }

        private void SetState(ActorState next)
        {
            if (hasState && currentState == next)
            {
                return;
            }

            hasState = true;
            currentState = next;
            OnStateChanged(next);
        }

        private ActorRouteSegment ResolveSegment(float simTime, out float sampleTime)
        {
            var first = route[0];
            if (simTime <= first.startTime)
            {
                sampleTime = first.startTime;
                return first;
            }

            for (var i = 0; i < route.Count; i++)
            {
                var segment = route[i];
                if (simTime >= segment.startTime && simTime <= segment.endTime)
                {
                    sampleTime = simTime;
                    return segment;
                }

                if (i < route.Count - 1 && simTime < route[i + 1].startTime)
                {
                    sampleTime = segment.endTime;
                    return segment;
                }
            }

            var last = route[route.Count - 1];
            sampleTime = last.endTime;
            return last;
        }

        private static float InterpolationAlpha(ActorRouteSegment segment, float simTime)
        {
            if (segment.endTime <= segment.startTime)
            {
                return simTime <= segment.startTime ? 0f : 1f;
            }

            return Mathf.Clamp01((simTime - segment.startTime) / (segment.endTime - segment.startTime));
        }

        private void UpdateFacing(ActorRouteSegment segment)
        {
            var direction = segment.to - segment.from;
            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static int CompareSegments(ActorRouteSegment left, ActorRouteSegment right)
        {
            var startComparison = left.startTime.CompareTo(right.startTime);
            if (startComparison != 0)
            {
                return startComparison;
            }

            return left.endTime.CompareTo(right.endTime);
        }
    }
}
