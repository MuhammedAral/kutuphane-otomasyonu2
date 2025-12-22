# Kütüphane API - Docker Image
# Railway.app için optimize edilmiş

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Tüm projeyi kopyala
WORKDIR /src
COPY . .

# API'yi derle
WORKDIR /src/api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Website ve Mobile'ı kopyala
RUN cp -r /src/website /app/publish/website
RUN mkdir -p /app/publish/mobile && \
    cp /src/mobile/index.html /app/publish/mobile/ && \
    cp /src/mobile/manifest.json /app/publish/mobile/ && \
    cp /src/mobile/sw.js /app/publish/mobile/ && \
    cp -r /src/mobile/css /app/publish/mobile/ && \
    cp -r /src/mobile/js /app/publish/mobile/

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Türkçe karakter desteği
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Tüm dosyaları kopyala
COPY --from=build /app/publish .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
