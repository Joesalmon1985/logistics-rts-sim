extends ViewerRoot

## RUN_05 economy trial debug viewer entry scene.


func _init() -> void:
	variant_label = "economy"
	viewer_title = "Economy Trial Debug Viewer"
	map_title = "Economy map"
	demo_builder = Callable(EconomyDemo, "build_production_chain")
	build_tutorial_builder = Callable(EconomyDemo, "build_build_tutorial")
	help_text = (
		"[b]Production chain demo[/b] (default)\n"
		+ "HQ + woodcutter on forest + sawmill on roads. Step to watch [color=#75ff75]D:n[/color] forest density drop, "
		+ "[color=#f2a826]carrier routes[/color], store chips (T:/P:), and production events.\n\n"
		+ "[b]Build tutorial[/b] — Load Build Tutorial\n"
		+ "Road stub toward hex 6,2. Place a road-adjacent storehouse site, connect roads, step until completion.\n\n"
		+ "1. Pick a command → Submit Command. 2. Step Tick / Step 10 / Step Until Event.\n"
		+ "3. Toggle Logistics / Forest D layers. 4. Checklist tracks site → road → deliver → complete."
	)


func _enter_tree() -> void:
	SimBootstrap.load_all()
