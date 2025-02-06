# 🐺 Wolfpack-RL: Multi-Agent Reinforcement Learning for Cooperative Hunting

---

## Abstract

**Wolfpack-RL** is a multi-agent reinforcement learning (MARL) project that investigates cooperative hunting strategies within a 2D grid-based environment. Utilizing Deep Q-Networks (DQN), this work focuses on the interplay of cooperation, competition, and reward dynamics within the framework of Sequential Social Dilemmas (SSD). The project aims to shed light on emergent social behavior and the environmental factors that drive collaborative strategies among learning agents.

---

## 1. Introduction

Multi-agent reinforcement learning has emerged as a powerful paradigm for modeling complex interactions in dynamic environments. In **Wolfpack-RL**, agents—conceptualized as wolves—are trained to perform coordinated hunting maneuvers. By simulating scenarios that mimic natural social dilemmas, this project explores how reinforcement learning can lead to the emergence of cooperative behavior even in competitive settings.

---

## 2. Related Work

This project builds upon established research in the field of multi-agent reinforcement learning. A seminal work that underpins this study is:

- **Multi-Agent Reinforcement Learning in Sequential Social Dilemmas**  
  *Joel Z. Leibo et al., DeepMind, 2017*  
  [Read the paper](https://arxiv.org/pdf/1702.03037.pdf)

A thorough understanding of this paper is recommended for those looking to delve deeper into the theoretical foundations of the project.

---

## 3. Methodology

### 3.1 Environment and Simulation

- **Environment:** A 2D grid-based arena where agents interact with one another and with various elements representing prey and obstacles.
- **Dynamics:** The environment is designed to emulate Sequential Social Dilemmas (SSD), where agents face trade-offs between individual incentives and group benefits.

### 3.2 Agent Architecture

- **Learning Algorithm:** Agents are trained using Deep Q-Networks (DQN), which allow them to approximate optimal policies in discrete state and action spaces.
- **Reward Structure:** The reward system is crafted to encourage both cooperative hunting and competitive behaviors, thereby simulating real-world social dilemmas.

### 3.3 Research Questions

This project is motivated by the following key questions:
- **Cooperation:** How do agents (wolves) learn to cooperate under varying environmental conditions?
- **Environmental Influences:** What roles do factors such as prey density and reward structures play in fostering collaboration?
- **Emergent Behavior:** Can reinforcement learning models simulate the emergence of complex social behaviors typically observed in biological systems?

---

## General workflow
If you're inexperienced with Git, here is a general workflow that I have used for previous team projects:

### 1. Create a New Branch:
Within your IDE, create a new branch from the main branch.  
Name the branch in a descriptive manner, such as feature/feature-name, bugfix/bug-name.  
`git checkout -b feature/feature-name main`

### 2. Implement Your Feature:
Checkout to your newly created branch.  
Start implementing your feature, regularly committing changes with meaningful commit messages.  
`git add <file1> <file2> <file3>`  
`git commit -m "Implemented a new feature: feature-name"`

### 3. Fetch and Merge Main:
Before pushing your changes, fetch the latest changes from the main branch and merge them into your feature branch to resolve any conflicts and ensure smooth integration.  
`git checkout main`  
`git pull`  
`git checkout feature/feature-name`  
`git merge main`  

### 4. Push Your Branch:
Once you've resolved any conflicts and are satisfied with your changes, push your feature branch to the remote GitHub repository on your remote branch.  
`git push -u origin feature/feature-name`

### 5. Create a Pull Request:
Go to the GitHub repository online and navigate to the “Pull requests” tab.  
Click on “New pull request”.  
Set the base branch to main and the compare branch to your feature branch.

### 6. Request Reviews:
Request reviews from at least two other team members. 
Respond to any comments or requested changes.

### 7. Get Approval and Merge:
Click on “Merge pull request” to merge your changes into the main branch.  

### 8. Clean Up:
Delete the remote feature branch from GitHub.  
Switch to the main branch in your local environment, pull the latest changes, and delete the local feature branch.  
`git branch -d feature/feature-name`  
`git push origin --delete feature/feature-name`

---
