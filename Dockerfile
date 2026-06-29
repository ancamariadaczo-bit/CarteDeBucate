FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CarteDeBucate.slnx ./
COPY CarteDeBucate.Web/CarteDeBucate.Web.csproj CarteDeBucate.Web/
COPY CarteDeBucate.App/CarteDeBucate.App.csproj CarteDeBucate.App/
COPY CarteDeBucate.Tests/CarteDeBucate.Tests.csproj CarteDeBucate.Tests/

RUN dotnet restore CarteDeBucate.Web/CarteDeBucate.Web.csproj

COPY . .

RUN dotnet publish CarteDeBucate.Web/CarteDeBucate.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CarteDeBucate.Web.dll"]