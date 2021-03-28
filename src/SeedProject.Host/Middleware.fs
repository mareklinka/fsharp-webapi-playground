namespace SeedProject.Host

open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open System.Net

[<RequireQualifiedAccess>]
module Middleware =
    let exceptionHandler (next: RequestDelegate) =
        RequestDelegate
            (fun (ctx: HttpContext) ->
                async {
                    try
                        do! next.Invoke(ctx) |> Async.AwaitTask
                    with e ->
                        ctx.Response.StatusCode <- HttpStatusCode.InternalServerError |> int
                        do!
                            ctx.Response.WriteAsJsonAsync(
                                {| Code = "Internal Server Error"
                                   Message = "Sorry, something went wrong"
                                   Exception = e.ToString() |}
                            )
                            |> Async.AwaitTask
                } |> Async.StartAsTask :> Task)