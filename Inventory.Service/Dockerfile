FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["Inventory.Service/Inventory.Service.csproj", "Inventory.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Inventory.Service/Inventory.Service.csproj"
COPY . .
WORKDIR "/src/Inventory.Service"
RUN dotnet build "Inventory.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Inventory.Service.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Service.dll"]
