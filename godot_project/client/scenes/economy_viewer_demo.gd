extends ViewerRoot

## RUN_05 economy trial debug viewer entry scene.


func _init() -> void:
	variant_label = "economy"
	demo_builder = Callable(EconomyDemo, "build_state")


func _enter_tree() -> void:
	SimBootstrap.load_all()
