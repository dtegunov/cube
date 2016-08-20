using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp;
using Warp.Headers;
using Warp.Tools;
using Image = Warp.Image;

namespace Cube
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public static Options Options = new Options();
        bool FreezeUpdates = false;

        ObservableCollection<Particle> Particles = new ObservableCollection<Particle>();
        List<Particle>[] SliceXYParticles = null;
        List<Particle>[] SliceZYParticles = null;
        List<Particle>[] SliceXZParticles = null;
        Particle ActiveParticle = null;

        SolidColorBrush ParticleBrush;

        float PixelSize = 1f;
        Image Tomogram;
        float[] TomogramContinuous;

        int2 DragStart = new int2(0, 0);
        bool IsDragging = false;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = Options;
            Options.PropertyChanged += Options_PropertyChanged;

            PreviewMouseUp += MainWindow_PreviewMouseUp;

            Particles.CollectionChanged += Particles_CollectionChanged;

            if (File.Exists("Previous.settings"))
                Options.Load("Previous.settings");

            ParticleBrush = new SolidColorBrush(Colors.Lime);
            ParticleBrush.Freeze();
        }

        private void MainWindow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
                IsDragging = false;
        }

        private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PathTomogram")
            {
                ButtonTomogramPathText.Text = Options.PathTomogram != "" ? Options.PathTomogram : "Select tomogram...";
            }
            else if (e.PropertyName == "PathWarpMetadata")
            {
                ButtonWarpMetaPathText.Text = Options.PathWarpMetadata != "" ? Options.PathWarpMetadata : "Select Warp metadata...";
            }
            else if (e.PropertyName == "PathReference")
            {
                ButtonReferencePathText.Text = Options.PathReference != "" ? Options.PathReference : "Select reference...";
            }

            if (FreezeUpdates)
                return;

            if (e.PropertyName == "PathTomogram")
            {
                LoadTomogram();
            }
            else if (e.PropertyName == "InputLowpass")
            {
                UpdateTomogramPlanes();
                UpdateActiveParticle();
            }
            else if (e.PropertyName == "InputAverageSlices")
            {
                UpdateTomogramPlanes();
                UpdateActiveParticle();
                UpdateCrosshairs();
            }
            else if (e.PropertyName == "DisplayIntensityMin" || e.PropertyName == "DisplayIntensityMax")
            {
                UpdateTomogramPlanes();
                UpdateActiveParticle();
            }
            else if (e.PropertyName == "ZoomLevel")
            {
                UpdateTomogramPlanes();
                UpdateActiveParticle();
                UpdateView();
                UpdateBoxes();
            }
            else if (e.PropertyName == "PlaneX")
            {
                UpdateTomogramZY();
                UpdateView();
                UpdateBoxesZY();
            }
            else if (e.PropertyName == "PlaneY")
            {
                UpdateTomogramXZ();
                UpdateView();
                UpdateBoxesXZ();
            }
            else if (e.PropertyName == "PlaneZ")
            {
                UpdateTomogramXY();
                UpdateView();
                UpdateBoxesXY();
            }
            else if (e.PropertyName == "ViewX" || e.PropertyName == "ViewY" || e.PropertyName == "ViewZ")
            {
                UpdateView();
            }
            else if (e.PropertyName == "MouseX" || e.PropertyName == "MouseY" || e.PropertyName == "MouseZ")
            {
                UpdateCrosshairs();
            }
            else if (e.PropertyName == "ParticlePlaneX")
            {
                UpdateParticleZY();
                UpdateView();
            }
            else if (e.PropertyName == "ParticlePlaneY")
            {
                UpdateParticleXZ();
                UpdateView();
            }
            else if (e.PropertyName == "ParticlePlaneZ")
            {
                UpdateParticleXY();
                UpdateView();
            }
            else if (e.PropertyName == "BoxSize")
            {
                UpdateBoxes();
            }
        }

        private void CanvasXY_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            /*if (e.HeightChanged)
            {
                MainGridColumn1.Width = new GridLength(MainGridRow0.ActualHeight);
            }
            else if (e.WidthChanged)
            {
                MainGridRow0.Height = new GridLength(MainGridColumn1.ActualWidth);
            }*/

            UpdateView();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                Options.Save("Previous.settings");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Particles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SliceXYParticles == null || SliceZYParticles == null || SliceXZParticles == null)
                return;

            Options.NParticles = Particles.Count;

            if (e.OldItems != null)
                foreach (var oldItem in e.OldItems)
                {
                    Particle OldParticle = (Particle)oldItem;

                    if (SliceXYParticles[OldParticle.Position.Z].Contains(OldParticle))
                        SliceXYParticles[OldParticle.Position.Z].Remove(OldParticle);
                    if (SliceZYParticles[OldParticle.Position.X].Contains(OldParticle))
                        SliceZYParticles[OldParticle.Position.X].Remove(OldParticle);
                    if (SliceXZParticles[OldParticle.Position.Y].Contains(OldParticle))
                        SliceXZParticles[OldParticle.Position.Y].Remove(OldParticle);
                }

            if (e.NewItems != null)
                foreach (var newItem in e.NewItems)
                {
                    Particle NewParticle = (Particle)newItem;

                    SliceXYParticles[NewParticle.Position.Z].Add(NewParticle);
                    SliceZYParticles[NewParticle.Position.X].Add(NewParticle);
                    SliceXZParticles[NewParticle.Position.Y].Add(NewParticle);
                }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var slice in SliceXYParticles)
                    slice.Clear();
                foreach (var slice in SliceZYParticles)
                    slice.Clear();
                foreach (var slice in SliceXZParticles)
                    slice.Clear();
            }

            if (!FreezeUpdates)
                UpdateBoxes();
        }

        private void LoadTomogram()
        {
            if (!File.Exists(Options.PathTomogram))
                return;

            HeaderMRC Header = (HeaderMRC)MapHeader.ReadFromFile(Options.PathTomogram);
            PixelSize = Header.Pixelsize.X;

            float[][] TomoData = IOHelper.ReadMapFloat(Options.PathTomogram, new int2(1, 1), 0, typeof (float));
            Tomogram = new Image(TomoData, Header.Dimensions);

            TomogramContinuous = Tomogram.GetHostContinuousCopy();

            FreezeUpdates = true;

            Options.PlaneX = Tomogram.Dims.X / 2;
            Options.PlaneY = Tomogram.Dims.Y / 2;
            Options.PlaneZ = Tomogram.Dims.Z / 2;

            Options.ViewX = Tomogram.Dims.X / 2;
            Options.ViewY = Tomogram.Dims.Y / 2;
            Options.ViewZ = Tomogram.Dims.Z / 2;

            Particles.Clear();
            ActiveParticle = null;

            SliceXYParticles = new List<Particle>[Tomogram.Dims.Z];
            for (int i = 0; i < SliceXYParticles.Length; i++)
                SliceXYParticles[i] = new List<Particle>();
            SliceZYParticles = new List<Particle>[Tomogram.Dims.X];
            for (int i = 0; i < SliceZYParticles.Length; i++)
                SliceZYParticles[i] = new List<Particle>();
            SliceXZParticles = new List<Particle>[Tomogram.Dims.Y];
            for (int i = 0; i < SliceXZParticles.Length; i++)
                SliceXZParticles[i] = new List<Particle>();

            UpdateTomogramPlanes();
            UpdateActiveParticle();
            UpdateView();

            FreezeUpdates = false;
        }

        private void UpdateTomogramPlanes()
        {
            if (Tomogram == null)
                return;

            Task TaskXY = new Task(UpdateTomogramXY);
            Task TaskZY = new Task(UpdateTomogramZY);
            Task TaskXZ = new Task(UpdateTomogramXZ);

            TaskXY.Start();
            TaskZY.Start();
            TaskXZ.Start();

            TaskXY.Wait();
            TaskZY.Wait();
            TaskXZ.Wait();
        }

        private void UpdateTomogramXY()
        {
            if (Tomogram == null)
                return;

            int PlaneElements = Tomogram.Dims.X * Tomogram.Dims.Y;
            float[] Data = new float[PlaneElements];

            unsafe
            {
                fixed (float* DataPtr = Data)
                fixed (float* FilteredPtr = TomogramContinuous)
                {
                    int FirstSlice = Math.Max(0, Options.PlaneZ - Options.InputAverageSlices / 2);
                    int LastSlice = Math.Min(Tomogram.Dims.Z, FirstSlice + Options.InputAverageSlices);
                    for (int s = FirstSlice; s < LastSlice; s++)
                    {
                        float* DataP = DataPtr;
                        float* FilteredP = FilteredPtr + s * PlaneElements; // Offset to the right Z
                        for (int i = 0; i < PlaneElements; i++)
                            *DataP++ += *FilteredP++;
                    }
                }
            }

            ImageSource Result = GetImage(Data, new int2(Tomogram.Dims.X, Tomogram.Dims.Y));
            Result.Freeze();
            Dispatcher.InvokeAsync(() => ImageXY.Source = Result);
        }

        private void UpdateTomogramZY()
        {
            if (Tomogram == null)
                return;

            int PlaneElements = Tomogram.Dims.Z * Tomogram.Dims.Y;
            int XYElements = Tomogram.Dims.X * Tomogram.Dims.Y;
            float[] Data = new float[PlaneElements];

            unsafe
            {
                fixed (float* DataPtr = Data)
                fixed (float* FilteredPtr = TomogramContinuous)
                {
                    int FirstSlice = Math.Max(0, Options.PlaneX - Options.InputAverageSlices / 2);
                    int LastSlice = Math.Min(Tomogram.Dims.X, FirstSlice + Options.InputAverageSlices);
                    for (int s = FirstSlice; s < LastSlice; s++)
                    {
                        float* DataP = DataPtr;
                        for (int y = 0; y < Tomogram.Dims.Y; y++)
                        {
                            float* FilteredP = FilteredPtr + y * Tomogram.Dims.X + s;
                            for (int z = 0; z < Tomogram.Dims.Z; z++)
                                *DataP++ += FilteredP[z * XYElements];
                        }
                    }
                }
            }

            ImageSource Result = GetImage(Data, new int2(Tomogram.Dims.Z, Tomogram.Dims.Y));
            Result.Freeze();
            Dispatcher.InvokeAsync(() => ImageZY.Source = Result);
        }

        private void UpdateTomogramXZ()
        {
            if (Tomogram == null)
                return;

            int PlaneElements = Tomogram.Dims.X * Tomogram.Dims.Z;
            int XYElements = Tomogram.Dims.X * Tomogram.Dims.Y;
            float[] Data = new float[PlaneElements];

            unsafe
            {
                fixed (float* DataPtr = Data)
                fixed (float* FilteredPtr = TomogramContinuous)
                {
                    int FirstSlice = Math.Max(0, Options.PlaneY - Options.InputAverageSlices / 2);
                    int LastSlice = Math.Min(Tomogram.Dims.Y, FirstSlice + Options.InputAverageSlices);
                    for (int s = FirstSlice; s < LastSlice; s++)
                    {
                        float* DataP = DataPtr;
                        for (int z = 0; z < Tomogram.Dims.Z; z++)
                        {
                            float* FilteredP = FilteredPtr + z * XYElements + s * Tomogram.Dims.X;
                            for (int x = 0; x < Tomogram.Dims.X; x++)
                                *DataP++ += FilteredP[x];
                        }
                    }
                }
            }

            ImageSource Result = GetImage(Data, new int2(Tomogram.Dims.X, Tomogram.Dims.Z));
            Result.Freeze();
            Dispatcher.InvokeAsync(() => ImageXZ.Source = Result);
        }

        private void UpdateActiveParticle()
        {
            if (ActiveParticle == null)
                return;
        }

        private void UpdateParticlePlanes()
        {
            UpdateParticleXY();
            UpdateParticleZY();
            UpdateParticleXZ();
        }

        private void UpdateParticleXY()
        {
            if (ActiveParticle == null)
                return;
        }

        private void UpdateParticleZY()
        {
            if (ActiveParticle == null)
                return;
        }

        private void UpdateParticleXZ()
        {
            if (ActiveParticle == null)
                return;
        }

        private void UpdateView()
        {
            // XY
            {
                float2 CanvasCenter = new float2((float)CanvasXY.ActualWidth / 2, (float)CanvasXY.ActualHeight / 2);
                float2 ImageCenter = new float2((float)(Options.ViewX * Options.ZoomLevel), (float)(Options.ViewY * Options.ZoomLevel));
                float2 Delta = CanvasCenter - ImageCenter;

                Canvas.SetLeft(ImageXY, Math.Round(Delta.X));
                Canvas.SetBottom(ImageXY, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasOverlayXY, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasOverlayXY, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasParticlesXY, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasParticlesXY, Math.Round(Delta.Y));
            }
            // ZY
            {
                float2 CanvasCenter = new float2((float)CanvasZY.ActualWidth / 2, (float)CanvasZY.ActualHeight / 2);
                float2 ImageCenter = new float2((float)(Options.ViewZ * Options.ZoomLevel), (float)(Options.ViewY * Options.ZoomLevel));
                float2 Delta = CanvasCenter - ImageCenter;

                Canvas.SetLeft(ImageZY, Math.Round(Delta.X));
                Canvas.SetBottom(ImageZY, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasOverlayZY, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasOverlayZY, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasParticlesZY, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasParticlesZY, Math.Round(Delta.Y));
            }
            // XZ
            {
                float2 CanvasCenter = new float2((float)CanvasXZ.ActualWidth / 2, (float)CanvasXZ.ActualHeight / 2);
                float2 ImageCenter = new float2((float)(Options.ViewX * Options.ZoomLevel), (float)(Options.ViewZ * Options.ZoomLevel));
                float2 Delta = CanvasCenter - ImageCenter;

                Canvas.SetLeft(ImageXZ, Math.Round(Delta.X));
                Canvas.SetBottom(ImageXZ, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasOverlayXZ, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasOverlayXZ, Math.Round(Delta.Y));

                Canvas.SetLeft(CanvasParticlesXZ, Math.Round(Delta.X));
                Canvas.SetBottom(CanvasParticlesXZ, Math.Round(Delta.Y));
            }
        }

        private void UpdateCrosshairs()
        {
            // XY
            {
                CrosshairXYX.X1 = 0;
                CrosshairXYX.X2 = 0;
                CrosshairXYX.Y1 = 0;
                CrosshairXYX.Y2 = ImageXY.ActualHeight;

                //CrosshairXYX.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairXYX.Opacity = CrosshairXYX.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairXYX, 0);
                Canvas.SetLeft(CrosshairXYX, (int)((Options.MouseX) * Options.ZoomLevel));

                CrosshairXYY.X1 = 0;
                CrosshairXYY.X2 = ImageXY.ActualWidth;
                CrosshairXYY.Y1 = 0;
                CrosshairXYY.Y2 = 0;

                //CrosshairXYY.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairXYY.Opacity = CrosshairXYX.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairXYY, (int)((Options.MouseY) * Options.ZoomLevel));
                Canvas.SetLeft(CrosshairXYY, 0);
            }
            // ZY
            {
                CrosshairZYZ.X1 = 0;
                CrosshairZYZ.X2 = 0;
                CrosshairZYZ.Y1 = 0;
                CrosshairZYZ.Y2 = ImageZY.ActualHeight;

                //CrosshairZYZ.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairZYZ.Opacity = CrosshairXYX.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairZYZ, 0);
                Canvas.SetLeft(CrosshairZYZ, (int)((Options.MouseZ) * Options.ZoomLevel));

                CrosshairZYY.X1 = 0;
                CrosshairZYY.X2 = ImageZY.ActualWidth;
                CrosshairZYY.Y1 = 0;
                CrosshairZYY.Y2 = 0;

                //CrosshairZYY.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairZYY.Opacity = CrosshairXYX.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairZYY, (int)((Options.MouseY) * Options.ZoomLevel));
                Canvas.SetLeft(CrosshairZYY, 0);
            }
            // XZ
            {
                CrosshairXZX.X1 = 0;
                CrosshairXZX.X2 = 0;
                CrosshairXZX.Y1 = 0;
                CrosshairXZX.Y2 = ImageXZ.ActualHeight;

                //CrosshairXZX.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairXZX.Opacity = CrosshairXZX.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairXZX, 0);
                Canvas.SetLeft(CrosshairXZX, (int)((Options.MouseX) * Options.ZoomLevel));

                CrosshairXZZ.X1 = 0;
                CrosshairXZZ.X2 = ImageXZ.ActualWidth;
                CrosshairXZZ.Y1 = 0;
                CrosshairXZZ.Y2 = 0;

                //CrosshairXZZ.StrokeThickness = Math.Max(1, Options.InputAverageSlices * (double)Options.ZoomLevel);
                CrosshairXZZ.Opacity = CrosshairXZZ.StrokeThickness == 1 ? 0.8 : 0.2;
                Canvas.SetBottom(CrosshairXZZ, (int)((Options.MouseZ) * Options.ZoomLevel));
                Canvas.SetLeft(CrosshairXZZ, 0);
            }
        }

        private void UpdateBoxes()
        {
            UpdateBoxesXY();
            UpdateBoxesZY();
            UpdateBoxesXZ();
        }

        private void UpdateBoxesXY()
        {
            if (SliceXYParticles != null)
            {
                List<Particle> VisibleParticles = new List<Particle>();

                int FirstSlice = Options.PlaneZ - Options.BoxSize / 2;
                int LastSlice = FirstSlice + Options.BoxSize;

                for (int s = Math.Max(0, FirstSlice); s < Math.Min(LastSlice, Tomogram.Dims.Z); s++)
                    VisibleParticles.AddRange(SliceXYParticles[s]);

                CanvasParticlesXY.Children.Clear();

                double BoxRadius = Options.BoxSize / 2;

                foreach (var part in VisibleParticles)
                {
                    double Dist = (Options.PlaneZ - part.Position.Z) / BoxRadius;
                    double Angle = Math.Asin(Dist);
                    double R = Math.Cos(Angle) * BoxRadius;

                    Ellipse Circle = new Ellipse()
                    {
                        Width = R * 2 * (double)Options.ZoomLevel,
                        Height = R * 2 * (double)Options.ZoomLevel,
                        Stroke = ParticleBrush,
                        StrokeThickness = 1,
                        Opacity = 1.0 - Math.Abs(Dist * 0.0),
                        IsHitTestVisible = false
                    };

                    CanvasParticlesXY.Children.Add(Circle);
                    Canvas.SetLeft(Circle, (part.Position.X - R) * (double)Options.ZoomLevel);
                    Canvas.SetBottom(Circle, (part.Position.Y - R) * (double)Options.ZoomLevel);
                }
            }
        }

        private void UpdateBoxesZY()
        {
            if (SliceZYParticles != null)
            {
                List<Particle> VisibleParticles = new List<Particle>();

                int FirstSlice = Options.PlaneX - Options.BoxSize / 2;
                int LastSlice = FirstSlice + Options.BoxSize;

                for (int s = Math.Max(0, FirstSlice); s < Math.Min(LastSlice, Tomogram.Dims.X); s++)
                    VisibleParticles.AddRange(SliceZYParticles[s]);

                CanvasParticlesZY.Children.Clear();

                double BoxRadius = Options.BoxSize / 2;

                foreach (var part in VisibleParticles)
                {
                    double Dist = (Options.PlaneX - part.Position.X) / BoxRadius;
                    double Angle = Math.Asin(Dist);
                    double R = Math.Cos(Angle) * BoxRadius;

                    Ellipse Circle = new Ellipse()
                    {
                        Width = R * 2 * (double)Options.ZoomLevel,
                        Height = R * 2 * (double)Options.ZoomLevel,
                        Stroke = ParticleBrush,
                        StrokeThickness = 1,
                        Opacity = 1.0 - Math.Abs(Dist * 0.0),
                        IsHitTestVisible = false
                    };

                    CanvasParticlesZY.Children.Add(Circle);
                    Canvas.SetLeft(Circle, (part.Position.Z - R) * (double)Options.ZoomLevel);
                    Canvas.SetBottom(Circle, (part.Position.Y - R) * (double)Options.ZoomLevel);
                }
            }
        }

        private void UpdateBoxesXZ()
        {
            if (SliceXZParticles != null)
            {
                List<Particle> VisibleParticles = new List<Particle>();

                int FirstSlice = Options.PlaneY - Options.BoxSize / 2;
                int LastSlice = FirstSlice + Options.BoxSize;

                for (int s = Math.Max(0, FirstSlice); s < Math.Min(LastSlice, Tomogram.Dims.Y); s++)
                    VisibleParticles.AddRange(SliceXZParticles[s]);

                CanvasParticlesXZ.Children.Clear();

                double BoxRadius = Options.BoxSize / 2;

                foreach (var part in VisibleParticles)
                {
                    double Dist = (Options.PlaneY - part.Position.Y) / BoxRadius;
                    double Angle = Math.Asin(Dist);
                    double R = Math.Cos(Angle) * BoxRadius;

                    Ellipse Circle = new Ellipse()
                    {
                        Width = R * 2 * (double)Options.ZoomLevel,
                        Height = R * 2 * (double)Options.ZoomLevel,
                        Stroke = ParticleBrush,
                        StrokeThickness = 1,
                        Opacity = 1.0 - Math.Abs(Dist * 0.0),
                        IsHitTestVisible = false
                    };

                    CanvasParticlesXZ.Children.Add(Circle);
                    Canvas.SetLeft(Circle, (part.Position.X - R) * (double)Options.ZoomLevel);
                    Canvas.SetBottom(Circle, (part.Position.Z - R) * (double)Options.ZoomLevel);
                }
            }
        }

        private ImageSource GetImage(float[] data, int2 dims)
        {
            float NyquistFraction = 2f * PixelSize / (float)Options.InputLowpass;

            Image Filtered = new Image(data, new int3(dims.X, dims.Y, 1));
            if (NyquistFraction < 1f)
                Filtered.Bandpass(0, NyquistFraction, false);

            /*if (Options.ZoomLevel != 1)
            {
                int2 DimsScaled = new int2((int)(dims.X * Options.ZoomLevel), (int)(dims.Y * Options.ZoomLevel));
                Image Scaled = Filtered.AsScaledMassive(DimsScaled);

                data = Scaled.GetHostContinuousCopy();
                dims = DimsScaled;

                Scaled.Dispose();
            }*/
            else
            {
                data = Filtered.GetHostContinuousCopy();
            }

            Filtered.Dispose();

            float2 Stats = MathHelper.MeanAndStd(data);
            float MinVal = Stats.X + Stats.Y * (float)Options.DisplayIntensityMin;
            float MaxVal = Stats.X + Stats.Y * (float)Options.DisplayIntensityMax;
            float Range = MaxVal - MinVal;

            byte[] DataBytes = new byte[data.Length];

            unsafe
            {
                fixed (float* DataPtr = data)
                fixed (byte* BytePtr = DataBytes)
                {
                    for (int y = 0; y < dims.Y; y++)
                    {
                        int yy = dims.Y - y - 1;
                        float* DataP = DataPtr + y * dims.X;
                        byte* ByteP = BytePtr + yy * dims.X;

                        for (int x = 0; x < dims.X; x++)
                            ByteP[x] = (byte)(Math.Max(0, Math.Min(1, (DataP[x] - MinVal) / Range)) * 255);
                    }
                }
            }

            ImageSource Result = BitmapSource.Create(dims.X, dims.Y, 96 / (double)Options.ZoomLevel, 96 / (double)Options.ZoomLevel, PixelFormats.Indexed8, BitmapPalettes.Gray256, DataBytes, dims.X);
            return Result;
        }

        private void ButtonTomogramPath_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "MRC Volume|*.mrc",
                Multiselect = false
            };
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                try
                {
                    Options.PathTomogram = Dialog.FileName;

                    HeaderMRC Header = (HeaderMRC)MapHeader.ReadFromFile(Dialog.FileName);
                    long Elements = (long)Header.Dimensions.X * Header.Dimensions.Y * Header.Dimensions.Z;
                    if (Elements > int.MaxValue)
                        throw new Exception("Volumes with more than 2^31-1 elements are not supported, please scale it down.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't read volume: " + ex.Message);
                }
            }
        }

        private void ImageZY_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Tomogram == null)
                return;

            int Delta = Math.Sign(e.Delta);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                decimal Factor = Delta > 0 ? 2M : 0.5M;
                Options.ZoomLevel = Math.Min(4M, Math.Max(0.125M, Options.ZoomLevel * Factor));
            }
            else
            {
                Options.PlaneX = Math.Max(0, Math.Min(Tomogram.Dims.X - 1, Options.PlaneX + Delta));
                Options.MouseX = Options.PlaneX;
            }
        }

        private void ImageXY_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Tomogram == null)
                return;

            int Delta = Math.Sign(e.Delta);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                decimal Factor = Delta > 0 ? 2M : 0.5M;
                Options.ZoomLevel = Math.Min(4M, Math.Max(0.125M, Options.ZoomLevel * Factor));
            }
            else
            {
                Options.PlaneZ = Math.Max(0, Math.Min(Tomogram.Dims.Z - 1, Options.PlaneZ + Delta));
                Options.MouseZ = Options.PlaneZ;
            }
        }

        private void ImageXZ_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Tomogram == null)
                return;

            int Delta = Math.Sign(e.Delta);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                decimal Factor = Delta > 0 ? 2M : 0.5M;
                Options.ZoomLevel = Math.Min(4M, Math.Max(0.125M, Options.ZoomLevel * Factor));
            }
            else
            {
                Options.PlaneY = Math.Max(0, Math.Min(Tomogram.Dims.Y - 1, Options.PlaneY + Delta));
                Options.MouseY = Options.PlaneY;
            }
        }

        private void Image_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                IsDragging = true;
                DragStart = new int2((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y);
            }
            else
            {
                Options.PlaneX = Options.MouseX;
                Options.PlaneY = Options.MouseY;
                Options.PlaneZ = Options.MouseZ;
            }
        }

        private void Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            IsDragging = false;
        }

        private void ImageZY_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDragging)
            {
                int2 NewPosition = new int2((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y);
                int2 Delta = NewPosition - DragStart;
                Delta.Y *= -1;

                Options.ViewZ -= Delta.X / Options.ZoomLevel;
                Options.ViewY -= Delta.Y / Options.ZoomLevel;

                DragStart = NewPosition;
            }

            Options.MouseZ = (int)Math.Round(e.GetPosition(ImageZY).X / (double)Options.ZoomLevel);
            Options.MouseY = (int)Math.Round((ImageZY.ActualHeight - 1 - e.GetPosition(ImageZY).Y) / (double)Options.ZoomLevel);
            Options.MouseX = Options.PlaneX;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Options.PlaneZ = Options.MouseZ;
                Options.PlaneY = Options.MouseY;
            }
        }

        private void ImageXY_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDragging)
            {
                int2 NewPosition = new int2((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y);
                int2 Delta = NewPosition - DragStart;
                Delta.Y *= -1;

                Options.ViewX -= Delta.X / Options.ZoomLevel;
                Options.ViewY -= Delta.Y / Options.ZoomLevel;

                DragStart = NewPosition;
            }

            Options.MouseX = (int)Math.Round(e.GetPosition(ImageXY).X / (double)Options.ZoomLevel);
            Options.MouseY = (int)Math.Round((ImageXY.ActualHeight - 1 - e.GetPosition(ImageXY).Y) / (double)Options.ZoomLevel);
            Options.MouseZ = Options.PlaneZ;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Options.PlaneX = Options.MouseX;
                Options.PlaneY = Options.MouseY;
            }
        }

        private void ImageXZ_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDragging)
            {
                int2 NewPosition = new int2((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y);
                int2 Delta = NewPosition - DragStart;
                Delta.Y *= -1;

                Options.ViewX -= Delta.X / Options.ZoomLevel;
                Options.ViewZ -= Delta.Y / Options.ZoomLevel;

                DragStart = NewPosition;
            }

            Options.MouseX = (int)Math.Round(e.GetPosition(ImageXZ).X / (double)Options.ZoomLevel);
            Options.MouseZ = (int)Math.Round((ImageXZ.ActualHeight - 1 - e.GetPosition(ImageXZ).Y) / (double)Options.ZoomLevel);
            Options.MouseY = Options.PlaneY;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Options.PlaneX = Options.MouseX;
                Options.PlaneZ = Options.MouseZ;
            }
        }

        private void ImageZY_OnMouseEnter(object sender, MouseEventArgs e)
        {
            //Options.ViewX = Options.PlaneX;
        }

        private void ImageXY_OnMouseEnter(object sender, MouseEventArgs e)
        {
            //Options.ViewZ = Options.PlaneZ;
        }

        private void ImageXZ_OnMouseEnter(object sender, MouseEventArgs e)
        {
            //Options.ViewY = Options.PlaneY;
        }

        private void Image_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                Particles.Add(new Particle(new int3(Options.MouseX, Options.MouseY, Options.MouseZ), new float3(0, 0, 0)));
            }
        }

        private void ButtonPointsImport_OnClick(object sender, RoutedEventArgs e)
        {
            if (Tomogram == null)
            {
                MessageBox.Show("This will not work without a tomogram loaded.");
                return;
            }

            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "Text File|*.txt|STAR File|*.star";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                FreezeUpdates = true;
                Particles.Clear();

                try
                {
                    FileInfo Info = new FileInfo(Dialog.FileName);
                    if (Info.Extension.ToLower().Replace(".", "") == "txt")
                    {
                        using (TextReader Reader = new StreamReader(File.OpenRead(Dialog.FileName)))
                        {
                            string Line;
                            while ((Line = Reader.ReadLine()) != null)
                            {
                                string[] Parts = Line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                                if (Parts.Length < 3)
                                    continue;

                                if (Parts.Length != 3 && Parts.Length != 6)
                                    throw new Exception("Tab-delimited text file must have either 3 (XYZ) or 6 (XYZ Rot Tilt Psi columns.");

                                float X = 0, Y = 0, Z = 0, Rot = 0, Tilt = 0, Psi = 0;
                                if (Parts.Length >= 3)
                                {
                                    X = float.Parse(Parts[0]);
                                    Y = float.Parse(Parts[1]);
                                    Z = float.Parse(Parts[2]);
                                }
                                if (Parts.Length == 6)
                                {
                                    Rot = float.Parse(Parts[3]);
                                    Tilt = float.Parse(Parts[4]);
                                    Psi = float.Parse(Parts[5]);
                                }

                                X = Math.Max(0, Math.Min(X, Tomogram.Dims.X - 1));
                                Y = Math.Max(0, Math.Min(Y, Tomogram.Dims.Y - 1));
                                Z = Math.Max(0, Math.Min(Z, Tomogram.Dims.Z - 1));

                                Particle NewParticle = new Particle(new int3((int)X, (int)Y, (int)Z), new float3(Rot, Tilt, Psi));
                                Particles.Add(NewParticle);
                            }
                        }
                    }
                    else if (Info.Extension.ToLower().Replace(".", "") == "star")
                    {
                        Star Table = new Star(Dialog.FileName);
                        string[] ColumnX = Table.GetColumn("rlnCoordinateX");
                        string[] ColumnY = Table.GetColumn("rlnCoordinateY");
                        string[] ColumnZ = Table.GetColumn("rlnCoordinateZ");
                        string[] ColumnRot = Table.GetColumn("rlnAngleRot");
                        string[] ColumnTilt = Table.GetColumn("rlnAngleTilt");
                        string[] ColumnPsi = Table.GetColumn("rlnAnglePsi");

                        for (int i = 0; i < Table.RowCount; i++)
                        {
                            float X = 0, Y = 0, Z = 0, Rot = 0, Tilt = 0, Psi = 0;

                            if (ColumnX != null)
                                X = float.Parse(ColumnX[i]);
                            if (ColumnY != null)
                                Y = float.Parse(ColumnY[i]);
                            if (ColumnZ != null)
                                Z = float.Parse(ColumnZ[i]);

                            if (ColumnRot != null)
                                Rot = float.Parse(ColumnRot[i]);
                            if (ColumnTilt != null)
                                Tilt = float.Parse(ColumnTilt[i]);
                            if (ColumnPsi != null)
                                Psi = float.Parse(ColumnPsi[i]);

                            X = Math.Max(0, Math.Min(X, Tomogram.Dims.X - 1));
                            Y = Math.Max(0, Math.Min(Y, Tomogram.Dims.Y - 1));
                            Z = Math.Max(0, Math.Min(Z, Tomogram.Dims.Z - 1));

                            Particle NewParticle = new Particle(new int3((int)X, (int)Y, (int)Z), new float3(Rot, Tilt, Psi));
                            Particles.Add(NewParticle);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't parse file: " + ex.Message);
                }

                FreezeUpdates = false;
                UpdateBoxes();
            }
        }

        private void ButtonPointsExport_OnClick(object sender, RoutedEventArgs e)
        {
            if (Tomogram == null)
            {
                MessageBox.Show("This will not work without a tomogram loaded.");
                return;
            }

            System.Windows.Forms.SaveFileDialog Dialog = new System.Windows.Forms.SaveFileDialog();
            Dialog.Filter = "STAR File|*.star";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                FileInfo Info = new FileInfo(Options.PathTomogram);
                string MicName = Info.Name;

                Star Table = new Star(new[]
                {
                    "rlnCoordinateX",
                    "rlnCoordinateY",
                    "rlnCoordinateZ",
                    "rlnOriginX",
                    "rlnOriginY",
                    "rlnOriginZ",
                    "rlnAngleRot",
                    "rlnAngleTilt",
                    "rlnAnglePsi",
                    "rlnMicrographName"
                });

                foreach (var particle in Particles)
                    Table.AddRow(new List<string>()
                    {
                        particle.Position.X.ToString(),
                        particle.Position.Y.ToString(),
                        particle.Position.Z.ToString(),
                        "0",
                        "0",
                        "0",
                        particle.Angle.X.ToString(CultureInfo.InvariantCulture),
                        particle.Angle.Y.ToString(CultureInfo.InvariantCulture),
                        particle.Angle.Z.ToString(CultureInfo.InvariantCulture),
                        MicName
                    });

                Table.Save(Dialog.FileName);
            }
        }

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                if (Particles.Count > 0)
                    Particles.Remove(Particles.Last());
            }
        }
    }
}
