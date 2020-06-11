module BigHack.Fumble.Api.DataAccess

open System
open System.Diagnostics
open System.Net
open Amazon
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open Amazon.DynamoDBv2.Model

open TypeExtensions
open BigHack.Fumble.Api
open BigHack.Fumble.Api.Infrastructure
open BigHack.Fumble.Api.Model

type TimeProvider = unit -> System.DateTimeOffset

type LocalAwsDevConfig =
    { ServiceUrl : string }

type CloudAwsConfig =
    { Region : RegionEndpoint }

type DataAccessConnectionConfig =
    | CloudConfig of CloudAwsConfig
    | LocalConfig of LocalAwsDevConfig

type DataAccessConfig =
    { ConnectionConfig : DataAccessConnectionConfig
      CardsTableName : string }

let getClient connectionConfig =
    let config =
        match connectionConfig with
        | CloudConfig { Region = r } ->
            AmazonDynamoDBConfig(RegionEndpoint = r)
        | LocalConfig { ServiceUrl = url } ->
            AmazonDynamoDBConfig(ServiceURL = url)
    config.Timeout <- Nullable (TimeSpan.FromSeconds 2.)

    match connectionConfig with
    | CloudConfig _ ->
        new AmazonDynamoDBClient(config)
    | LocalConfig _ ->
        // Use Placeholder Credentials for local tests
        // Without these AWS will try to load credentials and fail
        new AmazonDynamoDBClient("abc", "xyz", config)

let logDataAccessError (logger : Serilog.ILogger)
                       tableName
                       event
                       customerId
                       (sw : Stopwatch)
                       (ex : Exception) =
    logger.Error(ex, "{Category}: Error performing '{DataAccessEvent}' for customer: {CustomerId}, in table: {TableName}, in {Elapsed} ms",
                    "DataAccess", event, customerId, tableName, sw.ElapsedMilliseconds)
    let msg = sprintf "DataAccess: Error performing %s for customer: %s, into table: %s"
                      event (customerId.ToString()) tableName
    Error (DataAccessError (msg, Some ex))

let storeCardCollection config
                        (logger : Serilog.ILogger)
                        (cardCollection : Model.CardCollection) =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        // don't do any mapping for now, just store the raw collection
        let dto = cardCollection
        try
            use client = getClient config.ConnectionConfig
            result {
                let! eventJson = trySerialize dto
                let table = Table.LoadTable(client, TableConfig(config.CardsTableName))
                table.PutItemAsync(Document.FromJson(eventJson)).Result |> ignore
                logger.Information("{Category}: Completed {DataAccessEvent} for collection: {CardCollection} in {Elapsed} ms",
                                   "DataAccess", "Store Card Collection", cardCollection.Name, sw.ElapsedMilliseconds)
                ()
            }

        with ex ->
            logger.Error(ex, "{Category}: Error performing {DataAccessEvent} for collection: {CardCollection}, in table: {TableName}, in {Elapsed} ms",
                             "DataAccess", "Store Card Collection",
                             cardCollection.Name, config.CardsTableName, sw.ElapsedMilliseconds)
            let msg = sprintf "DataAccess Error storing card collection: %s, in table: %s"
                              cardCollection.Name config.CardsTableName
            Error (DataAccessError (msg, Some ex))

let getCardCollection config
                      (logger : Serilog.ILogger)
                      (collectionName : string)
                      : Result<CardCollection, FumbleError> =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        // don't do any mapping for now, just store the raw collection
        try
            use client = getClient config.ConnectionConfig

            let table = Table.LoadTable(client, TableConfig(config.CardsTableName))
            let document = table.GetItemAsync(Primitive(collectionName)).Result
            if document = null || document.Count = 0
            then Error (NotFound (sprintf "Could not find collection data for %s" collectionName))
            else
                result {
                    let! cardCollection = tryDeserialize DynamoDbDeserializationError (document.ToJson())
                    logger.Information("{Category}: Completed '{DataAccessEvent}' for {CardCollection} in {Elapsed} ms",
                                       "DataAccess", "Get Card Collection", collectionName, sw.ElapsedMilliseconds)
                    return cardCollection
                }

        with ex ->
            logger.Error(ex, "{Category}: Error performing {DataAccessEvent} for collection: {CardCollection}, in table: {TableName}, in {Elapsed} ms",
                             "DataAccess", "Get Card Collection",
                             collectionName, config.CardsTableName, sw.ElapsedMilliseconds)
            let msg = sprintf "DataAccess Error getting card collection: %s, in table: %s"
                              collectionName config.CardsTableName
            Error (DataAccessError (msg, Some ex))


type FumbleDataAccess =
    { StoreCardCollection : CardCollection -> Result<unit, FumbleError>
      GetCardCollection : string -> Result<CardCollection, FumbleError> }

let configureDataAccess dataAccessConfig (logger : Serilog.ILogger) =
    logger.Debug("Configuring Dynamo db access with config: {DataAccessConfig}",
                 sprintf "(%A)" dataAccessConfig)

    { StoreCardCollection = storeCardCollection dataAccessConfig logger
      GetCardCollection = getCardCollection dataAccessConfig logger }

