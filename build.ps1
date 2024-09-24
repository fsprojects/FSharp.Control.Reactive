dotnet tool restore
dotnet build -c Release
dotnet test --no-build -c Release tests
dotnet pack --no-restore -c Release -o $psscriptroot/bin