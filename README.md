# Wolfpack-RL: Multi-Agent Reinforcement Learning for Cooperative Hunting

## Abstract

Wolfpack-RL is a multi-agent reinforcement learning (MARL) project that investigates cooperative hunting strategies in both 2D and 3D environments. Using Deep Q-Networks (DQN) for the grid-based 2D setup and Proximal Policy Optimization (PPO) for the Unity-based 3D simulation, the project explores how agents learn to coordinate, communicate, and adapt their behavior in social dilemmas involving pursuit and cooperation. It emphasizes reward shaping, training efficiency, and emergent collective intelligence.

## 1. Introduction

Multi-agent reinforcement learning provides a powerful framework to simulate interactions between autonomous agents in dynamic environments. In Wolfpack-RL, we simulate wolves that must collaborate to catch a prey. The goal is to explore how cooperation arises from individual learning in environments where agents share rewards, face obstacles, and only perceive partial information. Two environments were developed:

- 2D: Lightweight grid world for rapid prototyping with DQN.
- 3D: Realistic Unity simulation using ML-Agents and PPO.

## 2. Related Work

Our approach is inspired by several key works in MARL:

- Leibo et al. (2017): [Sequential Social Dilemmas](https://arxiv.org/abs/1702.03037)
- Foerster et al. (2018): [COMA - Counterfactual Multi-Agent Policy Gradients](https://arxiv.org/abs/1705.08926)
- Lowe et al. (2017): [MADDPG - Multi-Agent Deep Deterministic Policy Gradients](https://arxiv.org/abs/1706.02275)
- Sukhbaatar et al. (2016): [Learning Multiagent Communication](https://arxiv.org/abs/1605.07736)
- Juliani et al. (2018): [Unity ML-Agents Toolkit](https://arxiv.org/abs/1809.02627)

## 3. Methodology

### Environment Setup

In the 2D environment, agents evolve on a configurable grid populated with obstacles and a prey. Each wolf receives partial local observations and learns through Q-learning to navigate, coordinate, and capture the prey. The grid encodes positions of agents, obstacles, and prey over multiple channels.

In the 3D setting, we use Unity ML-Agents to simulate a realistic arena. Wolves must perceive the environment through raycasting and learn motor control strategies (walk, sneak, sprint) to approach and capture the prey, which is static in the current implementation.

### Agent Architecture

For the 2D experiments, each agent is trained using a DQN model composed of convolutional layers followed by fully connected layers. The network outputs Q-values for each action, and training is stabilized using experience replay, target networks, and gradient clipping.

In the 3D version, the PPO algorithm is used with default hyperparameters provided by the ML-Agents framework. Agents learn a policy that maps raycast observations to action probabilities.

### Reward Design

Several reward schemes were tested:
- A constant reward for moving toward or away from the prey.
- A linear reward based on proximity.
- An exponential distance-based shaping function, which was found to produce more stable learning.

### Evaluation Metrics

Performance is assessed via:
- Mean episodic reward
- Capture success rate
- Trajectory efficiency (real path vs. optimal path length)

Additional visualizations (e.g., heatmaps and reward curves) are generated using Jupyter notebooks.

## 4. Installation and Usage

### Requirements
- Python 3.10+
- PyTorch
- NumPy, Matplotlib, Jupyter
- Unity ML-Agents (for 3D setup)

Install dependencies:
```bash
pip install -r requirements.txt
```

### Running 2D Training
```bash
python train.py --episodes 100000 --reward wolfpack3
```

### Running 3D Simulation
Open `3d_env/` in Unity Hub and follow ML-Agents instructions.


## 5. Contributors

- Ilias Zouine
- Kevin Chelfi
- Thierry Poey
- Yacine Mkhinini
