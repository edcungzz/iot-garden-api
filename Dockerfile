FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["IoTGardenApi/IoTGardenApi.csproj", "IoTGardenApi/"]
RUN dotnet restore "IoTGardenApi/IoTGardenApi.csproj"
COPY . .
WORKDIR "/src/IoTGardenApi"
RUN dotnet build "IoTGardenApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IoTGardenApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT:-8000}
EXPOSE ${PORT:-8000}
ENTRYPOINT ["dotnet", "IoTGardenApi.dll"]
