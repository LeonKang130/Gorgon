module Gorgon.Printer

open Gorgon.IR
open System

type IPrinter =
    abstract member PrintExpr : Expr -> string
    abstract member PrintStmt : Stmt -> string
    abstract member PrintFunction : Function -> string

type Printer(indent: int) =
    let indentation = String.replicate indent " "
    new() = Printer(4)
    member this.PrintExpr = (this :> IPrinter).PrintExpr
    member this.PrintStmt = (this :> IPrinter).PrintStmt
    member this.PrintFunction = (this :> IPrinter).PrintFunction
    interface IPrinter with
        member this.PrintExpr expr =
            match expr with
            | Literal value -> value.ToString()
            | Variable name -> name
            | Unary (op, e) ->
                let opStr = match op with Square -> "sqr" | Inverse -> "inv"
                $"{opStr}({this.PrintExpr e})"
            | Binary (op, left, right) ->
                let opStr = match op with Add -> "+" | Subtract -> "-" | Multiply -> "*" | Divide -> "/"
                $"({this.PrintExpr left} {opStr} {this.PrintExpr right})"
            | Ternary (op, e1, e2, e3) ->
            let opStr = match op with FusedMultiplyAdd -> "fma"
            $"{opStr}({this.PrintExpr e1}, {this.PrintExpr e2}, {this.PrintExpr e3})"
        member this.PrintStmt stmt =
            match stmt with
            | Assign (name, value) -> $"let {name} = {this.PrintExpr value};"
            | Return expr -> $"return {this.PrintExpr expr};"
        member this.PrintFunction func =
            let args = String.Join(", ", func.args)
            let body =
                func.body @ [func.ret]
                |> List.map this.PrintStmt
                |> List.map (fun s -> indentation + s)
            $"func {func.name}({args}) -> float {{\n{String.Join('\n', body)}\n}}"

type DSLPrinter(costModel: CostModel) =
    let definitions =
        $"""
(datatype Expr
    (Literal f64 :cost {costModel.LiteralCost})
    (Variable String :cost {costModel.VariableCost})
    (Square Expr :cost {costModel.UnaryCost Square})
    (Inverse Expr :cost {costModel.UnaryCost Inverse})
    (Add Expr Expr :cost {costModel.BinaryCost Add})
    (Subtract Expr Expr :cost {costModel.BinaryCost Subtract})
    (Multiply Expr Expr :cost {costModel.BinaryCost Multiply})
    (Divide Expr Expr :cost {costModel.BinaryCost Divide})
    (FusedMultiplyAdd Expr Expr Expr :cost {costModel.TernaryCost FusedMultiplyAdd}))
"""
    let mutable localBindings = Set.empty
    member this.PrintExpr = (this :> IPrinter).PrintExpr
    member this.PrintStmt = (this :> IPrinter).PrintStmt
    member this.PrintFunction = (this :> IPrinter).PrintFunction
    interface IPrinter with
        member this.PrintExpr expr =
            match expr with
            | Literal value -> $"(Literal %f{value})"
            | Variable name ->
                if localBindings.Contains name then name
                else $"(Variable \"{name}\")"
            | Unary (op, e) ->
                let opStr = match op with Square -> "Square" | Inverse -> "Inverse"
                $"({opStr} {this.PrintExpr e})"
            | Binary (op, left, right) ->
                let opStr = match op with Add -> "Add" | Subtract -> "Subtract" | Multiply -> "Multiply" | Divide -> "Divide"
                $"({opStr} {this.PrintExpr left} {this.PrintExpr right})"
            | Ternary (op, e1, e2, e3) ->
                let opStr = match op with FusedMultiplyAdd -> "FusedMultiplyAdd"
                $"({opStr} {this.PrintExpr e1} {this.PrintExpr e2} {this.PrintExpr e3})"
        member this.PrintStmt stmt =
            match stmt with
            | Assign (name, value) ->
                localBindings <- localBindings.Add name
                $"(let {name} {this.PrintExpr value})"
            | Return expr -> $"(extract {this.PrintExpr expr})"
        member this.PrintFunction func =
            localBindings <- Set.empty
            definitions + String.Join('\n', List.map this.PrintStmt (func.body @ [func.ret])) + "\n(run 10)"            
