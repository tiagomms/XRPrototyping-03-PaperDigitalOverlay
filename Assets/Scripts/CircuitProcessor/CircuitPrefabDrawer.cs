using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CircuitProcessor;

namespace CircuitProcessor
{
    /// <summary>
    /// Component data to be attached to instantiated component prefabs
    /// </summary>
    [Serializable]
    public class ComponentData : MonoBehaviour
    {
        public Component component;

        public string id => component.id;
        public string type => component.type;
        public float value => component.value;
        public Vector2Int gridPosition => component.gridPosition;
        public Vector2Int asciiPosition => component.asciiPosition;
        public Vector2 rectPosition => component.rectPosition;
    }

    /// <summary>
    /// Wire data to be attached to instantiated wire prefabs
    /// </summary>
    [Serializable]
    public class WireData : MonoBehaviour
    {
        public Wire wire;

        public string id => wire.id;
        public Vector2Int fromGrid => wire.fromGrid;
        public Vector2Int toGrid => wire.toGrid;
        public Vector2Int fromASCII => wire.fromASCII;
        public Vector2Int toASCII => wire.toASCII;
        public Vector2 fromRect => wire.fromRect;
        public Vector2 toRect => wire.toRect;
        public bool isHorizontal => wire.isHorizontal;
        public bool startTouchesComponent => wire.startTouchesComponent;
        public bool endTouchesComponent => wire.endTouchesComponent;
        public bool isPartOfFork => wire.isPartOfFork;
        public bool isPartOfMerge => wire.isPartOfMerge;
    }

    /// <summary>
    /// Handles the conversion of circuit data into 3D prefab representation
    /// </summary>
    public class CircuitPrefabDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject circuitTable;
        // Fixed layout parameters from specification
        [SerializeField] private int WIRE_LENGTH = 5;
        [SerializeField] private int HORIZONTAL_PADDING = 0;
        [SerializeField] private int VERTICAL_PADDING = 0;

        [Header("Parent Transform")]
        [SerializeField] private Transform parentTransform;

        [Header("Component Prefabs")]
        [SerializeField] private GameObject resistorPrefab;
        [SerializeField] private GameObject lightbulbPrefab;
        [SerializeField] private GameObject batteryPrefab;
        [SerializeField] private GameObject switchPrefab;
        [SerializeField] private GameObject forkPrefab;
        [SerializeField] private GameObject mergePrefab;

        [Header("Wire Prefab")]
        [SerializeField] private GameObject wirePrefab;

        [Header("Wire Offset Settings")]
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfResistor = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfLightbulb = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfBattery = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfSwitch = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfFork = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfMerge = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float wireOffsetIfWire = 0f;

        // Public list to store instantiated objects
        public List<GameObject> InstantiatedObjects { get; private set; } = new List<GameObject>();

        private void Awake()
        {
            // Check for required prefabs
            if (resistorPrefab == null)
                Debug.LogError("ResistorPrefab is not assigned in CircuitPrefabDrawer!");
            if (lightbulbPrefab == null)
                Debug.LogError("LightbulbPrefab is not assigned in CircuitPrefabDrawer!");
            if (batteryPrefab == null)
                Debug.LogError("BatteryPrefab is not assigned in CircuitPrefabDrawer!");
            if (switchPrefab == null)
                Debug.LogError("SwitchPrefab is not assigned in CircuitPrefabDrawer!");
            if (wirePrefab == null)
                Debug.LogError("WirePrefab is not assigned in CircuitPrefabDrawer!");
            if (parentTransform == null)
                Debug.LogError("ParentTransform is not assigned in CircuitPrefabDrawer!");
        }

        private void Start()
        {
            circuitTable.SetActive(false);
        }

        /// <summary>
        /// Clears all previously instantiated objects
        /// </summary>
        public void ClearInstantiatedObjects()
        {
            foreach (var obj in InstantiatedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            InstantiatedObjects.Clear();
        }

        /// <summary>
        /// Places the complete circuit using prefabs
        /// </summary>
        public void InitializeDrawPrefabCircuit(CircuitData circuitData)
        {
            circuitTable.SetActive(true);
            
            // Clear previous objects before creating new ones
            ClearInstantiatedObjects();

            // Initialize positions (reuse logic from ASCII drawer)
            InitializePositions(circuitData);

            // Place all wires first
            foreach (var wire in circuitData.wires)
            {
                var wireObject = PlaceWire(circuitData, wire);
                if (wireObject != null)
                    InstantiatedObjects.Add(wireObject);
            }

            // Place all components
            foreach (var component in circuitData.components)
            {
                var componentObject = PlaceComponent(circuitData, component);
                if (componentObject != null)
                    InstantiatedObjects.Add(componentObject);
            }

        }

        /// <summary>
        /// Calculates ASCII position based on grid position with padding
        /// </summary>
        private Vector2Int CalculateAsciiPosition(Vector2Int gridPosition)
        {
            return new Vector2Int(
                gridPosition.x * WIRE_LENGTH + HORIZONTAL_PADDING,
                gridPosition.y * WIRE_LENGTH + VERTICAL_PADDING
            );
        }

        /// <summary>
        /// Initializes positions for all components and wires
        /// </summary>
        private void InitializePositions(CircuitData circuitData)
        {
            // Calculate ASCII positions for all components and wires
            foreach (var component in circuitData.components)
            {
                component.asciiPosition = CalculateAsciiPosition(component.gridPosition);
            }

            foreach (var wire in circuitData.wires)
            {
                wire.fromASCII = CalculateAsciiPosition(wire.fromGrid);
                wire.toASCII = CalculateAsciiPosition(wire.toGrid);
            }
        }

        /// <summary>
        /// Places a wire prefab in the scene
        /// </summary>
        private GameObject PlaceWire(CircuitData circuitData, Wire wire)
        {
            if (wirePrefab == null)
                return null;
            // Instantiate wire
            GameObject wireObject = Instantiate(wirePrefab, parentTransform);
            
            // Calculate initial position: (fromASCII.y, 0, fromASCII.x)
            wireObject.transform.localPosition = new Vector3(wire.fromASCII.y, 0, wire.fromASCII.x);
            
            // Calculate rotation based on wire direction
            float yRotation = 0f;
            int deltaY = wire.toASCII.y - wire.fromASCII.y;
            
            if (deltaY == 0) // Horizontal
                yRotation = 0f;
            else if (deltaY > 0) // Vertical positive
                yRotation = 90f;
            else // Vertical negative
                yRotation = -90f;

            wireObject.transform.localRotation = Quaternion.Euler(0, yRotation, 0);

            // Calculate initial scale based on distance
            Vector3 scale = wireObject.transform.localScale;
            if (deltaY == 0) // Horizontal wire
            {
                scale.z = Mathf.Abs(wire.toASCII.x - wire.fromASCII.x);
            }
            else // Vertical wire
            {
                scale.z = Mathf.Abs(wire.toASCII.y - wire.fromASCII.y);
            }
            wireObject.transform.localScale = scale;

            // Add WireData component
            WireData wireData = wireObject.AddComponent<WireData>();
            wireData.wire = wire;

            // Apply wire offsets and scaling
            ApplyWireOffsets(circuitData, wire, wireObject);

            return wireObject;
        }

        /// <summary>
        /// Applies offset logic to wire prefabs
        /// </summary>
        private void ApplyWireOffsets(CircuitData circuitData, Wire wire, GameObject wireObject)
        {
            Vector3 position = wireObject.transform.localPosition;
            Vector3 scale = wireObject.transform.localScale;
            
            // Apply component-based offsets
            if (wire.startTouchesComponent)
            {
                var startComponent = FindComponentAtPosition(circuitData, wire.fromASCII);
                if (startComponent != null)
                {
                    float offset = GetWireOffsetForComponentType(startComponent.type);
                    position.z += offset;
                    scale.z -= offset;
                }
            }

            if (wire.endTouchesComponent)
            {
                var endComponent = FindComponentAtPosition(circuitData, wire.toASCII);
                if (endComponent != null)
                {
                    float offset = GetWireOffsetForComponentType(endComponent.type);
                    scale.z -= offset;
                }
            }

            // Apply fork/merge offsets
            if (wire.isPartOfFork)
            {
                scale.z -= wireOffsetIfFork;
            }

            if (wire.isPartOfMerge)
            {
                position.z += wireOffsetIfMerge;
            }

            // Apply general wire offset
            if (!wire.startTouchesComponent && !wire.isPartOfMerge)
            {
                position.z += wireOffsetIfWire;
                scale.z -= wireOffsetIfWire;
            }

            if (!wire.endTouchesComponent && !wire.isPartOfFork)
            {
                scale.z -= wireOffsetIfWire;
            }

            wireObject.transform.localPosition = position;
            wireObject.transform.localScale = scale;
        }

        /// <summary>
        /// Finds a component at the specified ASCII position
        /// </summary>
        private Component FindComponentAtPosition(CircuitData circuitData, Vector2Int asciiPosition)
        {
            return circuitData.components.FirstOrDefault(c => c.asciiPosition == asciiPosition);
        }

        /// <summary>
        /// Gets the wire offset value for a specific component type
        /// </summary>
        private float GetWireOffsetForComponentType(string componentType)
        {
            return componentType switch
            {
                "resistor" => wireOffsetIfResistor,
                "lightbulb" => wireOffsetIfLightbulb,
                "battery" => wireOffsetIfBattery,
                "switch" => wireOffsetIfSwitch,
                "fork" => wireOffsetIfFork,
                "merge" => wireOffsetIfMerge,
                _ => 0f
            };
        }

        /// <summary>
        /// Places a component prefab in the scene
        /// </summary>
        private GameObject PlaceComponent(CircuitData circuitData, Component component)
        {
            GameObject prefab = GetPrefabForComponentType(component.type);
            if (prefab == null)
                return null;

            // Instantiate component
            GameObject componentObject = Instantiate(prefab, parentTransform);
            // Calculate position: (asciiPosition.y, 0, asciiPosition.x)
            componentObject.transform.localPosition = new Vector3(component.asciiPosition.y, 0, component.asciiPosition.x);
            componentObject.transform.localRotation = Quaternion.identity;
            
            // Add ComponentData component
            ComponentData componentData = componentObject.AddComponent<ComponentData>();
            componentData.component = component;

            return componentObject;
        }

        /// <summary>
        /// Gets the appropriate prefab for a component type
        /// </summary>
        private GameObject GetPrefabForComponentType(string componentType)
        {
            return componentType switch
            {
                "resistor" => resistorPrefab,
                "lightbulb" => lightbulbPrefab,
                "battery" => batteryPrefab,
                "switch" => switchPrefab,
                "fork" => forkPrefab != null ? forkPrefab : null,
                "merge" => mergePrefab != null ? mergePrefab : null,
                _ => null
            };
        }
    }
}