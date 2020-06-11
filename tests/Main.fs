module Main

open Expecto
open Expecto.Flip

[<Tests>]
let tests =

  testList "tests" [

    testCase "Hello world" <| fun _ ->
      Expect.isTrue "" true

  ]

[<EntryPoint>]
let main args =
  Tests.runTestsInAssemblyWithCLIArgs [ No_Spinner ] args
