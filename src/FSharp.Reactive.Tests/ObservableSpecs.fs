module ReactiveSpecs

open System
open System.Collections.Generic
open FSharp.Reactive
open NaturalSpec

[<Scenario>]
let ``When subscribing to an observable event, it should raise three items.``() =
  let result = ref 0
  let incr a = result := !result + 1
  let testEvent = Event<EventHandler, EventArgs>()
  let obs = Observable.fromEvent(testEvent.Publish)

  let ``listening with`` (f: 'a -> unit) lst (obs: IObservable<'a>) =
    printMethod ""
    let d = obs.Subscribe(f)
    for i in lst do testEvent.Trigger(obs, EventArgs())
    d.Dispose()
    !result

  let ``been called 3 times`` times =
    printMethod ""
    times = 3

  Given obs
  |> When ``listening with`` (incr) [1..3]
  |> It should have ``been called 3 times``
  |> Verify

[<Scenario>]
let ``When subscribing to an observable async workflow, it should only raise one item before completion.``() =
  let result = ref 0
  let incr a = result := !result + 1
  let testAsync = async { return 1 }
  let obs = Observable.fromAsync testAsync

  let ``listening with`` (f: _ -> unit) (obs: IObservable<'a>) =
    printMethod ""
    let d = obs.Subscribe(f)
    !result

  Given obs
  |> When ``listening with`` (incr)
  |> It should equal 1
  |> Verify