import torch
import torch.nn as nn
import numpy as np
import ReplayBuffer
import QNetwork
import torch.optim as optim
import CooperativeLoss




class Agent:
    def __init__(self, input_dim=150, buffer_capacity=10000, action_space=8,
                 epsilon_start=1.0, epsilon_final=0.01, epsilon_decay=2000, lr=0.01, l1=1.0, l2=1.0):
        self.q_network = QNetwork.QNetwork(input_dim, action_space)
        self. optimizer = optim.Adam(self.q_network.parameters(), lr=lr)
        self.loss_fn = CooperativeLoss.CooperativeLoss(l1, l2)  # nn.MSELoss()
        self.buffer = ReplayBuffer.ReplayBuffer(capacity=buffer_capacity)
        self.epsilon = epsilon_start
        self.epsilon_final = epsilon_final
        self.epsilon_decay = epsilon_decay
        self.step_count = 0
        self.action_space = action_space



    def select_action(self, state):
        self.step_count += 1
        epsilon_threshold = (self.epsilon_final +
                             (self.epsilon - self.epsilon_final) *
                             np.exp(-self.step_count / self.epsilon_decay))
        if np.random.rand() > epsilon_threshold:
            with torch.no_grad():
                state = state.float().unsqueeze(0)  # .to(device)
                q_values = self.q_network(state)
                action = q_values.max(1)[1].item()
        else:
            action = np.random.randint(self.action_space)
        return action

    def update_epsilon(self):
        self.epsilon -= (self.epsilon - self.epsilon_final) / self.epsilon_decay
        self.epsilon = max(self.epsilon, self.epsilon_final)
