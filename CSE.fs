module Gorgon.CSE

open Gorgon.IR

type private ENode =
    | ELiteral of value: float32
    | EVariable of name: string
    | EUnary of op: UnaryOp * expr: int
    | EBinary of op: BinaryOp * left: int * right: int
    | ETernary of op: TernaryOp * expr1: int * expr2: int * expr3: int

type private DAG() =
    let mutable classes = ResizeArray<ENode>()
    let mutable memo = Map.empty<ENode, int>
    let mutable localBindings = Map.empty<string, int>
    let mutable counter = 0
    member this.FreshVar() : string =
        let name = $"v{counter}"
        counter <- counter + 1
        name
    member this.AddENode(enode: ENode) : int =
        match memo.TryFind enode with
        | Some eid -> eid
        | None ->
            let eid = classes.Count
            classes.Add(enode)
            memo <- memo.Add(enode, eid)
            eid
    member this.AddExpr(expr: Expr) : int =
        match expr with
        | Literal value -> this.AddENode(ELiteral value)
        | Variable name ->
            match localBindings.TryFind name with
            | Some eid -> eid
            | None ->
                this.AddENode(EVariable name)
        | Unary (op, expr) ->
            this.AddENode(EUnary(op, this.AddExpr(expr)))
        | Binary (op, left, right) ->
            this.AddENode(EBinary(op, this.AddExpr(left), this.AddExpr(right)))
        | Ternary (op, expr1, expr2, expr3) ->
            this.AddENode(ETernary(op, this.AddExpr(expr1), this.AddExpr(expr2), this.AddExpr(expr3)))
    member this.AddBinding(name: string, eid: int) =
        localBindings <- localBindings.Add(name, eid)
    static member Create(func: Function) : DAG * int =
        let dag = DAG()
        for arg in func.args do
            dag.AddENode(EVariable arg) |> ignore
        for stmt in func.body do
            match stmt with
            | Return _ -> failwith "Return statements are not allowed in the body of a function"
            | Assign(name, value) -> dag.AddBinding(name, dag.AddExpr(value)) |> ignore
        match func.ret with
        | Assign _ -> failwith "Function must end with a return statement"
        | Return expr -> 
            let root = dag.AddExpr(expr)
            dag, root
    member this.Extract(root: int) : Stmt list * Stmt =
        let stmts = ResizeArray<Stmt>()
        let mutable localLookup = Map.empty<int, string>
        let rec traverse(eid: int) =
            match localLookup.TryFind eid with
            | Some name -> Variable name
            | None ->
                match classes[eid] with
                | ELiteral value -> Literal value
                | EVariable name -> Variable name
                | EUnary(op, expr) ->
                    match traverse expr with
                    | Literal value ->
                        match op with
                        | Square -> Literal (value * value)
                        | Inverse ->
                            if value = 0.0f then
                                failwith "Division by zero"
                            Literal (1.0f / value)
                    | expr' ->
                        let name = this.FreshVar()
                        localLookup <- localLookup.Add(eid, name)
                        stmts.Add(Assign (name, Unary(op, expr')))
                        Variable name
                | EBinary(op, left, right) ->
                    match traverse left, traverse right with
                    | Literal a, Literal b ->
                        match op with
                        | Add -> Literal (a + b)
                        | Subtract -> Literal (a - b)
                        | Multiply -> Literal (a * b)
                        | Divide ->
                            if b = 0.0f then
                                failwith "Division by zero"
                            Literal (a / b)
                    | left', right' ->
                        let name = this.FreshVar()
                        localLookup <- localLookup.Add(eid, name)
                        stmts.Add(Assign (name, Binary(op, left', right')))
                        Variable name
                | ETernary(op, expr1, expr2, expr3) ->
                    match traverse expr1, traverse expr2, traverse expr3 with
                    | Literal a, Literal b, Literal c ->
                        match op with
                        | FusedMultiplyAdd -> Literal (a * b + c)
                    | _ ->
                        let name = this.FreshVar()
                        localLookup <- localLookup.Add(eid, name)
                        stmts.Add(Assign (name, Ternary(op, traverse expr1, traverse expr2, traverse expr3)))
                        Variable name
        let expr = traverse root
        List.ofArray(stmts.ToArray()), Return expr

let EliminateCommonSubexpressions(func: Function) : Function =
    let dag, root = DAG.Create func
    let body, ret = dag.Extract root
    { name = func.name; args = func.args; body = body; ret = ret }