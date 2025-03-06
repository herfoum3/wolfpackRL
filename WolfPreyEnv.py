import gym
import numpy as np


class WolfPreyEnv(gym.Env):
    def __init__(self, grid_size=10, num_obstacles=10, reward_radius=2):
        
        self.grid_size = grid_size
        self.num_obstacles = num_obstacles
        self.reward_radius = reward_radius
        self.rewards = {
          "radius" : 0,
          "capture": 25
        }
                
        # the grid positions take:
        #   0: free space, 1: obstacle, 2: wolf, 3: prey.
        self.observation_space = gym.spaces.Box(low=0, high=3,
                                                shape=(grid_size, grid_size),
                                                dtype=np.int32)
        # each wolf with 4 actions.
        self.action_space = gym.spaces.Discrete(4)
        self.reset()

    def reset(self):
            
        #we create the grid
        self.create_grid()

        # we place the 2 wolves
        self.wolf_positions = []
        for _ in range(2):
            while True:
                pos = [np.random.randint(self.grid_size), np.random.randint(self.grid_size)]
                if self.grid[pos[0], pos[1]] == 0 and pos not in self.wolf_positions:
                    self.wolf_positions.append(pos)
                    break

        # we place the prey
        while True:
            self.prey_pos = [np.random.randint(self.grid_size), np.random.randint(self.grid_size)]
            if self.grid[self.prey_pos[0], self.prey_pos[1]] == 0 and self.prey_pos not in self.wolf_positions:
                break

        return self._get_obs()
    
    def create_grid(self):
        
        def cluster(x,y):
            rnd = np.random.randint(0,3)            
            if rnd == 0: #left
                y = max(0,y-1)
                self.grid[x][y] = 1                
            if rnd == 1: #right
                y = min(n-1,y+1)
                self.grid[x][y] = 1
            if rnd == 2: #up
                x = max(x-1,0)
                self.grid[x][y] = 1
            if rnd == 3: #down
                x = min(n-1,x+1)
                self.grid[x][y] = 1                
            return x,y  
        
        def zeros_connected():
            """
            checking that no zero are trapped in surrounding ones
            """
            n, m = self.grid.shape
            # all 0 positions
            zero_positions = np.argwhere(self.grid == 0)
            if zero_positions.size == 0:                
                return True

            # using BFS from the first zero cell
            start = tuple(zero_positions[0])
            visited = set()
            stack = [start]
            
            directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]

            while stack:
                cx, cy = stack.pop()
                if (cx, cy) in visited:
                    continue
                visited.add((cx, cy))
                for dx, dy in directions:
                    nx = cx + dx
                    ny = cy + dy
                    if 0 <= nx < n and 0 <= ny < m:
                        if self.grid[nx, ny] == 0 and (nx, ny) not in visited:
                            stack.append((nx, ny))
                            
            # compare the number of visited 0's and the total of zeros.
            return len(visited) == len(zero_positions)    
        
        gridOK_attemps = 100
        while gridOK_attemps:
            gridOK_attemps -= 1
            n = self.grid_size
            self.grid = np.zeros((n, n), dtype=np.int32)
            positions = np.random.choice(n*n, self.num_obstacles, replace=False)
            for p in positions:
                x, y = divmod(p, n)
                self.grid[x, y] = 1
                for _ in range(np.random.randint(1,2)):
                    x,y = cluster(x,y)
            if zeros_connected():
                break
            
        for i in range(n):
            print(self.grid[i])
        print("grid OK:",zeros_connected())
        
                 
    def step(self, action1, action2):
        """        
        Actions:  0: up, 1: down, 2: left, 3: right.
        Returns:  next observation, [reward_wolf1, reward_wolf2], done flag, info dict.
        """
        actions = [action1, action2]
        new_positions = []
        rewards = [0.0, 0.0]
        done = False

        wolves_in_radius = []
        # loop to process the actions
        for idx, action in enumerate(actions):
            pos = self.wolf_positions[idx].copy()
            # moving to the new position based on the action 
            if action == 0: #up
                pos[0] = max(0, pos[0] - 1)
            elif action == 1: #down
                pos[0] = min(self.grid_size - 1, pos[0] + 1)
            elif action == 2: #left
                pos[1] = max(0, pos[1] - 1)
            elif action == 3: #right
                pos[1] = min(self.grid_size - 1, pos[1] + 1)

            # we check if it's an obstacle.
            if self.grid[pos[0], pos[1]] == 1:
                # we decided to add a penalty for hitting an obstacle
                # then keep the wolf in place
                reward = -1
                pos = self.wolf_positions[idx]
            else:
                # and give a small time penalty
                reward = -0.1 

            # here we reward the wolf when in a radius distance from the prey 
     
            distance = np.linalg.norm(np.array(pos) - np.array(self.prey_pos))
            if distance <= self.reward_radius:
                wolves_in_radius.append(idx)
                reward += self.rewards.radius

            rewards[idx] = reward
            new_positions.append(pos)

        # rewarding the wolves in radius if any captures the prey and the episode ends.
        if any(pos == self.prey_pos for pos in new_positions):
            n_wolves = len(wolves_in_radius)
            for idx in wolves_in_radius:
                rewards[idx] += n_wolves * self.rewards.capture
            #rewards = [r + 50 for r in rewards]
            done = True
            
        self.wolf_positions = new_positions
        return self._get_obs(), rewards, done, {}

    def _get_obs(self):
        # marking the wolves and prey in the grid, 2 for wolves and 3 for prey
        obs = self.grid.copy()
        for pos in self.wolf_positions:
            obs[pos[0], pos[1]] = 2  
        obs[self.prey_pos[0], self.prey_pos[1]] = 3  
        return obs

    def render(self, mode='human'):
        print(self._get_obs())
        
if __name__ == '__main__':
    env = WolfPreyEnv(grid_size=10, num_obstacles=15, reward_radius=2)

