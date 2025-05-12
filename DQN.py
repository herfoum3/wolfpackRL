import torch.nn as nn
import torch.nn.functional as F

class DQN(nn.Module):
    def __init__(self, grid_size, n_actions):
        super(DQN, self).__init__()
        # We have 5 channels: free, obstacle, self, other_wolves, prey
        self.conv = nn.Sequential(
            nn.Conv2d(5, 16, kernel_size=3, stride=1, padding=1),
            nn.BatchNorm2d(16),
            nn.ReLU(),
            nn.Conv2d(16, 32, kernel_size=3, stride=1, padding=1),
            nn.BatchNorm2d(32),
            nn.ReLU()
        )
        self.fc = nn.Sequential(
            nn.Linear(32 * grid_size * grid_size, 128),
            nn.ReLU(),
            nn.Linear(128, n_actions)
        )

    def forward(self, x):
        x = self.conv(x)
        x = x.view(x.size(0), -1)
        x = self.fc(x)
        return x