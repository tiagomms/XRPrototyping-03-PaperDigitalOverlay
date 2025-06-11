using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CircuitProcessor;

namespace CircuitProcessor
{
    /// <summary>
    /// Handles the assignment of grid positions and wire generation for circuit components.
    /// This is a direct port of the Python circuit_grid_assigner.py script.
    /// </summary>
    public class CircuitGridAssigner : MonoBehaviour
    {
        /// <summary>
        /// Main entry point for processing the circuit data
        /// </summary>
        public CircuitData InitializeGridAssigner(CircuitData data)
        {
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Starting grid assignment with {data.components?.Count ?? 0} components");
            
            if (data == null)
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] ERROR: Input data is null");
                return null;
            }

            if (string.IsNullOrEmpty(data.verbalPlan))
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] ERROR: Verbal plan is empty");
                return null;
            }

            var orderedIds = ParseVerbalPlan(data.verbalPlan);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Parsed {orderedIds.Count} components from verbal plan");

            var positionsMap = AssignPositions(orderedIds);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Assigned positions to {positionsMap.Count} components");

            // Update components with positions while preserving original properties
            var updatedComponents = new List<Component>();
            foreach (var c in data.components)
            {
                if (positionsMap.ContainsKey(c.id))
                {
                    var updatedComp = new Component
                    {
                        id = c.id,
                        type = c.type,
                        value = c.value,
                        gridPosition = positionsMap[c.id],
                        asciiPosition = Vector2Int.zero, // Placeholder for Step 3
                        pixelPosition = Vector2.zero
                    };
                    updatedComponents.Add(updatedComp);
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Updated component {c.id} with grid position {positionsMap[c.id]}");
                }
                else
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] WARNING: Component {c.id} not found in positions map");
                }
            }

            // Generate wires
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Starting wire generation for {updatedComponents.Count} components");
            var wires = GenerateWires(updatedComponents);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Generated {wires.Count} wires");

            // STEP: Normalize all grid positions so minimum is 0 on both axes
            NormalizeGridPositions(updatedComponents, wires);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Normalized all grid positions");

            // Create final output by copying all original data
            var finalOutput = new CircuitData
            {
                components = updatedComponents,
                wires = wires,
                formula = data.formula,
                verbalPlan = data.verbalPlan,
                conditionalBranches = data.conditionalBranches,
                notes = data.notes,
                additionalData = data.additionalData
            };

            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Completed grid assignment. Output contains {finalOutput.components.Count} components and {finalOutput.wires.Count} wires");
            return finalOutput;
        }

        /// <summary>
        /// Normalizes all grid positions so minimum X and Y are 0
        /// </summary>
        private void NormalizeGridPositions(List<Component> components, List<Wire> wires)
        {
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Starting grid position normalization");
            
            // Find minimum X and Y across all components and wires
            var allPositions = new List<Vector2Int>();
            
            // Add component positions
            foreach (var comp in components)
            {
                allPositions.Add(comp.gridPosition);
            }
            
            // Add wire positions
            foreach (var wire in wires)
            {
                allPositions.Add(wire.fromGrid);
                allPositions.Add(wire.toGrid);
            }

            if (allPositions.Count == 0)
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] No positions to normalize");
                return;
            }

            var minX = allPositions.Min(p => p.x);
            var minY = allPositions.Min(p => p.y);

            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Minimum positions - X: {minX}, Y: {minY}");

            // Only normalize if there are negative positions
            if (minX < 0 || minY < 0)
            {
                var offsetX = minX < 0 ? -minX : 0;
                var offsetY = minY < 0 ? -minY : 0;
                
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Applying offset - X: {offsetX}, Y: {offsetY}");

                // Normalize component positions
                foreach (var comp in components)
                {
                    var oldPos = comp.gridPosition;
                    comp.gridPosition = new Vector2Int(oldPos.x + offsetX, oldPos.y + offsetY);
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Normalized component {comp.id}: {oldPos} -> {comp.gridPosition}");
                }

                // Normalize wire positions
                foreach (var wire in wires)
                {
                    var oldFrom = wire.fromGrid;
                    var oldTo = wire.toGrid;
                    wire.fromGrid = new Vector2Int(oldFrom.x + offsetX, oldFrom.y + offsetY);
                    wire.toGrid = new Vector2Int(oldTo.x + offsetX, oldTo.y + offsetY);
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Normalized wire {wire.id}: {oldFrom}->{oldTo} to {wire.fromGrid}->{wire.toGrid}");
                }
            }
            else
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] No normalization needed - all positions are non-negative");
            }
        }

        /// <summary>
        /// Parses the verbal plan into an ordered list of component IDs
        /// </summary>
        private List<string> ParseVerbalPlan(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] ERROR: Empty verbal plan");
                return new List<string>();
            }

            // Replace arrow types with consistent delimiter
            plan = plan.Replace("→", "->").Replace("⇒", "->");

            // TODO: actually return wires in the future (not now)
            // NOTE: right now, just ignore the return part
            plan = plan.Replace(" -> return", "");
            var tokens = plan.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();

            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Parsed verbal plan: {plan}");
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Found {tokens.Count} tokens: {string.Join(", ", tokens)}");
            return tokens;
        }

        /// <summary>
        /// Checks if a token represents a parallel branch
        /// </summary>
        private bool IsParallelBranch(string token)
        {
            return token.StartsWith("[") && token.EndsWith("]");
        }

        /// <summary>
        /// Parses components within a parallel branch
        /// </summary>
        private List<string> ParseParallelBranch(string token)
        {
            // Remove brackets and split by parallel operator
            var inner = token.Trim('[', ']');
            if (inner.Contains("||"))
            {
                return inner.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(c => c.Trim())
                           .ToList();
            }
            else if (inner.Contains("+"))
            {
                return inner.Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(c => c.Trim())
                           .ToList();
            }
            return new List<string> { inner.Trim() };
        }

        /// <summary>
        /// Assigns grid positions based on verbal plan with parallel branch handling
        /// </summary>
        private Dictionary<string, Vector2Int> AssignPositions(List<string> componentsOrder)
        {
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Starting position assignment for {componentsOrder.Count} components");
            
            var positions = new Dictionary<string, Vector2Int>();
            var currentX = 0;
            var currentY = 0;

            for (int i = 0; i < componentsOrder.Count; i++)
            {
                var token = componentsOrder[i];
                if (IsParallelBranch(token))
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Processing parallel branch: {token}");
                    // Handle parallel branch
                    var parallelComponents = ParseParallelBranch(token);
                    var branchCount = parallelComponents.Count;
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Found {branchCount} parallel components");

                    // Add spacing for fork point
                    currentX += 1;

                    // Assign positions to parallel components at current_x
                    for (int j = 0; j < parallelComponents.Count; j++)
                    {
                        var compId = parallelComponents[j];
                        // Calculate Y position based on number of branches
                        var yOffset = (j * 2) - (branchCount - 1);
                        positions[compId] = new Vector2Int(currentX, currentY + yOffset);
                        XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Assigned position {positions[compId]} to parallel component {compId}");
                    }

                    // Add spacing for merge point
                    currentX += 2;
                }
                else
                {
                    // Handle series component
                    positions[token] = new Vector2Int(currentX, currentY);
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Assigned position {positions[token]} to series component {token}");
                    currentX += 1;
                }
            }

            // NOTE: Grid normalization is now done at the end, after wire generation
            // This allows parallel merges to be clearer during wire generation
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Completed position assignment for {positions.Count} components (normalization deferred)");
            return positions;
        }

        /// <summary>
        /// Creates a wire with basic properties
        /// </summary>
        private Wire CreateWire(Vector2Int start, Vector2Int end, int wireId)
        {
            return new Wire
            {
                id = $"W{wireId:D2}",
                fromGrid = start,
                toGrid = end,
                fromASCII = Vector2Int.zero, // ASCII position placeholder
                toASCII = Vector2Int.zero,   // ASCII position placeholder
                fromPixel = Vector2.zero,
                toPixel = Vector2.zero,
                isHorizontal = start.y == end.y
            };
        }

        /// <summary>
        /// Checks if wire has valid distance between fromGrid and toGrid
        /// </summary>
        private bool IsValidWire(Wire wire)
        {
            var fromX = wire.fromGrid.x;
            var fromY = wire.fromGrid.y;
            var toX = wire.toGrid.x;
            var toY = wire.toGrid.y;

            // Calculate Manhattan distance
            var distance = Math.Abs(toX - fromX) + Math.Abs(toY - fromY);
            return distance >= 1;
        }

        /// <summary>
        /// Removes duplicate wires (same fromGrid and toGrid)
        /// </summary>
        private List<Wire> RemoveDuplicateWires(List<Wire> wires)
        {
            var seen = new HashSet<(Vector2Int, Vector2Int)>();
            var uniqueWires = new List<Wire>();

            foreach (var wire in wires)
            {
                var key = (wire.fromGrid, wire.toGrid);
                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    uniqueWires.Add(wire);
                }
            }

            return uniqueWires;
        }

        /// <summary>
        /// Analyzes wire connections to identify forks and merges
        /// </summary>
        private (Dictionary<(int, int), List<Wire>> from, Dictionary<(int, int), List<Wire>> to) AnalyzeWireConnections(List<Wire> wires)
        {
            var connections = (
                from: new Dictionary<(int, int), List<Wire>>(),
                to: new Dictionary<(int, int), List<Wire>>()
            );

            foreach (var wire in wires)
            {
                var fromKey = (wire.fromGrid.x, wire.fromGrid.y);
                var toKey = (wire.toGrid.x, wire.toGrid.y);

                if (!connections.from.ContainsKey(fromKey))
                    connections.from[fromKey] = new List<Wire>();
                if (!connections.to.ContainsKey(toKey))
                    connections.to[toKey] = new List<Wire>();

                connections.from[fromKey].Add(wire);
                connections.to[toKey].Add(wire);
            }

            return connections;
        }

        /// <summary>
        /// Determines if the circuit has any parallel branches
        /// </summary>
        private bool HasParallelBranches(List<Component> components)
        {
            // Group components by X position
            var xGroups = components.GroupBy(c => c.gridPosition.x)
                                  .ToDictionary(g => g.Key, g => g.ToList());
            
            // Check if any X position has more than one component
            return xGroups.Any(group => group.Value.Count > 1);
        }

        /// <summary>
        /// Generates wires between components based on their positions
        /// </summary>
        private List<Wire> GenerateWires(List<Component> components)
        {
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Starting wire generation for {components.Count} components");
            
            var wires = new List<Wire>();
            var wireId = 1;

            // Sort components by X position (and Y position for same X)
            var sortedComponents = components.OrderBy(c => c.gridPosition.x)
                                           .ThenBy(c => c.gridPosition.y)
                                           .ToList();
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Sorted components by position");

            // Group components by X position to identify parallel branches
            var xGroups = sortedComponents.GroupBy(c => c.gridPosition.x)
                                        .ToDictionary(g => g.Key, g => g.ToList());
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Grouped components into {xGroups.Count} X positions");

            // Check if circuit has parallel branches
            bool hasParallelBranches = HasParallelBranches(components);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Circuit has parallel branches: {hasParallelBranches}");

            // Process each X position in order
            var xPositions = xGroups.Keys.OrderBy(x => x).ToList();
            for (int i = 0; i < xPositions.Count - 1; i++)
            {
                var currentX = xPositions[i];
                var nextX = xPositions[i + 1];
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Processing X positions {currentX} -> {nextX}");

                // Get components at current and next X positions
                var currentComps = xGroups[currentX];
                var nextComps = xGroups[nextX];

                // If we have parallel branches at current X
                if (currentComps.Count > 1)
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Processing parallel branch at X={currentX} with {currentComps.Count} components");
                    // Create fork point at X+1
                    var forkX = currentX + 1;
                    var forkY = 1; // Middle Y position

                    // Connect each parallel component to fork point
                    foreach (var comp in currentComps)
                    {
                        // Horizontal wire to fork point
                        wires.Add(CreateWire(
                            comp.gridPosition,
                            new Vector2Int(forkX, comp.gridPosition.y),
                            wireId++
                        ));

                        // Vertical wire to fork point
                        wires.Add(CreateWire(
                            new Vector2Int(forkX, comp.gridPosition.y),
                            new Vector2Int(forkX, forkY),
                            wireId++
                        ));
                    }

                    // Connect fork point to next components
                    foreach (var nextComp in nextComps)
                    {
                        // Horizontal wire from fork point
                        wires.Add(CreateWire(
                            new Vector2Int(forkX, forkY),
                            new Vector2Int(nextX - 1, forkY),
                            wireId++
                        ));

                        // Vertical wire to next component
                        wires.Add(CreateWire(
                            new Vector2Int(nextX - 1, forkY),
                            new Vector2Int(nextX - 1, nextComp.gridPosition.y),
                            wireId++
                        ));

                        // Final horizontal wire to component
                        wires.Add(CreateWire(
                            new Vector2Int(nextX - 1, nextComp.gridPosition.y),
                            nextComp.gridPosition,
                            wireId++
                        ));
                    }
                }
                else
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Processing series connection at X={currentX}");
                    // Single component at current X
                    var currentComp = currentComps[0];

                    // For each component at next X
                    foreach (var nextComp in nextComps)
                    {
                        // Horizontal wire to X-1 of next component
                        wires.Add(CreateWire(
                            currentComp.gridPosition,
                            new Vector2Int(nextX - 1, currentComp.gridPosition.y),
                            wireId++
                        ));

                        // Vertical wire to match Y of next component
                        wires.Add(CreateWire(
                            new Vector2Int(nextX - 1, currentComp.gridPosition.y),
                            new Vector2Int(nextX - 1, nextComp.gridPosition.y),
                            wireId++
                        ));

                        // Final horizontal wire to component
                        wires.Add(CreateWire(
                            new Vector2Int(nextX - 1, nextComp.gridPosition.y),
                            nextComp.gridPosition,
                            wireId++
                        ));
                    }
                }
            }

            // TODO: Implement proper parallel branch merging logic
            // Current implementation assumes max 1 parallel branch and merges at a fixed position.
            // Future enhancement needed: 
            // - Detect where parallel branches end (not just fixed positions)
            // - Handle multiple parallel branches in a single circuit
            // - Implement intelligent merge point detection based on circuit topology
            // - Consider electrical properties when determining merge points
            
            // Only add final merge junction if we have actual parallel branches
            // AND the last components need to be merged
            if (hasParallelBranches)
            {
                var lastComps = xGroups[xPositions.Last()];
                if (lastComps.Count > 1)
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Adding final merge junction for {lastComps.Count} parallel components");
                    var mergeX = xPositions.Last() + 1;
                    var mergeY = 0; // Use Y=0 as the merge point (simplified assumption)

                    foreach (var comp in lastComps)
                    {
                        // Horizontal wire to merge point
                        wires.Add(CreateWire(
                            comp.gridPosition,
                            new Vector2Int(mergeX, comp.gridPosition.y),
                            wireId++
                        ));

                        // Vertical wire to merge point (only if not at Y=0)
                        if (comp.gridPosition.y != mergeY)
                        {
                            wires.Add(CreateWire(
                                new Vector2Int(mergeX, comp.gridPosition.y),
                                new Vector2Int(mergeX, mergeY),
                                wireId++
                            ));
                        }
                    }
                }
                else
                {
                    XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] No final merge junction needed - single component at end");
                }
            }
            else
            {
                XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] No final merge junction needed - series circuit");
            }

            // Filter out invalid wires (distance < 1)
            wires = wires.Where(IsValidWire).ToList();
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Filtered to {wires.Count} valid wires");

            // Remove duplicate wires
            wires = RemoveDuplicateWires(wires);
            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Removed duplicates, final wire count: {wires.Count}");

            // Now that all wires are created, calculate metadata
            var connections = AnalyzeWireConnections(wires);
            foreach (var wire in wires)
            {
                if (wire.isHorizontal)
                {
                    var fromKey = (wire.fromGrid.x, wire.fromGrid.y);
                    var toKey = (wire.toGrid.x, wire.toGrid.y);

                    // Check if wire touches components
                    wire.startTouchesComponent = components.Any(
                        comp => comp.gridPosition.x == wire.fromGrid.x &&
                               comp.gridPosition.y == wire.fromGrid.y
                    );
                    wire.endTouchesComponent = components.Any(
                        comp => comp.gridPosition.x == wire.toGrid.x &&
                               comp.gridPosition.y == wire.toGrid.y
                    );

                    // Check if wire is part of fork or merge
                    wire.isPartOfFork = connections.from.ContainsKey(toKey) &&
                                      connections.from[toKey].Count > 1;
                    wire.isPartOfMerge = connections.to.ContainsKey(fromKey) &&
                                       connections.to[fromKey].Count > 1;
                }
            }

            // Renumber wire IDs in order
            for (int i = 0; i < wires.Count; i++)
            {
                wires[i].id = $"W{i + 1:D2}";
            }

            XRDebugLogViewer.Log($"[{nameof(CircuitGridAssigner)}] Completed wire generation with {wires.Count} wires");
            return wires;
        }
    }
}