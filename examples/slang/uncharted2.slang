func uncharted2(x: float) -> float {
	let A = 0.15;
	let B = 0.50;
	let C = 0.10;
	let D = 0.20;
	let E = 0.02;
	let F = 0.30;
	let W = 11.2;
	return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}