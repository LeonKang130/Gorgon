module Gorgon.Test

open Gorgon.IR
open Gorgon.Parser
open Gorgon.Printer
open Gorgon.CSE
open NUnit.Framework

[<Literal>]
let SmoothStepSource = """
func smoothstep(a: float, b: float, x: float) -> float {
	let t = (x - a) / (b - a);
	return 3.0 * t * t - 2.0 * t * t * t;
}
"""

[<Literal>]
let SmoothStepDSL = """
(Subtract (Multiply (Square (Divide (Subtract (Variable "x") (Variable "a")) (Subtract (Variable "b") (Variable "a")))) (Literal 3.0)) (Multiply (Square (Divide (Subtract (Variable "x") (Variable "a")) (Subtract (Variable "b") (Variable "a")))) (Multiply (Literal 2.0) (Divide (Subtract (Variable "x") (Variable "a")) (Subtract (Variable "b") (Variable "a"))))))
"""

[<Literal>]
let SchlickFresnelSource = """
func schlick_fresnel(n0: float, n1: float, cos_theta: float) -> float {
	let r0 = (n0 - n1) * (n0 - n1) / ((n0 + n1) * (n0 + n1));
	let c = 1.0 - cos_theta;
	return r0 + (1.0 - r0) * c * c * c * c * c;
}
"""

[<Literal>]
let SchlickFresnelDSL = """
(FusedMultiplyAdd (Subtract (Literal 1.0) (Variable "cos_theta")) (Multiply (Square (Square (Subtract (Literal 1.0) (Variable "cos_theta")))) (Subtract (Literal 1.0) (Divide (Square (Subtract (Variable "n0") (Variable "n1"))) (Square (Add (Variable "n0") (Variable "n1")))))) (Divide (Square (Subtract (Variable "n0") (Variable "n1"))) (Square (Add (Variable "n0") (Variable "n1")))))
"""

[<Literal>]
let AcesToneMappingSource = """
func aces(x: float) -> float {
	let a = 2.51;
	let b = 0.03;
	let c = 2.43;
	let d = 0.59;
	let e = 0.14;
	return min(1.0, max(0.0, (x * (a * x + b)) / (x * (c * x + d) + e)));
}
"""

[<Literal>]
let AcesToneMappingDSL = """
(Min (Literal 1.0) (Max (Literal 0.0) (Divide (Multiply (Variable "x") (FusedMultiplyAdd (Literal 2.51) (Variable "x") (Literal 0.03))) (FusedMultiplyAdd (Variable "x") (FusedMultiplyAdd (Literal 2.43) (Variable "x") (Literal 0.59)) (Literal 0.14)))))
"""

[<Literal>]
let Uncharted2ToneMappingSource = """
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
"""

[<Literal>]
let Uncharted2ToneMappingDSL = """
(Subtract (Divide (FusedMultiplyAdd (Variable "x") (FusedMultiplyAdd (Literal 0.15) (Variable "x") (Literal 0.05)) (Literal 0.004)) (FusedMultiplyAdd (Variable "x") (FusedMultiplyAdd (Literal 0.15) (Variable "x") (Literal 0.5)) (Literal 0.06))) (Literal 0.066667))
"""

[<Literal>]
let GGXSource = """
func ggx_d(alpha: float, cos_theta: float) -> float {
	let pi = 3.14159265358979323846;
	let nominator = alpha * alpha;
	let a = 1.0 + (alpha * alpha - 1.0) * cos_theta * cos_theta;
	let denominator = pi * a * a;
	return nominator / denominator;
}
"""

[<Literal>]
let GGXDSL = """
(Divide (Square (Variable "alpha")) (Multiply (Square (FusedMultiplyAdd (Square (Variable "cos_theta")) (Subtract (Square (Variable "alpha")) (Literal 1.0)) (Literal 1.0))) (Literal 3.141593)))
"""

let TestFunctionSourceAndDSL (source: string) (dsl: string) =
    match ParseShaderFunction source with
    | Ok func ->
        let cost = EvaluateFunctionCost(func, CostModel.Default)
        let func' = EliminateCommonSubexpressions func
        let cost' = EvaluateFunctionCost(func', CostModel.Default)
        Assert.That(cost' <= cost, Is.True, "CSE optimization should not increase cost.")
        match ParseDSLFunction dsl with
        | Ok func'' ->
            let cost'' = EvaluateFunctionCost(func'', CostModel.Default)
            Assert.That(cost'' <= cost', Is.True, "Equality saturation should not increase the cost.")
            let printer = Printer()
            printfn $"Original function (cost: {cost}):\n%s{printer.PrintFunction func}"
            printfn $"CSE-optimized function (cost: {cost'}):\n%s{printer.PrintFunction func'}"
            printfn $"Equality-saturated function (cost: {cost''}):\n%s{printer.PrintFunction func''}"
        | Error errorMsg -> Assert.Fail $"Parsing DSL function failed: %s{errorMsg}"
    | Error errorMsg -> Assert.Fail $"Parsing shader source failed: %s{errorMsg}"

[<Test>]
let ``TestSmoothStep`` () =
    printfn "Testing SmoothStep function..."
    TestFunctionSourceAndDSL SmoothStepSource SmoothStepDSL

[<Test>]
let ``TestSchlickFresnel`` () =
    printfn "Testing Schlick Fresnel function..."
    TestFunctionSourceAndDSL SchlickFresnelSource SchlickFresnelDSL

[<Test>]
let ``TestAcesToneMapping`` () =
    printfn "Testing ACES Tone Mapping function..."
    TestFunctionSourceAndDSL AcesToneMappingSource AcesToneMappingDSL

[<Test>]
let ``TestUncharted2ToneMapping`` () =
    printfn "Testing Uncharted2 Tone Mapping function..."
    TestFunctionSourceAndDSL Uncharted2ToneMappingSource Uncharted2ToneMappingDSL

[<Test>]
let ``TestGGX`` () =
    printfn "Testing GGX function..."
    TestFunctionSourceAndDSL GGXSource GGXDSL