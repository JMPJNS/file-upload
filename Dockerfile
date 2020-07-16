# Build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY *.sln .
COPY ShareXUploadAPI/*.csproj ShareXUploadAPI/

RUN dotnet restore

COPY ShareXUploadAPI ./
RUN dotnet publish -c Release -o out

# Run
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as run-env
WORKDIR /app

COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "ShareXUploadAPI.dll"]
