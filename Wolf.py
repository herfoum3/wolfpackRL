from DQN import DQN
from ReplayBuffer import ReplayBuffer
import torch.optim as optim
import torch

class Wolf:
    def __init__(self, grid_size=10, n_actions=8, buffer_capacity=10000, learning_rate=1.0):
        
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        
        self.policy_net = DQN(grid_size, n_actions).to(device)
        self.target_net = DQN(grid_size, n_actions).to(device)
        self.target_net.load_state_dict(self.policy_net.state_dict())
        self.target_net.eval()
        self.optimizer = optim.Adam(self.policy_net.parameters(), lr=learning_rate)
        self.replay_buffer = ReplayBuffer(buffer_capacity)



    def select_action(self, state):
       pass

        

