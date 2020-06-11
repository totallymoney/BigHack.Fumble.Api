module BigHack.Fumble.Api.LambdaUtils

open System
open System.Collections.Generic
open FSharp.Data
open Amazon.Lambda.Core
open Amazon.Lambda.APIGatewayEvents

open BigHack.Fumble.Api
open BigHack.Fumble.Api.TypeExtensions
open BigHack.Fumble.Api.Model
open BigHack.Fumble.Api.ErrorHelper
open BigHack.Fumble.Api.ResponseDto
open BigHack.Fumble.Api.Infrastructure

module ApiGatewayProxyResponse =
    let private create statusCode body headers =
        let resp = APIGatewayProxyResponse()
        resp.Body <- body
        resp.Headers <- headers |> dict |> Dictionary<string, string>
        resp.IsBase64Encoded <- false
        resp.StatusCode <- statusCode

        resp

    let success body =
        create HttpStatusCodes.OK
               body
               [HttpResponseHeaders.ContentType, HttpContentTypes.Json]

    let notFound = create HttpStatusCodes.NotFound null []

    let error errorCode body =
        create errorCode
               body
               [HttpResponseHeaders.ContentType, HttpContentTypes.Json]

let responseBuilder (errorDto : ErrorDto option) =
    match errorDto with
    | None -> ApiGatewayProxyResponse.success
    | Some err ->
        match err.ErrorType with
        | ErrorType.NoError ->
            ApiGatewayProxyResponse.success
        | ErrorType.NotFound ->
            (fun _ -> ApiGatewayProxyResponse.notFound)
        | ErrorType.ApiRequestDeserializationError ->
            ApiGatewayProxyResponse.error HttpStatusCodes.BadRequest
        | ErrorType.ConfigurationError
        | ErrorType.SerializationError
        | ErrorType.DynamoDbDeserializationError ->
            ApiGatewayProxyResponse.error HttpStatusCodes.InternalServerError
        | ErrorType.DataAccessError ->
            ApiGatewayProxyResponse.error HttpStatusCodes.ServiceUnavailable

let jsonSetupError =
    """
    {
        "Error": {
            "ErrorType": "ConfigurationError",
            "ErrorMessage": "Application setup and configuration failed"
        }
    }
    """
let setupErrorResponse =
    (ApiGatewayProxyResponse.error HttpStatusCodes.InternalServerError jsonSetupError)

let jsonSerializationError =
    """
    {
        "Error": {
            "ErrorType": "SerializationError",
            "ErrorMessage": "Could not serialize record or error. Serilization is broken."
        }
    }
    """

let logError ctx category failedAction result =
    match result with
    | Ok _ -> result
    | Error err ->
        let errorDto = mapErrorDto err
        ctx.Logger.Error("{Category}, {FailedAction}, {ErrorType}: {Error}",
                         category,
                         failedAction,
                         sprintf "%A" errorDto.ErrorType,
                         errorDto.ErrorMessage)
        result

// Note we return an `errorDto` as well as the json so that
// we can create the correct error code with `responseBuilder`
let trySerializeForApiResponse logError
                               (dto : ResponseDto.IApiResponse)
                               : ErrorDto option * string=
    trySerialize dto
    |> logError "Api Response Serialization"
    |> function
        | Ok jsonStr -> dto.GetError(), jsonStr
        | Error err ->
            // there was an error serializing the dto
            // let's log it and create a new response dto for that
            let errDto = Some (ErrorHelper.mapErrorDto err)
            let dto = { OnlyError.Error = Some (ErrorHelper.mapErrorDto err) }
            trySerialize dto
            |> logError "Serialization Error Serialization"
            |> function
                | Ok errJsonStr -> errDto, errJsonStr
                | Error errorErr ->
                    // There was an error serializing the error dto
                    let errDto = Some (ErrorHelper.mapErrorDto errorErr)
                    // Serialization must be broken somehow. The only thing we
                    // can do now is return a pre-created response
                    errDto, jsonSerializationError

let tryDeserializeUpdateCardsRequestJson json : Result<Card list, FumbleError>  =
    tryDeserialize ApiRequestDeserializationError json

let handleAsyncEvent (lambdaContext : ILambdaContext)
                     eventName
                     workflowCall
                     input =
    // `setupAndCreateAppContext` and `workflowCall` are both expected to
    // handle their own errors. This will handle any input error
    result {
        let! ctx = Config.setupAndCreateAppContext lambdaContext
        return!
            input
            |> logError ctx eventName "ParseInput"
            >>= workflowCall ctx
    }
    |> tee (fun _ -> Serilog.Log.CloseAndFlush())
    |> Result.either (fun _ -> ()) (failwithf "%A")

let handleJsonRequest (lambdaContext : ILambdaContext)
                      eventName
                      workflowCall
                      mapResultToDto
                      input =
    // `setupAndCreateAppContext` and `workflowCall` are both expected to
    // handle their own errors. This will handle any input error
    result {
        let! ctx = Config.setupAndCreateAppContext lambdaContext

        return
            input
            |> logError ctx eventName "ParseInput"
            >>= workflowCall ctx
            |> mapResultToDto
            |> trySerializeForApiResponse (logError ctx eventName)
            ||> responseBuilder
    }
    |> tee (fun _ -> Serilog.Log.CloseAndFlush())
    |> Result.either id (fun _ -> setupErrorResponse)
