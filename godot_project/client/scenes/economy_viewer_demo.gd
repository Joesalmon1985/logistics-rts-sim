extends ViewerRoot

## RUN_05 economy trial debug viewer entry scene.


func _init() -> void:
	variant_label = "economy"
	viewer_title = "Economy Trial Debug Viewer"
	map_title = "Economy map"
	demo_builder = Callable(EconomyDemo, "build_state")
	help_text = (
		"[b]How to use this viewer[/b]\n"
		+ "1. Map shows HQ, woodcutter, sawmill, [color=#facc15]roads[/color], and a pre-placed storehouse [i]construction site[/i] (dashed hex + % ring).\n"
		+ "2. Orange/blue dots = carriers on road nodes. W# badge = workers assigned to a building.\n"
		+ "3. Pick a command (Roads / Buildings / Workers categories), press [b]Submit Command[/b]. Building commands create construction sites.\n"
		+ "4. [b]Step Tick[/b], [b]Step 10[/b], or [b]Step Until Event[/b] to watch logistics/production progress.\n"
		+ "5. Summary panel shows wares, site/carrier/job counts. Inspector shows site delivered/required progress.\n"
		+ "6. [b]Load/Reset Demo[/b] restores the starting layout."
	)


func _enter_tree() -> void:
	SimBootstrap.load_all()
