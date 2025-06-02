module Gorgon.Fold

open Gorgon.IR

let rec private FoldConstantsInExpr (expr: Expr): Expr =
    match expr with
    | Binary (op, left, right) ->
        match FoldConstantsInExpr left, FoldConstantsInExpr right with
        | Literal a, Literal b ->
            match op with
            | Add -> Literal (a + b)
            | Subtract -> Literal (a - b)
            | Multiply -> Literal (a * b)
            | Divide ->
                if b = 0.0f then
                    failwith "Division by zero"
                Literal (a / b)
        | left', right' -> Binary (op, left', right')
    | Unary (op, expr) ->
        match FoldConstantsInExpr expr with
        | Literal value ->
            match op with
            | Square -> Literal (value * value)
            | Inverse ->
                if value = 0.0f then
                    failwith "Division by zero"
                Literal (1.0f / value)
        | expr' -> Unary (Square, expr')
    | Ternary (FusedMultiplyAdd, a, b, c) ->
        let a' = FoldConstantsInExpr a
        let b' = FoldConstantsInExpr b
        let c' = FoldConstantsInExpr c
        match a', b', c' with
        | Literal a, Literal b, Literal c ->
            Literal (a * b + c)
        | _ -> Ternary (FusedMultiplyAdd, a', b', c')
    | _ -> expr

let FoldConstantsInFunction (func: Function): Function =
    let foldStmt (stmt: Stmt): Stmt =
        match stmt with
        | Assign (name, value) -> Assign (name, FoldConstantsInExpr value)
        | Return expr -> Return (FoldConstantsInExpr expr)
    { func with body = List.map foldStmt func.body; ret = foldStmt func.ret }