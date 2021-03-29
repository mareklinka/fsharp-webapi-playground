namespace SeedProject.Domain

open System

open AbsenceRequests.Types
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.Constructors

module AbsenceRequestOperations =
    type UpdateData =
        {
          StartDate: DateTime
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string
          Duration: decimal option
          PersonalDayType: PersonalDayType option }

    let approveNewRequest : ApproveRequestFunction = fun (New r) -> Approved r
    let rejectNewRequest : RejectRequestFunction = fun (New r) -> Rejected r

    let private updateHolidayRequest : UpdateHolidayRequestFunction =
        fun ({Start = newStart; End = newEnd }, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription }
            updated, updated <> r

    let private updatePersonalRequest : UpdatePersonalDayRequestFunction =
        fun (newDate, newDescription, newType) r ->
            let updated = { r with Date = newDate; Description = newDescription; Type = newType }
            updated, updated <> r

    let private updateSickdayRequest : UpdateSickdayRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let private updateDoctorVisitRequest : UpdateDoctorVisitRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let private updateDoctorVisitWithFamilyRequest : UpdateDoctorVisitWithFamilyRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let private updateSicknessRequest : UpdateSicknessRequestFunction =
        fun ({SicknessDatePair.Start = newStart; End = newEnd}, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription; }
            updated, updated <> r

    let private updatePandemicSicknessRequest : UpdatePandemicSicknessRequestFunction =
        fun ({SicknessDatePair.Start = newStart; End = newEnd}, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription; }
            updated, updated <> r

    let private operation =
        OperationResult.OperationResultBuilder.Instance

    module internal Helpers =
        let getHolidayUpdatePayload (data: UpdateData) =
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

        let getDurationUpdatePayload (data: UpdateData) =
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

        let getSicknessUpdatePayload (data: UpdateData) =
            operation {
                return
                    ({ SicknessDatePair.Start = data.StartDate
                       End = data.EndDate },
                     Description data.Description)
            }

        let getPersonalDayUpdatePayload (data: UpdateData) =
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
        | NewRequest r -> r |> approveNewRequest |> OperationResult.Success
        | ApprovedRequest r -> r |> OperationResult.Success
        | RejectedRequest _ ->
            OperationResult.OperationError(
                ApprovalOfRejectedRequest,
                OperationMessage "The request cannot be approved as it has already been rejected"
            )

    let rejectRequest request =
        match request with
        | NewRequest r -> r |> rejectNewRequest |> OperationResult.Success
        | RejectedRequest r -> r |> OperationResult.Success
        | ApprovedRequest _ ->
            OperationResult.OperationError(
                RejectionOfApprovedRequest,
                OperationMessage "The request cannot be rejected as it has already been approved"
            )

    let updateRequest (request, data) =
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


