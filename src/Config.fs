namespace BigHack.Fumble.Api

open System
open Amazon
open Amazon.Lambda.Core
open Serilog
open Serilog.Formatting.Json

open BigHack.Fumble.Api
open BigHack.Fumble.Api.TypeExtensions
open BigHack.Fumble.Api.Infrastructure
open BigHack.Fumble.Api.DataAccess

type Config =
    { Version : string
      Environment : string
      AwsRegion : RegionEndpoint
      DynamoCardsTable : string }

module Config =

    let getConfig () : Result<Config, FumbleError> =
        try
            result {
                let! version = Environment.tryItem "VERSION"
                let! environment = Environment.tryItem "ENVIRONMENT"
                let! awsRegion = Environment.tryItem "AWS_REGION"
                let! dynamoCardsTable = Environment.tryItem "DYNAMO_CARDS_TABLE"

                return
                    { Version = version
                      Environment = environment
                      AwsRegion = awsRegion |> RegionEndpoint.GetBySystemName
                      DynamoCardsTable = dynamoCardsTable }
            }
            |> Result.mapError ConfigurationError
        with ex ->
            Error (ConfigurationError (ex.ToString()))

    let configureLogging cfg (lambdaContext : ILambdaContext) =
        try
            let logger =
                LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console(JsonFormatter(renderMessage = true))
                    .CreateLogger()

            logger.ForContext("Environment", cfg.Environment)
            |> fun l -> l.ForContext("LambdaName", lambdaContext.FunctionName)
            |> Ok

        with ex ->
            Error (ConfigurationError (ex.ToString()))


    let setupAndCreateAppContext lambdaContext =
        match getConfig () with
        | Error error ->
            Console.Error.WriteLine (sprintf "Configuration Error: %A" error)
            Error error

        | Ok cfg ->
            match configureLogging cfg lambdaContext with
            | Error error ->
                Console.Error.WriteLine (sprintf "Logging Setup Error: %A" error)
                Error error

            | Ok logger ->
                let dataAccessConfig =
                    { ConnectionConfig = CloudConfig { CloudAwsConfig.Region = cfg.AwsRegion }
                      CardsTableName = cfg.DynamoCardsTable }

                AppContext.configure dataAccessConfig
                                     logger
                                     (fun () -> DateTimeOffset.Now)
                |> Ok

