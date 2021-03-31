namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

open Giraffe.Core

open SeedProject.Infrastructure.Operators
open SeedProject.Domain.Constructors
open SeedProject.Persistence
open SeedProject.Host
open SeedProject.Infrastructure.Logging
open SeedProject.Persistence.Model

module GetRequest =
    let handler id : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            let db = context |> Context.resolve<DatabaseContext>
            let ct = Context.cancellationToken context
            let logger = "GetRequest" |> Context.loggerFactory context

            task {
                let! pipeline =
                    DatabaseId.createAsync
                    &=> AbsenceRequestPersistence.getSingleRequest db ct
                    &=> (CommonMethods.toModel |> Context.asOperation)
                    &== (fun _ -> unitTask { SemanticLog.absenceRequestRetrieved logger id })
                    &=! Context.jsonOutput
                    <| id

                return! pipeline next context
            }