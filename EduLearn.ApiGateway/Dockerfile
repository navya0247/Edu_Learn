FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.ApiGateway/EduLearn.ApiGateway.csproj", "EduLearn.ApiGateway/"]
RUN dotnet restore "EduLearn.ApiGateway/EduLearn.ApiGateway.csproj"
COPY . .
WORKDIR "/src/EduLearn.ApiGateway"
RUN dotnet build "EduLearn.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.ApiGateway.dll"]