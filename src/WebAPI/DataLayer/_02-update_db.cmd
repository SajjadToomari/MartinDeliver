dotnet tool update --global dotnet-ef --version 7.0.3
dotnet tool restore
dotnet build
dotnet ef database update
pause