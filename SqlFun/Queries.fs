﻿namespace SqlFun

open System
open System.Collections
open System.Data
open System.Data.Common
open System.Threading.Tasks
open System.Reflection
open System.Linq
open System.Linq.Expressions
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

open Future
open ExpressionExtensions
open Types
open SqlFun.Exceptions

module Queries =

    let private dataRecordColumnAccessMethodByType = 
        [
            typeof<Boolean>,    "GetBoolean"
            typeof<Byte>,       "GetByte"
            typeof<Char>,       "GetChar"
            typeof<DateTime>,   "GetDateTime"
            typeof<Decimal>,    "GetDecimal"
            typeof<Double>,     "GetDouble"
            typeof<float>,      "GetFloat"
            typeof<Guid>,       "GetGuid"
            typeof<Int16>,      "GetInt16"
            typeof<Int32>,      "GetInt32"
            typeof<Int64>,      "GetInt64" 
            typeof<string>,     "GetString"
        ] 
        |> List.map (fun (t, name) -> t, typeof<IDataRecord>.GetMethod(name))

    let private dataRecordGetValueMethod = typeof<IDataRecord>.GetMethod("GetValue")

    let private rmap (f: IDataReader -> 't) (r: IDataReader):'t list =
        List.unfold (fun (r: IDataReader) -> if r.Read() then Some (f(r), r) else None) r

    let rec private rmapAsync (f: IDataReader -> 't) (r: DbDataReader):'t list Async =
        async {
            let! read = Async.AwaitTask(r.ReadAsync())
            if read then
                let head = f(r)
                let! tail = rmapAsync f r
                return head :: tail
            else
                return []
        }


    let private getEnumValues (enumType: Type) = 
        enumType.GetFields()
            |> Seq.filter (fun f -> f.IsStatic)
            |> Seq.map (fun f -> f.GetValue(null), f.GetCustomAttributes<EnumValueAttribute>() |> Seq.map (fun a -> a.Value) |> Seq.tryHead)
            |> Seq.map (fun (e, vopt) -> e, match vopt with Some x -> x | None -> e)
            |> List.ofSeq

    type Toolbox() = 

        static member buildSingleResult (resultBuilder: Func<IDataReader, 't>) = 
            Func<IDataReader, 't>(fun (reader: IDataReader) -> if reader.Read() then resultBuilder.Invoke(reader) else failwith "Value does not exist. Use option type.")
        
        static member buildOptionalResult (resultBuilder: Func<IDataReader, 't>) = 
            Func<IDataReader, 't option>(fun (reader: IDataReader) -> if reader.Read() then Some (resultBuilder.Invoke(reader)) else None)

        static member buildCollectionResult (resultBuilder: Func<IDataReader, 't>) = 
            Func<IDataReader, 't list>(fun (reader: IDataReader) -> reader |> rmap resultBuilder.Invoke)

        static member executeSql (connection: IDbConnection) (transaction: IDbTransaction option) (commandText: string) (assignParams: Func<IDbCommand, int>) (buildResult: Func<IDataReader, 't>) =
            use command = connection.CreateCommand()
            match transaction with
            | Some t -> command.Transaction <- t
            | None -> ()
            command.CommandText <- commandText
            assignParams.Invoke(command) |> ignore
            use reader = command.ExecuteReader()
            buildResult.Invoke(reader)

        static member executeProcedure (connection: IDbConnection) (transaction: IDbTransaction option) (procName: string) (assignParams: Func<IDbCommand, int>) (buildResult: Func<IDataReader, 't>) (buildOutParams: Func<IDbCommand, 'u>) =
            use command = connection.CreateCommand()
            command.CommandText <- procName
            command.CommandType <- CommandType.StoredProcedure
            match transaction with
            | Some t -> command.Transaction <- t
            | None -> ()
            assignParams.Invoke(command) |> ignore
            let retValParam = command.CreateParameter()
            retValParam.ParameterName <- "@RETURN_VALUE"
            retValParam.DbType <- DbType.Int32
            retValParam.Direction <- ParameterDirection.ReturnValue
            command.Parameters.Add(retValParam) |> ignore
            use reader = command.ExecuteReader()
            let retVal = if retValParam.Value = null then 0 else unbox (retValParam.Value)
            let outParamVals = buildOutParams.Invoke(command)
            let result = buildResult.Invoke(reader)
            retVal, outParamVals, result

        static member buildSingleResultAsync (resultBuilder: Func<IDataReader, 't>) = 
            Func<DbDataReader, Async<'t>>(fun (reader: DbDataReader) -> 
                async {
                    let! read = Async.AwaitTask(reader.ReadAsync())
                    if read 
                    then return resultBuilder.Invoke(reader) 
                    else return failwith "Value does not exist. Use option type."
                })
        

        static member buildOptionalResultAsync (resultBuilder: Func<IDataReader, 't>) = 
            Func<DbDataReader, Async<'t option>>(fun (reader: DbDataReader) -> 
                async {
                    let! read = Async.AwaitTask(reader.ReadAsync())
                    if read 
                    then return Some(resultBuilder.Invoke(reader))
                    else return None
                })

        static member buildCollectionResultAsync (resultBuilder: Func<IDataReader, 't>) = 
            Func<DbDataReader, Async<'t list>>(fun (reader: DbDataReader) -> reader |> rmapAsync resultBuilder.Invoke)

        static member executeSqlAsync (connection: IDbConnection) (transaction: IDbTransaction option) (commandText: string) (assignParams: Func<IDbCommand, int>) (buildResult: Func<DbDataReader, Async<'t>>) =
            async {
                use command = (connection :?> DbConnection).CreateCommand()
                command.CommandText <- commandText
                match transaction with
                | Some t -> command.Transaction <- t :?> DbTransaction
                | None -> ()
                assignParams.Invoke(command) |> ignore
                use! reader = Async.AwaitTask(command.ExecuteReaderAsync())
                return! buildResult.Invoke(reader)
            }

        static member executeProcedureAsync (connection: IDbConnection) (transaction: IDbTransaction option) (procName: string) (assignParams: Func<IDbCommand, int>) (buildResult: Func<DbDataReader, Async<'t>>) (buildOutParams: Func<IDbCommand, 'u>) =
            async {
                use command = (connection :?> DbConnection).CreateCommand()
                command.CommandText <- procName
                command.CommandType <- CommandType.StoredProcedure
                match transaction with
                | Some t -> command.Transaction <- t :?> DbTransaction
                | None -> ()
                assignParams.Invoke(command) |> ignore
                let retValParam = command.CreateParameter()
                retValParam.ParameterName <- "@RETURN_VALUE"
                retValParam.DbType <- DbType.Int32
                retValParam.Direction <- ParameterDirection.ReturnValue
                command.Parameters.Add(retValParam) |> ignore
                use! reader = Async.AwaitTask(command.ExecuteReaderAsync())
                let! result = buildResult.Invoke(reader)
                return (if retValParam.Value = null then 0 else retValParam.Value :?> int), buildOutParams.Invoke(command), result                   
            }

        static member asyncBind (v: 't Async, f: Func<'t, 'u Async>) =
            async.Bind(v, f.Invoke)

        static member taskBind (v: Task<'t>, f: Func<'t, 'u Async>) =
            async.Bind(Async.AwaitTask(v), f.Invoke)

        static member asyncReturn  (v: 't) =
            async.Return(v)

        static member unpackOption (value: 't option) = 
            match value with
            | Some v -> v :> obj
            | None -> DBNull.Value :> obj

        static member mapOption (f: Func<'t, obj>) (opt: 't option) = Option.map f.Invoke opt

        static member newList(): 't list =
            List.empty

        static member intToEnum(value: int): 't =
                LanguagePrimitives.EnumOfValue value

        static member throwOnInvalidEnumValue(value: 'v): 't = 
            raise (ArgumentException("The " + value.ToString() + " value can not be converted to " + typeof<'t>.Name + " type."))

        static member compileCaller<'t> (parameters: ParameterExpression list, caller: Expression) =
            Expression.Lambda< Action<'t> >(caller.Reduce(), parameters).Compile()

        static member compileCaller<'t1, 't2> (parameters: ParameterExpression list, caller: Expression) =      
            let compiled = Expression.Lambda< Func<'t1, 't2> >(caller.Reduce(), parameters).Compile()
            fun a -> compiled.Invoke(a)

        static member compileCaller<'t1, 't2, 't3> (parameters: ParameterExpression list, caller: Expression) =
            let compiled = Expression.Lambda< Func<'t1, 't2, 't3> >(caller.Reduce(), parameters).Compile()
            fun a1 a2 -> compiled.Invoke(a1, a2)

        static member compileCaller<'t1, 't2, 't3, 't4> (parameters: ParameterExpression list, caller: Expression) =
            let compiled = Expression.Lambda< Func<'t1, 't2, 't3, 't4> >(caller.Reduce(), parameters).Compile()
            fun a1 a2 a3 -> compiled.Invoke(a1, a2, a3)

        static member compileCaller<'t1, 't2, 't3, 't4, 't5> (parameters: ParameterExpression list, caller: Expression) =
            let compiled = Expression.Lambda< Func<'t1, 't2, 't3, 't4, 't5> >(caller.Reduce(), parameters).Compile()
            fun a1 a2 a3 a4 -> compiled.Invoke(a1, a2, a3, a4)

        static member compileCaller<'t1, 't2, 't3, 't4, 't5, 't6> (parameters: ParameterExpression list, caller: Expression) =
            let compiled = Expression.Lambda< Func<'t1, 't2, 't3, 't4, 't5, 't6> >(caller.Reduce(), parameters).Compile()
            fun a1 a2 a3 a4 a5 -> compiled.Invoke(a1, a2, a3, a4, a5)


    let private getConcreteMethod concreteType methodName = 
        let m = typeof<Toolbox>.GetMethod(methodName, BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic)
        m.MakeGenericMethod([| concreteType |])

    let private getConcreteMethodN concreteTypes methodName = 
        let m = typeof<Toolbox>.GetMethod(methodName, BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic)
        m.MakeGenericMethod(concreteTypes)


    let private getSomeUnionCase (optionType: Type) (value: Expression) = 
        Expression.Call(optionType, "Some", value)

    let private getNoneUnionCase optionType = 
        Expression.Property(null, optionType, "None")


    let private convertIfEnum (expr: Expression) = 
        if expr.Type.IsEnum
        then
            let exprAsObj = Expression.Convert(expr, typeof<obj>) :> Expression
            let values = getEnumValues expr.Type
            if values |> Seq.exists (fun (e, v) -> e <> v)
            then            
                let comparer e = Expression.Call(Expression.Constant(e), "Equals", exprAsObj) :> Expression
                values 
                    |> Seq.fold (fun cexpr (e, v) -> Expression.Condition(comparer e, Expression.Constant(v, typeof<obj>), cexpr) :> Expression) exprAsObj
            else
                exprAsObj
        else
            expr

    let private convertEnumOption (expr: Expression) =
        let param = Expression.Parameter(getUnderlyingType expr.Type, "v")
        if param.Type.IsEnum
        then 
            Expression.Call(getConcreteMethod param.Type "mapOption", Expression.Lambda(convertIfEnum (param), param), expr) :> Expression
        else 
            expr

    let private convertAsNeeded (expr: Expression) =
        if isOption expr.Type 
        then 
            let converter = convertEnumOption expr
            Expression.Call(getConcreteMethod (getUnderlyingType converter.Type) "unpackOption", converter) :> Expression
        else
            Expression.Convert(convertIfEnum expr, typeof<obj>) :> Expression

    let private createInParam (command: Expression) (name: string, getter: Expression, assigner: (obj -> IDbCommand -> int), fakeVal: obj) =
        let adapter = Expression.Constant(Func<obj, Func<IDbCommand, int>>(fun value -> Func<IDbCommand, int>(assigner value)))    
        let valueBound = Expression.Invoke(adapter, convertAsNeeded getter)
        Expression.Invoke(valueBound, command) :> Expression

    let private getDbTypeEnum name = 
        match name with
        | "int" -> DbType.Int32
        | "varchar" -> DbType.String
        | "nvarchar" -> DbType.String
        | "text" -> DbType.String
        | "date" -> DbType.Date
        | "datetime" -> DbType.DateTime
        | "bit" -> DbType.Boolean
        | _ -> DbType.String

    let private createOutParam (command: Expression) (name: string, dbtype: string) =
        let param = Expression.Variable(typeof<IDataParameter>)
        Expression.Block(
            [| param |],
            Expression.Assign(param, Expression.Call(command, "CreateParameter")),
            Expression.Assign(Expression.Property(param, "ParameterName"), Expression.Constant("@" + name)),
            Expression.Assign(Expression.Property(param, "DbType"), Expression.Constant(getDbTypeEnum dbtype)),
            Expression.Assign(Expression.Property(param, "Direction"), Expression.Constant(ParameterDirection.Output)),
            Expression.Call(Expression.Property(command, "Parameters"), "Add", param)) 
        :> Expression

    let private genParamAssigner (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) (outParams: (string * string) list)= 
        let command = Expression.Parameter(typeof<IDbCommand>, "command")
        if paramDefs.Length > 0 then
            let inAssignments = paramDefs |> List.map (createInParam command)  
            let outAssignments = outParams |> List.map (createOutParam command)                                      
            Expression.Lambda(Expression.Block(Seq.append inAssignments outAssignments), command) :> Expression
        else 
            Expression.Lambda(Expression.Constant(0), command) :> Expression

    let private coerce colType (targetType: Type) (expr: Expression) = 
        if targetType.IsEnum then
            let values = getEnumValues targetType       
            if values |> Seq.exists (fun (e, v) -> e <> v)
            then        
                let comparer v = Expression.Call(Expression.Constant(v), typeof<obj>.GetMethod("Equals", [| typeof<obj> |]), Expression.Convert(expr, typeof<obj>))
                let exprAsEnum = Expression.Call(getConcreteMethodN [| expr.Type; targetType|] "throwOnInvalidEnumValue", expr) :> Expression
                values 
                    |> Seq.fold (fun cexpr (e, v) -> Expression.Condition(comparer v, Expression.Constant(e), cexpr) :> Expression) exprAsEnum
            else 
                Expression.Call(getConcreteMethod targetType "intToEnum", expr) :> Expression
        elif targetType = colType 
        then expr
        else Expression.Convert(expr, targetType) :> Expression

    let private getColumnAccessExpr (reader: ParameterExpression) colType ordinal targetType = 
        let accessMethod = match dataRecordColumnAccessMethodByType |> List.tryFind (fun (t, _) -> t = colType) with
                            | Some (_, mth) -> mth
                            | None -> dataRecordGetValueMethod
        let call = Expression.Call(reader, accessMethod, Expression.Constant(ordinal))    
        if isOption targetType
        then 
            Expression.Condition(
                Expression.Call(reader, "IsDBNull", Expression.Constant(ordinal)),                              
                getNoneUnionCase targetType,
                getSomeUnionCase targetType (coerce colType (getUnderlyingType targetType) call)) :> Expression  
        else
            coerce colType targetType call
  
 
    let private getFieldPrefix (field: PropertyInfo) = 
        field.GetCustomAttributes<PrefixedAttribute>() 
        |> Seq.map (fun a -> if a.Name <> "" then a.Name else field.Name)
        |> Seq.fold (fun last next -> next) ""

    let rec private genRowNullCheck reader metadata prefix returnType: Expression = 
        if isOption returnType
        then genRowNullCheck reader metadata prefix (getUnderlyingType returnType)
        elif FSharpType.IsTuple returnType
        then
            FSharpType.GetTupleElements returnType
                |> Seq.map (genRowNullCheck reader metadata prefix)
                |> Seq.reduce (fun left right -> Expression.And(left, right) :> Expression) 
        elif isCollectionType returnType
        then Expression.Constant(true) :> Expression
        else
            FSharpType.GetRecordFields returnType
                |> Seq.map (genRecFieldValueCheck reader metadata prefix)
                |> Seq.reduce (fun left right -> Expression.And(left, right) :> Expression) 
    and
        private genRecFieldValueCheck (reader: Expression) (metadata: Map<string, int * Type>) (prefix: string) (field: PropertyInfo) = 
            if isComplexType field.PropertyType || FSharpType.IsTuple field.PropertyType || isCollectionType field.PropertyType
            then
                genRowNullCheck reader metadata (prefix + getFieldPrefix field) field.PropertyType
            else
                try
                    let (ordinal, colType) = metadata |> Map.find (prefix.ToLower() + field.Name.ToLower())
                    Expression.Call(reader, "IsDBNull", Expression.Constant(ordinal)) :> Expression        
                with ex ->
                    raise (Exception(sprintf "No column found for %s field. Expected: %s%s" field.Name prefix field.Name, ex))


    let rec private genRowBuilderExpr reader metadata prefix returnType =
        if isOption returnType
        then
            Expression.Condition(
                genRowNullCheck reader metadata prefix returnType,
                getNoneUnionCase returnType, 
                getSomeUnionCase returnType (genRowBuilderExpr reader metadata prefix (getUnderlyingType returnType))) :> Expression
        elif FSharpType.IsTuple returnType
        then
            let accessors = FSharpType.GetTupleElements returnType 
                            |> Seq.map (genRowBuilderExpr reader metadata prefix) 
                            |> List.ofSeq                
            Expression.NewTuple(accessors)
        elif isCollectionType returnType
        then
            Expression.Call(getConcreteMethod (getUnderlyingType returnType) "newList", []) :> Expression
        else
            let accessors = FSharpType.GetRecordFields returnType
                            |> Seq.map (genRecFieldValueExpr reader metadata prefix)
                            |> List.ofSeq
            Expression.NewRecord(returnType, accessors)
    and
        private genRecFieldValueExpr (reader: ParameterExpression) (metadata: Map<string, int * Type>) (prefix: string) (field: PropertyInfo) = 
            if isComplexType field.PropertyType || FSharpType.IsTuple field.PropertyType || isCollectionType field.PropertyType
            then
                genRowBuilderExpr reader metadata (prefix + getFieldPrefix field) field.PropertyType
            else
                try 
                    let (ordinal, colType) = metadata |> Map.find (prefix.ToLower() + field.Name.ToLower())
                    getColumnAccessExpr reader colType ordinal field.PropertyType
                with ex ->
                    raise (Exception(sprintf "No column found for %s field. Expected: %s%s" field.Name prefix field.Name, ex))

    let private genRowBuilder metadata returnType =     
        let reader = Expression.Parameter(typeof<IDataReader>, "reader")
        let builder = genRowBuilderExpr reader metadata "" returnType
        Expression.Lambda(builder, reader)


    let private genScalarBuilder metadata returnType =
        let (_, colType) = metadata |> Map.toSeq |> Seq.map (fun (k, v) -> v) |> Seq.find (fun (ord, typ) -> ord = 0)
        let reader = Expression.Parameter(typeof<IDataReader>, "reader")
        let getter = getColumnAccessExpr reader colType 0 returnType
        Expression.Lambda(getter, reader)

    let private nextResult (reader: IDataReader) =
        reader.NextResult() |> ignore

    let private generateResultBuilder metadata returnType isAsync = 

        let generateCall returnType methodName resultBuilder = 
            let concreteMethod = getConcreteMethod returnType (methodName + if isAsync then "Async" else "")
            Expression.Call(concreteMethod, [resultBuilder])

        let buildOneResultSet metadata returnType = 
            if returnType = typeof<unit> then     
                let result = if isAsync then Expression.UnitAsyncConstant else Expression.UnitConstant           
                Expression.Lambda(result, Expression.Parameter(typeof<IDataReader>)) :> Expression
            elif isSimpleType returnType then
                let resultBuilder = genScalarBuilder metadata returnType
                generateCall returnType "buildSingleResult" resultBuilder :> Expression
            elif isSimpleTypeOption returnType then
                let unwrappedRetType = getUnderlyingType returnType
                let resultBuilder = genScalarBuilder metadata unwrappedRetType
                generateCall unwrappedRetType "buildOptionalResult" resultBuilder :> Expression
            elif isCollectionType returnType then
                let unwrappedRetType = getUnderlyingType returnType
                let resultBuilder = if isSimpleType unwrappedRetType 
                                    then genScalarBuilder metadata unwrappedRetType
                                    else genRowBuilder metadata unwrappedRetType
                generateCall unwrappedRetType "buildCollectionResult" resultBuilder :> Expression
            elif isOption returnType then
                let unwrappedRetType = getUnderlyingType returnType
                let resultBuilder = genRowBuilder metadata unwrappedRetType
                generateCall unwrappedRetType "buildOptionalResult" resultBuilder :> Expression
            else
                let resultBuilder = genRowBuilder metadata returnType
                generateCall returnType "buildSingleResult" resultBuilder :> Expression

        let genResultBuilderCall resultBuilder readerExpr tupleIndex = 
            if tupleIndex = 0 
            then
                Expression.Invoke(resultBuilder, [readerExpr]) :> Expression
            else
                let nextResultExpr = Expression.Call(readerExpr, "NextResult")
                Expression.Block(nextResultExpr, Expression.Invoke(resultBuilder, [readerExpr])) :> Expression

        let genResultBuilderCallAsync resultBuilder reader tupleIndex itemType = 
            if tupleIndex = 0 
            then
                Expression.Invoke(resultBuilder, [reader]) :> Expression
            else
                let nextResultExpr = Expression.Call(reader, "NextResultAsync")
                let adaptedBuilder = Expression.Lambda(Expression.Invoke(resultBuilder, reader), Expression.Parameter(typeof<bool>))
                Expression.Call(getConcreteMethodN [| typeof<bool>; itemType |] "taskBind", nextResultExpr, adaptedBuilder) :> Expression

        if (List.length metadata) = 1 
        then
            buildOneResultSet (Seq.head metadata) returnType
        else
            if not (FSharpType.IsTuple returnType) then
                failwith "Function return type for multiple result queries must be a tuple."
            if isAsync 
            then
                let reader = Expression.Parameter(typeof<DbDataReader>, "reader")
                let builders = FSharpType.GetTupleElements returnType 
                                |> Seq.zip metadata
                                |> Seq.mapi (fun i (md, rt) -> Expression.Parameter(rt, "item" + i.ToString()), genResultBuilderCallAsync (buildOneResultSet md rt) reader i rt)
                                |> List.ofSeq                            
                let parameters = builders |> List.map fst
                let tupleBuilder = Expression.Call(getConcreteMethod returnType "asyncReturn", Expression.NewTuple(parameters |> List.map (fun p -> p:> Expression))) :> Expression   
            
                let bindParam (bld: Expression) (param: ParameterExpression, itemExpr: Expression) = 
                    Expression.Call(getConcreteMethodN [| param.Type; returnType |] "asyncBind", itemExpr, Expression.Lambda(bld, param)) :> Expression
                                    
                Expression.Lambda(builders |> List.rev |> List.fold bindParam tupleBuilder, reader) :> Expression
            else
                let reader = Expression.Parameter(typeof<IDataReader>, "reader")
                let builders = FSharpType.GetTupleElements returnType 
                                |> Seq.zip metadata
                                |> Seq.mapi (fun i (md, rt) -> genResultBuilderCall (buildOneResultSet md rt) reader i)
                                |> List.ofSeq
                Expression.Lambda(Expression.NewTuple(builders), reader) :> Expression
               
    let private extractParameterNames commandText = 
        let matches = Regex.Matches(commandText, "\@[a-zA-Z0-9_]+")
        matches.Cast<Match>() 
        |> Seq.collect (fun m -> m.Captures.Cast<Capture>()) 
        |> Seq.map (fun c -> c.Value.Substring(1))
        |> Seq.distinct
        |> List.ofSeq
    
    let private isConnection (expr: Expression) = 
        typeof<IDbConnection>.IsAssignableFrom(expr.Type)

    let private isTransactionOption (expr: Expression) = 
        typeof<IDbTransaction option>.IsAssignableFrom(expr.Type)


    let private buildInParam (name: string, expr: Expression) value (command: IDbCommand) =
        let param = command.CreateParameter()
        param.ParameterName <- "@" + name
        param.Value <- value
        command.Parameters.Add(param)            

    let rec private getFakeValue (dataType: Type) = 
        if isOption dataType
        then getFakeValue (getUnderlyingType dataType)
        elif dataType = typeof<DateTime>
        then DateTime.Now :> obj
        elif dataType = typeof<string>
        then "" :> obj
        elif dataType.IsClass || dataType.IsInterface
        then null 
        else Activator.CreateInstance(dataType)

    let private skipUsedParamNames paramExprs paramNames = 
        let usedNames = paramExprs |> Seq.map (fun (name, _, _, _) -> name) |> Seq.except ["<connection>"; "<transaction>"] |> List.ofSeq
        let length = List.length usedNames
        if paramNames |> Seq.take length |> Seq.except usedNames |> Seq.isEmpty
        then paramNames |> List.skip length
        else failwith "Inconsistent parameter list."

    let rec private getTupleParamExpressions (customPB: ParamBuilder) (expr: Expression) (index: int) (paramNames: string list) = 
        let tupleItemTypes = FSharpType.GetTupleElements (expr.Type)
        if index = tupleItemTypes.Length
        then
            []
        else
            let param = Expression.TupleGet(expr, index)
            let paramExprs = customPB "" (Seq.head paramNames) param paramNames
            List.append paramExprs (getTupleParamExpressions customPB expr (index + 1) (skipUsedParamNames paramExprs paramNames))

    let rec private getParamExpressions (customPB: ParamBuilder) (prefix: string) (name: string) (expr: Expression) (paramNames: string list) = 
        if isConnection expr
        then ["<connection>", expr, (fun _ _ -> 0), null :> obj] 
        elif isTransactionOption expr
        then ["<transaction>", expr, (fun _ _ -> 0), null :> obj] 
        elif isComplexType expr.Type 
        then
            expr.Type.GetProperties()
            |> Seq.collect (fun p -> customPB (prefix + getFieldPrefix p) p.Name (Expression.Property(expr, p)) paramNames)
            |> Seq.filter (fun (name, _, _, _) -> ("<connection>" :: "<transaction>" :: paramNames) |> Seq.exists ((=) name))
            |> List.ofSeq
        elif FSharpType.IsTuple expr.Type
        then
            getTupleParamExpressions customPB expr 0 paramNames
        else
            [prefix + name, expr, buildInParam (prefix + name, expr), getFakeValue expr.Type]        
        

    let private compileCaller (paramDefs: ParameterExpression list) (caller: Expression) = 
        let compiler = typeof<Toolbox>.GetMethods(BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic) 
                        |> Seq.find (fun m -> m.Name = "compileCaller" && m.GetGenericArguments().Length = (List.length paramDefs) + 1)
        let compiler = compiler.MakeGenericMethod(Seq.append (paramDefs |> Seq.map (fun p -> p.Type)) [caller.Type] |> Seq.toArray)
        compiler.Invoke(null, [| paramDefs; caller |])

    let private withoutConnectionAndTransaction (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) = 
        paramDefs |> List.filter (fun (name, _, _, _) -> not (["<connection>"; "<transaction>"] |> Seq.exists ((=) name)))

    let private getConnectionExpr (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) =
        match paramDefs |> List.tryFind (fun (name, _, _, _) -> name = "<connection>") with
        | Some (_, cexpr, _, _) -> cexpr
        | None -> failwith "Connection parameter required."

    let private getTransactionExpr (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) =
        match paramDefs |> List.tryFind (fun (name, _, _, _) -> name = "<transaction>") with
        | Some (_, texpr, _, _) -> texpr
        | None -> getNoneUnionCase typeof<IDbTransaction option> :> Expression

    let private getTableTypeName (collectionType: Type) = 
        (getUnderlyingType collectionType).Name


    let private wireUpPBs (customPB: ParamBuilder -> ParamBuilder) (defaultPB: ParamBuilder -> ParamBuilder): ParamBuilder = 
        let emptyPB: ParamBuilder = fun _ _ _ _ -> []        
        let boundDefault = ref emptyPB
        let boundCustom = (fun prefix name expr names -> customPB boundDefault.Value prefix name expr names)
        boundDefault.Value <- (fun prefix name expr names -> defaultPB boundCustom prefix name expr names)
        boundCustom
    
    let private unfoldFunction (t: Type) =     
        if not (FSharpType.IsFunction t)
        then
            [], t
        else
            let args, rets = List.unfold (fun f -> 
                                if FSharpType.IsFunction f
                                then Some (let a, r = FSharpType.GetFunctionElements f in (a, r), r)
                                else None) t
                             |> List.unzip
            args, Seq.last rets

    let private foldFunction (args: Type list, ret: Type) = 
       args 
       |> List.rev 
       |> List.fold (fun f t -> FSharpType.MakeFunctionType (t, f)) ret

    let private generateSqlCommandCaller<'c when 'c :> IDbConnection> (createConnection: unit -> 'c) (customParamBuilder: ParamBuilder -> ParamBuilder) (commandText: string)  (t: Type): obj = 

        let customPB = wireUpPBs customParamBuilder getParamExpressions

        let getResultMetadata (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) = 
            use connection = createConnection()
            connection.Open()
            use command = connection.CreateCommand()
            command.CommandText <- commandText
            for _, expr, buildParam, fakeVal in paramDefs do
                buildParam fakeVal command |> ignore
            use schemaOnlyReader = command.ExecuteReader(CommandBehavior.SchemaOnly)
            let getOneResultMetadata () = [0.. schemaOnlyReader.FieldCount - 1] 
                                            |> Seq.map (fun i -> schemaOnlyReader.GetName(i).ToLower(), (i, schemaOnlyReader.GetFieldType(i))) 
                                            |> Map.ofSeq
            let initial = getOneResultMetadata ()
            initial :: List.unfold (fun _ -> if schemaOnlyReader.NextResult() then Some (getOneResultMetadata(), ()) else None) ()

        let genExecutor paramDefs returnType = 
            let queryParamDefs = paramDefs |> withoutConnectionAndTransaction
            let assignParams = genParamAssigner queryParamDefs []    
            let metadata = if returnType <> typeof<unit> && not (Types.isAsyncOf returnType typeof<unit>) // Npgsql hangs on NextResult if no first result exists
                            then getResultMetadata queryParamDefs
                            else [Map.empty]
            let connection = getConnectionExpr paramDefs
            let transaction = getTransactionExpr paramDefs
            let sql = Expression.Constant(commandText) :> Expression
            if isAsync returnType
            then
                let underlyingType = getUnderlyingType returnType
                let buildResult = generateResultBuilder metadata underlyingType true
                Expression.Call(getConcreteMethod underlyingType "executeSqlAsync", [ connection; transaction; sql; assignParams; buildResult ])
            else 
                let buildResult = generateResultBuilder metadata returnType false
                Expression.Call(getConcreteMethod returnType "executeSql", [ connection; transaction; sql; assignParams; buildResult ])

        let rec generateCaller t paramNames paramDefs = 
            if not (FSharpType.IsFunction t)
            then 
                [], genExecutor paramDefs t
            else 
                let firstParamType, remainingParams = FSharpType.GetFunctionElements t
                let param = Expression.Parameter(firstParamType, Seq.head paramNames)
                let paramGetters = if FSharpType.IsTuple firstParamType 
                                    then getTupleParamExpressions customPB param 0 paramNames
                                    else customPB "" param.Name param paramNames
                let (paramExprs, caller) = generateCaller remainingParams (skipUsedParamNames paramGetters paramNames) (List.append paramDefs paramGetters)
                (param :: paramExprs), caller

        try
            let parameterNames = extractParameterNames commandText
            let (paramExprs, caller) = generateCaller t (List.append parameterNames [""]) []
            compileCaller paramExprs caller 
        with ex ->
            raise <| CompileTimeException(t, "sql command", commandText, ex)
                
    /// <summary>
    /// Generates function executing a sql command.
    /// </summary>
    /// <typeparam name="'t">
    /// The function type.
    /// </typeparam>
    /// <param name="createConnection">
    /// The function providing a database connection used in generation.
    /// </param>
    /// <param name="commandText">
    /// The sql statement to be executed.
    /// </param>
    /// <returns>
    /// A function of type 't executing command given by commandText parameter.
    /// </returns>
    let sql<'t, 'c when 'c :> IDbConnection> (createConnection: unit -> 'c) (customParamBuilder: ParamBuilder -> ParamBuilder) (commandText: string): 't = 
        generateSqlCommandCaller createConnection customParamBuilder commandText typeof<'t> :?> 't

    let getStoredProcElementTypes returnType =
        if FSharpType.IsTuple returnType
        then
            let elementTypes = FSharpType.GetTupleElements returnType
            if elementTypes.Length = 3 then
                elementTypes.[1], elementTypes.[2]
            else
                failwith "StoredProcedure return type must be 3-element tuple."
        else 
            failwith "StoredProcedure return type must be 3-element tuple."

    let genOutParamsBuilder (outParams: (string * string) list) (outParamsType: Type) = 
        let command = Expression.Parameter(typeof<IDbCommand>)
        let getParamExpr name = Expression.Property(
                                    Expression.Convert(
                                        Expression.Property(
                                            Expression.Property(command, "Parameters"), "Item", Expression.Constant("@" + name)), 
                                            typeof<IDataParameter>), "Value")
        let wrapInOption = if isOption outParamsType
                           then (fun (expr: Expression) (coerce: Expression -> Expression) -> 
                                    Expression.Condition(
                                        Expression.Call(expr, "Equals", Expression.Constant(DBNull.Value) :> Expression), 
                                        getNoneUnionCase outParamsType, 
                                        getSomeUnionCase outParamsType (coerce expr)) 
                                        :> Expression)
                           else (fun expr coerce -> coerce expr)
        let outParamsType = if isOption outParamsType 
                            then getUnderlyingType outParamsType 
                            else outParamsType       
        let expr = 
            if isSimpleType outParamsType
            then wrapInOption (getParamExpr (outParams |> Seq.map fst |> Seq.head)) (coerce typeof<obj> outParamsType)
            elif FSharpType.IsTuple outParamsType 
            then Expression.NewTuple (FSharpType.GetTupleElements outParamsType 
                                        |> Seq.mapi (fun i t -> wrapInOption (getParamExpr (fst outParams.[i])) (coerce typeof<obj> t)) 
                                        |> Seq.toList)
            elif FSharpType.IsRecord outParamsType
            then Expression.NewRecord(outParamsType, FSharpType.GetRecordFields outParamsType 
                                                        |> Seq.map (fun p -> wrapInOption (getParamExpr p.Name) (coerce typeof<obj> p.PropertyType)) 
                                                        |> Seq.toList)
            else Expression.UnitConstant :> Expression
        Expression.Lambda(expr, command) :> Expression
        

    /// <summary>
    /// Generates a function executing stored procedure.
    /// </summary>
    /// <typeparam name="t">
    /// The function type.
    /// </typeparam>
    /// <param name="createConnection">
    /// The function providing a database connection used in generation.
    /// </param>
    /// <param name="procedureName">
    /// The stored procedure to be executed.
    /// </param>
    /// <returns>
    /// A function of type 't executing stored procedure given by procedureName parameter.
    /// </returns>
    let private generateStoredProcCaller<'c when 'c :> IDbConnection> (createConnection: unit -> 'c) (customParamBuilder: ParamBuilder -> ParamBuilder) (procedureName: string) (t: Type): obj =

        let customPB = wireUpPBs customParamBuilder getParamExpressions

        let extractProcParamNames (procedureName: string) = 
            use connection = createConnection()
            connection.Open()
            use command = connection.CreateCommand()
            command.CommandText <- "select p.parameter_name, p.parameter_mode, p.data_type
                                    from information_schema.parameters p
                                         join information_schema.routines r on r.specific_name = p.specific_name
                                    where r.routine_name = @procedure_name
                                    order by p.ordinal_position"
            let param = command.CreateParameter()
            param.ParameterName <- "@procedure_name"
            param.Value <- procedureName.Split('.') |> Seq.last
            command.Parameters.Add(param) |> ignore
            use reader = command.ExecuteReader()
            reader |> rmap (fun r -> (if r.GetString(0).StartsWith("@") then r.GetString(0).Substring(1) else r.GetString(0)), r.GetString(1) <> "IN", r.GetString(2))

        let getResultMetadata (paramDefs: (string * Expression * (obj -> IDbCommand -> int) * obj) list) (outParams: (string * string) list) = 
            use connection = createConnection()
            connection.Open()
            use command = connection.CreateCommand()
            command.CommandText <- procedureName
            command.CommandType <- CommandType.StoredProcedure
            for _, expr, buildParam, fakeVal in paramDefs do
                buildParam fakeVal command |> ignore
            for name, dbtype in outParams do
                let param = command.CreateParameter()
                param.ParameterName <- "@" + name
                param.DbType <- getDbTypeEnum dbtype
                param.Direction <- ParameterDirection.Output
                command.Parameters.Add param |> ignore

            use schemaOnlyReader = command.ExecuteReader(CommandBehavior.SchemaOnly)
            let getOneResultMetadata () = [0.. schemaOnlyReader.FieldCount - 1] 
                                            |> Seq.map (fun i -> schemaOnlyReader.GetName(i).ToLower(), (i, schemaOnlyReader.GetFieldType(i))) 
                                            |> Map.ofSeq
            let initial = getOneResultMetadata ()
            initial :: List.unfold (fun _ -> if schemaOnlyReader.NextResult() then Some (getOneResultMetadata(), ()) else None) ()

        let genExecutor paramDefs outParams returnType = 
            let queryParamDefs = paramDefs |> withoutConnectionAndTransaction
            let assignParams = genParamAssigner queryParamDefs outParams      
            let metadata = if returnType <> typeof<unit> && not (Types.isAsyncOf returnType typeof<unit>) // Npgsql hangs on NextResult if no first result exists
                            then getResultMetadata queryParamDefs outParams
                            else [Map.empty]
            let connection = getConnectionExpr paramDefs
            let transaction = getTransactionExpr paramDefs
            let sql = Expression.Constant(procedureName) :> Expression
            if isAsync returnType
            then
                let underlyingType = getUnderlyingType returnType
                let (outParamsType, resultType) = getStoredProcElementTypes underlyingType
                let buildResult = generateResultBuilder metadata resultType true
                let outParamBuilder = genOutParamsBuilder outParams outParamsType
                Expression.Call(getConcreteMethodN [| resultType; outParamsType |] "executeProcedureAsync", [ connection; transaction; sql; assignParams; buildResult; outParamBuilder ])
            else 
                let (outParamsType, resultType) = getStoredProcElementTypes returnType
                let buildResult = generateResultBuilder metadata resultType false
                let outParamBuilder = genOutParamsBuilder outParams outParamsType
                Expression.Call(getConcreteMethodN [| resultType; outParamsType |] "executeProcedure", [ connection; transaction; sql; assignParams; buildResult; outParamBuilder ])

        let rec generateCaller t inParams outParams paramDefs = 
            if not (FSharpType.IsFunction t)
            then 
                [], genExecutor paramDefs outParams t
            else 
                let firstParamType, remainingParams = FSharpType.GetFunctionElements t
                let param = Expression.Parameter(firstParamType, Seq.head inParams)
                let paramGetters = if FSharpType.IsTuple firstParamType
                                    then
                                        getTupleParamExpressions customPB param 0 inParams
                                    else 
                                        customPB "" param.Name param inParams
                let (paramExprs, caller) = generateCaller remainingParams (skipUsedParamNames paramGetters inParams) outParams (List.append paramDefs paramGetters)
                (param :: paramExprs), caller

        try
            let parameters = extractProcParamNames procedureName 
            let inParams = parameters 
                            |> Seq.filter (fun (_, isOut, _) -> not isOut) 
                            |> Seq.map (fun (name, _, _) -> name) 
                            |> List.ofSeq
            let outParams = parameters 
                            |> Seq.filter (fun (_, isOut, _) -> isOut)
                            |> Seq.map (fun (name, _, dbtype) -> name, dbtype)
                            |> List.ofSeq
            let (paramExprs, caller) = generateCaller t (List.append inParams [""]) outParams []
            compileCaller paramExprs caller
        with ex ->
            raise <| CompileTimeException(t, "stored procedure", procedureName, ex) 

    /// <summary>
    /// Generates a function executing stored procedure.
    /// </summary>
    /// <typeparam name="'t">
    /// The function type.
    /// </typeparam>
    /// <param name="createConnection">
    /// The function providing a database connection used in generation.
    /// </param>
    /// <param name="procedureName">
    /// The stored procedure to be executed.
    /// </param>
    /// <returns>
    /// A function of type 't executing stored procedure given by procedureName parameter.
    /// </returns>
    let storedproc<'t, 'c when 'c :> IDbConnection> (createConnection: unit -> 'c) (customParamBuilder: ParamBuilder -> ParamBuilder) (procedureName: string): 't =
        generateStoredProcCaller createConnection customParamBuilder procedureName typeof<'t> :?> 't

    /// <summary>
    /// The parameter builder function passing control to internal parameter builder.
    /// </summary>
    /// <param name="defaultPB">
    /// The internal parameter builder.
    /// </param>
    let defaultParamBuilder (defaultPB: ParamBuilder): ParamBuilder = 
        defaultPB
