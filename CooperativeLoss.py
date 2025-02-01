import torch
import torch.nn as nn

class CooperativeLoss(nn.Module):
    def __init__(self, lambda_coop=1.0, lambda_pena=1.0):
        """
        lambda_coop: weight for the cooperative loss component
        lambda_pena: weight for the aggression penalty component
        """

        super(CooperativeLoss, self).__init__()
        self.lambda_coop = lambda_coop
        self.lambda_pena = lambda_pena

    def forward(self, predicted, target, r, rtot, penalty):
        """
        predicted: Predicted Q-values from the model (tensor)
        target: target Q-values calculated from Bellman equation (tensor)
        r: reward (float)
        rtot: total reward (float)
        penalty: Penalties incurred for aggressive actions (tensor or float).
        return: Computed loss value.
        """

        # the standard MSE loss between predicted and target Q-values
        mse_loss = torch.nn.functional.mse_loss(predicted, target)

        # cooperative loss component
        r = torch.as_tensor(r, device=predicted.device, dtype=predicted.dtype)
        rtot = torch.as_tensor(rtot, device=predicted.device, dtype=predicted.dtype)
        coop_loss = 1-r/(rtot+0.001)  #adding a small value to prevent divide by zero
        if coop_loss.dim() > 0:
            coop_loss = coop_loss.mean()

        # penalty loss component
        penalty_loss = torch.as_tensor(penalty, device=predicted.device, dtype=predicted.dtype)
        if penalty_loss.dim() > 0:
            penalty_loss = penalty_loss.mean()

        # total loss
        total_loss = mse_loss - self.lambda_coop * coop_loss + self.lambda_pena * penalty_loss
        return total_loss
