namespace FSharp.Control.Reactive

open System
open System.Reactive.Disposables

module Disposables =

     /// Returns an IDisposable that disposes all the underlying disposables
     let compose (disposables: #seq<IDisposable>) =
         Disposable.Create(fun _ ->
             disposables
             |> Seq.iter(fun x -> x.Dispose()))

type Disposable () =

    /// Creates a new composite disposable with no disposables contained initially.
    static member Composite with get () = new CompositeDisposable ()

    /// Represents a disposable resource whose underlying disposable resource can be replaced by another disposable resource,
    /// causing automatic disposal of the previous underlying disposable resource.
    static member Serial with get () = new SerialDisposable ()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Operators to work on disposable types
module Disposable =

    /// Creates an disposable object that invokes the specified function when disposed.
    let create f = Disposable.Create (Action f)

    /// An empty disposable which does nothing when disposed
    let empty = Disposable.Empty

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

    /// Compose two disposables together so they are both disposed when disposed is called on the 'composite' disposable.
    /// The expected usage is disposable1 |> compose disposable2,
    //  and disposable1 will be disposed before disposable2.
    let compose (disposable2 : #IDisposable) (disposable1 : #IDisposable) : IDisposable =
        // Do not replace Disposable.Create here
        // with create. F# will convert the lambda to
        // one which returns unit (null), eliminating
        // the possibility of any tail-call
        Disposable.Create (fun () ->
            disposable1.Dispose()
            disposable2.Dispose()
        )

    /// Uses the double-indirection pattern to assign the disposable returned by the specified disposableFactory
    /// to the 'Disposable' property of the specified serial disposable.
    let setIndirectly disposableFactory (d : SerialDisposable) =
        let indirection = new SingleAssignmentDisposable ()
        d.Disposable <- indirection
        indirection.Disposable <- disposableFactory ()

    let setInnerDisposalOf (d : SerialDisposable) x = d.Disposable <- x

    /// Registers a disposable object into a composite disposable object
    /// ensuring it's disposition when composite disposable object is disposed.
    let inline disposeWith (d: CompositeDisposable) x =
        d.Add x

open System.Threading

type WaitHandle =

    /// Initializes a new instance of the ManualResetEvent class with initial state set to 'false'.
    static member Signal with get () = new ManualResetEvent (initialState=false)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WaitHandle =

    /// Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
    let flag (s : EventWaitHandle) = s.Set () |> ignore

    /// Blocks the current thread until the WaitHandle receives a signal.
    let wait (s : System.Threading.WaitHandle) = s.WaitOne () |> ignore