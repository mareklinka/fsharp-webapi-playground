namespace SeedProject.Host

open System
open Microsoft.Extensions.Logging

open Giraffe

[<RequireQualifiedAccess>]
module Middleware =
    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse
        >=> ServerErrors.INTERNAL_ERROR {| Code = "Internal Server Error"
                                           Message = "Sorry, something went wrong"
                                           Exception = ex.ToString() |}