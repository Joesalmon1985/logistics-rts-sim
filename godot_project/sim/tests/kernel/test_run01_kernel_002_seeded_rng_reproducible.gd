extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_002_seeded_rng_reproducible"


func run() -> void:
	var rng_a := SeededRng.from_seed(12345)
	var rng_b := SeededRng.from_seed(12345)
	var draws_a: Array = []
	var draws_b: Array = []
	for _i in range(20):
		draws_a.append(rng_a.next_int(0, 100))
		draws_b.append(rng_b.next_int(0, 100))
	assert_eq(JSON.stringify(draws_a), JSON.stringify(draws_b), "RNG sequences must match")

	var rng_c := SeededRng.from_seed(99999)
	rng_c.next_int(0, 1000)
	var saved_state: int = rng_c.serialize()
	var second: int = rng_c.next_int(0, 1000)

	var rng_restored := SeededRng.new()
	rng_restored.deserialize(saved_state)
	var replay_second: int = rng_restored.next_int(0, 1000)
	assert_eq(second, replay_second, "serialize/deserialize must preserve RNG sequence")
