FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SecoItemHarvester.sln ./
COPY SecoItemHarvester.Web/SecoItemHarvester.Web.csproj SecoItemHarvester.Web/
COPY SecoItemHarvester.Tests/SecoItemHarvester.Tests.csproj SecoItemHarvester.Tests/
RUN dotnet restore SecoItemHarvester.Web/SecoItemHarvester.Web.csproj
COPY . .
RUN dotnet publish SecoItemHarvester.Web/SecoItemHarvester.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
VOLUME ["/app/data"]
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/seco-harvester.db"
ENTRYPOINT ["dotnet", "SecoItemHarvester.Web.dll"]
