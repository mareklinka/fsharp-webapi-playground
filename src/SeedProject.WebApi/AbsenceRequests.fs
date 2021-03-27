namespace SeedProject.WebApi

open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Domain.Constructors
open SeedProject.Domain.AbsenceRequests
open SeedProject.Domain.Common
open SeedProject.Domain

open System
open Operators

module AbsenceRequests =
    module Types =
        type UpdateData =
            {
              Id: DatabaseId;
              StartDate: DateTime
              EndDate: Option<DateTime>
              HalfDayStart: Option<bool>
              HalfDayEnd: Option<bool>
              Description: string
              Duration: Option<decimal>
              PersonalDayType: Option<PersonalDayType> }

    module Operations =
        let private operation =
            OperationResult.OperationResultBuilder.Instance

        module internal Helpers =
            let getHolidayUpdatePayload (data: Types.UpdateData) =
                operation {
                    match (data.HalfDayStart, data.EndDate, data.HalfDayEnd) with
                    | (Some halfDayStart, Some endDate, Some halfDayEnd) ->
                        let! datePair = HolidayDatePair.create data.StartDate halfDayStart endDate halfDayEnd
                        return (datePair, Description data.Description)
                    | _ ->
                        return!
                            OperationResult.validationError (
                                IncompleteData,
                                ValidationMessage "Incomplete data provided"
                            )
                }

            let getDurationUpdatePayload (data: Types.UpdateData) =
                operation {
                    match data.Duration with
                    | Some duration ->
                        let! duration = RequestDuration.create duration
                        return (data.StartDate, Description data.Description, duration)
                    | _ ->
                        return!
                            OperationResult.validationError (
                                IncompleteData,
                                ValidationMessage "Incomplete data provided"
                            )
                }

            let getSicknessUpdatePayload (data: Types.UpdateData) =
                operation {
                    return
                        ({ SicknessDatePair.Start = data.StartDate
                           End = data.EndDate },
                         Description data.Description)
                }

            let getPersonalDayUpdatePayload (data: Types.UpdateData) =
                operation {
                    match data.PersonalDayType with
                    | Some t -> return (data.StartDate, Description data.Description, t)
                    | _ ->
                        return!
                            OperationResult.validationError (
                                IncompleteData,
                                ValidationMessage "Incomplete data provided"
                            )
                }

        let approveRequest request =
            match request with
            | NewRequest r -> r |> approveRequest |> OperationResult.Success
            | ApprovedRequest r -> r |> OperationResult.Success
            | RejectedRequest _ ->
                OperationResult.OperationError(
                    ApprovalOfRejectedRequest,
                    OperationMessage "The request cannot be approved as it has already been rejected"
                )

        let rejectRequest request =
            match request with
            | NewRequest r -> r |> rejectRequest |> OperationResult.Success
            | RejectedRequest r -> r |> OperationResult.Success
            | ApprovedRequest _ ->
                OperationResult.OperationError(
                    RejectionOfApprovedRequest,
                    OperationMessage "The request cannot be rejected as it has already been approved"
                )

        let updateRequest data request =
            async {
                return operation {
                    match request with
                    | NewRequest (New r) ->
                        match r with
                        | HolidayRequest hr ->
                            let! payload = data |> Helpers.getHolidayUpdatePayload
                            return NewRequest(New(HolidayRequest((payload |> updateHolidayRequest <| hr) |> fst)))
                        | PersonalDayRequest pdr ->
                            let! payload = data |> Helpers.getPersonalDayUpdatePayload
                            return NewRequest(New(PersonalDayRequest((payload |> updatePersonalRequest <| pdr) |> fst)))
                        | SickdayRequest sr ->
                            let! payload = data |> Helpers.getDurationUpdatePayload
                            return NewRequest(New(SickdayRequest((payload |> updateSickdayRequest <| sr) |> fst)))
                        | DoctorVisitRequest dvr ->
                            let! payload = data |> Helpers.getDurationUpdatePayload

                            return
                                NewRequest(
                                    New(
                                        DoctorVisitRequest(
                                            (payload |> updateDoctorVisitRequest <| dvr)
                                            |> fst
                                        )
                                    )
                                )
                        | DoctorVisitWithFamilyRequest dvfr ->
                            let! payload = data |> Helpers.getDurationUpdatePayload

                            return
                                NewRequest(
                                    New(
                                        DoctorVisitWithFamilyRequest(
                                            (payload |> updateDoctorVisitWithFamilyRequest
                                             <| dvfr)
                                            |> fst
                                        )
                                    )
                                )
                        | SicknessRequest sr ->
                            let! payload = data |> Helpers.getSicknessUpdatePayload
                            return NewRequest(New(SicknessRequest((payload |> updateSicknessRequest <| sr) |> fst)))
                        | PandemicSicknessRequest psr ->
                            let! payload = data |> Helpers.getSicknessUpdatePayload

                            return
                                NewRequest(
                                    New(
                                        PandemicSicknessRequest(
                                            (payload |> updatePandemicSicknessRequest <| psr)
                                            |> fst
                                        )
                                    )
                                )
                    | ApprovedRequest _ ->
                        return!
                            OperationResult.OperationError(
                                RejectionOfApprovedRequest,
                                OperationMessage "The request cannot be rejected as it has already been approved"
                            )
                    | RejectedRequest _ ->
                        return!
                            OperationResult.OperationError(
                                UpdateForbidden,
                                OperationMessage "Rejected requests cannot be updated"
                            )
                }
            }
