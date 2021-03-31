namespace SeedProject.Domain

open System
open FSharp.Control.Tasks

open AbsenceRequests.Types
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.Constructors

module AbsenceRequestOperations =
    type RequestType =
        | Holiday
        | DoctorVisit
        | DoctorVisitWithFamily
        | Sickday
        | PersonalDay
        | Sickness
        | PandemicSickness

    type CreateData =
        {
          Type: RequestType
          StartDate: DateTime
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string option
          Duration: decimal option
          PersonalDayType: PersonalDayType option }

    type UpdateData =
        {
          StartDate: DateTime
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string option
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

    module internal Helpers =
        let getHolidayUpdatePayload (data: UpdateData) =
            operation {
                match (data.HalfDayStart, data.EndDate, data.HalfDayEnd) with
                | (Some halfDayStart, Some endDate, Some halfDayEnd) ->
                    let! datePair = HolidayDatePair.create data.StartDate halfDayStart endDate halfDayEnd
                    return (datePair, Description data.Description)
                | _ ->
                    return!
                        ValidationError (
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
                        ValidationError (
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
                        ValidationError (
                            IncompleteData,
                            ValidationMessage "Incomplete data provided"
                        )
            }

        let getHolidayCreatePayload (data: CreateData) =
            operation {
                match (data.HalfDayStart, data.EndDate, data.HalfDayEnd) with
                | (Some halfDayStart, Some endDate, Some halfDayEnd) ->
                    let! datePair = HolidayDatePair.create data.StartDate halfDayStart endDate halfDayEnd
                    return (datePair, Description data.Description)
                | _ ->
                    return!
                        ValidationError (
                            IncompleteData,
                            ValidationMessage "Incomplete data provided"
                        )
            }

        let getDurationCreatePayload (data: CreateData) =
            operation {
                match data.Duration with
                | Some duration ->
                    let! duration = RequestDuration.create duration
                    return (data.StartDate, Description data.Description, duration)
                | _ ->
                    return!
                        ValidationError (
                            IncompleteData,
                            ValidationMessage "Incomplete data provided"
                        )
            }

        let getSicknessCreatePayload (data: CreateData) =
            operation {
                return
                    ({ SicknessDatePair.Start = data.StartDate
                       End = data.EndDate },
                     Description data.Description)
            }

        let getPersonalDayCreatePayload (data: CreateData) =
            operation {
                match data.PersonalDayType with
                | Some t -> return (data.StartDate, Description data.Description, t)
                | _ ->
                    return!
                        ValidationError (
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
        | NewRequest r -> r |> rejectNewRequest |> Success
        | RejectedRequest r -> r |> Success
        | ApprovedRequest _ ->
            OperationResult.OperationError(
                RejectionOfApprovedRequest,
                OperationMessage "The request cannot be rejected as it has already been approved"
            )

    let updateRequest (request, data) =
        task {
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
                        OperationError(
                            RejectionOfApprovedRequest,
                            OperationMessage "The request cannot be rejected as it has already been approved"
                        )
                | RejectedRequest _ ->
                    return!
                        OperationError(
                            UpdateForbidden,
                            OperationMessage "Rejected requests cannot be updated"
                        )
            }
        }

    let createRequest (data: CreateData) =
        task {
            return operation {
                match data.Type with
                | Holiday ->
                    let! (datePair, description) = data |> Helpers.getHolidayCreatePayload
                    return NewRequest(New(HolidayRequest( { Id = Id 0; Start = datePair.Start; End = datePair.End; Description = description })))
                | PersonalDay ->
                    let! (date, description, t) = data |> Helpers.getPersonalDayCreatePayload
                    return NewRequest(New(PersonalDayRequest({ Id = Id 0; Date = date; Description = description; Type = t })))
                | Sickday ->
                    let! (date, description, duration) = data |> Helpers.getDurationCreatePayload
                    return NewRequest(New(SickdayRequest({ Id = Id 0; Date = date; Description = description; Duration = duration })))
                | DoctorVisit ->
                    let! (date, description, duration) = data |> Helpers.getDurationCreatePayload

                    return
                        NewRequest(
                            New(
                                DoctorVisitRequest(
                                    { Id = Id 0; Date = date; Description = description; Duration = duration }
                                )
                            )
                        )
                | DoctorVisitWithFamily ->
                    let! (date, description, duration) = data |> Helpers.getDurationCreatePayload

                    return
                        NewRequest(
                            New(
                                DoctorVisitWithFamilyRequest(
                                    { Id = Id 0; Date = date; Description = description; Duration = duration }
                                )
                            )
                        )
                | Sickness ->
                    let! (datePair, description) = data |> Helpers.getSicknessCreatePayload
                    return NewRequest(New(SicknessRequest({ Id = Id 0; Start = datePair.Start; End = datePair.End; Description = description })))
                | PandemicSickness ->
                    let! (datePair, description) = data |> Helpers.getSicknessCreatePayload

                    return
                        NewRequest(
                            New(
                                PandemicSicknessRequest(
                                    { Id = Id 0; Start = datePair.Start; End = datePair.End; Description = description }
                                )
                            )
                        )
            }
        }

