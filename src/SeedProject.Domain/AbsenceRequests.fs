namespace SeedProject.Domain

open System
open SeedProject.Domain.Common

module AbsenceRequests =
    type HolidayDate =
        | FullDay of DateTime
        | HalfDay of DateTime

    type RequestDurationHour = Hour of int
    type RequestDurationMinute = Full | Half

    type DescriptionText = Description of string
    type RequestDuration = RequestDurationHour * RequestDurationMinute

    type HolidayRequest =
        {
            Id: DatabaseId;
            Start: HolidayDate;
            End: HolidayDate;
            Description: DescriptionText;
        }

    type PersonalDayType =
        | Wedding
        | Childbirth
        | Funeral
        | Moving
        | BloodDonation
    type PersonalDayRequest =
        {
            Id: DatabaseId;
            Date: DateTime;
            Type: PersonalDayType;
            Description: DescriptionText
        }

    type DoctorVisitRequest =
        {
            Id: DatabaseId;
            Date: DateTime;
            Description: DescriptionText
            Duration: RequestDuration;
        }

    type DoctorVisitWithFamilyRequest =
        {
            Id: DatabaseId;
            Date: DateTime;
            Description: DescriptionText
            Duration: RequestDuration;
        }

    type SickdayRequest =
        {
            Id: DatabaseId;
            Date: DateTime;
            Description: DescriptionText;
            Duration: RequestDuration;
        }

    type SicknessRequest =
        {
            Id: DatabaseId;
            Start: DateTime;
            End: Option<DateTime>;
            Description: DescriptionText;
        }

    type PandemicSicknessRequest =
        {
            Id: DatabaseId;
            Start: DateTime;
            End: Option<DateTime>;
            Description: DescriptionText;
        }

    type AbsenceRequestType =
        | HolidayRequest of HolidayRequest
        | PersonalDayRequest of PersonalDayRequest
        | SickdayRequest of SickdayRequest
        | DoctorVisitRequest of DoctorVisitRequest
        | DoctorVisitWithFamilyRequest of DoctorVisitWithFamilyRequest
        | SicknessRequest of SicknessRequest
        | PandemicSicknessRequest of PandemicSicknessRequest

    type NewAbsenceRequest = New of AbsenceRequestType
    type ApprovedAbsenceRequest = Approved of AbsenceRequestType
    type RejectedAbsenceRequest = Rejected of AbsenceRequestType

    type AbsenceRequest =
        | NewRequest of NewAbsenceRequest
        | ApprovedRequest of ApprovedAbsenceRequest
        | RejectedRequest of RejectedAbsenceRequest

    type ApproveRequestFunction = NewAbsenceRequest -> ApprovedAbsenceRequest
    type RejectRequestFunction = NewAbsenceRequest -> RejectedAbsenceRequest

    type HolidayDatePair = { Start: HolidayDate; End: HolidayDate }
    type SicknessDatePair = { Start: DateTime; End: Option<DateTime> }
    type HolidayUpdateData = HolidayDatePair * DescriptionText
    type PersonalDayUpdateData = DateTime * DescriptionText * PersonalDayType
    type DurationAbsenceUpdateData = DateTime * DescriptionText * RequestDuration
    type SicknessUpdateData = SicknessDatePair * DescriptionText

    type UpdateRequestFunction<'a, 'b> = 'a -> 'b -> 'b * bool

    type UpdateHolidayRequestFunction = UpdateRequestFunction<HolidayUpdateData, HolidayRequest>
    type UpdatePersonalDayRequestFunction = UpdateRequestFunction<PersonalDayUpdateData, PersonalDayRequest>
    type UpdateDoctorVisitRequestFunction = UpdateRequestFunction<DurationAbsenceUpdateData, DoctorVisitRequest>
    type UpdateDoctorVisitWithFamilyRequestFunction = UpdateRequestFunction<DurationAbsenceUpdateData, DoctorVisitWithFamilyRequest>
    type UpdateSickdayRequestFunction = UpdateRequestFunction<DurationAbsenceUpdateData, SickdayRequest>
    type UpdateSicknessRequestFunction= UpdateRequestFunction<SicknessUpdateData, SicknessRequest>
    type UpdatePandemicSicknessRequestFunction = UpdateRequestFunction<SicknessUpdateData, PandemicSicknessRequest>

    let ApproveRequest : ApproveRequestFunction = fun (New r) -> Approved r
    let RejectRequest : RejectRequestFunction = fun (New r) -> Rejected r

    let UpdateHolidayRequest : UpdateHolidayRequestFunction =
        fun ({Start = newStart; End = newEnd }, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription }
            updated, updated <> r

    let UpdatePersonalRequest : UpdatePersonalDayRequestFunction =
        fun (newDate, newDescription, newType) r ->
            let updated = { r with Date = newDate; Description = newDescription; Type = newType }
            updated, updated <> r

    let UpdateSickdayRequest : UpdateSickdayRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let UpdateDoctorVisitRequest : UpdateDoctorVisitRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let UpdateDoctorVisitWithFamilyRequest : UpdateDoctorVisitWithFamilyRequestFunction =
        fun (newDate, newDescription, newDuration) r ->
            let updated = { r with Date = newDate; Description = newDescription; Duration = newDuration }
            updated, updated <> r

    let UpdateSicknessRequest : UpdateSicknessRequestFunction =
        fun ({SicknessDatePair.Start = newStart; End = newEnd}, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription; }
            updated, updated <> r

    let UpdatePandemicSicknessRequest : UpdatePandemicSicknessRequestFunction =
        fun ({SicknessDatePair.Start = newStart; End = newEnd}, newDescription) r ->
            let updated = { r with Start = newStart; End = newEnd; Description = newDescription; }
            updated, updated <> r


[<RequireQualifiedAccess>]
module RequestDuration =
    open AbsenceRequests

    let create (decimal:decimal) =
        let shifted = decimal * 10M
        let whole = System.Math.Truncate(shifted)
        let remainder = whole % 10M

        match (shifted = whole, remainder) with
        | (true, 0M) -> OperationResult.fromResult (Hour (System.Math.Truncate(decimal) |> int), Full)
        | (true, 5M) -> OperationResult.fromResult (Hour (System.Math.Truncate(decimal) |> int), Half)
        | _ -> OperationResult.validationError (OutOfRange, ValidationMessage "Invalid duration specified")
