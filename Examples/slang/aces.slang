func aces(x: float) -> float {
	let a = 2.51;
	let b = 0.03;
	let c = 2.43;
	let d = 0.59;
	let e = 0.14;
	return min(1.0, max(0.0, (x * (a * x + b)) / (x * (c * x + d) + e)));
}