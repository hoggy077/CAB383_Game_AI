# CAB383 | Game AI
## Assignment 1 | A-star & FSM's
### A-star Algorithm
For the A-star algorithm, I revisited the Fibonnaci Heap with the knowledge I may use it one day.
My implementation is akin to a node based graph, requiring "walking" in order to effectively. Due to the static nature of the nodes from the provided project base I felt it unnecessary to implement a method to update the values of a given node.

### FSM
For the Finite State Machine, I took a object literal approach, creating an FSM Manager that stored the available steps. 
Essentially, the FSM Manager would contain all states by an assigned name, which could be used to switch state. Each state, would initialize with the manager provided as a reference; later used by the state to switch to another state.

### Additional
Within this project I implemented a crude form of Dynamic Hide searching. The base project requested a Greedy Search to find a valid place for the AI to hide, however, finding this to be a bit unnecessary given the level layout, I implemented a reverse raycast search.
The AI generated a number of raycast radially. 
Each environmental object hit then generate an opposing raycast using the object as a target. 
Utilizing the hit marker of the opposing raycast, the AI determines if the closest accessible node is out of view of the player before attempting to hide.

## Assignment 2 | Boids & Bee Algorithm
For this project we were presented a basic space miner simulation with the express purpose of implementing the Flocking Algorithm.
For this I took a slight deviation from the original content, and implemented the algorithm utilizing the optimizations of the physics engine to offload some of the heavy lifting.

The core assignment specifically desired a per Update evaluation of all available drones in the scene. This was replaced with [OverlapSphere](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphere.html) to access all valid neighboring drones.
To expand on the drones functionality I also implemented a cascading group system, complete with leader allocation. In this system, drones, prior to boiding, would determine if the neighbor was apart of the same group & set said drones leader to its own, or remove its leader if none exists anymore. This allowed for some interesting functionality that was not refined in time such as mining groups or combat groups.

To provide the drones with some restrictions, they were given battery efficiency, mining efficiency & battery capacity. These values were later used to generate a heuristic that would determine the role of a drone. This heuristic is _1 - ((Battery Efficiency - Minimum Efficiency) / (Maximum Efficiency - Minimum Efficiency)) - Mining Efficiency_. Using this, a value ranging from -1 to positive 1 is generated; where -1 represents an Elite Miner & 1 represents a Scout.

Additionally, all drones have the capablity to consume some of the collected resource whilst moving to generate additional charge in order to return to the mother ship. This system is only utilized when the drone discovers it lacks enough battery to effectively return & recharge.

## Assignment 3 | Mech Battle
For this assignment the primary focus was on the implementation of a single script player-like Decision Making Tree's utilizing PandaBT.
To achieve this I implemented a visual confirmation & memory system within the AI. This system managed observational systems of the mech, which would begin wandering. During this wandering the observational nodes would fire, checking for the existence of non-registered supply crates, which it would then note the position of internally, and the last confirmed time it was seen. These "remembered" crates would then go on to be used by the mech when requiring a resupply. Additionally, should a known crate not be seen when observing, the drone would consider if the crate would respawn soon, as all crates had a fixed spawn, if the last time last seen plus five seconds was within two seconds of the current time, the mech would include that crate in the selection of close viable crates for resupply. This shows when the mech finishes a combat encounter, as it will proceed to camp the supply crate till it spawns.