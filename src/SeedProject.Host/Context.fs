namespace SeedProject.Host

open System.Text.Json

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open SeedProject.Persistence.Model
open SeedProject.Persistence
open SeedProject.Domain
open SeedProject.Domain.Common

[<RequireQualifiedAccessAttribute>]
module Context =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    module Database =
        let resolve (context: HttpContext) =
            context.RequestServices.GetRequiredService<DatabaseContext>()

        let save context r =
            async {
                do!
                    context
                    |> resolve
                    |> Db.saveChanges context.RequestAborted

                return operation { return r }
            }

        let commit (context: HttpContext) r =
            async {
                let db = context |> resolve

                match db.Database.CurrentTransaction with
                | null -> ()
                | t -> do! t |> Db.commit context.RequestAborted

                return operation { return r }
            }

        let beginTransaction (context: HttpContext) r =
            async {
                let db = context |> resolve

                do!
                    db.Database.BeginTransactionAsync(context.RequestAborted)
                    |> Async.AwaitTask
                    |> Async.Ignore

                return operation { return r }
            }

    let private jsonOptions = new JsonSerializerOptions()
    jsonOptions.Converters.Add(new OptionConverter())

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
                context.Response.StatusCode <- code

                match value with
                | Some value ->
                    do!
                        context.Response.WriteAsJsonAsync(value, jsonOptions, context.RequestAborted)
                        |> Async.AwaitTask
                | None -> ()

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
