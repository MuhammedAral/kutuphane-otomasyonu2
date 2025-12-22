# Kütüphane API - Docker Image
# Railway.app için optimize edilmiş

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# API projesini kopyala ve derle
COPY api/ ./api/
WORKDIR /src/api
RUN dotnet restore
RUN dotnet publish -c Release -o /publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Türkçe karakter desteği
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Derlenmiş uygulamayı kopyala
COPY --from=build /publish .

# Website klasörünü kopyala
COPY website/ ./website/

# Mobile klasörünü kopyala (sadece gerekli dosyalar)
COPY mobile/index.html mobile/manifest.json mobile/sw.js ./mobile/
COPY mobile/css/ ./mobile/css/
COPY mobile/js/ ./mobile/js/

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
