namespace SeedProject.Host

open System
open Microsoft.AspNetCore.Http

open SeedProject.Domain
open SeedProject.Domain.Common
open SeedProject.Domain.Operators
open SeedProject.Domain.AbsenceRequests.Types

open SeedProject.Persistence.AbsenceRequests
open SeedProject.Domain.Constructors

[<RequireQualifiedAccess>]
module Handlers =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    [<RequireQualifiedAccess>]
    module AbsenceRequests =
        let private parseId = Context.Route.readInt "id"

        let private toDatabaseId : (int -> Async<OperationResult.OperationResult<DatabaseId>>) =
            fun i -> async { return operation { return! DatabaseId.create i } }

        let private loadById context =
            context
            |> Context.Database.resolve
            |> getSingleRequest
            <| context.RequestAborted

        let private store context =
            context
            |> Context.Database.resolve
            |> updateRequestEntity

        type AbsenceRequestModel =
            { Id: int
              StartDate: DateTime
              EndDate: Option<DateTime>
              HalfDayStart: Option<bool>
              HalfDayEnd: Option<bool>
              Description: string }

        [<CLIMutable>]
        type UpdateDataInputModel =
            { StartDate: Option<DateTime>
              EndDate: Option<DateTime>
              HalfDayStart: Option<bool>
              HalfDayEnd: Option<bool>
              Description: string
              Duration: Option<decimal>
              PersonalDayType: Option<PersonalDayType> }

        let private unwrapRequest request =
            match request with
            | NewRequest (New r) -> r
            | ApprovedRequest (Approved r) -> r
            | RejectedRequest (Rejected r) -> r

        let private transformer value =
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
                  Description = d }
            | _ ->
                { AbsenceRequestModel.Id = 0
                  StartDate = DateTime.Today
                  EndDate = None
                  HalfDayStart = None
                  HalfDayEnd = None
                  Description = "" }

        let private validate (inputTask: Async<UpdateDataInputModel>) =
            async {
                let! input = inputTask

                return
                    operation {
                        match input.StartDate with
                        | Some startDate ->
                            return
                                { AbsenceRequestOperations.UpdateData.Description = input.Description
                                  AbsenceRequestOperations.UpdateData.Duration = input.Duration
                                  AbsenceRequestOperations.UpdateData.StartDate = startDate
                                  AbsenceRequestOperations.UpdateData.EndDate = input.EndDate
                                  AbsenceRequestOperations.UpdateData.HalfDayStart = input.HalfDayStart
                                  AbsenceRequestOperations.UpdateData.HalfDayEnd = input.HalfDayEnd
                                  AbsenceRequestOperations.UpdateData.PersonalDayType = input.PersonalDayType }
                        | None ->
                            return!
                                OperationResult.validationError (
                                    IncompleteData,
                                    ValidationMessage "Start date is missing"
                                )
                    }
            }

        let getRequest (context: HttpContext) =
            parseId
            >=> toDatabaseId
            >=> loadById context
            >=> Context.ResponseBody.okWith context transformer
            <| context

        let updateRequest (context: HttpContext) =
            let readInputModel =
                Context.RequestBody.readAs<UpdateDataInputModel>
                >> validate

            let readId =
                parseId
                >=> toDatabaseId
                >=> Context.Database.beginTransaction context
                >=> loadById context

            readId >=< readInputModel
            >=> AbsenceRequestOperations.updateRequest
            >=> store context
            >=> Context.Database.save context
            >=> Context.Database.commit context
            >=> Context.ResponseBody.ok context
            <| context
