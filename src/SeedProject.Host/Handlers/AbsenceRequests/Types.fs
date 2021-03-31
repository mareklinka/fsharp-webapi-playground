namespace SeedProject.Host.Handlers.AbsenceRequests

open System
open SeedProject.Domain.AbsenceRequests.Types

module Types =
    type RequestType =
        | Holiday = 0
        | DoctorVisit = 1
        | DoctorVisitWithFamily = 2
        | Sickday = 3
        | PersonalDay = 4
        | Sickness = 5
        | PandemicSickness = 6

    type AbsenceRequestModel =
        { Id: int
          StartDate: DateTime
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string option
          Type: RequestType }

    [<CLIMutable>]
    type CreateRequestInputModel =
      { StartDate: DateTime option
        EndDate: DateTime option
        HalfDayStart: bool option
        HalfDayEnd: bool option
        Description: string option
        Duration: decimal option
        PersonalDayType: PersonalDayType option
        Type: RequestType }

    [<CLIMutable>]
    type UpdateRequestInputModel =
        { StartDate: DateTime option
          EndDate: DateTime option
          HalfDayStart: bool option
          HalfDayEnd: bool option
          Description: string option
          Duration: decimal option
          PersonalDayType: PersonalDayType option }

module CommonMethods =
    open SeedProject.Infrastructure.Common
    open SeedProject.Domain.Constructors
    open Types

    let private unwrapRequest request =
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
              Description = None
              Type = RequestType.PersonalDay }