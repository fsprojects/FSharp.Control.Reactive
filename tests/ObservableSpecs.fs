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

  let subscription = observable |> Observable.subscribe (fun _ -> incr next) (fun _ -> error := true) (fun () -> completed := true)

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
let ``When subscribing to an event, only OnNext should be fired once.``() =
  let next = ref 0
  let error = ref false
  let completed = ref false

  let tester = TestType()
//  let subscription = tester.TestEvent.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
  let observable = Observable.fromEventPattern tester "TestEvent"
  let subscription = observable.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
  tester.Trigger()

  Assert.That(!next, Is.EqualTo(1))
  Assert.That(!error, Is.False)
  Assert.That(!completed, Is.False)

[<Test>]
let ``When subscribing to an observable that fires an exception using the ObservableBuilder, only OnError should be fired``() =
  let builder = observe {
    failwith "Test"
    return 1 }
  builder |> ``should be`` 0 true false

[<Test>]
let ``When zip is defined with the applicative, it should match the result of Observable_zip``() =
  let a = mreturn 1
  let b = mreturn 2
  let zip b a = (fun x y -> x,y) <!> a <*> b // allows you to call a |> zip b, similar to Observable.zip

  let actual = ref (0,0)
  let expected = ref (0,0)

  (zip a b).Subscribe(fun x -> actual := x) |> ignore
  (Observable.zip a b).Subscribe(fun x -> expected := x) |> ignore

  Assert.That(!actual, Is.EqualTo(!expected))

[<Test>]
let ``Test should show the stack overflow is fixed with Rx 2 beta``() =
  let test() =
    let rec g x = observe {
      yield x
      if x < 100000 then
        yield! g (x + 1) }
    g 5 |> Observable.subscribe ignore ignore ignore |> ignore
  Assert.DoesNotThrow(TestDelegate(fun () -> test()))
