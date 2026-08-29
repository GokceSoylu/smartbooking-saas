# 1. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Sadece csproj dosyalarını kopyalayıp restore cache'inden faydalanıyoruz
COPY src/SmartBooking.Domain/SmartBooking.Domain.csproj src/SmartBooking.Domain/
COPY src/SmartBooking.Application/SmartBooking.Application.csproj src/SmartBooking.Application/
COPY src/SmartBooking.Infrastructure/SmartBooking.Infrastructure.csproj src/SmartBooking.Infrastructure/
COPY src/SmartBooking.Api/SmartBooking.Api.csproj src/SmartBooking.Api/

RUN dotnet restore src/SmartBooking.Api/SmartBooking.Api.csproj

# Tüm kaynak kodları kopyala ve derle
COPY src/ src/
WORKDIR /src/src/SmartBooking.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 2. Aşama: Çalışma Zamanı (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartBooking.Api.dll"]