FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobTracker.Api/JobTracker.Api.csproj JobTracker.Api/
RUN dotnet restore JobTracker.Api/JobTracker.Api.csproj
COPY JobTracker.Api/ JobTracker.Api/
RUN dotnet publish JobTracker.Api/JobTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ConnectionStrings__Default="Data Source=/tmp/jobtracker.db"
ENTRYPOINT ["dotnet", "JobTracker.Api.dll"]