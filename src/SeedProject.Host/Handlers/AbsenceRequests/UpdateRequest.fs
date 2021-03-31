namespace SeedProject.Host.Handlers.AbsenceRequests

open Microsoft.AspNetCore.Http

open Giraffe

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure.Operators
open SeedProject.Domain.Constructors
open SeedProject.Persistence
open SeedProject.Persistence.Model
open SeedProject.Host
open SeedProject.Domain
open SeedProject.Infrastructure
open SeedProject.Infrastructure.Logging

open FSharp.Control.Tasks

module UpdateRequest =
    module Private =
        open Types

        let validate =
            fun input ->
                task {
                    return
                        match input.StartDate with
                        | Some startDate ->
                            Success
                                { AbsenceRequestOperations.UpdateData.Description = input.Description
                                  AbsenceRequestOperations.UpdateData.Duration = input.Duration
                                  AbsenceRequestOperations.UpdateData.StartDate = startDate
                                  AbsenceRequestOperations.UpdateData.EndDate = input.EndDate
                                  AbsenceRequestOperations.UpdateData.HalfDayStart = input.HalfDayStart
                                  AbsenceRequestOperations.UpdateData.HalfDayEnd = input.HalfDayEnd
                                  AbsenceRequestOperations.UpdateData.PersonalDayType = input.PersonalDayType }
                        | None ->
                                ValidationError (
                                    IncompleteData,
                                    ValidationMessage "Start date is missing"
                                )
                }

    let handler id model : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let db = Context.resolve<DatabaseContext> context
                let ct = Context.cancellationToken context
                let logger = "UpdateRequest" |> Context.loggerFactory context

                let loadRequest = DatabaseId.createAsync &=> AbsenceRequestPersistence.getSingleRequest db ct

                let! pipeline =
                    loadRequest >&< Private.validate
                    &=> Pipeline.beginTransaction ct db
                    &=> AbsenceRequestOperations.updateRequest
                    &=> AbsenceRequestPersistence.updateEntity db
                    &=> Pipeline.saveChanges ct db
                    &=> Pipeline.commit ct db
                    &=> Pipeline.sideEffect (fun _ -> SemanticLog.absenceRequestUpdated logger id)
                    &=! Pipeline.response (fun _ -> setStatusCode 200)
                    <| (id, model)

                return! pipeline next context
            }