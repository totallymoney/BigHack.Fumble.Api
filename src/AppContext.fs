namespace BigHack.Fumble.Api

open System
open BigHack.Fumble.Api

type AppContext =
    { DataAccess : DataAccess.FumbleDataAccess
      AirTable : AirTable.FumbleAirTable
      Logger : Serilog.ILogger
      Now : unit -> DateTimeOffset }

module AppContext =

    type TimeProvider = unit -> DateTimeOffset

    let create dataAccess airTable logger now =
        { DataAccess = dataAccess
          AirTable = airTable
          Logger = logger
          Now = now }

    let configure dataAccessConfig
                  airTableConfig
                  logger
                  (timeProvider : TimeProvider) =
        let dataAccess = DataAccess.configureDataAccess dataAccessConfig logger
        let airTable = AirTable.configureAirTable airTableConfig logger
        create dataAccess airTable logger timeProvider



