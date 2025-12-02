# Great Dam North - Assignment 4 (Networking)

![alt text](greatdamnorth.png)

This assignment expands on the generated world from A3 by allowing multiple players within a single game session, competing with each other for higher score. Additionally, the game itself and all essential play mechanics have been implemented.

### Multiplayer Testing Guide

#### Initial Setup
1. Open Multiplayer Tools: Click Window → Multiplayer → Multiplayer Play Mode. This opens the Multiplayer Play Mode window

2. Configure Virtual Players: In the Multiplayer Play Mode window, you can set up virtual Unity GUIs to test multiplayer. Check the checkbox for each player you want (e.g., check 2 boxes for 2 players). Press Yes if prompted to build

3. Wait for Setup: Wait for the GUI to load and initialize

#### Starting the Host
4. Start Main Unity GUI: Press Play on the main Unity GUI (the original Game window)

5. Start Host Server: In the Hierarchy, find DontDestroyOnLoad → NetworkManager.
Select the NetworkManager GameObject. In the Inspector, click "Start Host" button
The world will be constructed and beavers will spawn

#### Connecting a Client
6. Open Virtual Unity GUI: In the virtual Unity GUI window, click Layout dropdown. Check Hierarchy and Inspector to view them.

7. Connect Client: In the Hierarchy, find NetworkManager. Select the NetworkManager GameObject In the Inspector, click "Start Client" button. You should now be connected to the host server

8. Verification: Both windows should show the same world. Both players should see each other's beavers. Test log chewing, dam building, and other multiplayer features

## Demo Video
- [Video Link](https://drive.google.com/file/d/1ZDnWCCWZCOHQEjJAoA-CBs207-exMHxl/view?usp=sharing)

## CISC 486 Group 18
- Owen Meima (21owm1)
- Gabriel Lemieux (19gml2)
- Charlie Kevill (21cmk11)


<br><br>

![alt text](gpguide.png)

<br>

## World Map Generation

#### Procedural Hexagonal World

- World is procedurally generated using a base hexagonal tile template
- Map is divided into six regions with their own river system
- The river system connects the tiles in a region together
- Each region connects to others via a number of roads
- A syrup farm spawns at starting point of each river system
- Beavers spawn in their own syrup farm at game start

<br>

## Gameplay Mechanics

The core gameplay involves you playing as a beaver who wants to export his farmed maple syrup to as much of the forest as possible. You have the ability to build a variety of structures to help you in this task. Below is a detailed list of all core mechanisms which exist in the core gameplay.

#### Chewable Trees

- Beavers chew (brown) trees to collect branches.
- Beavers must be holding a branch to build structures.

#### Beaver Dam

- Beavers can build dams using branches on any river section between two tiles.
- Beavers can obtain branches by dismantling a dam.
- Dams raise the water level of the upstream river, flooding tiles.

#### Beaver Mound

- Beavers can build mounds using branches on any road between two tiles.
- Beavers can obtain branches by dismantling a mound.
- The beaver who built a specific mound will 'own' in until it is dismantled.

#### Beaver Lodge

- Beavers can build lodges using branches in any tile that is at least partially flooded.
- Lodges will be automatically dismantled with no refund, if their tile stops being flooded.
- The beaver who built a specific lodge will 'own' in until it is dismantled.

#### Syrup Farm

- Each beaver spawns at a home maple syrup farm which they 'own'.
- Syrup farms will push syrup exports along roads and rivers towards lodges of the same owner.

#### Trade Route

- Maple syrup exports from a farm will travel along trade routes, those being the **shortest** possible path towards each lodge of the same owner beaver.
- Exports may travel along rivers in the direct of current (outward from that river's syrup farm), or along roads in either direction.
- Exports from a farm may only travel along roads which have a mound of the same owner. That is, if Beaver #1 has built a mound on a certain road, then only they may push exports along it. If no mounds exist on a road, no beavers may push exports along it.
- These travel paths may **not** include opponent lodges. That is, an opponent lodge will block a beaver's trade route until it is dismantled.
- No trade route may be a complete subset of another trade route.

#### Trade Scoring

- Every several seconds, each beaver will gain 1 point for each tile along at least one of their own trade routes. For example, if Beaver #1 has 3 trades routes collectively covering 5 tiles, and Beaver #2 has one trade route collectively covering 6 tiles, then each beaver will gain 5 and 6 points, respectively.
- Trade scoring may only increment. Trade scoring does not end at any time and the player(s) may choose when to consider a round over/won.

<br>

## AI System

The AI System implemented in A3 has been preserved during the implementation of the multiplayer framework. A link to the README of that assignment, which described all FSM states/transitions and other AI components, may be accessed with the link below. A singleplayer version of the game including AIs is located in SyrupFarmingScene in this branch.

[View Assignment 3 README](PreviousAssignments/A3_README.md)

<br>

## Controls

#### Player Controls

- **WASD** - Move beaver (movement is relative to camera orientation)
- **C** - Chew tree, dam, or mound
- **E** - Build dam, mound, or lodge (requires holding a branch)
- **Space** - When near a dam to jump over it

#### Camera Controls

- **Mouse** - Pan camera around the scene
- **Mouse Scroll** - Zoom in/out
