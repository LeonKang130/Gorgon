module Gorgon.IR

[<Struct>]
type UnaryOp =
    | Square
    | Inverse

[<Struct>]
type BinaryOp =
    | Add
    | Subtract
    | Multiply
    | Divide

[<Struct>]
type TernaryOp = | FusedMultiplyAdd

type Expr =
    | Literal of value: float32
    | Variable of name: string
    | Unary of op: UnaryOp * expr: Expr
    | Binary of op: BinaryOp * left: Expr * right: Expr
    | Ternary of op: TernaryOp * expr1: Expr * expr2: Expr * expr3: Expr

type Stmt =
    | Assign of name: string * value: Expr
    | Return of expr: Expr

type Function =
    { name: string
      args: string list
      body: Stmt list
      ret: Stmt }


type CostModel =
    { LiteralCost: int
      VariableCost: int
      UnaryCost: UnaryOp -> int
      BinaryCost: BinaryOp -> int
      TernaryCost: TernaryOp -> int }

    static member Default =
        { LiteralCost = 0
          VariableCost = 0
          UnaryCost =
            function
            | _ -> 1
          BinaryCost =
            function
            | _ -> 1
          TernaryCost =
            function
            | _ -> 1 }

let rec private EvaluateExprCost(expr: Expr, costModel: CostModel): int =
    match expr with
    | Literal _ -> costModel.LiteralCost
    | Variable _ -> costModel.VariableCost
    | Unary (op, e) -> costModel.UnaryCost op + EvaluateExprCost(e, costModel)
    | Binary (op, left, right) ->
        costModel.BinaryCost op + EvaluateExprCost(left, costModel) + EvaluateExprCost(right, costModel)
    | Ternary (op, e1, e2, e3) ->
        costModel.TernaryCost op + EvaluateExprCost(e1, costModel) + EvaluateExprCost(e2, costModel) + EvaluateExprCost(e3, costModel)

let EvaluateFunctionCost(func: Function, costModel: CostModel): int =
    let evalStmt (stmt: Stmt): int =
        match stmt with
        | Assign (_, value) -> EvaluateExprCost(value, costModel)
        | Return expr -> EvaluateExprCost(expr, costModel)
    List.sumBy evalStmt func.body + evalStmt func.ret