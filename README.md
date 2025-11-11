# Great Dam North - Assignment 3

## Controls

### Player Controls

- **WASD** - Move beaver (movement is relative to camera orientation)
- **C** - Chew tree or break dam
- **E** - Build dam (requires holding a log/branch)
- **Space** - When near a dam to jump over it

### Camera Controls

- **Mouse** - Pan camera around the scene
- **Mouse Scroll** - Zoom in/out
- **1** - Switch to player camera view
- **2** - Switch to next AI camera in list

## AI System

### Finite State Machine

[View Assignment 2 README](PreviousAssignments/A2_README.md)

#### AI Decision Logic

- AI prioritizes chewing trees for branches when available
- When no trees are available break enemy dams in other territories to get branches
- AI will only build dams in its territory and break dams in enemy territory
- Cannot chew trees or break dams while holding a log (must build dam first)
- Cannot build dam without holding a log

### Pathfinding System

- **Unity NavMesh System** - AI uses Unity's NavMeshAgent for efficient pathfinding
- **Path Distance Calculation** - AI calculates shortest path distance rather than straight-line distance when finding nearest targets
- **Water Prioritization** - AI prioritizes water routes since beavers move twice as fast in water (8 units/sec vs 4 units/sec on land)
- **Dynamic Obstacle Avoidance** - All beavers use NavMeshObstacle components that dynamically carve the NavMesh, ensuring AI beavers automatically avoid each other and the player

## World Generation

### Procedural Hexagonal World

- World is procedurally generated using a base hexagonal tile template
- Map is divided into six areas with their own river system
- The river system connects the tiles in an area together
- Each area connects to others via roads between tiles
- Beavers spawn in designated areas at game start

## Gameplay Mechanics

### Dam System

- Beavers can build dams using logs/branches collected from trees
- Dams can be built in own territory to flood tiles
- Dams can be broken in enemy territory to get logs and unflood tiles
- Dam levels affect water height in upstream tiles

### Log System

- Beavers chew trees to collect logs/branches
- Must hold a log/branch to build dams
- Breaking dams also provides logs/branches

## Demo Video

## CISC 486 Group 18

- Owen Meima (21owm1)
- Gabriel Lemieux (19gml2)
- Charlie Kevill (21cmk11)
