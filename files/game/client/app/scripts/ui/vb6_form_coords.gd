class_name Vb6FormCoords
extends RefCounted
const DESIGN_WIDTH := 1024.0
const DESIGN_HEIGHT := 768.0

static func position(left: int, top: int) -> Vector2:
	return Vector2(left / TWIPS_PER_PIXEL, top / TWIPS_PER_PIXEL)

static func size(width: int, height: int) -> Vector2:
	return Vector2(width / TWIPS_PER_PIXEL, height / TWIPS_PER_PIXEL)

static func centered_on_connect(form_width: float, form_height: float, bottom_margin_twips: int = 450) -> Vector2:
	return Vector2(
		(DESIGN_WIDTH - form_width) / 2.0,
		DESIGN_HEIGHT - form_height - bottom_margin_twips / TWIPS_PER_PIXEL
	)
