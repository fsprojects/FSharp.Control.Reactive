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

    // This is a private type to wrap
    // a list of disposables which can be cast to 
    // IDisposable and back
    type private ListDisposable = 
        ListDisposable of IDisposable list with
        interface IDisposable with
            member this.Dispose() = 
                let (ListDisposable disp) = this
                disp |> List.iter dispose    

    /// Compose two disposables together so they are both disposed when disposed is called on the 'composite' disposable.
    /// Equivalent to the append operator on collection types.
    /// Note that 1 is disposed before 2.
    let compose (disposable1 : IDisposable) (disposable2 : IDisposable) : IDisposable =
        let tail (ListDisposable list) = list
        let list = 
            match disposable1, disposable2 with
            | (:? ListDisposable as list1), (:? ListDisposable as list2) -> 
                tail list1 @ tail list2
            | :? ListDisposable as list, last -> 
                tail list @ [last]
            | head, (:? ListDisposable as list) ->                 
                head::(tail list)
            | a, b -> [a; b]

        ListDisposable list :> IDisposable

    /// Uses the double-indirection pattern to assign the disposable returned by the specified disposableFactory
    /// to the 'Disposable' property of the specified serial disposable.
    let setIndirectly disposableFactory (d : SerialDisposable) =
        let indirection = new SingleAssignmentDisposable ()
        d.Disposable <- indirection
        indirection.Disposable <- disposableFactory ()

    let setInnerDisposalOf (d : SerialDisposable) x = d.Disposable <- x

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