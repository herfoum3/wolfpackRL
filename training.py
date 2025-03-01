import numpy as np
import torch
import torch.nn.functional as F
from WolfPreyEnv import WolfPreyEnv
from Wolf import Wolf


def preprocess_observation(obs, num_channels=4):
    """
    this converts a 2D grid into a one-hot encoded tensor 
    with shape (num_channels, grid_size, grid_size).
    """
    grid_size = obs.shape[0]
    one_hot = np.zeros((num_channels, grid_size, grid_size), dtype=np.float32)
    for i in range(num_channels):
        one_hot[i][obs == i] = 1.0
    return one_hot


num_episodes = 500
batch_size = 32
gamma = 0.99
learning_rate = 1e-3
epsilon_start = 1.0
epsilon_final = 0.1
epsilon_decay = 300  
target_update = 10   
buffer_capacity = 10000

env = WolfPreyEnv(grid_size=10, num_obstacles=15, reward_radius=2)
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
n_actions = env.action_space.n  # 4 actions per wolf.
grid_size = env.grid_size

wolf1 = Wolf(grid_size,n_actions,buffer_capacity,learning_rate)
wolf2 = Wolf(grid_size,n_actions,buffer_capacity,learning_rate)

steps_done = 0


# training loop
for episode in range(num_episodes):
    obs = env.reset()
    obs_processed = preprocess_observation(obs) 
    state = torch.tensor(obs_processed, device=device).unsqueeze(0)
    done = False
    total_reward1 = 0
    total_reward2 = 0

    while not done:
        epsilon = epsilon_final + (epsilon_start - epsilon_final) * np.exp(-steps_done / epsilon_decay)
        steps_done += 1

        # wolf1 action ---
        if np.random.random() < epsilon:
            action1 = env.action_space.sample()
        else:
            with torch.no_grad():
                q_values1 = wolf1.policy_net(state)
                action1 = q_values1.max(1)[1].item()

        # wolf2 action 
        if np.random.random() < epsilon:
            action2 = env.action_space.sample()
        else:
            with torch.no_grad():
                q_values2 = wolf2.policy_net(state)
                action2 = q_values2.max(1)[1].item()

        # wolfs interact with the env      
        next_obs, rewards, done, _ = env.step(action1, action2)
        reward1, reward2 = rewards
        total_reward1 += reward1
        total_reward2 += reward2

        next_obs_processed = preprocess_observation(next_obs)
        next_state = torch.tensor(next_obs_processed, device=device).unsqueeze(0)

        
        wolf1.replay_buffer.push(state.cpu().numpy(), action1, reward1, next_state.cpu().numpy(), done)
        wolf2.replay_buffer.push(state.cpu().numpy(), action2, reward2, next_state.cpu().numpy(), done)

        state = next_state

        
        #updating wolf1
        if len(wolf1.replay_buffer) > batch_size:
            states, actions, rewards_batch, next_states, dones = wolf1.replay_buffer.sample(batch_size)
            states = torch.tensor(states, device=device).squeeze(1) 
            actions = torch.tensor(actions, device=device).unsqueeze(1)
            rewards_batch = torch.tensor(rewards_batch, device=device,dtype=torch.float32).unsqueeze(1)
            next_states = torch.tensor(next_states, device=device,dtype=torch.float32).squeeze(1)
            dones = torch.tensor(dones, device=device).unsqueeze(1)

            current_q1 = wolf1.policy_net(states).gather(1, actions)
            
            with torch.no_grad():
                next_q1 = wolf1.target_net(next_states).max(1)[0].unsqueeze(1)
                target_q1 = rewards_batch + gamma * next_q1 * (1 - dones.float())
            loss1 = F.mse_loss(current_q1, target_q1)

            wolf1.optimizer.zero_grad()
            loss1.backward()
            wolf1.optimizer.step()

        #updating wolf2
        if len(wolf2.replay_buffer) > batch_size:
            # Update Wolf 2.
            states, actions, rewards_batch, next_states, dones = wolf2.replay_buffer.sample(batch_size)
            states = torch.tensor(states, device=device).squeeze(1)
            actions = torch.tensor(actions, device=device).unsqueeze(1)
            rewards_batch = torch.tensor(rewards_batch, device=device,dtype=torch.float32).unsqueeze(1)
            next_states = torch.tensor(next_states, device=device).squeeze(1)
            dones = torch.tensor(dones, device=device).unsqueeze(1)

            current_q2 = wolf2.policy_net(states).gather(1, actions)
            with torch.no_grad():
                next_q2 = wolf2.target_net(next_states).max(1)[0].unsqueeze(1)
                target_q2 = rewards_batch + gamma * next_q2 * (1 - dones.float())
            loss2 = F.mse_loss(current_q2, target_q2)

            wolf2.optimizer.zero_grad()
            loss2.backward()
            wolf2.optimizer.step()

    # Update target networks periodically.
    if episode % target_update == 0:
        wolf1.target_net.load_state_dict(wolf1.policy_net.state_dict())
        wolf2.target_net.load_state_dict(wolf2.policy_net.state_dict())

    print(f"Episode: {episode}, Wolf1 Total Reward: {total_reward1:.2f}, Wolf2 Total Reward: {total_reward2:.2f}, Epsilon: {epsilon:.2f}")

