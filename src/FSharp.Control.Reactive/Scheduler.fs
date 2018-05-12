namespace FSharp.Control.Reactive

open System
open System.Collections.Generic
open System.Threading
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency
open System.Reactive.Disposables
open System.Threading.Tasks

module Scheduler =

    /// Returns the 'ISchedulerLongRunning' implementation of the specified scheduler, or 'None' if no such implementation is available.
    let asLongRunning (sch : IScheduler) = 
        match sch.AsLongRunning () with 
        | null -> None 
        | x -> Some x

    type Scheduler () =

         /// Creates a historical scheduler with a minimum value of DateTimeOffset as initial clock.
        static member Historical = (fun () -> new HistoricalScheduler ()) ()

        /// Creates an object that schedules units of work on a designated thread.
        static member EventLoop = (fun () -> new EventLoopScheduler ()) ()

/// The Reactive module provides operators for working with IScheduler<_> in F#.
module Schedule =

    /// Schedule multiple 'schedule' operations together
    let multiple (fs : (IScheduler -> IDisposable) list) sch = 
        List.map (fun f -> f sch) fs |> Disposables.compose

    (***************************************************************
     * Simple
     ***************************************************************)

    /// Schedules an function to be executed.
    let action f sch = Scheduler.Schedule (sch, Action f)

    /// Schedules an function to be executed at a specified absolute time.
    let actionOffset offset f sch =
        Scheduler.Schedule (sch, (offset : DateTimeOffset), Action f)
    
    /// Schedules an function to be executed.
    let actionLong f sch =
        Scheduler.ScheduleLongRunning (sch, Action<ICancelable> f)

    /// Schedules a long-running piece of work.
    let actionLongState state f (sch : ISchedulerLongRunning) =
        sch.ScheduleLongRunning (state, Action<_, _> (fun s c -> f s c |> ignore))

    /// Schedules a function to be executed.
    let func state f (sch : IScheduler) =
        sch.Schedule (state, Func<_, _, _> f)

    /// Schedules a function to be executed after dueTime.
    let funcSpan state (dueTime : TimeSpan) f (sch : IScheduler) =
        sch.Schedule (state, dueTime, Func<_, _, _> f)

    /// Schedules a function to be executed at dueTime.
    let funcOffset state (dueTime : DateTimeOffset) f (sch : IScheduler) =
        sch.Schedule (state, dueTime, Func<_, _, _> f)

    (***************************************************************
     * Recursive
     ***************************************************************)

    /// Schedules an function to be executed recursively.
    let actionRec ff sch =
        Scheduler.Schedule (sch, Action<Action> (fun f -> ff (fun () -> f.Invoke ())))

    /// Schedules an function to be executed recursively.
    let actionSpanRec time ff sch =
        Scheduler.Schedule (sch, time, Action<TimeSpan, Action<TimeSpan>> (fun t f -> ff t f.Invoke))

    /// Schedules an function to be executed recursively.
    let actionAccRec state ff sch =
        Scheduler.Schedule (sch, state, (Action<'state, Action<'state>> (fun s f -> ff s f.Invoke)))

    /// Schedules an function to be executed recursively after a specified relative due time.
    let actionAccSpanRec state dueTime ff sch =
        Scheduler.Schedule (sch, state, dueTime, Action<'state, Action<'state, TimeSpan>> (fun s f -> ff s (fun x y -> f.Invoke (x, y))))

    /// Schedules an function to be executed recursively at a specified absolute due time.
    let actionOffsetRec dueTime ff sch =
        Scheduler.Schedule (sch, dueTime, Action<Action<DateTimeOffset>> (fun f -> ff (fun () -> f.Invoke dueTime)))

    /// Schedules an function to be executed recursively at a specified absolute due time.
    let actionAccOffsetRec state dueTime ff sch =
        Scheduler.Schedule (sch, state, dueTime, Action<'state, Action<'state, DateTimeOffset>> (fun s f -> ff s (fun x y -> f.Invoke (x, y))))

    (***************************************************************
     * Services Emulation
     ***************************************************************)

    /// Schedules a periodic piece  of work by dynamically discovering the scheduler's capabilities.
    /// If the scheduler supports periodic scheduling, the request will be forwarded to the periodic scheduling implementation.
    /// If the scheduler provides stopwatch functionality, the periodic task will be emulated using recursive scheduling with a stopwatch to correct for time slippage.
    /// Otherwise, the periodic task will be emulated using recursive scheduling.
    let periodicAcc state period f sch =
        Scheduler.SchedulePeriodic (sch, state, period, Func<_, _> f)

     /// Schedules a periodic piece  of work by dynamically discovering the scheduler's capabilities.
    /// If the scheduler supports periodic scheduling, the request will be forwarded to the periodic scheduling implementation.
    /// If the scheduler provides stopwatch functionality, the periodic task will be emulated using recursive scheduling with a stopwatch to correct for time slippage.
    /// Otherwise, the periodic task will be emulated using recursive scheduling.
    let periodicAccAction state period f sch = 
        Scheduler.SchedulePeriodic (sch, state, period, Action<_> f)

     /// Schedules a periodic piece  of work by dynamically discovering the scheduler's capabilities.
    /// If the scheduler supports periodic scheduling, the request will be forwarded to the periodic scheduling implementation.
    /// If the scheduler provides stopwatch functionality, the periodic task will be emulated using recursive scheduling with a stopwatch to correct for time slippage.
    /// Otherwise, the periodic task will be emulated using recursive scheduling.
    let periodicAction period f sch =
        Scheduler.SchedulePeriodic (sch, period, Action f)

    (***************************************************************
     * Wrappers
     ***************************************************************)

    /// Returns a scheduler that represents the original scheduler, without any of its interface-based optimizations (e.g. long running scheduling).
    let disableOptimizations = Scheduler.DisableOptimizations
    
    /// Returns a scheduler that represents the original scheduler, without any of its interface-based optimizations (e.g. long running scheduling).
    let disableOptimizationsTypes (optimizationInterfaces : Type list) sch =
        Scheduler.DisableOptimizations (sch, optimizationInterfaces |> List.toArray)

    /// Returns a scheduler that wraps the original scheduler, adding exception handling for scheduled actions.
    let catch f sch = Scheduler.Catch (sch, Func<_, _> f)
   
    (***************************************************************
     * Async
     ***************************************************************)

    let asAsync (cta: SchedulerOperation) =
        Async.FromContinuations <| fun (completed, caught, _) ->
            let aw = cta.GetAwaiter ()
            aw.OnCompleted(
              new Action(fun _ ->
                try aw.GetResult (); completed ()
                with ex -> caught ex))

    /// Yields execution of the current work item on the scheduler to another work item on the scheduler.
    /// The caller should await the result of calling 'yield' to schedule the remainder of the current work item (known as the continuation).
    let yield_ x = Scheduler.Yield x |> asAsync

    /// Yields execution of the current work item on the scheduler to another work item on the scheduler.
    /// The caller should await the result of calling 'yield' to schedule the remainder of the current work item (known as the continuation).
    let yieldCancel ct sch = Scheduler.Yield (sch, ct) |> asAsync

    /// Suspends execution of the current work item on the scheduler for the specified duration.
    /// The caller should await the result of calling 'sleep' to schedule the remainder of the current work item (known as the continuation) after the specified duration.
    let sleep dueTime sch = Scheduler.Sleep (sch, (dueTime : TimeSpan)) |> asAsync
    
    /// Suspends execution of the current work item on the scheduler for the specified duration.
    /// The caller should await the result of calling 'sleep' to schedule the remainder of the current work item (known as the continuation) after the specified duration.
    let sleepCancel dueTime ct sch = Scheduler.Sleep (sch, (dueTime : TimeSpan), ct) |> asAsync

    /// Suspends execution of the current work item on the scheduler for the specified duration.
    /// The caller should await the result of calling 'sleep' to schedule the remainder of the current work item (known as the continuation) after the specified due time.
    let sleepOffset dueTime sch = Scheduler.Sleep (sch, (dueTime : DateTimeOffset)) |> asAsync

    /// Suspends execution of the current work item on the scheduler for the specified duration.
    /// The caller should await the result of calling 'sleep' to schedule the remainder of the current work item (known as the continuation) after the specified due time.
    let sleepOffsetCancel dueTime ct sch = Scheduler.Sleep (sch, (dueTime : DateTimeOffset), ct) |> asAsync

    let asTask (ct: CancellationToken) (async: Async<'T>) =
        let tcs = TaskCompletionSource<'T>()
        Async.StartWithContinuations(
              async,
              tcs.SetResult,
              tcs.SetException,
              tcs.SetException,
              ct)
        tcs.Task

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let async f sch =
        let ff = Func<IScheduler, CancellationToken, Task> (fun sc ct -> 
            f sc ct |> asTask ct :> Task) 
        Scheduler.ScheduleAsync (sch, ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncResult f sch =
        let ff = Func<IScheduler, CancellationToken, Task<IDisposable>> (fun sc ct ->
            f sc ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccUnit state f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task> (fun sc st ct -> 
            f sc st ct |> asTask ct :> Task)
        Scheduler.ScheduleAsync (sch, state, ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccResult state f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task<IDisposable>> (fun sc st ct ->
            f sc st ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, state, ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncSpanUnit dueTime f sch =
        let ff = Func<IScheduler, CancellationToken, Task> (fun sc ct ->
            f sc ct |> asTask ct :> Task)
        Scheduler.ScheduleAsync (sch, (dueTime : TimeSpan), ff)
    
    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncSpanResult dueTime f sch =
        let ff = Func<IScheduler, CancellationToken, Task<IDisposable>> (fun sc ct ->
            f sc ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, (dueTime : TimeSpan), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncOffsetUnit dueTime f sch =
        let ff = Func<IScheduler, CancellationToken, Task> (fun sc ct ->
            f sc ct |> asTask ct :> Task)
        Scheduler.ScheduleAsync (sch, (dueTime : DateTimeOffset), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncOffsetResult dueTime f sch =
        let ff = Func<IScheduler, CancellationToken, Task<IDisposable>> (fun sc ct ->
            f sc ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, (dueTime : DateTimeOffset), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccSpanUnit state dueTime f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task> (fun sc st ct -> 
            f sc st ct |> asTask ct :> Task)
        Scheduler.ScheduleAsync (sch, state, (dueTime : TimeSpan), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccSpanResult state dueTime f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task<IDisposable>> (fun sc st ct ->
            f sc st ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, state, (dueTime : TimeSpan), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccOffsetUnit state dueTime f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task> (fun sc st ct ->
            f sc st ct |> asTask ct :> Task)
        Scheduler.ScheduleAsync (sch, state, (dueTime : DateTimeOffset), ff)

    /// Schedules the work using an asynchonous function, allowing for cooperative scheduling in a imperative coding style.
    let asyncAccOffsetResult state dueTime f sch =
        let ff = Func<IScheduler, 'state, CancellationToken, Task<IDisposable>> (fun sc st ct ->
            f sc st ct |> asTask ct :> Task<_>)
        Scheduler.ScheduleAsync (sch, state, (dueTime : DateTimeOffset), ff)

    