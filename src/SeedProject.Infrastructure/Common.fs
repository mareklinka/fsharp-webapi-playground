namespace SeedProject.Infrastructure

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
        | AggregateError
        | NotFound of DatabaseId
        | InvariantBroken of InvariantType

    type OperationErrorMessage = OperationMessage of string
    type OperationError = OperationErrorCode * OperationErrorMessage

    type SerializationModel =
        {
            Code: string;
            Message: string;
            AdditionalInfo: string option
        }

    module Rendering =
        let private renderValidationCode code =
            match code with
            | OutOfRange -> nameof(OutOfRange)
            | IncompleteData -> nameof(IncompleteData)
            | InvalidFormat -> nameof(InvalidFormat)

        let private renderInvariant invariant =
            match invariant with
            | HolidayRequestMustHaveEndDate -> nameof(HolidayRequestMustHaveEndDate)

        let private renderOperationCode code =
            match code with
            | ApprovalOfRejectedRequest -> (nameof(ApprovalOfRejectedRequest), None)
            | RejectionOfApprovedRequest -> (nameof(RejectionOfApprovedRequest), None)
            | UpdateForbidden -> (nameof(UpdateForbidden), None)
            | AggregateError -> (nameof(AggregateError), None)
            | NotFound (Id id) -> ($"{nameof(NotFound)}", $"{id}" |> Some)
            | InvariantBroken invariant -> (nameof(InvariantBroken), invariant |> renderInvariant |> Some)

        let Validation (code, ValidationMessage message) =
            { Code = code |> renderValidationCode; Message = message; AdditionalInfo = None }

        let Operation (code, OperationMessage message) =
            let (code, additionalInfo) = renderOperationCode code
            { Code = code; Message = message; AdditionalInfo = additionalInfo }

