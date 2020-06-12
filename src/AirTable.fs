module BigHack.Fumble.Api.AirTable

open FSharp.Data
open BigHack.Fumble.Api.TypeExtensions
open BigHack.Fumble.Api.Infrastructure

type AirTableConfig =
    { Token : string }

// mine
//type AirTableFields =
//    { CardTitle : string
//      CardContent : string }

// Craig's
type AirTableFields =
    { CardTitle : string
      CardDescription : string }

type AirTableRecord =
    { id : string
      fields : AirTableFields }
      //createdTime : DateTime  // we don't need this now

type AirTableResponse =
   { records : AirTableRecord list
     offset : string }

//let AirTableUrl = "https://api.airtable.com/v0/appL88ejW6sKdbsMm/" // mine
let AirTableUrl = "https://api.airtable.com/v0/appIWJZ3lmdLXmeX6/" // Craig's

let getBodyFromRequest (ccResponse : HttpResponse) =
    match ccResponse.Body with
    | Binary binary ->
        AirTableRequestError
            (sprintf "Status Code: [%d]. Expecting text, but got a binary response (%d bytes)"
                     ccResponse.StatusCode binary.Length)
        |> Error
    | Text text ->
        if ccResponse.StatusCode >= 200 && ccResponse.StatusCode < 300
        then Ok text
        else
            AirTableRequestError (sprintf "Status Code: [%d]. Body: %s"
                                            ccResponse.StatusCode text)
            |> Error

let performGetRequest settings (path : string) =
    try
        Http.Request(
            sprintf "%s/%s" (AirTableUrl.TrimEnd('/')) (path.TrimStart('/')),
            httpMethod = "GET",
            // By default this module throws exceptions for 400/500 responses.
            // Setting silentHttpErrors = true stops those exceptions
            silentHttpErrors = true,
            timeout = int 2000,
            headers = [
                HttpRequestHeaders.Accept HttpContentTypes.Json
                HttpRequestHeaders.Authorization (sprintf "Bearer %s" settings.Token)
            ]
        ) |> Ok
     with ex ->
        Error (AirTableRequestError (ex.Message))

let logResult (logger : Serilog.ILogger)
              requestName
              (stopwatch : System.Diagnostics.Stopwatch)
              (result : Result<_, FumbleError>) =
    match result with
    | Ok outcome ->
        logger.Information("{Category}: Completed {AirTableEvent}, in {Elapsed} ms",
                           "AirTable", requestName, stopwatch.ElapsedMilliseconds)
    | Error err ->
        logger.Error("{Category}: Error Performing {AirTableEvent}, with error: {Error}, in {Elapsed} ms",
                     "AirTable", requestName, (sprintf "(%A)" err), stopwatch.ElapsedMilliseconds)

let getCards settings
             (logger : Serilog.ILogger)
             : Result<AirTableResponse, FumbleError> =

    let sw = System.Diagnostics.Stopwatch.StartNew()
    performGetRequest settings
                      "/Cards" // Craig's
                      //"/AirTableCards" // mine
    |> Result.bind getBodyFromRequest
    |> Result.bind (tryDeserialize AirTableDeserializationError)
    |> tee (logResult logger "GetCards" sw)

type FumbleAirTable =
    { GetCards : unit -> Result<AirTableResponse, FumbleError> }

let configureAirTable airTableConfig (logger : Serilog.ILogger) =
    { GetCards = (fun () -> getCards airTableConfig logger) }

