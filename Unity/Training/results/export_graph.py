import os
import matplotlib.pyplot as plt
import pandas as pd
from tensorboard.backend.event_processing import event_accumulator

event_file = "" #Wolf_step1a/Wolf1/events.out.tfevents...
output_dir = "plots"
os.makedirs(output_dir, exist_ok=True)


ea = event_accumulator.EventAccumulator(event_file)
ea.Reload()


wanted_tags = [
    "Environment/Cumulative Reward",
    "Environment/Episode Length",
    "Losses/Policy Loss",
    "Losses/Value Loss",
    "Policy/Entropy",
    "Policy/Extrinsic Reward",
    "Policy/Extrinsic Value Estimate",
    "Policy/Learning Rate",
    "Policy/Epsilon",
    "Policy/Beta"
]


window = 5

for tag in wanted_tags:
    events = ea.Scalars(tag)
    if not events:
        print(f"⛔ Aucun data pour le tag : {tag}")
        continue

    df = pd.DataFrame({
        "step": [e.step for e in events],
        "value": [e.value for e in events]
    })

    # Moyenne et std glissante
    df["mean"] = df["value"].rolling(window, min_periods=1).mean()
    df["std"] = df["value"].rolling(window, min_periods=1).std()


    plt.figure(figsize=(10, 5))
    plt.plot(df["step"], df["value"], color="gray", alpha=0.3, label="Valeurs brutes")
    plt.plot(df["step"], df["mean"], color="blue", label="Moyenne glissante")
    plt.fill_between(df["step"], df["mean"] - df["std"], df["mean"] + df["std"],
                     color="blue", alpha=0.2, label="± Écart-type")

    plt.title(tag)
    plt.xlabel("Steps")
    plt.ylabel("Value")
    plt.grid(True, linestyle="--", alpha=0.5)
    plt.legend()
    plt.tight_layout()

    safe_name = tag.replace("/", "_").replace(" ", "_")
    plt.savefig(os.path.join(output_dir, f"{safe_name}.png"))
    plt.show()
    plt.close()

print(f"End.")
