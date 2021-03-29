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
        let myFancyLogMessage (writer: Types.LogSink) =
            writer  "My fancy log message" Types.Information