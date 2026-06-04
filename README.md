# Wuming 🗡️ 



WUMING is a fast-paced, three-dimensional hack-and-slash action game steeped in Daoist themes.



![WUMING](https://github.com/user-attachments/assets/aa658cd9-e8a0-46bd-bde1-928031b917a3)




## 📌 Overview
**Wuming** is a dynamic action title built around satisfying, high-speed combat and seamless state transitions. The player must master a blend of melee strikes, ranged attacks, and defensive maneuvers to overcome enemies. This repository highlights the custom-built combat architecture, animation-driven logic, responsive input handling, and advanced "game feel" integration.

* **Engine:** Unity 6
* **Language:** C#
* **Genre:** 3D Action
* **Role:** Gameplay Programmer & Level Designer

## 👥 The Team
* **Lim Wei Qi:** Story Narrator, Game Programmer, Level 1,3, Boss Fight Designer, VFX Particle Designer, Audio Designer
* **Wong Jing Le:** Story Narrator, Sprite Creator, Animator, Level Designer, UI Designer, Video Editor, Cut Scene Designer
* **Ham Xiao Tong:** Environment Artist, Story Narrator, Level 2 Designer, Art Designer

## ⚙️ Key Technical Contributions (My Work)
If you are reviewing my code, I highly recommend checking out the player controller scripts. I was responsible for architecting the following systems to ensure maximum "game feel" and responsiveness:

### Offensive Architecture (`PlayerCombat.cs`)
* **Dynamic Combo System & Input Queueing:** Engineered a fluid 3-hit combo system that utilizes input caching (`inputQueued`). By reading player inputs mid-swing and queuing the next attack, the system prevents dropped inputs and ensures seamless combat flow without requiring frame-perfect button presses.
* **Animation-Driven State Logic:** Rather than relying on hard-coded timers, the combat script dynamically polls the Animator's state length (`clipLength`). This automatically synchronizes audio cues (`AudioManager`), combo transition windows, and weapon hitboxes to match the exact duration of the animations, ensuring the system remains scalable and unbreakable even if the animation assets are swapped.
* **Action Priority & State Cancellation:** Built a robust state-interruption hierarchy to prevent animation locking and physics glitches. Initiating a melee strike instantly and cleanly cancels lower-priority states (like blocking or charging a ranged attack), while evasive maneuvers (like dashing) retain top-level priority to cancel attacks mid-swing.
* **Camera-Relative Targeting:** Implemented dynamic rotation logic (`RotateToCameraView()`) that automatically aligns the player's forward vector with the camera's perspective upon initiating a combo, ensuring strikes always land exactly where the player intends.

### Defensive Architecture (`PlayerDefense.cs`)
* **Risk-Reward "Perfect Block" System:** Engineered a high-skill deflection mechanic utilizing a precise timing window (`perfectBlockWindow`) upon activation. Successful perfect blocks dynamically restore player stamina and execute an AoE damage shockwave (`Physics.OverlapSphere`). 
* **Integrated Game Feel (Juice):** Hooked the defense system into custom Singletons (`JuiceManager` and `VFXManager`) to trigger camera shake, particle sparks, and temporary hit-stop (freezing the frame on impact) to make defensive maneuvers feel incredibly heavy and satisfying.
* **Fail-Safe Input Interruption:** Solved a common combat bug where players get "stuck" in a block state if interrupted. Implemented an input-flagging system (`blockInterrupted`) that detects when a block is forcefully canceled by a higher-priority action (like attacking or dashing). It safely locks the shield until the player explicitly releases and re-presses the input key, ensuring smooth, predictable combat flow.
* **Coroutine-Synced Visuals:** Decoupled the underlying defense state from raw animation lengths. Utilized Coroutines to precisely time the toggling of external shield meshes and VFX with the Animator's exact transition states (`raiseAnimationTime`, `lowerAnimationTime`), preventing visual clipping.

## 🎮 Play the Game

A playable build is available on [itch.io](https://limwq.itch.io/wuming).

## 🚀 How to Run the Project Locally

1. Clone this repository.
2. Open Unity Hub and click `Add Project from Disk`.
3. Select the cloned folder.
4. Open the boot scene located in `Assets/Scenes/` to begin playing.
