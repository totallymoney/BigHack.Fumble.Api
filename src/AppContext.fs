namespace BigHack.Fumble.Api

open System
open BigHack.Fumble.Api

type AppContext =
    { DataAccess : DataAccess.FumbleDataAccess
      Logger : Serilog.ILogger
      Now : unit -> DateTimeOffset }

module AppContext =

    type TimeProvider = unit -> DateTimeOffset

    let create dataAccess logger now =
        { DataAccess = dataAccess
          Logger = logger
          Now = now }

    let configure dataAccessConfig
                  logger
                  (timeProvider : TimeProvider) =
        let dataAccess = DataAccess.configureDataAccess dataAccessConfig logger
        create dataAccess logger timeProvider



