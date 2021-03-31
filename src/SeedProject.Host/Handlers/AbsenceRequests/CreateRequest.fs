namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open Microsoft.AspNetCore.Http

open Giraffe

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure.Operators
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence
open SeedProject.Persistence.Model
open SeedProject.Host
open SeedProject.Domain
open SeedProject.Infrastructure
open SeedProject.Infrastructure.Logging

open FSharp.Control.Tasks

module CreateRequest =
    module Private =
        open Types

        let validateRequestType =
            fun t ->
                operation {
                    return!
                        match t with
                        | RequestType.Holiday -> Success AbsenceRequestOperations.RequestType.Holiday
                        | RequestType.DoctorVisit -> Success AbsenceRequestOperations.RequestType.DoctorVisit
                        | RequestType.DoctorVisitWithFamily ->
                            Success AbsenceRequestOperations.RequestType.DoctorVisitWithFamily
                        | RequestType.Sickday -> Success AbsenceRequestOperations.RequestType.Sickday
                        | RequestType.Sickness -> Success AbsenceRequestOperations.RequestType.Sickness
                        | RequestType.PandemicSickness -> Success AbsenceRequestOperations.RequestType.PandemicSickness
                        | RequestType.PersonalDay -> Success AbsenceRequestOperations.RequestType.PersonalDay
                        | _ -> ValidationError(IncompleteData, ValidationMessage "Start date is missing")
                }


        let validate =
            fun (input: CreateRequestInputModel) ->
                task {
                    return
                        operation {
                            let! t = input.Type |> validateRequestType

                            return!
                                match input.StartDate with
                                | Some startDate ->
                                    Success
                                        { AbsenceRequestOperations.CreateData.Description = input.Description
                                          AbsenceRequestOperations.CreateData.Duration = input.Duration
                                          AbsenceRequestOperations.CreateData.StartDate = startDate
                                          AbsenceRequestOperations.CreateData.EndDate = input.EndDate
                                          AbsenceRequestOperations.CreateData.HalfDayStart = input.HalfDayStart
                                          AbsenceRequestOperations.CreateData.HalfDayEnd = input.HalfDayEnd
                                          AbsenceRequestOperations.CreateData.PersonalDayType = input.PersonalDayType
                                          AbsenceRequestOperations.CreateData.Type = t }
                                | None -> ValidationError(IncompleteData, ValidationMessage "Start date is missing")
                        }

                }

    let handler model : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let db = Context.resolve<DatabaseContext> context
                let ct = Context.cancellationToken context

                let logger =
                    "AddRequest" |> Context.loggerFactory context

                let! pipeline =
                    Private.validate
                    &== (fun _ -> Db.beginTransaction ct db)
                    &=> AbsenceRequestOperations.createRequest
                    &=> AbsenceRequestPersistence.addEntity db
                    &== (fun _ -> Db.saveChanges ct db)
                    &== (fun _ -> Db.commit ct db)
                    &== (fun _ -> unitTask { SemanticLog.absenceRequestUpdated logger id })
                    &=! (Context.apiOutput (fun ar -> ar.Id |> json))
                    <| model

                return! pipeline next context
            }