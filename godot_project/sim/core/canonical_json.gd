class_name CanonicalJson
extends RefCounted

## Stable canonical JSON for replay hashing (sorted keys, integer-only values).


static func sort_value(value: Variant) -> Variant:
	match typeof(value):
		TYPE_DICTIONARY:
			return sort_dict(value)
		TYPE_ARRAY:
			return sort_array(value)
		TYPE_FLOAT:
			push_error("CanonicalJson: float found in authoritative state")
			return int(value)
		_:
			return value


static func sort_dict(data: Dictionary) -> Dictionary:
	var keys: Array = data.keys()
	keys.sort()
	var sorted: Dictionary = {}
	for key in keys:
		sorted[str(key)] = sort_value(data[key])
	return sorted


static func sort_array(data: Array) -> Array:
	var sorted: Array = []
	for item in data:
		sorted.append(sort_value(item))
	return sorted


static func stringify(value: Variant) -> String:
	return JSON.stringify(sort_value(value), "", false)


static func contains_float(value: Variant) -> bool:
	match typeof(value):
		TYPE_FLOAT:
			return true
		TYPE_DICTIONARY:
			for key in value.keys():
				if contains_float(value[key]):
					return true
		TYPE_ARRAY:
			for item in value:
				if contains_float(item):
					return true
	return false
