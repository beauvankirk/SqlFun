﻿namespace SqlFun.Tests

module Common =
    open System.Data.SqlClient
    open System.Configuration
    open SqlFun
    open SqlFun.Queries
    open SqlFun.ParamBuilder
    open SqlFun.Types

    let createConnection () = new SqlConnection(ConfigurationManager.ConnectionStrings.["SqlFunTests"].ConnectionString)

    let generatorConfig = 
        let defaultConfig = createDefaultConfig createConnection
        { defaultConfig with
            paramBuilder = 
                (listDirectParamBuilder (string >> Set([string typeof<int>]).Contains) string) <+> 
                (listParamBuilder isSimpleType "@") <+> 
                defaultConfig.paramBuilder
        }

    let run f = DbAction.run createConnection f

    let createDC() = DataContext.create <| createConnection()

    let runAsync f = AsyncDb.run createConnection f

    let sqlTm tm commandText = sql { generatorConfig with commandTimeout = Some tm } commandText

    let sql commandText = sql generatorConfig commandText

    let proc name = proc generatorConfig name

    let mapFst f (x, y) = f x, y
    let mapSnd f (x, y) = x, f y
