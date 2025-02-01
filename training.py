import torch
import ReplayBuffer
import GatheringEnv
import Agent
import numpy as np
import random
import os

device = 'cpu'  # 'cuda' if torch.cuda.is_available() else 'cpu'


def seed_everything(seed):
    random.seed(seed)
    os.environ['PYTHONHASHSEED'] = str(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
    torch.backends.cudnn.deterministic = True
    torch.backends.cudnn.benchmark = True
    # torch.use_deterministic_algorithms(True)

seed_everything(10)

def toTensor(states):
    states_tensors = []
    for s  in states:
        states_tensors.append(torch.tensor(s, dtype=torch.float32,device=device))
    return torch.stack(states_tensors)

def train_double_agents(env,
                        agent1, agent2,
                        episodes, batch_size, gamma):

    rates = []
    foods = np.zeros(2, dtype=int)
    for episode in range(episodes):
        states = toTensor(env.reset()) # TODO may be just use directly a tensor for the grid
        dones = [False, False]
        agent1.q_network.to(device)
        agent2.q_network.to(device)
        agent1.q_network.train()
        agent2.q_network.train()

        while not all(dones):

            action1 = agent1.select_action(states[0])
            action2 = agent2.select_action(states[1])

            next_states, rewards, penalties, dones = env.step([action1, action2])
            next_states = toTensor(next_states)
            #dones = toTensor(dones)
            foods += rewards

            #print(foods)

            rtot = np.sum(rewards)
            agent1.buffer.push(states[0], action1, rewards[0], rtot, penalties[0], next_states[0], dones[0])
            agent2.buffer.push(states[1], action2, rewards[1], rtot, penalties[1], next_states[1], dones[1])

            states = next_states

            # During training
            for agent in [agent1, agent2]:
                if len(agent.buffer) > batch_size:
                    batch = agent.buffer.sample(batch_size)
                    batch_states = []
                    batch_actions = []
                    batch_rewards = []
                    batch_penalties = []
                    batch_rtots = []
                    batch_next_states = []
                    batch_dones = []
                    for elem in batch:
                        batch_states.append(elem[0])
                        batch_actions.append(elem[1])
                        batch_rewards.append(elem[2])
                        batch_rtots.append(elem[3])
                        batch_penalties.append(elem[4])
                        batch_next_states.append(elem[5])
                        batch_dones.append(elem[6])

                    batch_states = torch.stack(batch_states).float().to(device)
                    batch_next_states = torch.stack(batch_next_states).float().to(device)
                    batch_actions = torch.tensor(batch_actions, dtype=torch.long, device=device)
                    batch_rewards = torch.tensor(batch_rewards, dtype=torch.float32, device=device)
                    batch_rtots = torch.tensor(batch_rtots, dtype=torch.float32, device=device)
                    batch_penalties = torch.tensor(batch_penalties, dtype=torch.float32, device=device)
                    batch_dones = torch.tensor(batch_dones, dtype=torch.int, device=device)

                    current_q_values  = agent.q_network(batch_states).gather(1, batch_actions.unsqueeze(1))
                    max_next_q_values = agent.q_network(batch_next_states).detach().max(1)[0]
                    expected_q_values = batch_rewards + (gamma * max_next_q_values * (1 - batch_dones))

                    agent.loss = agent.loss_fn(current_q_values, expected_q_values.unsqueeze(1), batch_rewards, batch_rtots, batch_penalties)

                    agent.optimizer.zero_grad()
                    agent.loss.backward()
                    agent.optimizer.step()

                    agent.update_epsilon()

        rates.append(env.beam_rate)

    return np.mean(rates), [np.mean(foods[0]), np.mean(foods[1])]






'''
import random
import numpy as np
import torch

num_episodes = 3
num_runs = 10

for run in range(num_runs):
    # Set the seed
    seed = run  # Or any function of run that gives a unique seed
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed_all(seed)

    # Perform the run
    rewards = []
    for episode in range(num_episodes):
        # Interact with the environment
        # Collect rewards and append to the list
        pass

    # Calculate the mean reward for this run
    mean_reward = np.mean(rewards)
    print(f"Run {run}, Seed {seed}, Mean Reward: {mean_reward}")
'''






apples = 15


Napples = [i for i in range(1, 15)]  # (2,20,40,80,100,150,200)
Ntaggs = [i for i in range(2, 3)]
beam_rates = np.zeros((len(Napples),len(Ntaggs)))

x_dim = 5
y_dim = 5

import time
t1 = time.time()
for i, N_apple in enumerate(Napples):
    for j, N_tagged in enumerate(Ntaggs):
        print(N_apple, '-->', N_tagged)
        env = GatheringEnv.GatheringEnv(grid_size=(x_dim, y_dim), apples=apples, N_apple=N_apple, N_tagged=N_tagged)
        agent1 = Agent.Agent(input_dim=x_dim*y_dim, lr=0.01)
        agent2 = Agent.Agent(input_dim=x_dim*y_dim, lr=0.01)
        beam_rate = train_double_agents(env, agent1, agent2, 10, 10, 0.99)
        beam_rates[(i, j)] = beam_rate

print('took: ', time.time() - t1)
print(beam_rates)



'''
count=0
rates = []
start = time.time()
for i , N_apple in enumerate(Napples):

   if count%10 == 0:
     print('-->', count)
   count+=1

   env2 = GatheringEnv.GatheringEnv(apples=apples, N_apple=N_apple, N_tagged=N_tagged)
   agent12 = Agent.Agent(lr=0.01, l2=0.0)
   agent22 = Agent.Agent(lr=0.01)
   beam_rate = train_double_agents(env2, agent12, agent22, 20, 10)
   rates.append(beam_rate)
   #logger['train_time'].append(time.time()-start)





print('took: ', time.time() - t1)
'''











draw = True
#draw = False
if draw:
    import matplotlib.pyplot as plt
    import seaborn as sns

    plt.figure(figsize=(10, 8))

    sns.heatmap(beam_rates, annot=True, fmt=".2f", xticklabels=N_tagged, yticklabels=N_apple, cmap='viridis')
    plt.title('Beam use rate as a function of apple respawn and agent respawn times')
    plt.xlabel('N_tagged')
    plt.ylabel('N_apple')

    #plt.scatter(hit_rates, Napples, color='green', label='Apples')
    #plt.scatter(hit_rate, Ntaggs, color='red', label='Tagged')
    #plt.title('Apple respawn times as a Function of Hit Rate')
    #plt.xlabel('Hit Rate')
    #plt.ylabel('N_apple')
    #plt.legend()

    plt.savefig('heatmap4.png')
def error_bar(scarcity, beams_player1, beams_player2, std_player1, std_player2):
    plt.figure(figsize=(10, 8))

    plt.errorbar(scarcity, beams_player1, yerr=std_player1, label='Player 1', capsize=5)
    plt.errorbar(scarcity, beams_player2, yerr=std_player2, label='Player 2', capsize=5)

    plt.title('Beam Use vs Scarcity')
    plt.xlabel('Scarcity')
    plt.ylabel('Mean Beam Use')
    plt.legend()
    plt.show()
