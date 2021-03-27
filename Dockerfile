FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY ShareXUploadAPI/*.csproj ./ShareXUploadAPI/
RUN dotnet restore

# copy everything else and build app
COPY ShareXUploadAPI/. ./ShareXUploadAPI/
WORKDIR /app/ShareXUploadAPI
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/ShareXUploadAPI/out ./
ENTRYPOINT ["dotnet", "ShareXUploadAPI.dll"]
