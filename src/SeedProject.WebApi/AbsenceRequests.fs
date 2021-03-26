namespace SeedProject.WebApi

open SeedProject.Domain.AbsenceRequests
open SeedProject.Domain.Common
open SeedProject.Domain

open System

module AbsenceRequests =
    module Types =
        type UpdateData =
            {
                StartDate: DateTime
                EndDate: Option<DateTime>
                HalfDayStart: Option<bool>
                HalfDayEnd: Option<bool>
                Description: string;
                Duration: Option<decimal>;
                PersonalDayType: Option<PersonalDayType>;
            }

    module Operations =
        let operation = OperationResult.OperationResultBuilder.Instance

        module internal Helpers =
            let getHolidayUpdatePayload (data:Types.UpdateData) =
                let makeHolidayDate date isHalfDay = if isHalfDay then HalfDay date else FullDay date

                match (data.HalfDayStart, data.EndDate, data.HalfDayEnd) with
                | (Some halfDayStart, Some endDate, Some halfDayEnd) ->
                    ({ HolidayDatePair.Start = makeHolidayDate data.StartDate halfDayStart; End = makeHolidayDate endDate halfDayEnd }, Description data.Description)
                    |> OperationResult.fromResult
                | _ ->
                    OperationResult.validationError (IncompleteData, ValidationMessage "Incomplete data provided")

            let getDurationUpdatePayload (data:Types.UpdateData) =
                operation {
                    match data.Duration with
                    | Some duration ->
                        let! duration = RequestDuration.create duration
                        return (data.StartDate, Description data.Description, duration)
                    | _ ->
                        return! OperationResult.validationError (IncompleteData, ValidationMessage "Incomplete data provided")
                }

            let getSicknessUpdatePayload (data:Types.UpdateData) =
                operation {
                    return ({ SicknessDatePair.Start = data.StartDate; End = data.EndDate }, Description data.Description)
                }

            let getPersonalDayUpdatePayload (data:Types.UpdateData) =
                operation {
                    match data.PersonalDayType with
                    | Some t ->
                        return (data.StartDate, Description data.Description, t)
                    | _ ->
                        return! OperationResult.validationError (IncompleteData, ValidationMessage "Incomplete data provided")
                }

        let ApproveRequest request =
            match request with
            | NewRequest r -> r |> ApproveRequest |> OperationResult.Success
            | ApprovedRequest r -> r |> OperationResult.Success
            | RejectedRequest _ ->
                OperationResult.OperationError(
                    ApprovalOfRejectedRequest,
                    OperationMessage "The request cannot be approved as it has already been rejected"
                )

        let RejectRequest request =
            match request with
            | NewRequest r -> r |> RejectRequest |> OperationResult.Success
            | RejectedRequest r -> r |> OperationResult.Success
            | ApprovedRequest _ ->
                OperationResult.OperationError(
                    RejectionOfApprovedRequest,
                    OperationMessage "The request cannot be rejected as it has already been approved"
                )

        let UpdateRequest request data =
            operation {
                match request with
                | NewRequest (New r) ->
                    match r with
                    | HolidayRequest hr ->
                        let! payload = data |> Helpers.getHolidayUpdatePayload
                        return New (HolidayRequest ((payload |> UpdateHolidayRequest <| hr) |> fst))
                    | PersonalDayRequest pdr ->
                        let! payload = data |> Helpers.getPersonalDayUpdatePayload
                        return New (PersonalDayRequest ((payload |> UpdatePersonalRequest <| pdr) |> fst))
                    | SickdayRequest sr ->
                        let! payload = data |> Helpers.getDurationUpdatePayload
                        return New (SickdayRequest ((payload |> UpdateSickdayRequest <| sr) |> fst))
                    | DoctorVisitRequest dvr ->
                        let! payload = data |> Helpers.getDurationUpdatePayload
                        return New (DoctorVisitRequest ((payload |> UpdateDoctorVisitRequest <| dvr) |> fst))
                    | DoctorVisitWithFamilyRequest dvfr ->
                        let! payload = data |> Helpers.getDurationUpdatePayload
                        return New (DoctorVisitWithFamilyRequest ((payload |> UpdateDoctorVisitWithFamilyRequest <| dvfr) |> fst))
                    | SicknessRequest sr ->
                        let! payload = data |> Helpers.getSicknessUpdatePayload
                        return New (SicknessRequest ((payload |> UpdateSicknessRequest <| sr) |> fst))
                    | PandemicSicknessRequest psr ->
                        let! payload = data |> Helpers.getSicknessUpdatePayload
                        return New (PandemicSicknessRequest ((payload |> UpdatePandemicSicknessRequest <| psr) |> fst))
                | ApprovedRequest _ ->
                    return! OperationResult.OperationError(
                        RejectionOfApprovedRequest,
                        OperationMessage "The request cannot be rejected as it has already been approved"
                    )
                | RejectedRequest _ ->
                    return! OperationResult.OperationError(
                        UpdateForbidden,
                        OperationMessage "Rejected requests cannot be updated"
                    )
            }
