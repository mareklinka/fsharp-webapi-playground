namespace SeedProject.Domain

module Common =
    type DatabaseId = Id of int

    type ValidationErrorCode =
        | OutOfRange
        | EmptyField
        | InvalidFormat
    type ValidationErrorMessage = Message of string
    type ValidationError = ValidationErrorCode * ValidationErrorMessage

    type OperationErrorCode =
        | SomeError
        | SomeOtherError
    type OperationErrorMessage = Message of string
    type OperationError = OperationErrorCode * OperationErrorMessage

    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of ValidationError
        | OperationError of OperationError