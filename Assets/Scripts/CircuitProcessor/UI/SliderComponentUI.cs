using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace CircuitProcessor
{
    public class SliderComponentUI : CircuitComponentUI
    {
        [Header("Slider References")]
        [SerializeField] private GameObject sliderParent;
        [SerializeField] private Slider slider;

        [SerializeField] private TextMeshProUGUI minValueUIText;
        [SerializeField] private TextMeshProUGUI maxValueUIText;

        [SerializeField] protected float minValue = 0f;
        [SerializeField] protected float maxValue = 10f;

        public override void Initialize(Component component)
        {
            hasEditableUI = true;
            editableType = EditableType.Slider;

            base.Initialize(component);

            sliderParent.gameObject.SetActive(true);

            minValueUIText.text = NumberFormatter.FormatRoundedAbbreviation(minValue, 0);
            maxValueUIText.text = NumberFormatter.FormatRoundedAbbreviation(maxValue, 0);

            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = component.value;
            slider.onValueChanged.AddListener(OnSliderChanged);
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void OnSliderChanged(float newValue)
        {
            component.value = newValue;
            UpdateDisplayUI();
        }
    }
}
