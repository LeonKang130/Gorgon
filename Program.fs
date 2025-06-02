open FParsec
open Gorgon.IR
open Gorgon.Parser
open Gorgon.Fold
open Gorgon.CSE
open Gorgon.Printer

[<Literal>]
let sourceCode = """
func test(x: float, y: float) -> float {
    let z1 = fma(x, y, 1);
    let z2 = fma(x, y, 1);
    let z3 = 3.14 * sqr(fma(1, 2, 3));
    return z1 + z2 + (z3 + 1.0);
}
"""

[<EntryPoint>]
let main _ =
    match run pFunction sourceCode with
    | Success (func, _, _) ->
        let printer = Printer()
        printfn $"Parsed function:\n%A{func}"
        let optimizedFunc = func |> FoldConstantsInFunction
        printfn $"Optimized function:\n%A{printer.PrintFunction optimizedFunc}"
        let egraph, root = EGraph.Create optimizedFunc
        printfn $"EGraph:\n%A{egraph}"
        let eggPrinter = DSLPrinter(CostModel.Default)
        printfn $"Egg representation:\n%s{eggPrinter.PrintFunction optimizedFunc}"
    | Failure (errorMsg, _, _) ->
        printfn $"Parsing failed: %s{errorMsg}"
    0