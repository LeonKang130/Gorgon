open FParsec
open Gorgon.IR
open Gorgon.Parser
open Gorgon.Printer
open System.IO

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
        let printer = DSLPrinter(CostModel.Default)
        File.WriteAllText("dsl.txt", printer.PrintFunction func)
    | Failure (errorMsg, _, _) ->
        printfn $"Parsing failed: %s{errorMsg}"
    0