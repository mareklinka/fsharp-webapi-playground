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
          Description: string
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
          Description: string
          Duration: decimal option
          PersonalDayType: PersonalDayType option }