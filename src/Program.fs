module Program

open Amazon.Lambda.Core
open Amazon.Lambda.Serialization.SystemTextJson

[<LambdaSerializer(typeof<LambdaJsonSerializer>)>]
let MyEvent (event: System.Object) = // todo: add the nuget package for the lambda trigger event type
  printfn "Hello World"
  0

/// prevents build warning; must be compiled last
[<EntryPoint>] 
let main _ =
  0