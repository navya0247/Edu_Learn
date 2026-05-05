FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.EnrollmentService/EduLearn.EnrollmentService.csproj", "EduLearn.EnrollmentService/"]
RUN dotnet restore "EduLearn.EnrollmentService/EduLearn.EnrollmentService.csproj"
COPY . .
WORKDIR "/src/EduLearn.EnrollmentService"
RUN dotnet build "EduLearn.EnrollmentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.EnrollmentService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.EnrollmentService.dll"]