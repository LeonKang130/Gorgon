open FParsec
open Gorgon.IR
open Gorgon.Parser
open Gorgon.CSE
open Gorgon.Printer
open System.IO

[<Literal>]
let sourceCode = """
func test(x: float, y: float) -> float {
    let z1 = fma(x, y, 1);
    let z2 = fma(x, y, 1);
    let z3 = 3.14 * sqr(fma(1, 2, 3));
    let z4 = z1 + z2 + z3;
    return z1 + z2 + (z3 + 1.0);
}
"""

[<EntryPoint>]
let main _ =
    match run pFunction sourceCode with
    | Success (func, _, _) ->
        printfn $"Parsed function: %s{Printer().PrintFunction func}"
        printfn $"Function cost: %d{EvaluateFunctionCost (func, CostModel.Default)}"
        let eliminatedFunc = EliminateCommonSubexpressions func
        printfn $"CSE processed function: %s{Printer().PrintFunction eliminatedFunc}"
        printfn $"CSE processed function cost: %d{EvaluateFunctionCost (eliminatedFunc, CostModel.Default)}"
        File.WriteAllText("dsl.txt", DSLPrinter(CostModel.Default).PrintFunction eliminatedFunc)
    | Failure (errorMsg, _, _) ->
        printfn $"Parsing failed: %s{errorMsg}"
    0