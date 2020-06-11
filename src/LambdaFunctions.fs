module BigHack.Fumble.Api.LambdaFunctions

open Amazon.Lambda.Core
open Amazon.Lambda.APIGatewayEvents
open Amazon.Lambda.Serialization.SystemTextJson

open BigHack.Fumble.Api
open BigHack.Fumble.Api.TypeExtensions
open BigHack.Fumble.Api.LambdaUtils

[<LambdaSerializer(typeof<DefaultLambdaJsonSerializer>)>]
let getCards (gatewayRequest : APIGatewayProxyRequest)
             (lambdaContext : ILambdaContext) =
    Ok ()
    |> handleJsonRequest lambdaContext
                         "GetCards"
                         (fun ctx () -> Workflow.getCards ctx)
                         ResponseDto.mapResultToDto

[<LambdaSerializer(typeof<DefaultLambdaJsonSerializer>)>]
let updateCards (gatewayRequest : APIGatewayProxyRequest)
                (lambdaContext : ILambdaContext) =
    tryDeserializeUpdateCardsRequestJson gatewayRequest.Body
    |> handleJsonRequest lambdaContext
                         "UpdateCards"
                         Workflow.updateCards
                         ResponseDto.mapEmptyResultToDto
