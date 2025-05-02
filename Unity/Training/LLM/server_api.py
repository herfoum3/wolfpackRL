import os
import time
import re
import requests
from flask import Flask, request, jsonify

MODEL_NAME = os.getenv("MODEL", "phi")
EVAL_TYPE = os.getenv("EVAL_TYPE", "basic")

app = Flask(__name__)

def ask_local_llm(prompt):
    t0 = time.time()
    response = requests.post("http://localhost:11434/api/generate", json={
        "model": MODEL_NAME,
        "prompt": prompt,
        "stream": False,
        "options": {
            "temperature": 0.1,
            "num_predict": 3
        }
    })
    t1 = time.time()
    print(f"[Time for API call: {t1-t0:.2f} seconds]")

    resp_json = response.json()
    if "response" in resp_json:
        return resp_json["response"]
    elif "error" in resp_json:
        print("[LLM ERROR]", resp_json["error"])
        return "0.0"
    else:
        print("[LLM Unexpected Response]", resp_json)
        return "0.0"

def extract_float(text):
    matches = re.findall(r"[0-1]\.\d+", text)
    if matches:
        score = float(matches[0])
        return min(max(score, 0.0), 1.0)
    else:
        return 0.0

def generate_cooperation_prompt(wolf_histories, deer_history, attacker_id, save_interval):
    prompt = (
        f"This simulation shows wolves cooperating to hunt a deer.\n"
        f"The positions were recorded every {save_interval:.2f} seconds.\n"
        "Over the last few seconds:\n"
    )

    for wolf in wolf_histories:
        traj = " -> ".join(f"({float(pos['x']):.2f},{float(pos['z']):.2f})" for pos in wolf["trajectory"])
        role = "attacker" if wolf["id"] == attacker_id else "support"
        prompt += f"- Wolf {wolf['id']} ({role}) moved: {traj}\n"

    deer_traj = " -> ".join(f"({float(pos['x']):.2f},{float(pos['z']):.2f})" for pos in deer_history)
    prompt += f"- Deer moved: {deer_traj}\n"

    if EVAL_TYPE == "quality":
        prompt += (
            "\nINSTRUCTIONS:\n"
            "- You are an expert cooperation evaluator.\n"
            "- Assess the strategic quality of the wolves’ cooperation in hunting the deer.\n"
            "- Consider whether they took complementary roles (e.g., flank, intercept, herd).\n"
            "- Consider whether their paths were synchronized, efficient, and non-redundant.\n"
            "- Evaluate how well the non-attacking wolf contributed to the success.\n"
            "- Give a single float between 0.0 (poor cooperation) and 1.0 (expert-level teamwork).\n"
            "- Do NOT explain your answer. ONLY respond with a number, e.g. '0.84'.\n"
        )
    else:
        prompt += (
            "\nINSTRUCTIONS:\n"
            "- You are an expert cooperation evaluator.\n"
            "- Wolves cooperate if they stay close, coordinate movements, and help the attacker.\n"
            "- Rate the wolves' cooperation strictly between 0.0 (no cooperation) and 1.0 (perfect cooperation).\n"
            "- Respond with ONLY a number like '0.75'. No extra text. No explanation.\n"
        )
    return prompt

@app.route('/get_coop_reward', methods=['POST'])
def get_coop_reward():
    data = request.json
    wolf_histories = data.get('wolf_histories', [])
    deer_history = data.get('deer_history', [])
    attacker_id = data.get('attacker_id', None)
    save_interval = data.get('save_interval', None)

    if not wolf_histories or not deer_history or attacker_id is None or save_interval is None:
        return jsonify(llmReward=0.0)

    prompt = generate_cooperation_prompt(wolf_histories, deer_history, attacker_id, save_interval)
    text_response = ask_local_llm(prompt)
    score = extract_float(text_response)
    print(f"[Extracted Score] {score}\n")
    return jsonify(llmReward=score)

if __name__ == '__main__':
    import argparse

    def parse_args():
        parser = argparse.ArgumentParser(description="LLM Cooperation Evaluator Server")
        parser.add_argument("--model", type=str, default=MODEL_NAME, help="Nom du modèle LLM (ex: phi, mistral, tinyllama)")
        parser.add_argument("--eval_type", type=str, default=EVAL_TYPE, choices=["basic", "quality"])
        return parser.parse_args()

    args = parse_args()
    MODEL_NAME = args.model
    EVAL_TYPE = args.eval_type

    app.run(host='0.0.0.0', port=5000)

