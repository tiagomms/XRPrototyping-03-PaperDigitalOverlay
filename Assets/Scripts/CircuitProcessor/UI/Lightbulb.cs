using UnityEngine;
using Utils;

namespace CircuitProcessor
{
    public class Lightbulb : CircuitComponentUI
    {
        private CircuitFormulaEvaluator formulaEvaluator;

        [Header("LightBulb features")]
        [SerializeField] private Renderer _bulbRenderer;

        [SerializeField] private float intensity;
        [SerializeField] private float intensityToBrightnessFactor = 0.001f;
        public float lerpSpeed = 10f; // how fast the transition is
        private Material mat;
        private Color currentColor;
        private Color targetColor;
        private bool isUpdating = false;

        void Awake()
        {
            mat = _bulbRenderer.material;
            mat.EnableKeyword("_EMISSION");

            currentColor = mat.GetColor("_EmissionColor");
            targetColor = currentColor;
        }

        public override void Initialize(Component component)
        {
            hasEditableUI = false;
            editableType = EditableType.None;

            base.Initialize(component);
        }

        public void AttachFormulaEvaluator(CircuitFormulaEvaluator formulaEvaluator)
        {
            this.formulaEvaluator = formulaEvaluator;
            SetNewLightIntensity(this.formulaEvaluator.GetCurrentValue());

            this.formulaEvaluator.OnCalculatingFormula.AddListener(SetNewLightIntensity);
        }

        public void DetachFormulaEvaluator()
        {
            if (formulaEvaluator != null)
            {
                this.formulaEvaluator.OnCalculatingFormula.RemoveListener(SetNewLightIntensity);
                intensity = 0f;
                this.formulaEvaluator = null;
            }

        }

        void OnDestroy()
        {
            DetachFormulaEvaluator();
        }

        public void SetNewLightIntensity(float newIntensity)
        {
            intensity = newIntensity;
            float newBrightness = GetMaterialBrightnessFromIntensity(newIntensity);
            XRDebugLogViewer.Log($"[{nameof(Lightbulb)}] - INTENSITY VALUE: {newIntensity.ToString("0.####################")}; BRIGHTNESS: {newBrightness.ToString("0.####################")}");

            Color original = mat.GetColor("_EmissionColor");
            Color.RGBToHSV(original, out float h, out float s, out _);

            Color newTarget = Color.HSVToRGB(h, s, Mathf.Clamp01(newBrightness));
            targetColor = newTarget;
            isUpdating = true;

            // Update the display UI with the new intensity value
            UpdateDisplayUI();
        }

        // TODO: see if it makes more sense to be linear or adjustable depending on circuit
        private float GetMaterialBrightnessFromIntensity(float newIntensity)
        {
            return newIntensity / intensityToBrightnessFactor;
        }

        protected override string WriteDisplayUIText()
        {
            if (Mathf.Approximately(intensity, -1f))
            {
                return $"{id}\nERROR";    
            }
            return $"{id}\n{NumberFormatter.FormatRoundedAbbreviation(intensity, 2)}";
        }

        void Update()
        {
            if (!isUpdating)
                return;

            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * lerpSpeed);
            mat.SetColor("_EmissionColor", currentColor);

            if (Vector4.Distance(currentColor, targetColor) < 0.01f)
            {
                currentColor = targetColor;
                isUpdating = false;
            }
        }
    }

}
