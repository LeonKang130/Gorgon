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
