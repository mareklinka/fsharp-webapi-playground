namespace SeedProject.Host.Handlers.AbsenceRequests

open Microsoft.AspNetCore.Http

open FSharp.Control.Tasks

open Giraffe.Core

open SeedProject.Persistence
open SeedProject.Persistence.Model

open SeedProject.Infrastructure.Logging

open SeedProject.Domain.Constructors

open SeedProject.Host
open SeedProject.Host.Pipeline.Operators

module GetRequest =
    let handler id : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            let db = context |> Context.resolve<DatabaseContext>
            let ct = Context.cancellationToken context
            let logger = "GetRequest" |> Context.loggerFactory context

            task {
                let! pipeline =
                    DatabaseId.createAsync
                    &=> AbsenceRequestStore.getSingleRequest db ct
                    &=> Pipeline.transform CommonMethods.toModel
                    &=> Pipeline.sideEffect (fun _ -> Log.AbsenceRequests.retrieved logger id)
                    &=! Pipeline.writeJson
                    <| id

                return! pipeline next context
            }