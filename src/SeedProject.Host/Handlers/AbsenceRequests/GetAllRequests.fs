namespace SeedProject.Host.Handlers.AbsenceRequests

open Microsoft.AspNetCore.Http

open FSharp.Control.Tasks

open Giraffe.Core

open SeedProject.Architecture.StructurizrExtensions
open SeedProject.Architecture.Common.Constants
open SeedProject.Persistence
open SeedProject.Persistence.Model
open SeedProject.Infrastructure.Logging
open SeedProject.Host
open SeedProject.Host.Pipeline.Operators

open Structurizr.Annotations

[<Component(Description = "Retrieves existing requests", Technology = "F#")>]
[<UsesComponentEx(nameof AbsenceRequestStore, Description = "Reads requests using")>]
[<UsedByPerson(MainUserName, Description = "Calls endpoint", Technology = "JSON/HTTPS")>]
module GetAllRequests =
    let toModelList = List.map CommonMethods.toModel

    let handler : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            let db = context |> Context.resolve<DatabaseContext>
            let ct = Context.cancellationToken context
            let logger = "GetAllRequests" |> Context.loggerFactory context

            task {
                let pipeline =
                    (fun () -> AbsenceRequestStore.getAllRequests db ct)
                    &=> Pipeline.transform toModelList
                    &=> Pipeline.sideEffect (fun items -> Log.AbsenceRequests.listRetrieved logger (items |> List.length))
                    &=! Pipeline.writeJson

                let! pipelineResult = pipeline()

                return! pipelineResult next context
            }