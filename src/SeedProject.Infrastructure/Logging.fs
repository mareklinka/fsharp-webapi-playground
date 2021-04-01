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
    module Log =
        open Types

        [<RequireQualifiedAccess>]
        module AbsenceRequests =
            let updated (writer: Types.LogSink) id =
                writer $"Absence request with ID %i{id} has been updated" Information

            let retrieved (writer: Types.LogSink) id =
                writer $"Retrieving absence request with ID %i{id}" Information

            let listRetrieved (writer: Types.LogSink) count =
                writer $"Retrieving %i{count} absence requests" Information

            let created (writer: Types.LogSink) id =
                writer $"A new absence request with id %i{id} has been created" Information

            let deleted (writer: Types.LogSink) id =
                writer $"A new absence request with id %i{id} has been deleted" Information

