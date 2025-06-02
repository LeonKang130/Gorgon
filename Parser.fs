module Gorgon.Parser

open FParsec
open System
open Gorgon.IR

let private pExpr, pExprRef = createParserForwardedToRef<Expr, unit> ()

let private pIdentifier: Parser<string, unit> =
    many1Satisfy2 Char.IsLetter Char.IsLetterOrDigit .>> spaces

let private pLiteral: Parser<Expr, unit> =
    pfloat |>> float32 |>> Literal .>> spaces

let private pVariable: Parser<Expr, unit> =
    pIdentifier |>> Variable

let private str_ws s = pstring s .>> spaces

let private betweenParens p = between (str_ws "(") (str_ws ")") p

let private betweenBraces p = between (str_ws "{") (str_ws "}") p

let private pUnary: Parser<Expr, unit> =
    let unary (name: string) (op: UnaryOp) =
        attempt (str_ws name >>. betweenParens pExpr |>> (fun expr -> Unary(op, expr)))

    choice [ unary "sqr" Square; unary "inv" Inverse ]

let private pTernary: Parser<Expr, unit> =
    attempt (
        str_ws "fma"
        >>. betweenParens (
            pipe3 (pExpr .>> str_ws ",") (pExpr .>> str_ws ",") pExpr (fun expr1 expr2 expr3 ->
                Ternary(FusedMultiplyAdd, expr1, expr2, expr3))
        )
    )

let private opp = OperatorPrecedenceParser<Expr, unit, unit>()

pExprRef.Value <- opp.ExpressionParser

opp.TermParser <- choice [
    pTernary
    pUnary
    pLiteral
    pVariable
    betweenParens pExpr
]

opp.AddOperator(InfixOperator("+", spaces, 1, Associativity.Left, fun x y -> Binary(Add, x, y)))
opp.AddOperator(InfixOperator("-", spaces, 1, Associativity.Left, fun x y -> Binary(Subtract, x, y)))
opp.AddOperator(InfixOperator("*", spaces, 2, Associativity.Left, fun x y -> Binary(Multiply, x, y)))
opp.AddOperator(InfixOperator("/", spaces, 2, Associativity.Left, fun x y -> Binary(Divide, x, y)))

let private pAssign: Parser<Stmt, unit> =
    str_ws "let" >>. pIdentifier
    .>> str_ws "=" .>>. pExpr .>> str_ws ";"
    |>> Assign

let private pReturn: Parser<Stmt, unit> =
    str_ws "return" >>. pExpr .>> str_ws ";"
    |>> Return

let private pStmt: Parser<Stmt, unit> =
    choice [ pAssign; pReturn ]

let pFunction: Parser<Function, unit> =
    let pName: Parser<string, unit> =
        str_ws "func" >>. pIdentifier
    let pArg: Parser<string, unit> =
        pIdentifier .>> (str_ws ":" >>. pstring "float" .>> spaces)
    let pArgs: Parser<string list, unit> =
        betweenParens (sepBy pArg (str_ws ",")) .>> str_ws "->" .>> str_ws "float"
    let pBody: Parser<Stmt list * Stmt, unit> =
        many pAssign .>>. pReturn .>> spaces
        |> betweenBraces
    spaces >>. pipe3 pName pArgs pBody (fun name args (body, ret) -> { name = name; args = args; body = body; ret = ret })