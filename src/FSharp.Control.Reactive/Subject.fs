namespace FSharp.Control.Reactive

open System.Reactive.Subjects

type Subject<'a> private () =
    
        /// Represents and object that is both an observable sequence as well as an observer.
        /// Each notification is broadcasted to all subscribed observers.
        static member broadcast = new System.Reactive.Subjects.Subject<'a> ()

        /// Represents the result of an asynchronous operation.
        /// The last value before the OnCompleted notification, or the error received through OnError, is sent to all subscribed observers.
        static member async = new AsyncSubject<'a> ()

        /// Represents an object that is both an observable sequence as well as an observer.
        /// Each notification is broadcasted to all subscribed and future observers, subject to buffer trimming policies.
        static member replay = new ReplaySubject<'a> ()

        /// Represents a value that changes over time.
        /// Observers can subscribe to the subject to receive the last (or initial) value and all subsequent notifications.
        static member behavior x = new BehaviorSubject<'a> (x)

/// The Reactive module provides operators working with Subjects in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Subject =

    /// Notifies all subscribed observers about the arrival of the specified element in the sequence.
    let onNext x (s : SubjectBase<'a>) =
        s.OnNext x; s

    /// Notifies all subscribed observers about the arrival of the specified elements in the sequence.
    let onNexts xs s =
        Seq.iter (fun x -> onNext x s |> ignore) xs; s
    
    /// Notifies all subscribed observers about the specified exception.
    let onError ex (s : SubjectBase<'a>) =
        s.OnError ex; s

    /// Notifies all subscribed observers about the end of the sequence.
    let onCompleted (s : SubjectBase<'a>) =
        s.OnCompleted (); s