namespace SeedProject.Host.Handlers.AbsenceRequests

open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

open Giraffe.Core

open SeedProject.Infrastructure.Operators
open SeedProject.Persistence
open SeedProject.Host
open SeedProject.Infrastructure.Logging
open SeedProject.Persistence.Model

module GetAllRequests =
    let toModelList = List.map CommonMethods.toModel

    let handler : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            let db = context |> Context.resolve<DatabaseContext>
            let ct = Context.cancellationToken context
            let logger = "GetAllRequests" |> Context.loggerFactory context

            task {
                let pipeline =
                    (fun () -> AbsenceRequestPersistence.getAllRequests db ct)
                    &=> Pipeline.transform toModelList
                    &=> Pipeline.sideEffect (fun items -> SemanticLog.absenceRequestsRetrieved logger (items |> List.length))
                    &=! Pipeline.writeJson

                let! pipelineResult = pipeline()

                return! pipelineResult next context
            }