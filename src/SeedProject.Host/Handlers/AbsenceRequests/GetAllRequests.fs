namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

open Giraffe.Core

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure.Operators
open SeedProject.Domain.Constructors
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence
open SeedProject.Host
open SeedProject.Infrastructure.Logging
open SeedProject.Persistence.Model

module GetAllRequests =
    [<RequireQualifiedAccess>]
    module Private =
        open Types

        let unwrapRequest request =
            match request with
            | NewRequest (New r) -> r
            | ApprovedRequest (Approved r) -> r
            | RejectedRequest (Rejected r) -> r

        let toModel value =
            match unwrapRequest value with
            | HolidayRequest { Id = Id id
                               Start = s
                               End = e
                               Description = Description d } ->
                let (startDate, isStartHalf) = s |> HolidayDate.extract
                let (endDate, isEndHalf) = e |> HolidayDate.extract

                { AbsenceRequestModel.Id = id
                  StartDate = startDate
                  EndDate = Some(endDate)
                  HalfDayStart = Some(isStartHalf)
                  HalfDayEnd = Some(isEndHalf)
                  Description = d
                  Type = RequestType.Holiday }
            | _ ->
                { AbsenceRequestModel.Id = 0
                  StartDate = DateTime.Today
                  EndDate = None
                  HalfDayStart = None
                  HalfDayEnd = None
                  Description = "";
                  Type = RequestType.PersonalDay }

        let toModelList = List.map toModel

    let handler : HttpHandler =
        fun (next: HttpFunc) (context: HttpContext) ->
            let db = context |> Context.resolve<DatabaseContext>
            let ct = Context.cancellationToken context
            let logger = "GetAllRequests" |> Context.loggerFactory context

            task {
                let g = (Private.toModelList |> Context.asOperation)

                let pipeline =
                    (fun () -> AbsenceRequestPersistence.getAllRequests db ct)
                    &=> (Private.toModelList |> Context.asOperation)
                    &== (fun _ -> unitTask { SemanticLog.absenceRequestRetrieved logger id })
                    &=! Context.jsonOutput

                let! pipelineResult = pipeline()

                return! pipelineResult next context
            }