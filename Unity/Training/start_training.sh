#!/bin/bash

# Usage: ./run_training.sh step1a wolf
STEP=$1       # ex: step1a
AGENT=$2      # "wolf"

UNITY_ENV_PATH="./Builds/DeerLinuxServer/Wolfpack.x86_64" 
RESULTS_DIR="./results"
CONFIG_DIR="./configs"
ADDITIONAL_ARGS=""

if [[ -z "$STEP" || -z "$AGENT" ]]; then
  echo "Usage: $0 {step1a|step1b|...} {wolf|deer}"
  exit 1
fi

CONFIG="$CONFIG_DIR/$AGENT/$STEP.yaml"
RUN_ID="${AGENT^}_${STEP}"

if [[ "$AGENT" == "wolf" ]]; then
  case "$STEP" in
    step1a|step1b)
      ADDITIONAL_ARGS="--num-envs=4 --time-scale=10"
      ;;
  esac
elif [[ "$AGENT" == "deer" ]]; then
  ADDITIONAL_ARGS="--num-envs=4 --time-scale=10"
fi

python3 -c "import torch; print('CUDA available:' , torch.cuda.is_available())"


# FLask server
if [[ "$AGENT" == "wolf" ]]; then
  if [[ "$STEP" == "step3a" || "$STEP" == "step3b" ]]; then
      export MODEL=phi
      export EVAL_TYPE=basic
      gnome-terminal -- bash -c "gunicorn --workers 4 --bind 0.0.0.0:5000 LLM.server_api:app; exec bash"
      echo "Waiting for LLM server to start..."
      sleep 3
  elif [[ "$STEP" == "step5a" || "$STEP" == "step5b" ]]; then
      export MODEL=phi
      export EVAL_TYPE=quality
      gnome-terminal -- bash -c "gunicorn --workers 4 --bind 0.0.0.0:5000 LLM.server_api:app; exec bash"
      echo "Waiting for LLM server to start..."
      sleep 3
  fi
fi

# Training command
mlagents-learn "$CONFIG" \
  --run-id="$RUN_ID" \
  --env="$UNITY_ENV_PATH" \
  --results-dir="$RESULTS_DIR" \
  --no-graphics \
  --force \
  $ADDITIONAL_ARGS
