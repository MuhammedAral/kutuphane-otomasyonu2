# Kütüphane API + Website + Mobile - Docker Image
# Railway.app için optimize edilmiş

# ============ BUILD AŞAMASI ============
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY api/*.csproj ./api/
WORKDIR /src/api
RUN dotnet restore

# API kaynak kodunu kopyala ve derle
COPY api/ ./
RUN dotnet publish -c Release -o /app/publish

# ============ ÇALIŞTIRMA AŞAMASI ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Türkçe karakter desteği için
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Build aşamasından derlenmiş dosyaları kopyala
COPY --from=build /app/publish .

# Website ve Mobile klasörlerini kopyala
COPY website ./website
COPY mobile ./mobile

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
