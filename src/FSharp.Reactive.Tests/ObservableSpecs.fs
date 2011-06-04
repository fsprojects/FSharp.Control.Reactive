module FSharp.Reactive.Tests.ObservableSpecs

open System
open System.Collections.Generic
open System.Reactive
open System.Reactive.Linq
open FSharp.Reactive
open NUnit.Framework

let ``should be`` expectedNext expectedError expectedCompleted (observable:'a IObservable) =
  let next = ref 0
  let error = ref false
  let completed = ref false

  let subscription = observable.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))

  Assert.That(!next, Is.EqualTo(expectedNext))
  Assert.That(!error, Is.EqualTo(expectedError))
  Assert.That(!completed, Is.EqualTo(expectedCompleted))

[<Test>]
let ``When subscribing to a single value observable, OnNext and OnCompleted should be fired``() =
  Observable.Return(1) |> ``should be`` 1 false true

[<Test>]
let ``When subscribing to an empty observable, only OnCompleted should be fired``() =
  Observable.Empty() |> ``should be`` 0 false true

[<Test>]
let ``When subscribing to an observable that fires an exception, only OnError should be fired``() =
  Observable.Throw(Exception()) |> ``should be`` 0 true false

[<Test>]
[<Ignore("Fails with \"Couldn't find declaring type with name 'FSharp.Reactive.Tests.ObservableSpecs.When subscribing to an observable async workflow, it should only raise one item before completion'\"")>]
let ``When subscribing to an observable async workflow, it should only raise one item before completion.``() =
  let testAsync = async { return 1 }
  Observable.fromAsync testAsync |> ``should be`` 1 false false

[<Test>]
let ``When subscribing to an F# event, only OnNext should be called``() =
  let next = ref 0
  let error = ref false
  let completed = ref false
  let testEvent = Event<EventHandler, EventArgs>()

  let subscription = testEvent.Publish.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
  testEvent.Trigger(null, EventArgs())

  Assert.That(!next, Is.EqualTo(1))
  Assert.That(!error, Is.False)
  Assert.That(!completed, Is.False)

type TestType() =
  let testEvent = Event<EventHandler, EventArgs>()
  [<CLIEvent>] member this.TestEvent = testEvent.Publish
  member this.Trigger() = testEvent.Trigger(this, EventArgs())

[<Test>]
[<Ignore("Fails with \"Couldn't find declaring type with name 'FSharp.Reactive.Tests.ObservableSpecs.When subscribing to an event, only OnNext should be fired once.'\"")>]
let ``When subscribing to an event, only OnNext should be fired once.``() =
  let next = ref 0
  let error = ref false
  let completed = ref false

  let tester = TestType()
//  let subscription = tester.TestEvent.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
  let observable = Observable.fromEvent tester.TestEvent
  let subscription = observable.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
  tester.Trigger()

  Assert.That(!next, Is.EqualTo(1))
  Assert.That(!error, Is.False)
  Assert.That(!completed, Is.False)
