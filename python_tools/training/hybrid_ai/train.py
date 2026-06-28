"""Train entrypoint for hybrid modular policy."""

from pathlib import Path

from python_tools.training.hybrid_ai.modular_policy import SharedTrunkModularPolicy


def main() -> None:
    policy = SharedTrunkModularPolicy()
    out = Path(__file__).resolve().parent / "checkpoints" / "latest.pt"
    policy.save_checkpoint(out)
    print(f"Wrote scaffold checkpoint to {out}")


if __name__ == "__main__":
    main()
