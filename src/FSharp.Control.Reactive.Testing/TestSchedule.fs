/// The Reactive module provides operators for working with TestScheduler in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Control.Reactive.Testing.TestSchedule

open System
open FSharp.Control.Reactive
open Microsoft.Reactive.Testing

/// Virtual time scheduler used for testing applications and libraries built using Reactive Extensions.
let usage f = 
    f <| TestScheduler ()
    
/// Creates a cold observable using the specified timestamped notification messages.
let coldObservable (s : TestScheduler) (TestNotifications ms) =
    s.CreateColdObservable<'a> (ms |> List.toArray)

/// Creates a hot observable using the specified timestamped notification messages.
let hotObservable (s : TestScheduler) (TestNotifications ms) = 
    s.CreateHotObservable (ms |> List.toArray)
    
/// Advances the scheduler's clock by the specified relative time, running all work scheduled for that timespan.
let advanceBy (s : TestScheduler) time =
    s.AdvanceBy time; s
    
/// Advances the scheduler's clock to the specified time, running all work till that point.
let advanceTo (s : TestScheduler) time =
    s.AdvanceTo time; s

/// Creates an observer that records received notification messages and timestamps those.
let createObserver<'a> (s : TestScheduler) =
    s.CreateObserver<'a> ()

/// Schedules an action to be executed at dueTime
[<Obsolete("Use the 'Schedule' module for scheduling actions")>]
let schedule (s : TestScheduler) state (dueTime : DateTimeOffset) f =
    s.Schedule (state, dueTime, fun x y -> f x y)

/// Starts the virtual time scheduler.
let start (s : TestScheduler) =
    s.Start (); s

/// Starts the test scheduler and uses default virtual times to "ReactiveTest.Created", to "ReactiveTest.Subscribed", and to "ReactiveTest.Disposed".
let startObservable (s : TestScheduler) source =
    s.Start (fun () -> source)

/// Subscribes to the observable with an observer that records received notification messages and timestamps those.
let subscribeTestObserver (sch : TestScheduler) source =
    let observer = sch.CreateObserver ()
    source |> Observable.subscribeObserver observer |> ignore
    observer

/// Subscribes (before starting) to the observable with an observer that records received notification messages and timestamps those.
let subscribeTestObserverStart sch source =
    let observer = subscribeTestObserver sch source
    sch.Start ()
    observer

/// Subscribes before and after and specified function to the observable with an observer that records received notification messasges and timestamps those.
let subscribeBeforeAfter sch source f =
    subscribeTestObserver sch source,
    (f source |> ignore; subscribeTestObserver sch source)