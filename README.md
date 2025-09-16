# Great Dam North

## 📌 Overview
Great Dam North is a 3D real time strategy game where each player controls a beaver and is competing to gain control of the map by flooding rivers and building lodges to expand their maple syrup exportation.

## 🕹️ Core Gameplay
- Players control a beaver and try to take over a forest map divided into different regions.
- Each region is connected to others by an intricate river network.
- Each beaver owns a single maple syrup farm in some region on the map. Beavers can build lodges in other regions to sell their own syrup there. Rivers push syrup exports from farms to lodges in the direction of the river current.
- Beavers can flood regions by chewing down trees and damming rivers.
- Each beaver’s (i.e. player’s) objective will be to have their own maple syrup sold in all regions of the map. To accomplish this, they will need to strategically place/dismantle dams to destroy opponent lodges and create space for their own.

## 🎯 Game Type
Real Time Strategy

## 👥 Player Setup
Online multiplayer; each of the 4 players control a beaver character representing a beaver colony in their own corner of the map and play against each other on a server. The game can also be played locally in single-player against AI (bot) players.

## 🤖 AI Design
The enemies in this game are the AI-controlled beaver players. For the first iteration of the game the AI will be controlled by an FSM and have different actions they can do based on their current state and the state of the game and will pick at random from the possible actions (move to a location, chew a tree, build a dam, build a lodge, etc). In the second iteration the AI will have path finding and decision-making trees that give weights to actions, increasing its difficulty to play against.

### ⚙️Beaver FSM contents:
- Idle
- Walk around the map
- Chew tree to get wood logs
- Build Dam
- Break Dam
- Swim into the next region through the river (same as walking around?)
- Build a lodge

## 🎬 Scripted Events
- Game over if one beaver remains 
- Pond is made on a region if a level 1 dam is constructed on its river output
- Region floods if level 2 dam is built on its output river, a lodge built on a region now flooded will break
- Region’s river dries up if the river input is blocked by a level 2 dam
- Player is eliminated if all the regions surrounding its syrup farm are controlled by other users
- Player gains control of a region when they build a lodge on it once there's a pond on the region
- Tree disappears and is cut down when a beaver chews it to use the wood to build dams
- Trees reappear on a region after a certain duration of time


## 🌍 Environment
- A hardcoded 3D low-poly map broken into various ‘regions’ like a Risk board. 
- A system of rivers winding through different regions
- Each region is surrounded by an unpassable mound and connected by a river
- A controllable beaver that the player will play as.
- Numerous opponent beavers also traversing the world
- Trees that can be interacted with and chew down
- Maple syrup farms for each beaver in the game
- Dams that can be built by beavers
- Lodges that can be built by beavers

## 🧪 Physics Scope
- Rigid body on beaver
- Collider for Trees, Dams, Lodges, Maple Syrup Farm

## 🧠 FSM Scope
- State machines implemented for AI beaver characters
- Event driven transitions through unity and C# events

## 🧩 Systems and Mechanics
- Each beaver has a syrup farm somewhere on the map.
- Syrup exports are pushed forward in the direction of river currents going out of the farm.
- Beavers can build lodges on regions to gain control of it and start exporting to that region
- Beavers can build dams to create ponds (level 1) or flood (level 2) regions
- Regions after a level 2 dam have dry rivers preventing the flow of maple syrup and water
- Eliminate players by controlling all the regions surrounding their syrup farm
- Win by being the last player standing

## 🎮 Controls (proposed)
- WASD to move around
- Mouse to move camera view
- E to interact (build dams, chew trees, build lodges, break dams)

## 📂 Project Setup aligned to course topics
- Unity (6.2 - 6000.2.4f1)
- Blender to create 3d models of some of the assets like the beavers, terrain, tree, rivers
- Free assets from the unity store whenever we find something that could be used in the game to save on animation time
- Free sounds from online sources
- Create some using free ai sound tools available online
- C# scripts for beaver controller
- GitHub repository with regular commits, readme files and comments when necessary
