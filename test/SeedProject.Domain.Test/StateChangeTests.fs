namespace SeedProject.Domain.Tests

module StateChangeTests =
    open FsCheck.Xunit
    open SeedProject.Domain.AbsenceRequests.Types
    open SeedProject.Domain

    [<Property>]
    let ``Approving a request does not change request data`` request =
        let (New original) = request
        let (Approved a) = request |> AbsenceRequestOperations.approveNewRequest
        a = original

    [<Property>]
    let ``Rejecting a request does not change request data`` request =
        let (New original) = request
        let (Rejected r) = request |> AbsenceRequestOperations.rejectNewRequest
        r = original

