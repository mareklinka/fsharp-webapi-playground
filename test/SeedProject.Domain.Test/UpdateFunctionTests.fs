namespace SeedProject.Domain.Tests.UpdateFunctions

module HolidayTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests.Types
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a holiday with its current data signals no change`` request =
        let (_, hasChanges) = request |> updateHolidayRequest ({ Start = request.Start; End = request.End }, request.Description)
        hasChanges = false

    [<Property>]
    let ``Updating a holiday applies changes`` request update =
        let (updated, _) = request |> updateHolidayRequest update
        let ({ HolidayDatePair.Start = newStart; End = newEnd }, newDescription) = update

        updated.Start = newStart && updated.End = newEnd && updated.Description = newDescription && updated.Id = request.Id

module PersonalDayTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a personal day with its current data signals no change`` request =
        let (_, hasChanges) = request |> updatePersonalRequest (request.Date, request.Description, request.Type)
        hasChanges = false

    [<Property>]
    let ``Updating a personal day applies changes`` request (newDate, newDescription, newType) =
        let (updated, _) = request |> updatePersonalRequest (newDate, newDescription, newType)

        updated.Date = newDate && updated.Description = newDescription && updated.Type = newType && updated.Id = request.Id

module SickdayTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a sickday with its current data signals no change`` request =
        let (_, hasChanges) = request |> updateSickdayRequest (request.Date, request.Description, request.Duration)
        hasChanges = false

    [<Property>]
    let ``Updating a sickday applies changes`` request (newDate, newDescription, newDuration) =
        let (updated, _) = request |> updateSickdayRequest (newDate, newDescription, newDuration)

        updated.Date = newDate && updated.Description = newDescription && updated.Duration = newDuration && updated.Id = request.Id

module DoctorVisitTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a doctor visit with its current data signals no change`` request =
        let (_, hasChanges) = request |> updateDoctorVisitRequest (request.Date, request.Description, request.Duration)
        hasChanges = false

    [<Property>]
    let ``Updating a doctor visit applies changes`` request (newDate, newDescription, newDuration) =
        let (updated, _) = request |> updateDoctorVisitRequest (newDate, newDescription, newDuration)

        updated.Date = newDate && updated.Description = newDescription && updated.Duration = newDuration && updated.Id = request.Id

module DoctorVisitWithFamilyTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a doctor visit with family with its current data signals no change`` request =
        let (_, hasChanges) = request |> updateDoctorVisitWithFamilyRequest (request.Date, request.Description, request.Duration)
        hasChanges = false

    [<Property>]
    let ``Updating a doctor visit with family applies changes`` request (newDate, newDescription, newDuration) =
        let (updated, _) = request |> updateDoctorVisitWithFamilyRequest (newDate, newDescription, newDuration)

        updated.Date = newDate && updated.Description = newDescription && updated.Duration = newDuration && updated.Id = request.Id

module SicknessTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests.Types
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a sickness with its current data signals no change`` request =
        let (_, hasChanges) = request |> updateSicknessRequest ({ Start = request.Start; End = request.End }, request.Description)
        hasChanges = false

    [<Property>]
    let ``Updating a sickness with family applies changes`` request ({ SicknessDatePair.Start = newStart; End = newEnd }, newDescription) =
        let (updated, _) = request |> updateSicknessRequest ({ SicknessDatePair.Start = newStart; End = newEnd }, newDescription)

        updated.Start = newStart && updated.End = newEnd && updated.Description = newDescription && updated.Id = request.Id

module PandemicSicknessTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests.Types
    open SeedProject.Domain.AbsenceRequests

    [<Property>]
    let ``Updating a pandemic sickness with its current data signals no change`` request =
        let (_, hasChanges) = request |> updatePandemicSicknessRequest ({ Start = request.Start; End = request.End }, request.Description)
        hasChanges = false

    [<Property>]
    let ``Updating a pandemic sickness with family applies changes`` request ({ SicknessDatePair.Start = newStart; End = newEnd }, newDescription) =
        let (updated, _) = request |> updatePandemicSicknessRequest ({ SicknessDatePair.Start = newStart; End = newEnd }, newDescription)

        updated.Start = newStart && updated.End = newEnd && updated.Description = newDescription && updated.Id = request.Id