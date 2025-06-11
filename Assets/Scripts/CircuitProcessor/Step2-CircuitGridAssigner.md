## 🟦 Step 2: Grid Layout Assignment and Wire Definition

This step receives the JSON with the structural circuit plan and returns a fully defined logical grid.

### 🧾 Step 2 I/O Structure

**Input:**
- Full Step 1 output JSON (`components`, `formula`, etc.)

**Output:**
- To our input JSON structure, add:
  - to all `components`:
    - `gridPosition`
    - `asciiPosition` (placeholder, calculated in Step 3)
    - `pixelPosition` placeholder
  - `wires` array: with routing, metadata, and from/to positions

### ✅ Component Grid Mapping

Each component must now receive a `gridPosition` field in the format:

***json***
"gridPosition": [x, y]

This reflects the logical layout of the circuit, not the visual position.

* Series paths should increment along the X-axis
* Parallel branches should vary along the Y-axis
* IDs must not be changed from Step 1

⚠️ Every time a parallel branch begins or ends, apply a +2 increment to the X-axis in gridPosition to allow wire routing space. This must be reflected in both the components and the wires.

Each component **must** also include:

* `asciiPosition`: placeholder with default value `[0, 0]`. Actual values are calculated in Step 3.
* `pixelPosition`: calculated later (in Step 4), but placeholder must be present with default value of `[0, 0]` in this step. Actual values are calculated in Step 4.

> Step 3 will compute an `asciiPosition` per component, based strictly on each element's `gridPosition`. This mapping is deterministic.
> Step 4 will compute a `pixelPosition` per component, based strictly on each element's `asciiPosition`. This mapping is deterministic.

---

### 🧭 Grid Position Assignment for Parallel Layouts

To support proper orthogonal wiring of **parallel branches**, the following spatial rules must be followed:

1. Components placed in **series** increase along the **X-axis** (`gridX += 1`).
2. Components placed in **parallel** stay on the same X but vary in **Y-axis** (e.g., `gridY = 0`, `1`, `2`, etc.).
3. Whenever a parallel branch is detected, it must apply a symmetric horizontal X-offset:
  - `+2` units to the `gridPosition.x` at the **start** of each branch (fork point),
  - and `+2` units again to the `gridPosition.x` of the **rejoining** point (merge point).

⚠️ This spacing must be reflected in both:
- `gridPosition` of the **components**
- and the `fromGrid`/`toGrid` of the **wires**

This ensures that:
- All branches align cleanly without overlapping wires.
- ASCII diagrams and pixel renderings preserve orthogonal routing.
- Rejoins happen at consistent horizontal levels.

Failure to offset the component placement will cause wires to route diagonally, which is invalid in ASCII logic.

---

### 🔌 Wire Path Definition

> All wires must route using strictly horizontal and vertical paths. Diagonal connections are prohibited.

If a wire needs to make an L-shaped path, it must be split into **two orthogonal wires** (horizontal + vertical). These can optionally meet at an **implicit junction**.

Each wire must connect components using their `gridPosition`, and also include:
- `fromGrid` and `toGrid`
- `fromASCII` and `toASCII` (placeholders with default value `[0, 0]`, calculated in Step 3)
- `fromPixel` and `toPixel` (placeholders at this stage)

> Step 3 will compute `fromASCII` and `toASCII` per wire, based strictly on each element's `fromGrid` and `toGrid` respectively. This mapping is deterministic.
> Step 4 will compute a `fromPixel` and `toPixel` per wire, based strictly on each element's `fromASCII` and `toASCII` respectively. This mapping is deterministic.

* Wires must connect only between defined `gridPosition` components
* Diagonal paths must be broken into at least three orthogonal wires (horizontal + vertical + horizontal)
* Wire list is mandatory for circuit to be rendered in future steps

#### ➕ Wire Metadata Fields (Horizontal Wires)

During Step 2, each wire object in the JSON must be extended with the following additional fields:

- `isHorizontal`: boolean
  - `true` if the wire only spans horizontally (i.e. only the X axis changes between `fromGrid` and `toGrid`)
  - `false` otherwise

If the wire is horizontal (`isHorizontal: true`), then you must also compute:

- `startTouchesComponent`: boolean
  - `true` if any component's `gridPosition` exactly matches the wire's `fromGrid`
  - `false` otherwise

- `endTouchesComponent`: boolean
  - `true` if any component's `gridPosition` exactly matches the wire's `toGrid`
  - `false` otherwise

- `isPartOfFork`: boolean
  - `true` if the wire's `toGrid` value matches the `fromGrid` of two or more other wires
  - `false` otherwise

- `isPartOfMerge`: boolean
  - `true` if the wire's `fromGrid` value matches the `toGrid` of two or more other wires
  - `false` otherwise

> 🧠 These fields are used to disambiguate wire rendering logic in Step 3.

All these fields must be placed **alongside each wire object** in the `wires` array inside the Step 2 JSON output. They must not be stored in separate structures.

##### 🧷 Mandatory Metadata Fields for Horizontal Wires
For every wire where `isHorizontal` is `true`, the following fields are required and must always be included in the JSON output:

- `isHorizontal`: must be explicitly set to `true`
- `startTouchesComponent`: whether the wire's starting point touches a component gridPosition
- `endTouchesComponent`: whether the wire's ending point touches a component gridPosition
- `isPartOfFork`: whether this wire originates from a branching point
- `isPartOfMerge`: whether this wire ends at a merging point

Even if these values are all `false`, they must still be declared. Omitting them for horizontal wires is a protocol violation.

##### 🎯 Contact Detection Clarification
If a wire starts or ends **at the same gridPosition as any component**, the corresponding contact flags must be set to `true`.

For example:
- If a wire's `toGrid` is `[2, 0]` and there is a resistor with `gridPosition: [2, 0]`, then `endTouchesComponent: true`
- If a wire's `fromGrid` is `[2, 0]` and there is a resistor with `gridPosition: [2, 0]`, then `startTouchesComponent: true`

All wires must follow this logic regardless of merge/fork presence.

##### 🔄 Wire Processing Rules

1. **Wire ID Generation**:
   - Each wire must have a unique ID in the format "W01", "W02", etc.
   - IDs must be assigned sequentially based on wire creation order

2. **Wire Deduplication**:
   - Duplicate wires (same fromGrid and toGrid) must be removed
   - Only the first occurrence of a wire should be kept

3. **Wire Distance Validation**:
   - The Manhattan distance between fromGrid and toGrid must be >= 1
   - Wires with distance < 1 must be discarded

4. **Y-axis Normalization**:
   - After all components are placed, y positions must be normalized
   - The minimum y value must be adjusted to 0
   - All component and wire positions must be adjusted accordingly

***json***
{
  "fromGrid": [x1, y1],
  "toGrid": [x2, y2],
  "fromASCII": [00 b1],  // derived in Step 3
  "toASCII": [0 0],    // derived in Step 3
  "fromPixel": [px1, py1], // derived in Step 4
  "toPixel": [px2, py2]    // derived in Step 4
  "isHorizontal": true/false,
  "startTouchesComponent": true/false,
  "endTouchesComponent": true/false,
  "isPartOfFork": true/false,
  "isPartOfMerge": true/false,
}

### 🔄 Positional Field Notes (Clarification)

- `pixelPosition`, `fromPixel`, and `toPixel` fields are **placeholders only** in Step 2.  
  These values will be computed in **Step 4** after pixel coordinate extraction and scaling.

- `asciiPosition` is a placeholder in Step 2 with default value `[0, 0]`.

- `fromASCII` and `toASCII` are placeholders in Step 2 with default value `[0, 0]`.

These calculated fields ensure consistency in ASCII rendering, and avoid reliance on raw pixel values too early in the pipeline.

--- 

### ✅ Canonical Examples Reference

#### ⚡ Example 1: Basic Series Circuit

##### ASCII Layout

   V01  R01  L01
    :----:----:


Grid positions:

* V01: [0,0]
* R01: [1,0]
* L01: [2,0]

##### Example JSON Output (Step 2)

{
"components": [
{ "id": "V01", "type": "battery", "gridPosition": [0,0], "asciiPosition": [0,0], "pixelPosition": [0,0] },
{ "id": "R01", "type": "resistor", "gridPosition": [1,0], "asciiPosition": [0,0], "pixelPosition": [0,0] },
{ "id": "L01", "type": "lightbulb", "gridPosition": [2,0], "asciiPosition": [0,0], "pixelPosition": [0,0] }
],
"wires": [
{ "id": "W01", "fromGrid": [0,0], "toGrid": [1,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "connectsToComponentStart": true, "connectsToComponentEnd": true, "isPartOfFork": false, "isPartOfMerge": false },
{ "id": "W02", "fromGrid": [1,0], "toGrid": [2,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "connectsToComponentStart": true, "connectsToComponentEnd": true, "isPartOfFork": false, "isPartOfMerge": false }
]
}

---

#### ⚡ Example 2: Basic Parallel Circuit

##### ASCII Layout

            R01            
        -----:----         
        |        |         
   V01  |        |  L01    
    :---+        +---:     
        |        |         
        |   R02  |         
        -----:----         



Grid positions:

* V01: [0,1]
* R01: [2,0]
* R02: [2,2]
* L01: [4,1]

##### Example JSON Output (Step 2)

{
"components": [
{ "id": "V01", "type": "battery", "value": 5, "gridPosition": [0,1], "asciiPosition": [0,0], "pixelPosition": [0,0] },
{ "id": "R01", "type": "resistor", "value": 10, "gridPosition": [2,0], "asciiPosition": [0,0], "pixelPosition": [0,0] },
{ "id": "R02", "type": "resistor", "value": 10, "gridPosition": [2,2], "asciiPosition": [0,0], "pixelPosition": [0,0] },
{ "id": "L01", "type": "lightbulb", "value": 0, "gridPosition": [4,1], "asciiPosition": [0,0], "pixelPosition": [0,0] }
],
"wires": [
{ "id": "W01", "fromGrid": [0,1], "toGrid": [1,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": true, "isPartOfMerge": false },
{ "id": "W02", "fromGrid": [1,1], "toGrid": [1,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
{ "id": "W03", "fromGrid": [1,1], "toGrid": [1,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
{ "id": "W04", "fromGrid": [1,0], "toGrid": [2,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": false },
{ "id": "W05", "fromGrid": [1,2], "toGrid": [2,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": false },
{ "id": "W06", "fromGrid": [2,0], "toGrid": [3,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": false, "isPartOfMerge": false },
{ "id": "W07", "fromGrid": [2,2], "toGrid": [3,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": false, "isPartOfMerge": false },
{ "id": "W08", "fromGrid": [3,0], "toGrid": [3,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
{ "id": "W09", "fromGrid": [3,2], "toGrid": [3,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
{ "id": "W10", "fromGrid": [3,1], "toGrid": [4,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": true }
],
  "formula": "V01 / (1 / (1/R01 + 1/R02) + L01)",
  "verbalPlan": "V01 → [ R01 || R02 ] → L01 → return",
}

#### ⚡ Example 3: Asymmetric Parallel Branches

##### ASCII Layout

            R01  R02            
        -----:----:----         
        |             |         
   V01  |             |  L01    
    :---+             +---:     
        |             |         
        |   R03       |         
        -----:---------         



Grid positions:

* V01: [0,1]
* R01: [2,0]
* R02: [3,0]
* R03: [2,2]
* L01: [5,1]

##### Example JSON Output (Step 2)

{
  "components": [
    { "id": "V01", "type": "battery", "value": 5, "gridPosition": [0,1], "asciiPosition": [0,0], "pixelPosition": [0,0] },
    { "id": "R01", "type": "resistor", "value": 10, "gridPosition": [2,0], "asciiPosition": [0,0], "pixelPosition": [0,0] },
    { "id": "R02", "type": "resistor", "value": 10, "gridPosition": [2,1], "asciiPosition": [0,0], "pixelPosition": [0,0] },
    { "id": "R03", "type": "resistor", "value": 10, "gridPosition": [2,2], "asciiPosition": [0,0], "pixelPosition": [0,0] },
    { "id": "L01", "type": "lightbulb", "value": 0, "gridPosition": [4,1], "asciiPosition": [0,0], "pixelPosition": [0,0] }
  ],
  "wires": [
    { "id": "W01", "fromGrid": [0,1], "toGrid": [1,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": true, "isPartOfMerge": false },
    { "id": "W02", "fromGrid": [1,1], "toGrid": [1,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
    { "id": "W03", "fromGrid": [1,1], "toGrid": [1,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
    { "id": "W04", "fromGrid": [1,0], "toGrid": [2,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": false },
    { "id": "W05", "fromGrid": [1,2], "toGrid": [2,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": false },
    { "id": "W06", "fromGrid": [2,0], "toGrid": [3,0], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": false, "isPartOfMerge": false },
    { "id": "W07", "fromGrid": [2,2], "toGrid": [3,2], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": true, "endTouchesComponent": false, "isPartOfFork": false, "isPartOfMerge": false },
    { "id": "W08", "fromGrid": [3,0], "toGrid": [3,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
    { "id": "W09", "fromGrid": [3,2], "toGrid": [3,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0], "isHorizontal": false },
    { "id": "W10", "fromGrid": [3,1], "toGrid": [4,1], "fromASCII": [0,0], "toASCII": [0,0], "fromPixel": [0,0], "toPixel": [0,0],
      "isHorizontal": true, "startTouchesComponent": false, "endTouchesComponent": true, "isPartOfFork": false, "isPartOfMerge": true }
  ],
  "formula": "V01 / (1 / (1/(R01 + R02) + 1/R03) + L01)",
  "verbalPlan": "V01 → [ R01 + R02 || R03 ] → L01 → return",
}

---

✅ These three examples must be used to test and validate all future Step 2 logic.
We treat these as canonical patterns for grid generation.

More complex examples must build upon this logic.

---

### 🔒 Enforcement Reminder

All future generated layouts **must exactly match** the grid spacing, offsets, and structure of Examples 1–3.  
Failure to apply the +2 X-axis increment on both forks and merges in parallel layouts will result in **invalid wiring logic**. These examples are not illustrative — they are **prescriptive**.

-----------------------------------------------------------------------------------------------------------
