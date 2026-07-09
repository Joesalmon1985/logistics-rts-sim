"""Python tests for hybrid_ai scaffold."""

from pathlib import Path

from python_tools.training.hybrid_ai.action_schema import MACRO_NAMES, validate_macro_id
from python_tools.training.hybrid_ai.modular_policy import SharedTrunkModularPolicy, deterministic_inference
from python_tools.training.hybrid_ai.observation_schema import MACRO_COUNT, VECTOR_SIZE


def test_observation_schema_stable():
    assert VECTOR_SIZE == 13
    assert MACRO_COUNT == 16


def test_action_masking():
    assert validate_macro_id(0)
    assert not validate_macro_id(99)


def test_modular_policy_deterministic_inference():
    policy = SharedTrunkModularPolicy()
    obs = [0.0] * VECTOR_SIZE
    mask = [1] * MACRO_COUNT
    a1 = deterministic_inference(policy, obs, mask)
    a2 = deterministic_inference(policy, obs, mask)
    assert a1 == a2


def test_modular_checkpoint_save_load(tmp_path: Path):
    policy = SharedTrunkModularPolicy()
    ckpt = tmp_path / "test.pt"
    policy.save_checkpoint(ckpt)
    policy2 = SharedTrunkModularPolicy()
    policy2.load_checkpoint(ckpt)
    assert len(MACRO_NAMES) == 16
