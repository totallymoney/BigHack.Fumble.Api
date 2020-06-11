module BigHack.Fumble.Api.Infrastructure

open System
open Microsoft.FSharpLu.Json
open Newtonsoft.Json

open TypeExtensions

type ResponseError =
    { ResponseBody : string
      Error : string }

type FumbleError =
    | NotFound of string
    | ConfigurationError of string
    | SerializationError of Exception
    | DynamoDbDeserializationError of string * string
    | DataAccessError of string * Exception option
    | ApiRequestDeserializationError of string * string

module Environment =
    let envVars = seq {
        let d = Environment.GetEnvironmentVariables()
        for key in d.Keys do yield (string key, string d.[key]) } |> Map.ofSeq

    let tryItem key =
        envVars
        |> Map.tryFind key
        |> Result.ofOption (sprintf "Env var `%s` not found" key)

    let getItem key =
        envVars |> Map.find key

    let getItemOrDefault key defaultValue =
        envVars
        |> Map.tryFind key
        |> Option.defaultValue defaultValue


type JsonSettings =
    static member settings =
        let settings =
            JsonSerializerSettings(
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore)
        settings.Converters.Add(CompactUnionJsonConverter())
        settings
    static member formatting = Formatting.None

type JsonSerializer = With<JsonSettings>

let inline trySerialize obj =
    try
        obj |> JsonSerializer.serialize |> Ok
    with ex ->
        Error (SerializationError ex)

let inline tryDeserialize errType jsonStr : Result<'a, FumbleError> =
    match JsonSerializer.tryDeserialize<'a> jsonStr with
    | Choice1Of2 result -> Ok result
    | Choice2Of2 err -> Error (errType (err, jsonStr))
