namespace SeedProject.Host

open System.Text.Json
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open SeedProject.Persistence.Model
open SeedProject.Persistence
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure

[<RequireQualifiedAccessAttribute>]
module Context =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    let optionConverter = new OptionConverter()

    let private jsonOptions = new JsonSerializerOptions()
    jsonOptions.Converters.Add(optionConverter)

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
    module RequestBody =
        let readAs<'a> (context: HttpContext) =
            context
                .Request
                .ReadFromJsonAsync<'a>(jsonOptions, context.RequestAborted)
                .AsTask()
            |> Async.AwaitTask

    module ResponseBody =
        let private writeContent (context: HttpContext) value code =
            async {

                let actionContext = new ActionContext(HttpContext = context)

                context.Response.StatusCode <- code

                match value with
                | Some value ->
                    let result = new OkObjectResult(value)
                    do! (result.ExecuteResultAsync(actionContext) |> Async.AwaitTask)
                | None ->
                    let result = new OkResult()
                    do! (result.ExecuteResultAsync(actionContext) |> Async.AwaitTask)

                return OperationResult.fromResult ()
            }

        let okWith (context: HttpContext) transformer value =
            writeContent context (Some(transformer value)) 200

        let ok (context: HttpContext) _ = writeContent context None 200
        let badRequest (context: HttpContext) value = writeContent context (Some value) 406
        let internalServerError (context: HttpContext) value = writeContent context (Some value) 500

    module Route =
        let readInt parameterName (context: HttpContext) =
            async {
                return
                    operation {
                        let value =
                            context.Request.RouteValues.[parameterName]

                        match value with
                        | null ->
                            return!
                                OperationResult.operationError (
                                    InvariantBroken RouteParameterMissing,
                                    OperationMessage $"The specified route parameter was not found: {parameterName}"
                                )
                        | :? string as s -> return (s |> int)
                        | _ ->
                            return!
                                OperationResult.operationError (
                                    InvariantBroken RouteParameterInvalid,
                                    OperationMessage
                                        "The specified route parameter was of unexpected type: {parameterName}"
                                )
                    }
            }
