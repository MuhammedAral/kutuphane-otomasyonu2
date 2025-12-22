# Kütüphane API + Website + Mobile - Docker Image
# Railway.app için optimize edilmiş (Single Stage)

FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Türkçe karakter desteği için
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Tüm projeyi kopyala
COPY . .

# API'yi derle
WORKDIR /app/api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Çalışma dizinini ayarla
WORKDIR /app/out

# Website ve mobile klasörlerini kopyala
RUN cp -r /app/website ./website 2>/dev/null || true
RUN cp -r /app/mobile/index.html /app/mobile/manifest.json /app/mobile/sw.js ./mobile/ 2>/dev/null || mkdir -p ./mobile
RUN cp -r /app/mobile/css ./mobile/css 2>/dev/null || true
RUN cp -r /app/mobile/js ./mobile/js 2>/dev/null || true
RUN cp -r /app/mobile/images ./mobile/images 2>/dev/null || true

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
