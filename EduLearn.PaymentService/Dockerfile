FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.PaymentService/EduLearn.PaymentService.csproj", "EduLearn.PaymentService/"]
RUN dotnet restore "EduLearn.PaymentService/EduLearn.PaymentService.csproj"
COPY . .
WORKDIR "/src/EduLearn.PaymentService"
RUN dotnet build "EduLearn.PaymentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.PaymentService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.PaymentService.dll"]