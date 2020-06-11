module BigHack.Fumble.Api.ResponseDto

open System
open BigHack.Fumble.Api.Model
open BigHack.Fumble.Api.ErrorHelper

type IApiResponse =
    abstract member GetError : unit -> ErrorDto option

type CardsResponse =
    { Cards : Card seq
      Error : ErrorDto option }
    interface IApiResponse with
        member x.GetError() = x.Error

type OnlyError =
    { Error : ErrorDto option }
    interface IApiResponse with
        member x.GetError() = x.Error

let mapDomainToDto (cardCollection : CardCollection) : CardsResponse =
    { Cards = cardCollection.Cards
      Error = None }

let mapErrorToDto error : CardsResponse =
    { Cards = []
      Error = Some (ErrorHelper.mapErrorDto error) }

let mapResultToDto = function
    | Ok x -> mapDomainToDto x
    | Error err -> mapErrorToDto err

let mapEmptyResultToDto = function
    | Ok () -> { Error = None }
    | Error err -> { Error = Some (ErrorHelper.mapErrorDto err) }

