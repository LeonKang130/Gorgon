module Gorgon.CSE

open Gorgon.IR
open System
open System.Collections.Generic

type EClassId = int

type ENode =
    | ELiteral of value: float32
    | EVariable of name: string
    | EUnary of op: UnaryOp * expr: EClassId
    | EBinary of op: BinaryOp * left: EClassId * right: EClassId
    | ETernary of op: TernaryOp * expr1: EClassId * expr2: EClassId * expr3: EClassId

type EClass = {
    id: EClassId
    mutable nodes: Set<ENode>
    mutable parents: Set<EClassId>
}

type EGraph() =
    let mutable nextId = 0
    let mutable classes = Dictionary<EClassId, EClass>()
    let mutable memo = Dictionary<ENode, EClassId>()
    let mutable unionFind = Dictionary<EClassId, EClassId>()
    let mutable localBindings = Dictionary<string, EClassId>()
    override this.ToString() =
        let sb = System.Text.StringBuilder()
        for KeyValue(eid, eclass) in classes do
            sb.AppendLine($"EClass {eid}({this.Find eid}):") |> ignore
            for node in eclass.nodes do
                sb.AppendLine($"  {node}") |> ignore
            if eclass.parents.Count > 0 then
                let parents = String.Join(", ", eclass.parents)
                sb.AppendLine($"  Parents: {parents}") |> ignore
        sb.ToString()
    member this.Find(eid: EClassId) : EClassId =
        match unionFind.TryGetValue(eid) with
        | true, parent when parent <> eid ->
            let root = this.Find(parent)
            unionFind[eid] <- root
            root
        | _ ->
            unionFind[eid] <- eid
            eid
    member this.Union(a: EClassId, b: EClassId) =
        let rootA = this.Find(a)
        let rootB = this.Find(b)
        if rootA <> rootB then
            unionFind[rootB] <- rootA
    member this.AddENode(enode: ENode): EClassId =
        match enode with
        | EVariable name when localBindings.ContainsKey name ->
            localBindings[name]
        | _ ->
            let canonical =
                match enode with
                | ELiteral _ | EVariable _ -> enode
                | EUnary(op, expr) -> EUnary(op, this.Find(expr))
                | EBinary(op, left, right) ->
                    EBinary(op, this.Find(left), this.Find(right))
                | ETernary(op, expr1, expr2, expr3) ->
                    ETernary(op, this.Find(expr1), this.Find(expr2), this.Find(expr3))
            match memo.TryGetValue(canonical) with
            | true, eid -> this.Find eid
            | _ ->
                let eid = nextId
                nextId <- nextId + 1
                let eclass = {
                    id = eid
                    nodes = Set.singleton canonical
                    parents = Set.empty
                }
                classes[eid] <- eclass
                memo[canonical] <- eid
                unionFind[eid] <- eid
                eid
    member this.AddExpr(expr: Expr) : EClassId =
        match expr with
        | Literal value ->
            this.AddENode(ELiteral value)
        | Variable name ->
            this.AddENode(EVariable name)
        | Unary (op, expr) ->
            this.AddENode(EUnary(op, this.AddExpr(expr)))
        | Binary (op, left, right) ->
            this.AddENode(EBinary(op, this.AddExpr(left), this.AddExpr(right)))
        | Ternary (op, expr1, expr2, expr3) ->
            this.AddENode(ETernary(op, this.AddExpr(expr1), this.AddExpr(expr2),this.AddExpr(expr3)))
    member this.AddBinding(name: string, eid: EClassId) =
        localBindings[name] <- eid
    static member Create(func: Function): EGraph * EClassId =
        let egraph = EGraph()
        for arg in func.args do
            egraph.AddENode(EVariable arg) |> ignore
        for stmt in func.body do
            match stmt with
            | Assign(name, value) -> egraph.AddBinding(name, egraph.AddExpr(value))
            | _ -> failwith "Only assignment statements are allowed in the body"
        match func.ret with
        | Return expr -> egraph, egraph.AddExpr(expr)
        | _ -> failwith "Function must end with a return statement"
    member this.Extract(root: EClassId, leaves: Set<ENode>, costModel: CostModel): Stmt list * Stmt =
        [], Return (Literal 0.0f)