module Gorgon.Parser

open FParsec
open System
open Gorgon.IR
open Gorgon.CSE

let private str_ws s = pstring s .>> spaces

let private betweenParens p = between (str_ws "(") (str_ws ")") p

let private betweenBraces p = between (str_ws "{") (str_ws "}") p

let private betweenQuotes p =
    between (str_ws "\"") (str_ws "\"") p

let private pIdentifier: Parser<string, unit> =
    many1Satisfy2 Char.IsLetter Char.IsLetterOrDigit .>> spaces

module ShaderParser =
    let private pExpr, pExprRef = createParserForwardedToRef<Expr, unit> ()

    let private pLiteral: Parser<Expr, unit> =
        pfloat |>> float32 |>> Literal .>> spaces

    let private pVariable: Parser<Expr, unit> =
        pIdentifier |>> Variable

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

module DSLParser =
    let pExpr, pExprRef = createParserForwardedToRef<Expr, unit> ()
    
    let private pLiteral: Parser<Expr, unit> =
        str_ws "Literal" >>. pfloat  |>> float32 |>> Literal
    
    let private pVariable: Parser<Expr, unit> =
        str_ws "Variable" >>. betweenQuotes pIdentifier |>> Variable
    
    let private pUnaryOp: Parser<UnaryOp, unit> =
        choice [
            str_ws "Square" >>% Square
            str_ws "Inverse" >>% Inverse
        ]
    
    let private pBinaryOp: Parser<BinaryOp, unit> =
        choice [
            str_ws "Add" >>% Add
            str_ws "Subtract" >>% Subtract
            str_ws "Multiply" >>% Multiply
            str_ws "Divide" >>% Divide
        ]
    
    let private pTernaryOp: Parser<TernaryOp, unit> =
        choice [
            str_ws "FusedMultiplyAdd" >>% FusedMultiplyAdd
        ]
    
    let private pUnary: Parser<Expr, unit> =
        pUnaryOp .>>. pExpr |>> Unary
    
    let private pBinary: Parser<Expr, unit> =
        pipe3 pBinaryOp pExpr pExpr (fun op left right -> Binary(op, left, right))

    let private pTernary: Parser<Expr, unit> =
        pipe4 pTernaryOp pExpr pExpr pExpr (fun op expr1 expr2 expr3 -> Ternary(op, expr1, expr2, expr3))
    
    pExprRef.Value <- betweenParens (choice [
        pTernary
        pBinary
        pUnary
        pVariable
        pLiteral
    ])

let ParseShaderFunction(source: string) : Result<Function, string> =
    match run ShaderParser.pFunction source with
    | Success (func, _, _) -> Result.Ok func
    | Failure (errorMsg, _, _) -> Result.Error errorMsg

let ParseDSLFunction(source: string) : Result<Function, string> =
    match run DSLParser.pExpr source with
    | Success (expr, _, _) ->
        let dag, root = DAG.FromExpr expr
        let body, ret = dag.Extract root
        let args = dag.Args()
        Result.Ok { name = "foo"; args = args; body = body; ret = ret }
    | Failure (errorMsg, _, _) -> Result.Error errorMsg