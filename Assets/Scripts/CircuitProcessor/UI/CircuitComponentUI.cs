using System;
using TMPro;
using UnityEngine;
using Utils;

namespace CircuitProcessor
{
    /// <summary>
    /// Component data to be attached to instantiated component prefabs
    /// </summary>
    // FIXME: separation of concerns - ideally I would make a super class for the CircuitComponentUI prefab
    // FIXME:   that based on the component type, enable, disable stuff. but don't have time. 
    // FIXME:   then have in the parent object of all a reference to this CircuitComponentUI
    public abstract class CircuitComponentUI : MonoBehaviour
    {
        public enum EditableType
        {
            None = 0,
            Slider = 1,
            Toggle = 2
        }

        protected Component component;

        [Header("Display UI")]
        // Reference to your display UI root
        [SerializeField] protected GameObject displayUI;
        // Optional: display value text
        [SerializeField] protected TextMeshProUGUI displayText;

        [Header("Editable UI")]
        [SerializeField] protected bool hasEditableUI = false;
        [SerializeField] protected GameObject editableUI;
        [SerializeField] protected EditableType editableType = EditableType.None;
        [SerializeField] protected TextMeshProUGUI editableDisplayUIText;
        [SerializeField] protected GameObject editableOptions;

        [Header("Editable UI Offsets")]
        [SerializeField] protected float initialOffset = 9.5f;
        [SerializeField] protected float gridFactor = 1.5f;

        public string id => component.id;
        public string type => component.type;
        public float value => component.value;
        public Vector2Int gridPosition => component.gridPosition;
        public Vector2Int asciiPosition => component.asciiPosition;
        public Vector2 rectPosition => component.rectPosition;

        public virtual void Initialize(Component component)
        {
            this.component = component;
            // NOTE: editable UI should be placed at the bottom of the table and ideally never let it
            // NOTE: since editableUI is child of the object, we need to counter the offset of the parent x position
            // NOTE: by observation - a grid factor looks good 
            editableUI.transform.localPosition = Vector3.right * (initialOffset + component.gridPosition.y * gridFactor - transform.localPosition.x);
            editableUI.SetActive(hasEditableUI);

            for (int i = 0; i < editableOptions.transform.childCount; i++)
            {
                var child = editableOptions.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }
            UpdateDisplayUI();
        }

        protected virtual string WriteDisplayUIText()
        {
            return $"{id}\n{NumberFormatter.FormatRoundedAbbreviation(value, 2)}";
        }

        protected virtual void UpdateDisplayUI()
        {
            string uiText = WriteDisplayUIText();
            if (displayText != null)
            {
                displayText.text = uiText;
            }
            if (hasEditableUI && editableDisplayUIText != null)
            {
                editableDisplayUIText.text = uiText;
            }
        }
    }
}