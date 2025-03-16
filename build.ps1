dotnet tool restore
dotnet build -c Release
dotnet test --no-build -c Release tests
dotnet pack --no-restore -c Release --include-symbols -o $psscriptroot/bin