namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open Microsoft.AspNetCore.Http

open Giraffe

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure.Operators
open SeedProject.Domain.Constructors
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence
open SeedProject.Host
open SeedProject.Domain
open SeedProject.Infrastructure
open SeedProject.Infrastructure.Logging

open FSharp.Control.Tasks

module UpdateRequest =

    [<CLIMutable>]
    type UpdateDataInputModel =
        { StartDate: DateTime option
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string
          Duration: decimal option
          PersonalDayType: PersonalDayType option }

    module Private =
        let validate =
            fun input ->
                task {
                    return
                        match input.StartDate with
                        | Some startDate ->
                            OperationResult.fromResult
                                { AbsenceRequestOperations.UpdateData.Description = input.Description
                                  AbsenceRequestOperations.UpdateData.Duration = input.Duration
                                  AbsenceRequestOperations.UpdateData.StartDate = startDate
                                  AbsenceRequestOperations.UpdateData.EndDate = input.EndDate
                                  AbsenceRequestOperations.UpdateData.HalfDayStart = input.HalfDayStart
                                  AbsenceRequestOperations.UpdateData.HalfDayEnd = input.HalfDayEnd
                                  AbsenceRequestOperations.UpdateData.PersonalDayType = input.PersonalDayType }
                        | None ->
                                OperationResult.validationError (
                                    IncompleteData,
                                    ValidationMessage "Start date is missing"
                                )
                }

    let handler id model : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let db = Context.Database.resolve context
                let ct = Context.cancellationToken context
                let logger = "UpdateRequest" |> Context.loggerFactory context

                let loadRequest = DatabaseId.createAsync &=> AbsenceRequestPersistence.getSingleRequest db ct

                let! pipeline =
                    loadRequest >&< Private.validate
                    &=> Context.Database.beginTransaction db ct
                    &=> AbsenceRequestOperations.updateRequest
                    &=> AbsenceRequestPersistence.updateRequestEntity db
                    &=> Context.Database.save db ct
                    &=> Context.Database.commit db ct
                    &== (fun _ -> task { SemanticLog.absenceRequestUpdated logger id })
                    &=? ((fun _ -> Successful.NO_CONTENT) |> Context.apiOutput)
                    <| (id, model)

                return! pipeline next context
            }