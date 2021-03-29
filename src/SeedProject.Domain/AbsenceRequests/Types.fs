namespace SeedProject.Domain.AbsenceRequests

open System
open SeedProject.Infrastructure.Common

module Types =
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
            End: DateTime option;
            Description: DescriptionText;
        }

    type PandemicSicknessRequest =
        {
            Id: DatabaseId;
            Start: DateTime;
            End: DateTime option;
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
    type SicknessDatePair = { Start: DateTime; End: DateTime option }
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