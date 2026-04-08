FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["Shipping.Service/Shipping.Service.csproj", "Shipping.Service/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Shipping.Service/Shipping.Service.csproj"
COPY . .
WORKDIR "/src/Shipping.Service"
RUN dotnet publish "Shipping.Service.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shipping.Service.dll"]
