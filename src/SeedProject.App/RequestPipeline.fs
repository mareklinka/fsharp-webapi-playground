namespace SeedProject.App

open SeedProject.Domain
open SeedProject.Domain.Common
open Microsoft.EntityFrameworkCore
open SeedProject.Persistence.Model

[<RequireQualifiedAccess>]
module DependencyInjection =
    let createContextOptions =
        let builder =
            new DbContextOptionsBuilder<DatabaseContext>()

        builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Database=FSharpSeedDatabase")
        |> ignore

        fun () -> builder.Options

    let createContext (options: DbContextOptions<DatabaseContext>) = new DatabaseContext(options)

[<RequireQualifiedAccess>]
module Request =
    open Microsoft.EntityFrameworkCore.Storage
    open SeedProject.Persistence
    open System.Threading

    let private operation =
        OperationResult.OperationResultBuilder.Instance

    type RequestContext<'a, 'b> =
        { Database: DatabaseContext
          Input: 'a
          Transaction: IDbContextTransaction
          CancellationToken: CancellationToken
          Result: Option<'b> }

    let private writeResponse = printfn "API RESPONSE: %s"

    let respond resultTask =
        async {
            let! result = resultTask

            match result with
            | OperationResult.Success context ->
                match context.Result with
                | Some _ -> writeResponse "OK"
                | None -> writeResponse "OK - No result"
            | OperationResult.ValidationError (code, ValidationMessage message) ->
                writeResponse (sprintf "Validation error %A: %s" code message)
            | OperationResult.OperationError (code, OperationMessage message) ->
                writeResponse (sprintf "Operation error %A: %s" code message)

            ()
        }

    let respondWithValidationError (code, ValidationMessage message) =
        async {
            writeResponse (sprintf "Validation error %A: %s" code message)
            ()
        }

    let respondWithOperationError (code, OperationMessage message) =
        async {
            writeResponse (sprintf "Validation error %A: %s" code message)
            ()
        }

    let buildRequestContext requestReader =
        async {
            let database =
                DependencyInjection.createContextOptions ()
                |> DependencyInjection.createContext

            let cts = new CancellationTokenSource()
            let! t = Db.beginTransaction cts.Token database
            let! readerResult = requestReader ()

            return
                operation {
                    let! requestReaderValue = readerResult

                    let context : RequestContext<'a, 'b> =
                        { RequestContext.Database = database
                          Input = requestReaderValue
                          Transaction = t
                          CancellationToken = cts.Token
                          Result = None }

                    return context
                }
        }

    let run contextBuilder handler =
        async {
            try
                let! contextBuildResult = contextBuilder ()

                match contextBuildResult with
                | OperationResult.Success context ->
                    try
                        do! context |> handler
                    finally
                        printfn "RUNNER: Request context cleanup"
                        context.Database.Dispose()
                        context.Transaction.Dispose()
                | OperationResult.ValidationError (code, message) ->
                    do! respondWithValidationError (code, message)
                | OperationResult.OperationError (code, message) ->
                    do! respondWithOperationError (code, message)
            with e -> printfn "%s" (e.ToString())
        }
