module BigHack.Fumble.Api.ErrorHelper

open BigHack.Fumble.Api.Infrastructure

type ErrorType =
    | NoError
    | NotFound
    | ConfigurationError
    | DataAccessError
    | SerializationError
    | DynamoDbDeserializationError
    | ApiRequestDeserializationError
    | AirTableRequestError
    | AirTableDeserializationError

type ErrorDto =
    { ErrorType : ErrorType
      ErrorMessage : string }
with
    static member empty =
        { ErrorType = NoError
          ErrorMessage = "" }

let mapErrorDto error =
    match error with
    | FumbleError.NotFound msg  ->
        { ErrorType = NotFound
          ErrorMessage = msg }
    | FumbleError.ConfigurationError msg ->
        { ErrorType = ConfigurationError
          ErrorMessage = msg }
    | FumbleError.SerializationError ex ->
        { ErrorType = SerializationError
          ErrorMessage = ex.Message }
    | FumbleError.DataAccessError (msg, exOpt) ->
        match exOpt with
        | Some ex ->
            { ErrorType = DataAccessError
              ErrorMessage = sprintf "%s: %s" msg ex.Message }
        | None ->
            { ErrorType = DataAccessError
              ErrorMessage = msg }
    | FumbleError.DynamoDbDeserializationError (msg, json) ->
        { ErrorType = DynamoDbDeserializationError
          ErrorMessage = sprintf "%s: Deserializing json: %s" msg json}
    | FumbleError.ApiRequestDeserializationError (msg, json) ->
        { ErrorType = ApiRequestDeserializationError
          ErrorMessage = sprintf "%s: Deserializing json: %s" msg json}
    | FumbleError.AirTableRequestError msg ->
        { ErrorType = AirTableRequestError
          ErrorMessage = msg }
    | FumbleError.AirTableDeserializationError (msg, json) ->
        { ErrorType = AirTableDeserializationError
          ErrorMessage = sprintf "%s: Deserializing json: %s" msg json}
