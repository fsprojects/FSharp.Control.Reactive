module FSharp.Reactive.Tests.Xunit_MarbleSpecs

open FSharp.Control.Reactive
open FSharp.Control.Reactive.Testing
//open FSharp.Control.Reactive.Testing.Marbles
open Xunit
open FsCheck

let test f =
    Check.QuickThrowOnFailure <| fun () ->
    TestSchedule.usage <| fun sch -> f sch

//[<Fact>]
//let ``cold: marble should parse simple test`` () =
//    test <| fun sch ->
//    let a = cold "--1--2--|" sch
//    let e =      "--2--3--|"
//    let s =        "^-----!"

//    a |> Observable.map (int >> (+) 1 >> string)
//      |> Marbles.expectMessages sch e
//    a |> TestObservable.subscriptions
//      |> Marbles.expectSubscriptions [s]

//[<Fact>]
//let ``cold: marble should parse concat`` () =
//    test <| fun sch ->
//    let a = cold "--1--2--|" sch
//    let sa =       "^-----!"
//    let b = cold        "--3--|" sch
//    let sb =        "------^---!"
//    let e =      "--1--2----3--|"

//    Observable.concat b a
//    |> Marbles.expectMessages sch e
//    TestObservable.subscriptions a |> expectSubscriptions [sa]
//    TestObservable.subscriptions b |> expectSubscriptions [sb]

//[<Fact>]
//let ``cold/hot: marble should parse concat`` () =
//    test <| fun sch ->
//    let a = cold "--1--2--|" sch
//    let sa =       "^-----!"
//    let b = hot  "^----3----4--|" sch
//    let sb =     "------^---!"
//    let e =      "--1--2----4--|"

//    Observable.concat b a
//    |> Marbles.expectMessages sch e
//    TestObservable.subscriptions a |> expectSubscriptions [sa]
//    TestObservable.subscriptions b |> expectSubscriptions [sb]

//[<Fact>]
//let ``cold/hot: marble concat with negative times`` () =
//    test <| fun sch ->
//    let a = cold    "--1--2--|" sch
//    let sa =          "^-----!"
//    let b = hot  "0--^----3----4--|" sch
//    let sb =           "------^---!"
//    let e =         "--1--2----4--|"

//    Observable.concat b a
//    |> Marbles.expectMessages sch e
//    TestObservable.subscriptions a |> expectSubscriptions [sa]
//    TestObservable.subscriptions b |> expectSubscriptions [sb]

//[<Fact>]
//let ``zip marbles`` () =
//    test <| fun sch ->
//    let a = cold "-- a  --- b  -|" sch
//    let sa =        "^  --- -  -!"
//    let b = cold "-   c ---- d -|" sch
//    let sb =         "^ ----   -!"
//    let e =      "--(ac)---(bd)-|"

//    Observable.zipWith (sprintf "%s%s") a b
//    |> Marbles.expectMessages sch e
//    TestObservable.subscriptions a |> expectSubscriptions [sa]
//    TestObservable.subscriptions b |> expectSubscriptions [sb]