FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.ProgressService/EduLearn.ProgressService.csproj", "EduLearn.ProgressService/"]
RUN dotnet restore "EduLearn.ProgressService/EduLearn.ProgressService.csproj"
COPY . .
WORKDIR "/src/EduLearn.ProgressService"
RUN dotnet build "EduLearn.ProgressService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.ProgressService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.ProgressService.dll"]