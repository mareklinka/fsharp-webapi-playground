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
            writer $"Absence request with ID %i{id} has been updated" Types.Information

        let absenceRequestRetrieved (writer: Types.LogSink) id =
            writer $"Retrieving absence request with ID %i{id}" Types.Information

        let absenceRequestsRetrieved (writer: Types.LogSink) count =
            writer $"Retrieving %i{count} absence requests" Types.Information

        let absenceRequestCreated (writer: Types.LogSink) id =
            writer $"A new absence request with id %i{id} has been created" Types.Information