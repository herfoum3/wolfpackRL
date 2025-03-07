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

env = WolfPreyEnv(grid_size=7, num_obstacles=2, num_wolves=2, reward_radius=2)
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
n_actions = env.action_space.n  # 4 actions per wolf.
grid_size = env.grid_size

wolves =[Wolf(grid_size,n_actions,buffer_capacity,learning_rate) for _ in range(env.num_wolves)]
#wolf1 = Wolf(grid_size,n_actions,buffer_capacity,learning_rate)
#wolf2 = Wolf(grid_size,n_actions,buffer_capacity,learning_rate)

steps_done = 0


# training loop
for episode in range(num_episodes):
    obs = env.reset()
    obs_processed = preprocess_observation(obs) 
    state = torch.tensor(obs_processed, device=device).unsqueeze(0)
    done = False
    total_reward = [0]*env.num_wolves
    
    steps_in_episode = 0 
    while not done:
        epsilon = epsilon_final + (epsilon_start - epsilon_final) * np.exp(-steps_done / epsilon_decay)      
        steps_done += 1
        steps_in_episode += 1
        
        actions = []
        for wolf in wolves:        
            if np.random.random() < epsilon:
                action = env.action_space.sample()
            else:
                with torch.no_grad():
                    q_values = wolf.policy_net(state)
                    action = q_values.max(1)[1].item()
            
            actions.append(action)
                        
        # wolfs interact with the env      
        next_obs, rewards, done, _ = env.step(actions) 
            
        for i in range(env.num_wolves):
            total_reward[i] += rewards[i]

        next_obs_processed = preprocess_observation(next_obs)
        next_state = torch.tensor(next_obs_processed, device=device).unsqueeze(0)

        for idx, wolf in enumerate(wolves):    
            wolf.replay_buffer.push(state.cpu().numpy(), actions[idx], rewards[idx], next_state.cpu().numpy(), done)

        state = next_state

        
        for wolf in wolves:    
            #updating wolf1
            if len(wolf.replay_buffer) > batch_size:
                states, actions, rewards_batch, next_states, dones = wolf.replay_buffer.sample(batch_size)
                states = torch.tensor(states, device=device).squeeze(1) 
                actions = torch.tensor(actions, device=device).unsqueeze(1)
                rewards_batch = torch.tensor(rewards_batch, device=device,dtype=torch.float32).unsqueeze(1)
                next_states = torch.tensor(next_states, device=device,dtype=torch.float32).squeeze(1)
                dones = torch.tensor(dones, device=device).unsqueeze(1)

                current_q = wolf.policy_net(states).gather(1, actions)
                
                with torch.no_grad():
                    next_q = wolf.target_net(next_states).max(1)[0].unsqueeze(1)
                    target_q = rewards_batch + gamma * next_q * (1 - dones.float())
                loss = F.mse_loss(current_q, target_q)

                wolf.optimizer.zero_grad()
                loss.backward()
                wolf.optimizer.step()



    # Update target networks periodically.
    if episode % target_update == 0:
        for wolf in wolves:
            wolf.target_net.load_state_dict(wolf.policy_net.state_dict())            

    print(f"Episode: {episode},", end=" ")
    for idx, wolf in enumerate(wolves):
        print(f"Wolf{idx+1} Total Reward: {total_reward[idx]:.2f},", end=" ")
    print(f"Epsilon: {epsilon:.2f}, steps per episode: {steps_in_episode}")

print("-->done")

