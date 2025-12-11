using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using ZXing.Windows.Compatibility;
using ZXing.Common;

namespace KutuphaneOtomasyon.Pages
{
    public partial class BarcodeScannerDialog : Window
    {
        private FilterInfoCollection? _videoDevices;
        private VideoCaptureDevice? _videoSource;
        private bool _isDecoding = false;
        public string ScannedBarcode { get; private set; } = string.Empty;

        // İki farklı okuyucu stratejisi
        private BarcodeReader? _readerHybrid;
        private BarcodeReader? _readerGlobal;

        public BarcodeScannerDialog()
        {
            InitializeComponent();
            SetupReaders();
            LoadDevices(); // Yüklenirken cihazları bulsun
        }

        private void SetupReaders()
        {
            // Ortak ayarlar
            var options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new System.Collections.Generic.List<BarcodeFormat>
                {
                    BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, 
                    BarcodeFormat.CODE_128, BarcodeFormat.QR_CODE, 
                    BarcodeFormat.CODE_39
                }
            };

            // 1. Standart Okuyucu (HybridBinarizer - Kontrastlı fotolar için)
            _readerHybrid = new BarcodeReader
            {
                AutoRotate = true,
                Options = options
            };

            // 2. Yedek Okuyucu (GlobalHistogramBinarizer - Bulanık fotolar için)
            _readerGlobal = new BarcodeReader(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                Options = options
            };
        }

        private void LoadDevices()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_videoDevices.Count > 0)
                {
                    foreach (FilterInfo device in _videoDevices)
                        cmbKameralar.Items.Add(device.Name);
                    
                    cmbKameralar.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Kamera bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception) { }
        }

        private void StartCamera()
        {
            StopCamera();

            if (cmbKameralar.SelectedIndex >= 0 && _videoDevices != null)
            {
                _videoSource = new VideoCaptureDevice(_videoDevices[cmbKameralar.SelectedIndex].MonikerString);
                
                // En yüksek çözünürlüğü seç
                if (_videoSource.VideoCapabilities.Length > 0)
                {
                    VideoCapabilities bestResolution = _videoSource.VideoCapabilities[0];
                    foreach (var cap in _videoSource.VideoCapabilities)
                    {
                        if (cap.FrameSize.Height * cap.FrameSize.Width > bestResolution.FrameSize.Height * bestResolution.FrameSize.Width)
                        {
                            bestResolution = cap;
                        }
                    }
                    _videoSource.VideoResolution = bestResolution;
                }

                _videoSource.NewFrame += Video_NewFrame;
                _videoSource.Start();
            }
        }

        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Kameradan gelen orijinal kare
                using (var originalBitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    // 1. UI Güncelleme (Hızlıca ekrana bas)
                    var bitmapSource = CreateBitmapSourceFromBitmap(originalBitmap);
                    Dispatcher.Invoke(() =>
                    {
                        imgKamera.Source = bitmapSource;
                    }, System.Windows.Threading.DispatcherPriority.Loaded);

                    // 2. Barkod Okuma (Arka Plan)
                    if (!_isDecoding)
                    {
                        _isDecoding = true;

                        // KRİTİK DÜZELTME: Thread başlamadan önce kopyasını al!
                        // Yoksa originalBitmap dispose edilir ve thread hata alır.
                        // Formatı da burada 24bppRgb yapıyoruz.
                        var decodeBitmap = originalBitmap.Clone(
                            new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height), 
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                        ThreadPool.QueueUserWorkItem((state) =>
                        {
                            try
                            {
                                using (decodeBitmap) // Thread işi bitince silsin
                                {
                                    Result? result = null;

                                    // 1. Deneme: Hybrid
                                    result = _readerHybrid.Decode(decodeBitmap);

                                    // 2. Deneme: Global (Eğer ilki bulamazsa)
                                    if (result == null)
                                    {
                                        result = _readerGlobal.Decode(decodeBitmap);
                                    }

                                    if (result != null)
                                    {
                                        ScannedBarcode = result.Text;
                                        System.Media.SystemSounds.Beep.Play();
                                        
                                        Dispatcher.Invoke(() =>
                                        {
                                            StopCamera();
                                            DialogResult = true;
                                            Close();
                                        });
                                    }
                                }
                            }
                            catch { }
                            finally
                            {
                                _isDecoding = false;
                            }
                        });
                    }
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private System.Windows.Media.ImageSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= Video_NewFrame;
                _videoSource = null;
            }
        }

        private void cmbKameralar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartCamera();
        }

        private void Iptal_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            DialogResult = false;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
        }
    }
}
