class_name EntityIdAllocator
extends RefCounted

var next_id: int = 1


func allocate() -> int:
	var id: int = next_id
	next_id += 1
	return id


func to_dict() -> Dictionary:
	return {"next_id": next_id}


static func from_dict(data: Dictionary) -> EntityIdAllocator:
	var allocator := EntityIdAllocator.new()
	allocator.next_id = int(data.get("next_id", 1))
	return allocator
