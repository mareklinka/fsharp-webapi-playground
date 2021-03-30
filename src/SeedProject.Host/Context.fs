namespace SeedProject.Host

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open SeedProject.Infrastructure

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


    let asOperation f value =
        task {
            return
                value |> f |> OperationResult.fromResult
        }

    open Giraffe
    let private validationErrorOutput = RequestErrors.NOT_ACCEPTABLE
    let private operationErrorOutput = RequestErrors.BAD_REQUEST

    let apiOutput apiOutput = (apiOutput, validationErrorOutput, operationErrorOutput)
    let jsonOutput = (json, validationErrorOutput, operationErrorOutput)