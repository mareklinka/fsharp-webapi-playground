namespace SeedProject.Domain

module Common =
    type DatabaseId = Id of int

    type ValidationErrorCode =
        | OutOfRange
        | IncompleteData
        | InvalidFormat
    type ValidationErrorMessage = ValidationMessage of string
    type ValidationError = ValidationErrorCode * ValidationErrorMessage

    type OperationErrorCode =
        | ApprovalOfRejectedRequest
        | RejectionOfApprovedRequest
        | UpdateForbidden
    type OperationErrorMessage = OperationMessage of string
    type OperationError = OperationErrorCode * OperationErrorMessage