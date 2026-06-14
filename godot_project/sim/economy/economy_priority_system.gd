class_name EconomyPrioritySystem
extends RefCounted

## Priority ordering for construction, production, and delivery jobs.


static func construction_priority(site: Dictionary) -> int:
	return int(site.get("priority", 0))


static func production_priority(building: Dictionary) -> int:
	return int(building.get("priority", 0))


static func ware_priority(job: Dictionary) -> int:
	return int(job.get("priority", 0))


static func sort_construction_sites(sites: Array) -> Array:
	var sorted: Array = sites.duplicate()
	sorted.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var pa: int = construction_priority(a)
		var pb: int = construction_priority(b)
		if pa != pb:
			return pa > pb
		return int(a.get("id", 0)) < int(b.get("id", 0))
	)
	return sorted


static func sort_buildings(buildings: Array) -> Array:
	var sorted: Array = buildings.duplicate()
	sorted.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var pa: int = production_priority(a)
		var pb: int = production_priority(b)
		if pa != pb:
			return pa > pb
		return int(a.get("id", 0)) < int(b.get("id", 0))
	)
	return sorted


static func sort_delivery_jobs(jobs: Array) -> Array:
	var sorted: Array = jobs.duplicate()
	sorted.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var pa: int = ware_priority(a)
		var pb: int = ware_priority(b)
		if pa != pb:
			return pa > pb
		return int(a.get("id", 0)) < int(b.get("id", 0))
	)
	return sorted


static func set_construction_priority(state: GameState, site_id: int, priority: int) -> void:
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if site.is_empty():
		return
	site["priority"] = priority
	var sites: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CONSTRUCTION_SITES)
	sites[str(site_id)] = site


static func set_production_priority(state: GameState, building_id: int, priority: int) -> void:
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return
	building["priority"] = priority
	EconomyState.add_building(state, building)


static func set_ware_priority(state: GameState, job_id: int, priority: int) -> void:
	var job: Dictionary = EconomyState.get_delivery_job(state, job_id)
	if job.is_empty():
		return
	job["priority"] = priority
	var jobs: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)
	jobs[str(job_id)] = job
