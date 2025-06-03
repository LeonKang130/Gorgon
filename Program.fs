open Gorgon.IR
open Gorgon.Parser
open Gorgon.Printer
open Gorgon.CSE
open System.IO

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "Usage: dotnet run <input> <output> "
    else if argv.Length = 2 then
        let source = File.ReadAllText argv[0]
        match ParseShaderFunction source with
        | Ok func ->
            let func' = EliminateCommonSubexpressions func
            File.WriteAllText(argv[1], DSLPrinter(CostModel.Default).PrintFunction func')
        | Error errorMsg -> printfn $"Parsing failed: %s{errorMsg}"
    else if argv.Length = 3 && argv[0] = "--dsl" then
        let source = File.ReadAllText argv[1]
        match ParseDSLFunction source with
        | Ok func ->
            File.WriteAllText(argv[2], Printer().PrintFunction func)
        | Error errorMsg -> printfn $"Parsing failed: %s{errorMsg}"
    0
