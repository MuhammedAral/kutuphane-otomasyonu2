# Kütüphane API - Docker Image
# Multi-stage build ile optimize edilmiş

# ============ BUILD AŞAMASI ============
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY api/KutuphaneApi.csproj ./api/
WORKDIR /src/api
RUN dotnet restore

# API kaynak kodunu kopyala
COPY api/ ./

# Release modunda derle
RUN dotnet publish -c Release -o /app/publish

# ============ ÇALIŞTIRMA AŞAMASI ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Türkçe karakter desteği için
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Port ayarı - Railway dinamik port atar
ENV ASPNETCORE_URLS=http://+:${PORT:-5026}
EXPOSE 5026

# Build aşamasından derlenmiş dosyaları kopyala
COPY --from=build /app/publish .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
