open Gorgon.IR
open Gorgon.Parser
open Gorgon.Printer
open Gorgon.CSE
open System.IO

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "Usage: dotnet run -- [--dsl|--shader] <input> <output>"
    else if argv.Length = 2 then
        let source = File.ReadAllText argv[0]
        match ParseShaderFunction source with
        | Ok func ->
            let func' = EliminateCommonSubexpressions func
            File.WriteAllText(argv[1], DSLPrinter(CostModel.Default).PrintFunction func')
        | Error errorMsg -> printfn $"Parsing failed: %s{errorMsg}"
    else if argv.Length = 3 then
        match argv[0] with
        | "--dsl" ->
            let source = File.ReadAllText argv[1]
            match ParseDSLFunction source with
            | Ok func ->
                File.WriteAllText(argv[2], Printer().PrintFunction func)
            | Error errorMsg -> printfn $"Parsing failed: %s{errorMsg}"
        | "--shader" ->
            let source = File.ReadAllText argv[1]
            match ParseShaderFunction source with
            | Ok func ->
                let func' = EliminateCommonSubexpressions func
                File.WriteAllText(argv[2], DSLPrinter(CostModel.Default).PrintFunction func')
            | Error errorMsg -> printfn $"Parsing failed: %s{errorMsg}"
        | _ ->
            printfn $"Unknown option: %s{argv[0]}, expected --dsl or --shader"
    else
        printfn "Invalid number of arguments. Expected 2 or 3 arguments."
        printfn "Usage: dotnet run -- [--dsl|--shader] <input> <output>"
    0
