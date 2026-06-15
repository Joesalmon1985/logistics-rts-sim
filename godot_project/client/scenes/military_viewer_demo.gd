extends ViewerRoot

## RUN_05 military trial debug viewer entry scene.


func _init() -> void:
	variant_label = "military"
	demo_builder = Callable(MilitaryDemo, "build_state")


func _enter_tree() -> void:
	SimBootstrap.load_all()
