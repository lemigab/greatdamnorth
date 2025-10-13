# Assignment 2 - FSM Machine Enemy

## FSM
<img width="503" height="541" alt="Screenshot 2025-10-12 at 5 18 43 PM" src="https://github.com/user-attachments/assets/6ebf5a6b-3a7b-4041-b79f-83d1fed4152c" />

## FSM States Description
When the game starts, the AI is put into the Patrol_With_No_Log state. From here, it checks if there is a dam built in its own territory. If there is one, the AI goes to the dam and enters the Break_Dam state where it breaks one level of the dam and receives a log in its mouth. This then takes the AI into the Patrol_With_Log state. From here, it goes to the player-side dam and enters the Build_Dam state. This state builds one level of dam in the player’s dam, taking the log from the AI’s mouth. This then takes it back to the Patrol_With_No_Log state.

Alternatively, if there are no dam levels in the AI’s dam, it will go to the nearest tree to itself, entering the Chew_Tree state. In this state the AI will chew the tree, breaking it, before transitioning to the Patrol_With_Log state. From here, it goes to the player-side dam and enters the Build_Dam state. This state builds one level of dam in the player’s dam, taking the log from the AI’s mouth. This then takes it back to the Patrol_With_No_Log state.

The AI will only break a dam in its own territory and will only build a dam in the player’s territory. It also prioritizes breaking a dam in its own territory over chewing a tree as both will give it a log but breaking the dam gives other benefits. The AI cannot break a dam or chew a tree while there is a log in its mouth, so it will complete building the dam before checking if there is a dam in its territory again.

## Demo Video
https://drive.google.com/file/d/1zi2KMyx2k87uFZ6gZ8g4rFiyfTBkeabk/view?usp=drive_link

## Group Members
CISC 486 Group 18 (Great Dam North)
- Owen Meima (21owm1)
- Gabriel Lemieux (19gml2) 
- Charlie Kevill (21cmk11)
