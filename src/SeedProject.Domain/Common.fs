namespace SeedProject.Domain

module Common =
    type DatabaseId = Id of int

    type ValidationErrorCode =
        | OutOfRange
        | IncompleteData
        | InvalidFormat
    type ValidationErrorMessage = ValidationMessage of string
    type ValidationError = ValidationErrorCode * ValidationErrorMessage

    type InvariantType =
    | HolidayRequestMustHaveEndDate

    type OperationErrorCode =
        | ApprovalOfRejectedRequest
        | RejectionOfApprovedRequest
        | UpdateForbidden
        | NotFound of DatabaseId
        | InvariantBroken of InvariantType

    type OperationErrorMessage = OperationMessage of string
    type OperationError = OperationErrorCode * OperationErrorMessage