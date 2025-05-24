# The Reinforcement Learning (RL) Castle 

*Training and Competing Against a Reinforcement Learning Agent in a 2D Grid-World*

This project demonstrates how reinforcement learning (RL) algorithms can be visualized and understood through gameplay. It is designed to make RL more approachable and interactive through an engaging 2D Unity environment.

---

## Project Contents

In this submission folder, you will find:

- Unity project Assets, Scripts, etc.
- `TheRLCastle_Demo.mp4` - short video thumbnail
- `RLGridSimulatorGame.prf` - formal research paper detailing the methodology, results, environment setup, hyperparameters, etc. 
- `README.md` — This very document 

---

## How to Play the Game

### Running the Build

1. Locate and Open the `Builds/` folder.
2. Launch the Windows executable file: `RL_EnemyMovement_SimulatorProj.exe` 
3. No installation required — the game will open directly.

**OR** 

1. Play the game in your web browser at [https://play.unity.com/en/games/6c5c0599-63b0-438a-9856-f7dc526cd3e3/webgl-builds](https://play.unity.com/en/games/6c5c0599-63b0-438a-9856-f7dc526cd3e3/webgl-builds)

### Game Modes

#### **Training Grounds**  
Watch the RL agent (the "chaser") learn how to capture the player over time. Toggle between SARSA and Q-Learning algorithms, visualize the live policy heatmap, and adjust training hyperparameters such as alpha, gamma, epsilon, and episode count. This mode is great for learning how the agent improves through trial and error.

#### **Playing Grounds**  
Control the player using arrow keys and try to evade the chaser! You can choose to play against:
- An untrained SARSA agent
- An untrained Q-Learning agent
- A pre-trained SARSA agent
- A pre-trained Q-Learning agent

Observe the difference in behavior and difficulty as you try to survive!

---

## Project Inspiration

Reinforcement Learning is often taught in abstract mathematical terms or inaccessible implementations. Our goal was to visualize how RL agents actually learn and behave in real time. By building a game that showcases both the learning process and the resulting policies, we hope to:

- Make RL approachable to newcomers and non-engineers
- Provide a visual learning tool to support AI education
- Encourage others to explore the AI space without intimidation
- Deepen our own understanding of SARSA and Q-Learning through practical application

This was both a technical and educational challenge — and a really fun one!

---

## 🔗 GitHub Repository

You can find the full source code, training data, and documentation on GitHub:  
👉 **[https://github.com/nhuante/TheRlCastle_RLSimulatorGame](https://github.com/nhuante/TheRlCastle_RLSimulatorGame)**

---

## Contributions

Natalie 
- Unity Project - Implementation of the Rl algorithms, UI, Tilemaps, Grid Setup, etc. 
- Research Paper - Revisions and Proofreading

Max 
- Research Paper - Initial draft and layout, revisions, proofreading, lit review 
- Unity Project - Brainstorming and ideation

---

Made by Natalie Huante and Max Starreveld  
Fowler School of Engineering  
Chapman University
May 2025
