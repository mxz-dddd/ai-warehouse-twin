using System;
using AIWarehouseTwin.Simulation;
using Sim.Contracts.Artifacts;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWarehouseTwin.UI
{
    public sealed class SetupPanel : MonoBehaviour
    {
        public const string TooManyShelfRowsMessage = "货架行数过多";
        public const string InvalidParametersMessage = "参数无效";
        public const string RunningStatusText = "计算中...";
        public const string ReadyStatusText = "Ready";
        public const string CompleteStatusText = "完成";

        [SerializeField] private UIDocument document;

        private readonly WarehouseConfig currentConfig = CreateDefaultConfig();

        private FloatField lengthField;
        private FloatField widthField;
        private IntegerField shelfRowsField;
        private IntegerField skuCountField;
        private IntegerField workerCountField;
        private IntegerField forkliftCountField;
        private IntegerField orderCountField;
        private Button runButton;
        private Button resetButton;
        private Label statusLabel;

        private string statusText = ReadyStatusText;
        private string lastValidationMessage = string.Empty;

        public Func<WarehouseConfig, RunArtifact> BuildScenario { get; set; } = ScenarioBuilder.Build;

        public Action<RunArtifact> OnScenarioBuilt { get; set; }

        public bool IsCollapsed { get; private set; }

        public string StatusText => statusText;

        public string LastValidationMessage => lastValidationMessage;

        public WarehouseConfig CurrentConfig => CloneConfig(currentConfig);

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            if (document != null)
            {
                Bind(document.rootVisualElement);
            }
        }

        public static WarehouseConfig CreateDefaultConfig()
        {
            return new WarehouseConfig
            {
                lengthM = 40f,
                widthM = 20f,
                shelfRows = 3,
                skuCount = 200,
                workerCount = 5,
                forkliftCount = 2,
                orderCount = 50
            };
        }

        public void Bind(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            lengthField = root.Q<FloatField>("length-field");
            widthField = root.Q<FloatField>("width-field");
            shelfRowsField = root.Q<IntegerField>("shelf-rows-field");
            skuCountField = root.Q<IntegerField>("sku-count-field");
            workerCountField = root.Q<IntegerField>("worker-count-field");
            forkliftCountField = root.Q<IntegerField>("forklift-count-field");
            orderCountField = root.Q<IntegerField>("order-count-field");
            statusLabel = root.Q<Label>("status-label");

            runButton = root.Q<Button>("run-button");
            if (runButton != null)
            {
                runButton.clicked -= OnRunClicked;
                runButton.clicked += OnRunClicked;
            }

            resetButton = root.Q<Button>("reset-button");
            if (resetButton != null)
            {
                resetButton.clicked -= ResetToDefaults;
                resetButton.clicked += ResetToDefaults;
            }

            SyncToUi();
        }

        public void SetParameters(
            float lengthM,
            float widthM,
            int shelfRows,
            int skuCount,
            int workerCount,
            int forkliftCount,
            int orderCount)
        {
            currentConfig.lengthM = lengthM;
            currentConfig.widthM = widthM;
            currentConfig.shelfRows = shelfRows;
            currentConfig.skuCount = skuCount;
            currentConfig.workerCount = workerCount;
            currentConfig.forkliftCount = forkliftCount;
            currentConfig.orderCount = orderCount;
            SyncToUi();
        }

        public void SetConfig(WarehouseConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            CopyConfig(config, currentConfig);
            SyncToUi();
        }

        public void ResetToDefaults()
        {
            CopyConfig(CreateDefaultConfig(), currentConfig);
            lastValidationMessage = string.Empty;
            SetStatus(ReadyStatusText);
            SyncToUi();
        }

        public void ToggleCollapsed()
        {
            IsCollapsed = !IsCollapsed;
        }

        public void SyncFromUi()
        {
            if (lengthField != null) currentConfig.lengthM = lengthField.value;
            if (widthField != null) currentConfig.widthM = widthField.value;
            if (shelfRowsField != null) currentConfig.shelfRows = shelfRowsField.value;
            if (skuCountField != null) currentConfig.skuCount = skuCountField.value;
            if (workerCountField != null) currentConfig.workerCount = workerCountField.value;
            if (forkliftCountField != null) currentConfig.forkliftCount = forkliftCountField.value;
            if (orderCountField != null) currentConfig.orderCount = orderCountField.value;
        }

        public void SyncToUi()
        {
            if (lengthField != null) lengthField.value = currentConfig.lengthM;
            if (widthField != null) widthField.value = currentConfig.widthM;
            if (shelfRowsField != null) shelfRowsField.value = currentConfig.shelfRows;
            if (skuCountField != null) skuCountField.value = currentConfig.skuCount;
            if (workerCountField != null) workerCountField.value = currentConfig.workerCount;
            if (forkliftCountField != null) forkliftCountField.value = currentConfig.forkliftCount;
            if (orderCountField != null) orderCountField.value = currentConfig.orderCount;
            if (statusLabel != null) statusLabel.text = statusText;
        }

        public bool ValidateCurrentConfig()
        {
            return TryValidate(currentConfig, out _);
        }

        public bool RunScenario()
        {
            SyncFromUi();

            if (!TryValidate(currentConfig, out var message))
            {
                lastValidationMessage = message;
                SetStatus(message);
                ToastNotification.Show(message, ToastNotification.ToastType.Warning);
                return false;
            }

            SetRunButtonEnabled(false);
            SetStatus(RunningStatusText);

            try
            {
                var builder = BuildScenario ?? ScenarioBuilder.Build;
                var artifact = builder(CloneConfig(currentConfig));
                OnScenarioBuilt?.Invoke(artifact);
                SetStatus(CompleteStatusText);
                return true;
            }
            finally
            {
                SetRunButtonEnabled(true);
            }
        }

        private void OnRunClicked()
        {
            RunScenario();
        }

        public static bool TryValidate(WarehouseConfig config, out string message)
        {
            if (config == null)
            {
                message = InvalidParametersMessage;
                return false;
            }

            if (!(config.lengthM > 0f) ||
                !(config.widthM > 0f) ||
                config.shelfRows < 1 ||
                config.skuCount < 1 ||
                config.workerCount < 1 ||
                config.forkliftCount < 0 ||
                config.orderCount < 1)
            {
                message = InvalidParametersMessage;
                return false;
            }

            if (config.shelfRows * 3f >= config.widthM)
            {
                message = TooManyShelfRowsMessage;
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static WarehouseConfig CloneConfig(WarehouseConfig source)
        {
            var clone = new WarehouseConfig();
            CopyConfig(source, clone);
            return clone;
        }

        private static void CopyConfig(WarehouseConfig source, WarehouseConfig target)
        {
            target.lengthM = source.lengthM;
            target.widthM = source.widthM;
            target.shelfRows = source.shelfRows;
            target.skuCount = source.skuCount;
            target.workerCount = source.workerCount;
            target.forkliftCount = source.forkliftCount;
            target.orderCount = source.orderCount;
        }

        private void SetStatus(string text)
        {
            statusText = text ?? string.Empty;
            if (statusLabel != null)
            {
                statusLabel.text = statusText;
            }
        }

        private void SetRunButtonEnabled(bool enabled)
        {
            if (runButton != null)
            {
                runButton.SetEnabled(enabled);
            }
        }
    }
}
