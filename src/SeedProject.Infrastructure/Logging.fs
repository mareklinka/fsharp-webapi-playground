namespace SeedProject.Infrastructure

module Logging =
    module Types =
        type LogLevel =
            | Debug
            | Information
            | Warning
            | Error
            | Critical

        type LogSink = string -> LogLevel -> unit

    [<RequireQualifiedAccess>]
    module SemanticLog =
        let absenceRequestUpdated (writer: Types.LogSink) id =
            writer $"Absence request with ID {id} has been updated" Types.Information
        let absenceRequestRetrieved (writer: Types.LogSink) id =
            writer $"Retrieving absence request with ID {id}" Types.Information