from pathlib import Path

from python_tools.tests.conftest import ROOT, require_godot
from python_tools.training import eval_config
from python_tools.training.evaluate import assert_holdout_improvement, evaluate_bc_holdout
from python_tools.training.train_loop import run_training


def test_trn_improve_mil_001():
    require_godot()
    run_dir = ROOT / eval_config.TRAINING_RUN_DIR
    checkpoint_dir = ROOT / eval_config.CHECKPOINT_DIR
    if not (checkpoint_dir / "military_policy.pt").exists():
        run_training(run_dir=run_dir, checkpoint_dir=checkpoint_dir)
    rows = evaluate_bc_holdout(
        "military_trial",
        scenario_path=eval_config.MILITARY_SCENARIO,
        bot_kind="military",
        holdout_seeds=eval_config.MILITARY_HOLDOUT_SEEDS,
        max_ticks=eval_config.MILITARY_TICK_BUDGET,
        checkpoint_path=checkpoint_dir / "military_policy.pt",
        dataset_dir=run_dir / "bc_holdout",
        fog_enabled=0,
    )
    assert_holdout_improvement(rows, "military_trial")
