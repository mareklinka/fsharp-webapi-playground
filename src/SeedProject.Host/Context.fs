namespace SeedProject.Host

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open SeedProject.Infrastructure
open SeedProject.Persistence

[<RequireQualifiedAccessAttribute>]
module Context =
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

[<RequireQualifiedAccess>]
module Pipeline =
    let beginTransaction ct db r =
        task {
            do! Db.beginTransaction ct db
            return Success r
        }

    let saveChanges ct db r =
        task {
            do! Db.saveChanges ct db
            return Success r
        }

    let commit ct db r =
        task {
            do! Db.commit ct db
            return Success r
        }

    let sideEffect f r =
        r |> f
        System.Threading.Tasks.Task.FromResult(Success r)

    let transform f r =
        System.Threading.Tasks.Task.FromResult(r |> f |> Success)

    open Giraffe

    let private validationErrorOutput = RequestErrors.NOT_ACCEPTABLE
    let private operationErrorOutput = RequestErrors.BAD_REQUEST

    let response apiOutput =
        (apiOutput, validationErrorOutput, operationErrorOutput)
    let writeJson = (json, validationErrorOutput, operationErrorOutput)