extends ViewerRoot

## RUN_05 military trial debug viewer entry scene.


func _init() -> void:
	variant_label = "military"
	viewer_title = "Military Trial Debug Viewer"
	map_title = "Military battlefield"
	demo_builder = Callable(MilitaryDemo, "build_state")
	help_text = (
		"[b]How to use this viewer[/b]\n"
		+ "1. [color=#4791ff]Blue[/color] = p1 (you). [color=#e84848]Red[/color] = p2 enemy. A nearby enemy rifle squad is pre-spawned for combat testing.\n"
		+ "2. HP bars appear above units/buildings. Dashed hexes = construction sites.\n"
		+ "3. Attack commands are prebuilt in the list (e.g. 'Attack: Rifle Squad attacks enemy …'). Submit, then Step 10/Until Event.\n"
		+ "4. Red ring highlights the attack target hex after submitting an attack.\n"
		+ "5. Summary shows credits, power supply/demand, unit/building/site counts.\n"
		+ "6. Fog is OFF. [b]Load/Reset Demo[/b] restores the skirmish start."
	)


func _enter_tree() -> void:
	SimBootstrap.load_all()
