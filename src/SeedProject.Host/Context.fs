namespace SeedProject.Host

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open SeedProject.Persistence.Model
open SeedProject.Persistence
open SeedProject.Infrastructure

[<RequireQualifiedAccessAttribute>]
module Context =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    let resolve<'a> (context: HttpContext) =
        context.RequestServices.GetRequiredService<'a>()

    let cancellationToken (context: HttpContext) = context.RequestAborted

    let loggerFactory (context: HttpContext) : string -> Logging.Types.LogSink =
        let loggerFactory = context |> resolve<ILoggerFactory>

        fun category ->
            let logger = loggerFactory.CreateLogger category

            fun message logLevel ->
                match logLevel with
                | Logging.Types.LogLevel.Debug -> logger.LogDebug(message)
                | Logging.Types.LogLevel.Information -> logger.LogInformation(message)
                | Logging.Types.LogLevel.Warning -> logger.LogWarning(message)
                | Logging.Types.LogLevel.Error -> logger.LogError(message)
                | Logging.Types.LogLevel.Critical -> logger.LogCritical(message)


    let asOperation f value =
        async {
            return
                value |> f |> OperationResult.fromResult
        }

    let apiOutput apiOutput = (apiOutput, Giraffe.Core.json)
    let jsonOutput = (Giraffe.Core.json, Giraffe.Core.json)

    module Database =
        let resolve = resolve<DatabaseContext>

        let save db ct r =
            async {
                do!
                    Db.saveChanges ct db

                return operation { return r }
            }

        let commit (db: DatabaseContext) ct r =
            async {
                match db.Database.CurrentTransaction with
                | null -> ()
                | t ->
                    do! t |> Db.commit ct

                return operation { return r }
            }

        let beginTransaction (db: DatabaseContext) ct r =
            async {
                do!
                    db.Database.BeginTransactionAsync(ct)
                    |> Async.AwaitTask
                    |> Async.Ignore

                return operation { return r }
            }