class_name ReplayHash
extends RefCounted

const HASH_PREFIX: String = "sha256:"


static func compute_state_hash(state: GameState) -> String:
	var canonical: Dictionary = state.to_hash_dict()
	return sha256_hex(CanonicalJson.stringify(canonical))


static func sha256_hex(text: String) -> String:
	var context := HashingContext.new()
	context.start(HashingContext.HASH_SHA256)
	context.update(text.to_utf8_buffer())
	return HASH_PREFIX + context.finish().hex_encode()


static func matches(expected: String, actual: String) -> bool:
	return expected == actual
