# Kütüphane API - Docker Image
# Railway.app için optimize edilmiş

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Tüm projeyi kopyala
WORKDIR /src
COPY . .

# API'yi derle
WORKDIR /src/api
RUN dotnet restore
RUN dotnet publish -c Release -o /output

# Website ve Mobile'ı /output altına kopyala
RUN cp -r /src/website /output/website
RUN mkdir -p /output/mobile && \
    cp /src/mobile/index.html /output/mobile/ && \
    cp /src/mobile/manifest.json /output/mobile/ && \
    cp /src/mobile/sw.js /output/mobile/ && \
    cp -r /src/mobile/css /output/mobile/ && \
    cp -r /src/mobile/js /output/mobile/

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Türkçe karakter desteği
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Tüm dosyaları /app'e kopyala VE oradan çalıştır
WORKDIR /app
COPY --from=build /output .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
