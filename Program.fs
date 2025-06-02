open Gorgon.IR
open Gorgon.Parser
open Gorgon.Printer
open Gorgon.CSE
open NUnit.Framework
open System.IO

module Tests =
    ()

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "Usage: dotnet run -- <file>"
    else
        let source = File.ReadAllText argv[0]
        match ParseShaderFunction source with
        | Result.Ok func ->
            let func' = EliminateCommonSubexpressions func
            File.WriteAllText("output.txt", DSLPrinter(CostModel.Default).PrintFunction func')
        | Result.Error errorMsg ->
            printfn $"Parsing failed: %s{errorMsg}"
    0


        