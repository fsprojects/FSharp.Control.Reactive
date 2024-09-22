### New in 6.0.1 - (Released 2024/9/22)

* Updated build system
  
### New in 6.0.0 - (Released 2024/4/3)

* Release with relaxed support for System.Reactive 6.0 and above
  
### New in 5.0.5 - (Released 2022/5/4)

* No upward bounds on FSharp.Core

### New in 5.0.4 - (Released 2022/5/4)

* Adds support for IL Trimming [#166](https://github.com/fsprojects/FSharp.Control.Reactive/pull/166)
  
### New in 5.0.3 - (Released 2022/5/3)

* Adds support for .NET 6.0 [#165](https://github.com/fsprojects/FSharp.Control.Reactive/pull/165)
  
### New in 5.0.2 - (Released 2021/3/6)

* Downgrade FSharp.Core requirement 4.7.2 upwards

### New in 5.0.1 - (Released 2021/3/5)

* Update TFMs to align with System.Reactive [#161](https://github.com/fsprojects/FSharp.Control.Reactive/pull/161)
* Now targets .NET Framework 4.7.2, .NET Standard 2.0, .NET Core 3.1 and .NET 5.0

### New in 5.0.0 - (Released 2021/2/24)

* Update [System.Reactive to 5.0](https://github.com/dotnet/reactive/releases/tag/rxnet-v5.0.0)
* Support for .Net Framework 4.6 has been removed, as it is no longer support by Rx 5.0
* Exceptions rethrown inside Rx now are guaranteed to preserve the original stack trace by using ExceptionDispatchInfo consistently across the board. 

### New in 4.5.0 - (Released 2020/12/12)
* Add Observable.replayBufferOn [#157](https://github.com/fsprojects/FSharp.Control.Reactive/pull/157)
* Add additional package targets for netstandard2.1 and net5.0

### New in 4.4.2 - (Released 2020/07/03)
* Replace custom IObservable implementations with Rx native [#152](https://github.com/fsprojects/FSharp.Control.Reactive/pull/152)

### New in 4.4.1 - (Released 2020/06/27)
* Update to System.Reactive 4.4.1 [#151](https://github.com/fsprojects/FSharp.Control.Reactive/pull/151)

### New in 4.4.0 - (Released 2020/06/10)
* Add `Disposable.disposeWith` [#145](https://github.com/fsprojects/FSharp.Control.Reactive/pull/146)
* Add `Observable.ignoreElements` [#148](https://github.com/fsprojects/FSharp.Control.Reactive/pull/148)

### New in 4.3.3 - (Released 2020/06/05)
* Fix try ... with in observe CE [#122](https://github.com/fsprojects/FSharp.Control.Reactive/pull/144)
* Refine fix for Observable.choose [#143](https://github.com/fsprojects/FSharp.Control.Reactive/pull/143)

### New in 4.3.2 - (Released 2020/06/05)
* Fix disposable composition [#139 and #140](https://github.com/fsprojects/FSharp.Control.Reactive/pull/141)

### New in 4.3.1 - (Released 2020/06/05)
* Fix issue with Observable.choose [#131](https://github.com/fsprojects/FSharp.Control.Reactive/pull/128)

### New in 4.3.0 - (Released 2020/05/21)
* Fix issue [#115](https://github.com/fsprojects/FSharp.Control.Reactive/pull/128)
* Fix issue [#126](https://github.com/fsprojects/FSharp.Control.Reactive/pull/127)
* Fix issue [#129](https://github.com/fsprojects/FSharp.Control.Reactive/pull/130)
* Add tests using Xunit [#118](https://github.com/fsprojects/FSharp.Control.Reactive/pull/118)

### New in 4.2.0 - (Released 2019/03/06)
* Extensions to F# API

### New in 4.1.0 - (Released 2018/06/06)
* Update to System.Reactive 4.0.

### New in 4.0.0 - (Released 2018/02/21)
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
