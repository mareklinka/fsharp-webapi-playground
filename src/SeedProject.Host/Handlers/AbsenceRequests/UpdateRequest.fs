namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open Microsoft.AspNetCore.Http

open Giraffe

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure.Operators
open SeedProject.Domain.Constructors
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence
open SeedProject.Persistence.Model
open SeedProject.Host
open SeedProject.Domain
open SeedProject.Infrastructure
open SeedProject.Infrastructure.Logging

open FSharp.Control.Tasks
open System.Threading.Tasks

module UpdateRequest =

    [<CLIMutable>]
    type UpdateRequestInputModel =
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
                    &== (fun _ -> Db.beginTransaction ct db)
                    &=> AbsenceRequestOperations.updateRequest
                    &=> AbsenceRequestPersistence.updateEntity db
                    &== (fun _ -> Db.saveChanges ct db)
                    &== (fun _ -> Db.commit ct db)
                    &== (fun _ -> unitTask { SemanticLog.absenceRequestUpdated logger id })
                    &=! ((fun _ -> setStatusCode 200) |> Context.apiOutput)
                    <| (id, model)

                return! pipeline next context
            }