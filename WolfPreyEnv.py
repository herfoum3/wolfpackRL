import gym
import numpy as np

class WolfPreyEnv(gym.Env):
    def __init__(self, grid_size=10, num_obstacles=10, num_wolves=2, num_prey=1, reward_radius=2, max_steps=1000):
        self.steps = 0
        self.max_steps = max_steps
        self.grid_size = grid_size
        self.num_obstacles = num_obstacles
        self.num_wolves = num_wolves
        self.num_prey = num_prey
        self.reward_radius = reward_radius
        self.min_path = 999

        # The grid positions take:
        #   0: free space, 1: obstacle, 2: wolf, 3: prey.
        self.observation_space = gym.spaces.Box(low=0, high=3,
                                                shape=(grid_size, grid_size),
                                                dtype=np.int32)
        # Each wolf with 4 actions.
        self.action_space = gym.spaces.Discrete(4)
        self.reset()

    def reset(self):
        # We create the grid
        solvable = False
        while not(solvable):
            self.create_grid()
            self.steps = 0

            # We place the wolves
            self.wolf_positions = []
            self.wolf_distances = []
            for _ in range(self.num_wolves):
                while True:
                    pos = [np.random.randint(self.grid_size), np.random.randint(self.grid_size)]
                    if self.grid[pos[0], pos[1]] == 0 and pos not in self.wolf_positions:
                        self.wolf_positions.append(pos)
                        break

            # We place the prey
            while True:
                self.prey_pos = [np.random.randint(self.grid_size), np.random.randint(self.grid_size)]
                if self.grid[self.prey_pos[0], self.prey_pos[1]] == 0 and self.prey_pos not in self.wolf_positions:
                    break

            # Check if grid is solvable
            solvable = True
            for wolf_pos in self.wolf_positions:
                min_path = self._calculate_shortest_path(self.grid, wolf_pos, self.prey_pos)
                if min_path < 0:
                    solvable = False
                    break

            for pos in self.wolf_positions:
                distance = abs(pos[0] - self.prey_pos[0]) + abs(pos[1] - self.prey_pos[1])
                self.wolf_distances.append(distance)



        self.min_path = min_path
        return self._get_obs()

    def create_grid(self):
        n = self.grid_size
        def cluster(x, y):
            rnd = np.random.randint(0, 4)
            if rnd == 0:  # left
                y = max(0, y-1)
                self.grid[x][y] = 1
            if rnd == 1:  # right
                y = min(n-1, y+1)
                self.grid[x][y] = 1
            if rnd == 2:  # up
                x = max(x-1, 0)
                self.grid[x][y] = 1
            if rnd == 3:  # down
                x = min(n-1, x+1)
                self.grid[x][y] = 1
            return x, y

        self.grid = np.zeros((n, n), dtype=np.int32)
        positions = np.random.choice(n*n, self.num_obstacles, replace=False)
        for p in positions:
            x, y = divmod(p, n)
            self.grid[x, y] = 1
            for _ in range(np.random.randint(1, 3)):
                x, y = cluster(x, y)

    def step(self, actions):
        """
        Actions:  0: up, 1: down, 2: left, 3: right.
        Returns:  next observation, [reward_wolf1, reward_wolf2], done flag, info dict.
        """
        self.steps += 1
        new_positions = []
        rewards = [0] * self.num_wolves
        done = False
        hunter_idx = -1  # Track which wolf caught the prey
        wolves_participating = []

        # Loop to process the actions
        for idx, action in enumerate(actions):
            pos = self.wolf_positions[idx].copy()
            # Moving to the new position based on the action
            if action == 0:  # up
                pos[0] = max(0, pos[0] - 1)
            elif action == 1:  # down
                pos[0] = min(self.grid_size - 1, pos[0] + 1)
            elif action == 2:  # left
                pos[1] = max(0, pos[1] - 1)
            elif action == 3:  # right
                pos[1] = min(self.grid_size - 1, pos[1] + 1)

            # We check if it's an obstacle.
            if self.grid[pos[0], pos[1]] == 1:
                # We add a penalty for hitting an obstacle
                reward = -0.5
                pos = self.wolf_positions[idx]
            else:
                    # And give a small time penalty
                    reward = -0.05

            distance = abs(pos[0] - self.prey_pos[0]) + abs(pos[1] - self.prey_pos[1])
            if distance <  self.wolf_distances[idx]:
                # give reward if closer to the prey
                reward +=  np.exp(1/(distance+1))/9
            elif distance > self.wolf_distances[idx]:
                # And give a small penalty for going backwards
                reward += -np.exp(1/(distance+1))/9

            self.wolf_distances[idx] = distance



            rewards[idx] = reward
            new_positions.append(pos)

        self.wolf_positions = new_positions
        # Check if any wolf captured the prey
        for idx, pos in enumerate(self.wolf_positions):
            if pos == self.prey_pos:
                rewards[idx] += 50
                done = True
                hunter_idx = idx  # Record which wolf caught the prey
                break



        # Count wolves participating in the hunt (within reward_radius)
        if done:
            for idx, pos in enumerate(self.wolf_positions):
                distance = abs(pos[0] - self.prey_pos[0]) + abs(pos[1] - self.prey_pos[1])  # Manhattan distance
                if distance <= self.reward_radius:
                    wolves_participating.append(idx)
            # Reward participating wolves accordingly
            for idx in wolves_participating:
                rewards[idx] += 30 * (len(wolves_participating)-1)


        if self.steps >= self.max_steps:
            done = True

        # Info dictionary with metrics
        info = {
            "min_path": self.min_path,
            "hunter_idx": hunter_idx,
            "wolves_participating": len(wolves_participating),
            "wolves_total": self.num_wolves
        }

        return self._get_obs(), rewards, done, info

    def _get_obs(self):
        # Marking the wolves and prey in the grid, 2 for wolves and 3 for prey
        obs = self.grid.copy()
        for pos in self.wolf_positions:
            obs[pos[0], pos[1]] = 2
        obs[self.prey_pos[0], self.prey_pos[1]] = 3
        return obs

    def _calculate_shortest_path(self, grid, start_pos, target_pos):
        """
        Calculate the shortest path length from start to target using BFS.
        Returns the minimum number of steps required.
        """
        if start_pos == target_pos:
            return 0

        grid_size = grid.shape[0]
        queue = deque([(start_pos, 0)])  # (position, distance)
        visited = set([tuple(start_pos)])

        while queue:
            (x, y), distance = queue.popleft()

            # Check all four directions
            for dx, dy in [(0, 1), (1, 0), (0, -1), (-1, 0)]:
                nx, ny = x + dx, y + dy

                # Check if in bounds and not an obstacle
                if (0 <= nx < grid_size and 0 <= ny < grid_size and
                    grid[nx, ny] != 1 and (nx, ny) not in visited):

                    if [nx, ny] == target_pos:
                        return distance + 1

                    queue.append(([nx, ny], distance + 1))
                    visited.add((nx, ny))

        return -1  # No path found

        
if __name__ == '__main__':
    env = WolfPreyEnv(grid_size=10, num_obstacles=15, num_wolves=2, reward_radius=2)

