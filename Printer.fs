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
                let opStr = match op with Square -> "sqr" | Inverse -> "inv" | Exponent -> "exp"
                $"{opStr}({this.PrintExpr e})"
            | Binary (op, left, right) ->
                match op with
                | Add -> $"({this.PrintExpr left} + {this.PrintExpr right})"
                | Subtract -> $"({this.PrintExpr left} - {this.PrintExpr right})"
                | Multiply -> $"({this.PrintExpr left} * {this.PrintExpr right})"
                | Divide -> $"({this.PrintExpr left} / {this.PrintExpr right})"
                | Min -> $"min({this.PrintExpr left}, {this.PrintExpr right})"
                | Max -> $"max({this.PrintExpr left}, {this.PrintExpr right})"
                | Power -> $"pow({this.PrintExpr left}, {this.PrintExpr right})"
            | Ternary (op, e1, e2, e3) ->
            let opStr = match op with FusedMultiplyAdd -> "fma"
            $"{opStr}({this.PrintExpr e1}, {this.PrintExpr e2}, {this.PrintExpr e3})"
        member this.PrintStmt stmt =
            match stmt with
            | Assign (name, value) -> $"let {name} = {this.PrintExpr value};"
            | Return expr -> $"return {this.PrintExpr expr};"
        member this.PrintFunction func =
            let args = String.Join(", ", List.map (fun arg -> $"{arg}: float") func.args)
            let body =
                func.body @ [func.ret]
                |> List.map this.PrintStmt
                |> List.map (fun s -> indentation + s)
            $"func {func.name}({args}) -> float {{\n{String.Join('\n', body)}\n}}"

[<Literal>]
let rules = """
; Unary operation rules
(rewrite (Multiply a a) (Square a))
(rewrite (Multiply a (Inverse b)) (Divide a b))
(rewrite (Multiply (Square a) (Square b)) (Square (Multiply a b)))
(rewrite (Divide (Square a) (Square b)) (Square (Divide a b)))
(rewrite (Multiply (Exponent a) (Exponent b)) (Exponent (Multiply a b)))
; Binary operation rules
(rewrite (Add a b) (Add b a))
(rewrite (Multiply a b) (Multiply b a))
(rewrite (Add a (Add b c)) (Add (Add a b) c))
(rewrite (Multiply a (Multiply b c)) (Multiply (Multiply a b) c))
(rewrite (Multiply a (Add b c)) (Add (Multiply a b) (Multiply a c)))
(rewrite (Add (Multiply a b) (Multiply a c)) (Multiply a (Add b c)))
; Ternary operation rules
(rewrite (FusedMultiplyAdd a b c) (Add (Multiply a b) c))
(rewrite (Add (Multiply a b) c) (FusedMultiplyAdd a b c))
; Constant folding rules
(rewrite (Add (Literal a) (Literal b)) (Literal (+ a b)))
(rewrite (Subtract (Literal a) (Literal b)) (Literal (- a b)))
(rewrite (Multiply (Literal a) (Literal b)) (Literal (* a b)))
(rewrite (Divide (Literal a) (Literal b)) (Literal (/ a b)))
(rewrite (Square (Literal a)) (Literal (* a a)))
(rewrite (Inverse (Literal a)) (Literal (/ 1.0 a)))
(rewrite (FusedMultiplyAdd (Literal a) (Literal b) (Literal c)) (Literal (+ (* a b) c)))
(rewrite (Subtract a a) (Literal 0.0))
(rewrite (Divide a (Literal 1.0)) a)
(rewrite (Add a (Literal 0.0)) a)
(rewrite (Multiply a (Literal 1.0)) a)
(rewrite (Multiply a (Literal 0.0)) (Literal 0.0))
"""

type DSLPrinter(costModel: CostModel) =
    let definitions = $"""
(datatype Expr
    (Literal f64 :cost {costModel.LiteralCost})
    (Variable String :cost {costModel.VariableCost})
    (Square Expr :cost {costModel.UnaryCost Square})
    (Inverse Expr :cost {costModel.UnaryCost Inverse})
    (Exponent Expr :cost {costModel.UnaryCost Exponent})
    (Add Expr Expr :cost {costModel.BinaryCost Add})
    (Subtract Expr Expr :cost {costModel.BinaryCost Subtract})
    (Multiply Expr Expr :cost {costModel.BinaryCost Multiply})
    (Divide Expr Expr :cost {costModel.BinaryCost Divide})
    (Min Expr Expr :cost {costModel.BinaryCost Min})
    (Max Expr Expr :cost {costModel.BinaryCost Max})
    (Power Expr Expr :cost {costModel.BinaryCost Power})
    (FusedMultiplyAdd Expr Expr Expr :cost {costModel.TernaryCost FusedMultiplyAdd}))"""
    let mutable bindings = Map.empty
    let mutable counter = 0
    member this.FreshVar() =
        let name = $"v{counter}"
        counter <- counter + 1
        name
    member this.PrintExpr = (this :> IPrinter).PrintExpr
    member this.PrintStmt = (this :> IPrinter).PrintStmt
    member this.PrintFunction = (this :> IPrinter).PrintFunction
    interface IPrinter with
        member this.PrintExpr expr =
            match expr with
            | Literal value -> $"(Literal %f{value})"
            | Variable name ->
                match bindings[name] with
                | Variable name -> $"(Variable \"{name}\")"
                | _ -> name
            | Unary (op, e) ->
                let opStr = match op with Square -> "Square" | Inverse -> "Inverse" | Exponent -> "Exponent"
                $"({opStr} {this.PrintExpr e})"
            | Binary (op, left, right) ->
                let opStr =
                    match op with
                    | Add -> "Add"
                    | Subtract -> "Subtract"
                    | Multiply -> "Multiply"
                    | Divide -> "Divide"
                    | Min -> "Min"
                    | Max -> "Max"
                    | Power -> "Power"
                $"({opStr} {this.PrintExpr left} {this.PrintExpr right})"
            | Ternary (op, e1, e2, e3) ->
                let opStr = match op with FusedMultiplyAdd -> "FusedMultiplyAdd"
                $"({opStr} {this.PrintExpr e1} {this.PrintExpr e2} {this.PrintExpr e3})"
        member this.PrintStmt stmt =
            match stmt with
            | Assign (name, value) ->
                bindings <- Map.add name value bindings
                $"(let {name} {this.PrintExpr value})"
            | Return expr ->
                match expr with
                | Literal _ | Variable _ ->
                    $"(run 10)\n(extract {this.PrintExpr expr})"
                | _ ->
                    let mutable freshVar = this.FreshVar()
                    while bindings.Keys.Contains(freshVar) do
                        freshVar <- this.FreshVar()
                    bindings <- Map.add freshVar expr bindings
                    $"(let {freshVar} {this.PrintExpr expr})\n(run 10)\n(query-extract {freshVar})"
        member this.PrintFunction func =
            bindings <- Map.ofList (func.args |> List.map (fun arg -> arg, Variable arg))
            let body = String.Join('\n', List.map this.PrintStmt func.body)
            let ret = this.PrintStmt func.ret
            $"{definitions.Trim()}\n{rules.Trim()}\n{body}\n{ret}"
