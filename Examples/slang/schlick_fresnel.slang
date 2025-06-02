func schlick_fresnel(n0: float, n1: float, cos_theta: float) -> float {
	let r0 = (n0 - n1) * (n0 - n1) / ((n0 + n1) * (n0 + n1));
	let c = 1.0 - cos_theta;
	return r0 + (1.0 - r0) * c * c * c * c * c;
}