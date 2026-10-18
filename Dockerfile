FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ShopAPI.Core/*.csproj ShopAPI.Core/
COPY ShopAPI.Infrastructure/*.csproj ShopAPI.Infrastructure/
COPY ShopAPI.Web/*.csproj ShopAPI.Web/
RUN dotnet restore ShopAPI.Web/ShopAPI.Web.csproj
COPY ShopAPI.Core/ ShopAPI.Core/
COPY ShopAPI.Infrastructure/ ShopAPI.Infrastructure/
COPY ShopAPI.Web/ ShopAPI.Web/
RUN dotnet publish ShopAPI.Web/ShopAPI.Web.csproj -c Release --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShopAPI.Web.dll"]
