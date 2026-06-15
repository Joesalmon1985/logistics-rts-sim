extends ViewerRoot

## RUN_05 economy trial debug viewer entry scene.


func _init() -> void:
	variant_label = "economy"
	viewer_title = "Economy Trial Debug Viewer"
	map_title = "Economy map"
	demo_builder = Callable(EconomyDemo, "build_state")
	help_text = (
		"[b]How to use this viewer[/b]\n"
		+ "1. The map shows your settlement: [color=#eab308]HQ[/color] (gold), [color=#a16207]woodcutter[/color], [color=#b8860b]sawmill[/color], and [color=#facc15]yellow roads[/color].\n"
		+ "2. Click any hex — details appear in the inspector below.\n"
		+ "3. Pick a command from the list (count shown above it), press [b]Submit Command[/b].\n"
		+ "4. Press [b]Step Tick[/b] to advance logistics/production.\n"
		+ "5. [b]Load/Reset Demo[/b] restores the starting layout.\n"
		+ "[i]Bot Step (optional): runs one bot-chosen legal command.[/i]"
	)


func _enter_tree() -> void:
	SimBootstrap.load_all()
