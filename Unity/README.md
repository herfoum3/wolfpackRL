# GUIDE Wolfpack 3D Unity 

## Arborescence

```bash
.
├── Inference
│   ├── Brains
│   └── Builds
├── Reward_function_deer.ggb
├── Reward_function_wolf.ggb
├── Training
│   ├── Builds
│   │   └── LinuxServer
│   ├── configs
│   │   ├── deer/
│   │   └── wolf/
│   ├── LLM
│   │   └── server_api.py
│   ├── Packages
│   │   ├── ollama.txt
│   │   └── requirements.txt
│   ├── Ressources/
│   ├── results/
│   ├── setup-vast-ai/
│   └── start_training.sh
└── Wolfpack-dilemma
```

## Test inference

```bash
cd Inference/Builds/
./Wolfpack.x86_64 -force-opengl
```

## Télécharger Packages

### 1. Package folder
```bash
cd Training/Packages/
```

### 2. ml-agents
```bash
git clone --branch release_22 https://github.com/Unity-Technologies/ml-agents.git
```
Note in Unity software: install ml-agents and extension.

### 3. Ollama

- Download link : https://ollama.com/download

Get a model (change name in `Training/LLM/server_api.py`)
```
ollama serve
ollama pull [MODEL_NAME]
```




## Vast.ai/Cluster

### 1. Connexion SSH

```bash
ssh -i ~/.ssh/id_rsa -p 43870 root@142.189.6.68 -L 8080:localhost:8080
```

### 2. Synchroniser le projet

```bash
rsync -avz --delete -e "ssh -p 43870" ./Training root@142.189.6.68:/workspace/
```

### 3. Environnement Python (optionnel)

```bash
conda create -n mlagents python=3.10.12
source ~/.bashrc
conda activate mlagents
```

## Démarrage

### 1. Installer les packages

```bash
cd .Training/Packages/
pip install -r requirements.txt
```

```bash
cd ./ml-agents
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents
```


### 2. Lancer un entraînement

```bash
chmod +x ./start_training.sh
./start_training.sh step1a wolf
```

## Ressoures utiles

- `Ressources/Training-ML-Agents.md`
- `Ressources/Training-Configuration-File.md`
- `Ressources/Training-Plugins.md`