class_name Hybrid3dCameraController
extends Camera3D

## Walkaround / follow camera for hybrid 3D spectator.

enum Mode { FREE, ORBIT, FOLLOW }

var move_speed: float = 8.0
var rotate_speed: float = 0.003
var zoom_speed: float = 2.5
var height_speed: float = 4.0
var min_height: float = 4.0
var max_height: float = 40.0
var orbit_distance: float = 14.0

var mode: Mode = Mode.FREE
var follow_position: Vector3 = Vector3.ZERO
var _yaw: float = -0.8
var _pitch: float = -0.55
var _dragging: bool = false
var _last_mouse: Vector2 = Vector2.ZERO


func _ready() -> void:
	position = Vector3(0, 12, 14)
	rotation = Vector3(_pitch, _yaw, 0)


func _process(delta: float) -> void:
	_apply_follow(delta)
	_apply_keyboard_move(delta)


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb: InputEventMouseButton = event
		if mb.button_index == MOUSE_BUTTON_RIGHT:
			_dragging = mb.pressed
			_last_mouse = mb.position
		if mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_adjust_height(-zoom_speed)
		if mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_adjust_height(zoom_speed)
	if event is InputEventMouseMotion and _dragging:
		var mm: InputEventMouseMotion = event
		_yaw -= (mm.position.x - _last_mouse.x) * rotate_speed
		_pitch = clampf(_pitch - (mm.position.y - _last_mouse.y) * rotate_speed, -1.2, -0.15)
		_last_mouse = mm.position
		_apply_rotation()


func set_mode_name(mode_name: String) -> void:
	match mode_name:
		"orbit", "follow_bot_a", "follow_bot_b", "battle", "objective":
			mode = Mode.FOLLOW
		"free":
			mode = Mode.FREE
		_:
			mode = Mode.FREE


func jump_to(position: Vector3) -> void:
	global_position = position + Vector3(0, 10, 10)
	look_at(position, Vector3.UP)


func _apply_keyboard_move(delta: float) -> void:
	if mode == Mode.FOLLOW:
		return
	var input_dir := Vector3.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		input_dir.z -= 1
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		input_dir.z += 1
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		input_dir.x -= 1
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		input_dir.x += 1
	if Input.is_key_pressed(KEY_Q):
		_adjust_height(height_speed * delta)
	if Input.is_key_pressed(KEY_E):
		_adjust_height(-height_speed * delta)
	if input_dir.length() > 0.01:
		var basis_rot := Basis.from_euler(Vector3(0, _yaw, 0))
		var move: Vector3 = basis_rot * input_dir.normalized() * move_speed * delta
		position += move


func _apply_follow(_delta: float) -> void:
	if mode != Mode.FOLLOW:
		return
	var target: Vector3 = follow_position
	var desired: Vector3 = target + Vector3(cos(_yaw) * orbit_distance, 10.0, sin(_yaw) * orbit_distance)
	global_position = global_position.lerp(desired, 0.08)
	look_at(target + Vector3(0, 0.5, 0), Vector3.UP)


func _apply_rotation() -> void:
	rotation = Vector3(_pitch, _yaw, 0)


func _adjust_height(amount: float) -> void:
	position.y = clampf(position.y + amount, min_height, max_height)
