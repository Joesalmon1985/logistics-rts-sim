"""Evaluate hybrid 4P policy scaffold."""

import argparse
from pathlib import Path

from python_tools.training.hybrid_ai.modular_policy import SharedTrunkModularPolicy, deterministic_inference
from python_tools.training.hybrid_ai.observation_schema import VECTOR_SIZE, MACRO_COUNT


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--checkpoint", default="checkpoints/latest.pt")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()
    policy = SharedTrunkModularPolicy()
    ckpt = Path(__file__).resolve().parent / args.checkpoint
    if ckpt.exists():
        policy.load_checkpoint(ckpt)
    obs = [0.0] * VECTOR_SIZE
    mask = [1] * MACRO_COUNT
    action = deterministic_inference(policy, obs, mask)
    print({"seed": args.seed, "action": action, "checkpoint": str(ckpt)})


if __name__ == "__main__":
    main()
