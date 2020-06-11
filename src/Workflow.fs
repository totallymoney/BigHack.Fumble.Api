module BigHack.Fumble.Api.Workflow

open System.Diagnostics
open TypeExtensions
open BigHack.Fumble.Api.Infrastructure
open BigHack.Fumble.Api.Model
open BigHack.Fumble.Api.ErrorHelper


let logApiResults (logger : Serilog.ILogger)
                  requestName
                  (stopWatch : Stopwatch)
                  result =
    match result with
    | Ok _ ->
        logger.Information("{Category}: Completed {RequestName} in {Elapsed} ms",
                           "ApiRequest", requestName, stopWatch.ElapsedMilliseconds)
    | Error err ->
        logger.ForContext("ErrorDto", JsonSerializer.serialize (mapErrorDto err))
              .Error("{Category}: Error Performing {RequestName} for: {CustomerId} in {Elapsed} ms",
                     "ApiRequest", requestName, stopWatch.ElapsedMilliseconds)

let DefaultCardsCollectionName = "Default"

let updateCards (ctx: AppContext) cards =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let model = { Name = DefaultCardsCollectionName; Cards = cards }
    ctx.DataAccess.StoreCardCollection model
    |> tee (fun x ->
        logApiResults ctx.Logger "AddCustomerValidation" sw x)

let getCards (ctx : AppContext) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    ctx.DataAccess.GetCardCollection DefaultCardsCollectionName
    |> tee (fun x ->
        logApiResults ctx.Logger "DeleteCustomer" sw x)
