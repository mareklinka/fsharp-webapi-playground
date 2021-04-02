namespace SeedProject.Host.Handlers.AbsenceRequests

open Microsoft.AspNetCore.Http

open Giraffe

open SeedProject.Infrastructure.Logging

open SeedProject.Persistence
open SeedProject.Persistence.Model

open SeedProject.Host
open SeedProject.Domain.Constructors
open SeedProject.Host.Pipeline.Operators

open FSharp.Control.Tasks

open Structurizr.Annotations
open SeedProject.Architecture.StructurizrExtensions
open SeedProject.Architecture.Common.Constants

[<Component(Description = "Deletes a request", Technology = "F#")>]
[<TypeUsesComponent(nameof AbsenceRequestStore, Description = "Deletes requests using")>]
[<UsedByPerson(MainUserName, Description = "Calls endpoint", Technology = "JSON/HTTPS")>]
module DeleteRequest =
    let handler id : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let db = Context.resolve<DatabaseContext> context
                let ct = Context.cancellationToken context

                let logger =
                    "DeleteRequest" |> Context.loggerFactory context

                let! pipeline =
                    DatabaseId.createAsync
                    &=> Pipeline.beginTransaction ct db
                    &=> AbsenceRequestStore.getSingleRequest db ct
                    &=> Pipeline.transform (fun _ -> id)
                    &=> AbsenceRequestStore.deleteEntity db
                    &=> Pipeline.saveChanges ct db
                    &=> Pipeline.commit ct db
                    &=> Pipeline.sideEffect (fun _ -> Log.AbsenceRequests.deleted logger id)
                    &=! (Pipeline.response (fun _ -> setStatusCode 200))
                    <| id

                return! pipeline next context
            }