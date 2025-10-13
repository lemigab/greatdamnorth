# Great Dam North

## 📌 Overview
Great Dam North is a 3D real time strategy game where each player controls a beaver and is competing to gain control of the map by flooding rivers and building lodges to expand their maple syrup exportation.

## 🕹️ Core Gameplay
- Players control a beaver and try to take over a forest map divided into different regions.
- Each region is connected to others by an intricate river network.
- Each beaver owns a single maple syrup farm in some region on the map. Beavers can build lodges in other regions, selling their own syrup from there. Rivers push syrup exports from farms to lodges in the direction of the river current.
- Beavers can chew down trees and dam rivers on the borders between regions. Dams raise and lower the water level of the upstream and downstream region, respectively. High enough water levels in a region can destroy the lodge there.
- Each beaver’s (i.e. player’s) objective will be to have their own maple syrup sold in all regions of the map. To accomplish this, they will need to strategically place/dismantle dams to destroy enemy lodges, dry up specific rivers, and buy time to expand (i.e. build lodges) faster than their opponents.

## 🎯 Game Type
Real Time Strategy

## 👥 Player Setup
Online multiplayer; each of the 4 players control a beaver character representing a beaver colony in their own corner of the map and play against each other on a server. The game can also be played locally in single-player against AI (bot) players. These AI players will act as the primary NPCs of the game.

## 🤖 AI Design
The enemy NPCs in this game are the AI-controlled beavers. For the first iteration of the game, the AI will be controlled by an FSM and have different actions they can do based on their current state and the state of the game; and will pick at random from the possible actions (move to a location, chew a tree, build a dam, build a lodge, etc). In the second iteration, the AI will have path-finding and decision-making trees that give weights to actions, increasing its difficulty to play against.

#### Beaver FSM contents:
- Idle
- Walk/swim around the map
- Chew tree to get logs
- Build/break dam
- Build a lodge

## 🎬 Scripted Events
- Game ends when one beaver is selling syrup across all map regions
- Contruction of a level 1 dam causes a pond to spawn in the upstream region
- Construction of a level 2 dam causes the upstream region to become flooded, and the downstream region river to dry up
- Beaver gains 'control' of a region when they build a lodge there
- Beaver is eliminated if all the regions surrounding its syrup farm are controlled by opponent beavers
- Beaver gains +1 wood after chewing a tree, and loses -1 wood after building 1 level of dam

## 🌍 Environment
- A hardcoded 3D low-poly map broken into various ‘regions’ like a Risk board. 
- A system of rivers winding through different regions
- Each region is surrounded by an unpassable mound and connected by a river
- A controllable beaver that the player will play as.
- Numerous opponent beavers also traversing the world
- Trees that can be interacted with and chew down
- Maple syrup farms for each beaver in the game
- Dams/lodges that can be built by beavers

## 🎮 Controls
- WASD to move around
- Mouse to move camera view
- E to interact (build dams, chew trees, build lodges, break dams)

## 📂 Project Setup
- Unity 6.2 as core game engine
- Blender to create 3d models of some of the assets like the beavers, terrain, tree, rivers
- Free assets from the unity store if necessary for what cannot be made via blender
- Sounds from free online sources
- C# scripts for beaver controller and AI behaviour

#### Group Information
- CISC 486 Group 18 (Great Dam North)
- Owen Meima (21owm1) - Graphics, world generation
- Gabriel Lemieux (19gml2) - Core gameplay mechanic implementation
- Charlie Kevill (21cmk11) - AI data structures
- All group members will collaborate equally on the intricacies of AI/networking systems.

