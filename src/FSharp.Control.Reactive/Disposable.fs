namespace FSharp.Control.Reactive

open System
open System.Reactive.Disposables

module Disposables = 
     
     /// Returns an IDisposable that disposes all the underlying disposables
     let compose (disposables: #seq<IDisposable>) =
         Disposable.Create(fun _ -> 
             disposables 
             |> Seq.iter(fun x -> x.Dispose()))

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Operators to work on disposable types
module Disposable =

    /// Creates an disposable object that invokes the specified function when disposed.
    let create f = Disposable.Create (Action f)

    /// Execute and action without the resource while the disposable is still 'active'.
    /// The used resource will be disposed afterwards.
    let ignoring f d =
        use x = d
        f () |> ignore

    /// Execute and action with the resource while the disposable is still 'active'.
    /// The used resource will be disposed afterwards.
    let using f d =
        use x = d 
        f x

    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    let dispose (x : IDisposable) = x.Dispose ()

    /// Creates a new composite disposable with no disposables contained initially.
    let composite = (fun () -> new CompositeDisposable ()) () :> IDisposable

    /// Compose two disposables together so they are both disposed when disposed is called on the 'composite' disposable.
    let compose (x : IDisposable) (d : IDisposable) =
        match d, x with
        | :? CompositeDisposable as d, x -> d.Add x; d :> IDisposable
        | d, (:? CompositeDisposable as x) -> x.Add d; x :> IDisposable
        | d, x -> let acc = new CompositeDisposable ()
                  acc.Add d
                  acc.Add x
                  acc :> IDisposable

    /// Uses the double-indirection pattern to assign the disposable returned by the specified disposableFactory
    /// to the 'Disposable' property of the specified serial disposable.
    let setIndirectly disposableFactory (d : SerialDisposable) =
        let indirection = new SingleAssignmentDisposable ()
        d.Disposable <- indirection
        indirection.Disposable <- disposableFactory ()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WaitHandle =
    open System.Threading

    /// Initializes a new instance of the ManualResetEvent class with initial state set to 'false'.
    let signal = (fun () -> new ManualResetEvent (initialState=false)) ()
    
    /// Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
    let flag (s : EventWaitHandle) = s.Set () |> ignore
    
    /// Blocks the current thread until the WaitHandle receives a signal.
    let wait (s : WaitHandle) = s.WaitOne () |> ignore