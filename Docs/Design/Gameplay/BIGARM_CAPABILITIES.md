# The Legger Capabilities

This document captures the intended AI fantasy and behavior model for the Legger (formerly BigARM). It is not the implementation plan for every system yet. It is the target behavior we want the code to grow toward.

## Core Role

The Legger is not just a storage object or a companion follower. His finite loadframe makes carrying a visible expedition role, but cargo remains physically local and access-gated.

He is a mobile field carrier, assistant, scout, and protector—not a safe zone or remote base.

The Legger should feel capable of operating independently for stretches of time, then returning to Booter when the player needs support.

## Primary Behavior

- Follow Booter most of the time.
- Protect Booter when danger appears.
- Scout ahead when the area looks safe enough.
- Harvest resources while away from the player.
- Return to Booter or the Legger's storage when called back.
- Place waypoint markers when he discovers something important.
- Disappear from the immediate play space when he is off doing his own tasks, to preserve the illusion that he is operating elsewhere in the world.

## Behavior Modes

### Follow

The Legger stays close to Booter and acts as a mobile support unit.

This is the default mode during normal travel.

### Protect

When threats are near Booter, the Legger should prioritize defense and positioning over scouting or harvesting.

This mode should interrupt lower-priority tasks.

### Scout

The Legger can move ahead of Booter to search for:

- Resources
- Threats
- Routes
- Points of interest

Scout behavior should feel deliberate, not random wandering.

### Harvest

When the Legger is away, he can gather resources and add them to his own inventory.

This supports the fantasy that he is productive even when the player is not standing next to him.

### Return

The Legger should be callable back when:

- The player needs access to the Legger's storage
- A danger appears ahead
- A task needs doing near Booter

Return behavior should interrupt scouting or harvesting when necessary.

### Hidden Task State

Sometimes the Legger will be logically active but visually absent from the immediate scene.

That hidden state is intentional. It creates the impression that he is somewhere else in the world doing work.

This should be represented as a real AI state, not just an animation trick.

## Waypoints

The Legger may place waypoint markers in the world when he finds something worth remembering.

These markers are part of the world itself, not a pure UI overlay.

The marker system should later support:

- Off-screen direction cues
- Screen-edge indicators
- World-space markers
- Different marker types for threats, resources, routes, and points of interest

The edge-marker UI is intentionally deferred to a later implementation pass.

## AI Design Principles

- The Legger should be autonomous enough that the player trusts him.
- The Legger should not behave like a random follower pet.
- Task switching should be priority-based and understandable.
- Movement should use real navigation and pathfinding.
- Replanning should be interval-based, not frame-based.
- The Legger should not thrash between tasks when the world is stable.
- Visual disappearance should be supported by gameplay logic, not only presentation logic.

## Implementation Direction

The eventual AI should likely separate into these layers:

- Perception: detect danger, resources, and player context.
- Decision-making: choose the active task based on priority.
- Navigation: pathfind to the chosen destination.
- Execution: move, harvest, protect, place markers, or return.
- State persistence: preserve position, inventory, and task-relevant data.

That structure keeps the AI extensible without turning the controller into one large block of logic.

## Current Scope

For the prototype, we do not need every behavior at once.

The near-term goal is to support:

- Follow Booter
- Return when called
- Harvest while away
- Store resources in the Legger's inventory
- Use pathfinding to move through the world

The later goal is to add:

- Threat assessment
- Combat assistance
- Scout routes
- Marker placement
- Off-screen navigation indicators

## Open Research Topics

- Best approach for world-edge and off-screen markers
- Best way to represent the Legger's away state visually and logically
- How to prioritize protection versus scouting versus harvesting
- How much autonomy should be visible to the player at any given time
