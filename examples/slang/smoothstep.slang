func smoothstep(a: float, b: float, x: float) -> float {
	let t = (x - a) / (b - a);
	return 3.0 * t * t - 2.0 * t * t * t;
}