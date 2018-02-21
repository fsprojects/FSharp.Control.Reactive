### New in 4.0.0 - (TBD)
* Support for netstandard-2.0
* Added Subjects to the core library.
* New FSharp.Control.Reactive.Testing library for a more idiomatic F# API over the Rx Testing library.

### New in 3.5.0 - (Released 2016/12/05)
* Update to Rx 3.0

### New in 3.4.1 - (Released 2016/04/26)
* Restore .NET 4.0 build instead of .NET 4.5.

### New in 3.4.0 - (Released 2016/04/24)
* .NET 4.5 and Profile259 builds available.

### New in 3.3.0 - (Released 2016/03/05)
* Added `Disposables` module with a `compose` function that will creates a new `IDisposable` that will dispose the sequence of provided `IDisposable`s.
* Fix `groupByCompare` to actually use the provided `comparer`.

### New in 3.2.0 - (Released 2015/05/16)
* Bug fix: `subscribeOn` should take an `IScheduler`
  * Renamed `subscribeOn` to `subscribeOnContext` to maintain consistency with other, similar functions
  * Added `subscribeOn` that takes an `IScheduler`
  * Added test for `subscribeOn`
* Added ofAsync, liftAsync, and flatmapAsync

### New in 3.1.2 - (Released 2015/04/03)
* Remove unnecessary signatures for Observable.Create and add test to demonstrate use.

### New in 3.1.1 - (Released 2015/02/12)
* Additional signature fixes to accept F# functions rather than `Func` #51

### New in 3.1.0 - (Released 2015/02/12)
* Fixed signature of `Observable.create` to better fit F#. #56
* Marked `ObservableBuilder.Return` and `ObservableBuilder.ReturnFrom` as `Obsolete` as users should use `Yield` and `YieldFrom`, which allow for multiple results. #53
* Changed signatures of `RxQueryBuilder` functions `ExactlyOne`, `ExactlyOneOrDefault`, `Find`, `Head`, `HeadOrDefault`, `Last`, and `LastOrDefault` to their Async equivalents. #57

### New in 3.0.1 - (Released 2014/12/07)
* #48 Some of the Observable functions aren't very composable
 * NOTE: Includes breaking changes due to updated function names and signatures
* #47 Replay overloads don't use the window or bufferSize parameters
* Use consistent function types in ObservableBuilder.
* Now using FSharp.Core from NuGet

### New in 2.4.0 - (Released 2014/10/14)
Updates courtesty of marklam:

* Changed the distinct functions to take F# functions
* Added a version of ofSeq which uses a scheduler (ofSeqOn)
* Reordered the parameters to scanInit to be more composable
* Corrected some spellings and added some tests

### New in 2.3.7 - (Released 2014/10/14)
* Updated to Rx 2.2.5
* Updated Paket bootstrapper

### New in 2.3.6 - (Released 2014/10/13)
* Now generating docs!

### New in 2.3.0 - (Released 2014/09/26)
* Add QueryBuilder
* Add many more wrappers to Rx from F#, thanks to Jared Hester!

**Breaking changes**
* New namespace is `FSharp.Control.Reactive`
* `Observable` module is no longer auto-opened with the namespace
* `Observable.zip` now takes parameters in the "correct" order of `obs1` then `obs2`
* `Observable.subscribe` has moved to `Observable.subscribeWithCallbacks`
* `Observable.subscribe` now takes the `onNext` callback only

**TODO**
* Add tests
* Add docs

### New in 2.2.131002 - (Released 2013/10/02)
* Restructure modules and namespaces to comply with F# coding guidelines

### New in 2.1.131002 - (Released 2013/10/02)
* Release of F# wrappers for Rx

### New in 2.0.121121-rc - (Released 2012/11/21)
* Release candidate for F# wrappers for Rx based on Rx 2.0-rtm

### New in 2.0.6-rc - (Released 2012/07/26)
* Initial NuGet release of F# wrappers for Rx 2.0-rc
