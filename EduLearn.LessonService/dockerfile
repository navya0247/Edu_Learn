FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.LessonService/EduLearn.LessonService.csproj", "EduLearn.LessonService/"]
RUN dotnet restore "EduLearn.LessonService/EduLearn.LessonService.csproj"
COPY . .
WORKDIR "/src/EduLearn.LessonService"
RUN dotnet build "EduLearn.LessonService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.LessonService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.LessonService.dll"]