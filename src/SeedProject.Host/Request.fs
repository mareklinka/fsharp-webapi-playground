namespace SeedProject.Host

open SeedProject.Domain
open SeedProject.Domain.Common
open Microsoft.AspNetCore.Http

[<RequireQualifiedAccess>]
module Request =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    let respond (context: HttpContext) resultTask =
        async {
            let! result = resultTask

            do!
                match result with
                | OperationResult.Success value ->
                    context.Response.WriteAsJsonAsync(value)
                | OperationResult.ValidationError (code, ValidationMessage message) ->
                    context.Response.WriteAsJsonAsync({| Code = code; Message = message |})
                | OperationResult.OperationError (code, OperationMessage message) ->
                    context.Response.WriteAsJsonAsync({| Code = code; Message = message |})
                |> Async.AwaitTask
                |> Async.Ignore
            ()
        }

    let run (context: HttpContext) handler =
        async {
            do! context |> handler
        }
