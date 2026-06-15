extends ViewerRoot

## RUN_05 military trial debug viewer entry scene.


func _init() -> void:
	variant_label = "military"
	viewer_title = "Military Trial Debug Viewer"
	map_title = "Military battlefield"
	demo_builder = Callable(MilitaryDemo, "build_state")
	help_text = (
		"[b]How to use this viewer[/b]\n"
		+ "1. [color=#4791ff]Blue[/color] = your forces (p1). [color=#e84848]Red[/color] = enemy (p2). Hubs are gold hexes in opposite corners.\n"
		+ "2. Click hexes to inspect units/buildings. Coordinates are printed on each hex.\n"
		+ "3. Select a legal command, press [b]Submit Command[/b]. Use [b]Step Tick[/b] to advance.\n"
		+ "4. Fog is OFF in this demo for clarity.\n"
		+ "5. [b]Load/Reset Demo[/b] restores the skirmish start.\n"
		+ "[i]Bot Step (optional): one bot-chosen legal command.[/i]"
	)


func _enter_tree() -> void:
	SimBootstrap.load_all()
