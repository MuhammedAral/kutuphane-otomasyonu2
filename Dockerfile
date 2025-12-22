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

# Website ve Mobile klasörlerini build aşamasında kopyala
WORKDIR /src
COPY website/ /app/website/
COPY mobile/index.html mobile/manifest.json mobile/sw.js /app/mobile/
COPY mobile/css/ /app/mobile/css/
COPY mobile/js/ /app/mobile/js/
COPY mobile/images/ /app/mobile/images/

# ============ ÇALIŞTIRMA AŞAMASI ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Türkçe karakter desteği için
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Build aşamasından tüm dosyaları kopyala
COPY --from=build /app/publish .
COPY --from=build /app/website ./website
COPY --from=build /app/mobile ./mobile

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
