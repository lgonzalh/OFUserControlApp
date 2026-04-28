FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OFUserControlApp.csproj", "./"]
RUN dotnet restore "OFUserControlApp.csproj"
COPY . .
RUN dotnet publish "OFUserControlApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto 8080 que es el estándar para Cloud Run
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OFUserControlApp.dll"]
