@echo off

rem DupeNukem - WebView attachable full-duplex asynchronous interoperable
rem messaging library between .NET and JavaScript.
rem
rem Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
rem
rem Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo.
echo "==========================================================="
echo "Build DupeNukem"
echo.

rem git clean -xfd

dotnet build -p:Configuration=Release DupeNukem\DupeNukem.csproj
dotnet pack -p:Configuration=Release -o artifacts DupeNukem.Core\DupeNukem.Core.csproj
dotnet pack -p:Configuration=Release -o artifacts DupeNukem\DupeNukem.csproj
