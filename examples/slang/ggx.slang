func ggx_d(alpha: float, cos_theta: float) -> float {
	let pi = 3.14159265358979323846;
	let nominator = alpha * alpha;
	let a = 1.0 + (alpha * alpha - 1.0) * cos_theta * cos_theta;
	let denominator = pi * a * a;
	return nominator / denominator;
}