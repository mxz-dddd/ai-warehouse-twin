using UnityEngine;

namespace AIWarehouseTwin.Playback
{
    public class PlaybackController : MonoBehaviour
    {
        private static readonly float[] SpeedOptions = { 1f, 5f, 10f, 999f };

        private static PlaybackController instance;
        private int speedIndex;

        public static PlaybackController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<PlaybackController>();
                }

                return instance;
            }

            private set => instance = value;
        }

        public float simulationTime { get; private set; }

        public float totalDuration = 60f;

        public float Speed => SpeedOptions[speedIndex];

        public string SpeedLabel
        {
            get
            {
                return Speed switch
                {
                    1f => "1×",
                    5f => "5×",
                    10f => "10×",
                    999f => "⚡",
                    _ => $"{Speed}×",
                };
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void CycleSpeed()
        {
            speedIndex = (speedIndex + 1) % SpeedOptions.Length;
        }

        public void Tick(float deltaTime)
        {
            if (totalDuration <= 0f || deltaTime <= 0f)
            {
                if (totalDuration <= 0f)
                {
                    simulationTime = 0f;
                }

                return;
            }

            simulationTime = (simulationTime + (deltaTime * Speed)) % totalDuration;
        }
    }
}
