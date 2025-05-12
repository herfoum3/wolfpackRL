import numpy as np
import torch

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

class ReplayBuffer:
    def __init__(self, capacity, state_dim):
        self.capacity = capacity
        self.position = 0
        self.size = 0

        # Pre-allocate memory for all components
        self.states = np.zeros((capacity, *state_dim), dtype=np.float32)
        self.actions = np.zeros(capacity, dtype=np.int64)
        self.rewards = np.zeros(capacity, dtype=np.float32)
        self.next_states = np.zeros((capacity, *state_dim), dtype=np.float32)
        self.dones = np.zeros(capacity, dtype=np.bool_)

    def push(self, state, action, reward, next_state, done):
        self.states[self.position] = state
        self.actions[self.position] = action
        self.rewards[self.position] = reward
        self.next_states[self.position] = next_state
        self.dones[self.position] = done

        self.position = (self.position + 1) % self.capacity
        self.size = min(self.size + 1, self.capacity)

    def sample(self, batch_size):
        indices = np.random.choice(self.size, batch_size, replace=False)

        states = torch.tensor(self.states[indices], device=device)
        actions = torch.tensor(self.actions[indices], device=device).unsqueeze(1)
        rewards = torch.tensor(self.rewards[indices], device=device).unsqueeze(1)
        next_states = torch.tensor(self.next_states[indices], device=device)
        dones = torch.tensor(self.dones[indices], device=device).unsqueeze(1)

        return states, actions, rewards, next_states, dones

    def __len__(self):
        return self.size