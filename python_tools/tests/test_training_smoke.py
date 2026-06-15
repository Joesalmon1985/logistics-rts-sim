from pathlib import Path

from python_tools.tests.conftest import ROOT, require_godot
from python_tools.training import eval_config
from python_tools.training.train_loop import run_training


def test_trn_smoke_003_training_writes_checkpoints_and_metrics():
    require_godot()
    run_dir = ROOT / "training_runs" / "run06_smoke_test"
    checkpoint_dir = ROOT / "checkpoints" / "run06_smoke_test"
    result = run_training(run_dir=run_dir, checkpoint_dir=checkpoint_dir)
    assert (checkpoint_dir / "economy_policy.pt").exists()
    assert (checkpoint_dir / "military_policy.pt").exists()
    assert (run_dir / "metrics.json").exists()
    assert result["economy"]["samples"] > 0
    assert result["military"]["samples"] > 0
