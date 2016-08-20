using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Warp;
using Warp.Tools;

namespace Cube
{
    public class Options : DataBase
    {
        private string _PathTomogram = "";
        public string PathTomogram
        {
            get { return _PathTomogram; }
            set { if (value != _PathTomogram) { _PathTomogram = value; OnPropertyChanged(); } }
        }

        private string _PathWarpMetadata = "";
        public string PathWarpMetadata
        {
            get { return _PathWarpMetadata; }
            set { if (value != _PathWarpMetadata) { _PathWarpMetadata = value; OnPropertyChanged(); } }
        }

        private decimal _InputLowpass = 20;
        public decimal InputLowpass
        {
            get { return _InputLowpass; }
            set { if (value != _InputLowpass) { _InputLowpass = value; OnPropertyChanged(); } }
        }

        private int _InputAverageSlices = 1;
        public int InputAverageSlices
        {
            get { return _InputAverageSlices; }
            set { if (value != _InputAverageSlices) { _InputAverageSlices = value; OnPropertyChanged(); } }
        }

        private decimal _DisplayIntensityMin = -2;
        public decimal DisplayIntensityMin
        {
            get { return _DisplayIntensityMin; }
            set { if (value != _DisplayIntensityMin) { _DisplayIntensityMin = value; OnPropertyChanged(); } }
        }

        private decimal _DisplayIntensityMax = 2;
        public decimal DisplayIntensityMax
        {
            get { return _DisplayIntensityMax; }
            set { if (value != _DisplayIntensityMax) { _DisplayIntensityMax = value; OnPropertyChanged(); } }
        }

        private decimal _ZoomLevel = 1;
        public decimal ZoomLevel
        {
            get { return _ZoomLevel; }
            set { if (value != _ZoomLevel) { _ZoomLevel = value; OnPropertyChanged(); } }
        }

        private int _NParticles = 0;
        public int NParticles
        {
            get { return _NParticles; }
            set { if (value != _NParticles) { _NParticles = value; OnPropertyChanged(); } }
        }

        private int _BoxSize = 16;
        public int BoxSize
        {
            get { return _BoxSize; }
            set { if (value != _BoxSize) { _BoxSize = value; OnPropertyChanged(); } }
        }

        private string _PathReference = "";
        public string PathReference
        {
            get { return _PathReference; }
            set { if (value != _PathReference) { _PathReference = value; OnPropertyChanged(); } }
        }

        private string _ReferenceSymmetry = "C1";
        public string ReferenceSymmetry
        {
            get { return _ReferenceSymmetry; }
            set { if (value != _ReferenceSymmetry) { _ReferenceSymmetry = value; OnPropertyChanged(); } }
        }

        private bool _DoSnap = false;
        public bool DoSnap
        {
            get { return _DoSnap; }
            set { if (value != _DoSnap) { _DoSnap = value; OnPropertyChanged(); } }
        }

        private int _PlaneX = 0;
        public int PlaneX
        {
            get { return _PlaneX; }
            set { if (value != _PlaneX) { _PlaneX = value; OnPropertyChanged(); } }
        }

        private int _PlaneY = 0;
        public int PlaneY
        {
            get { return _PlaneY; }
            set { if (value != _PlaneY) { _PlaneY = value; OnPropertyChanged(); } }
        }

        private int _PlaneZ = 0;
        public int PlaneZ
        {
            get { return _PlaneZ; }
            set { if (value != _PlaneZ) { _PlaneZ = value; OnPropertyChanged(); } }
        }

        private int _ParticlePlaneX = 0;
        public int ParticlePlaneX
        {
            get { return _ParticlePlaneX; }
            set { if (value != _ParticlePlaneX) { _ParticlePlaneX = value; OnPropertyChanged(); } }
        }

        private int _ParticlePlaneY = 0;
        public int ParticlePlaneY
        {
            get { return _ParticlePlaneY; }
            set { if (value != _ParticlePlaneY) { _ParticlePlaneY = value; OnPropertyChanged(); } }
        }

        private int _ParticlePlaneZ = 0;
        public int ParticlePlaneZ
        {
            get { return _ParticlePlaneZ; }
            set { if (value != _ParticlePlaneZ) { _ParticlePlaneZ = value; OnPropertyChanged(); } }
        }

        private decimal _ViewX = 0;
        public decimal ViewX
        {
            get { return _ViewX; }
            set { if (value != _ViewX) { _ViewX = value; OnPropertyChanged(); } }
        }

        private decimal _ViewY = 0;
        public decimal ViewY
        {
            get { return _ViewY; }
            set { if (value != _ViewY) { _ViewY = value; OnPropertyChanged(); } }
        }

        private decimal _ViewZ = 0;
        public decimal ViewZ
        {
            get { return _ViewZ; }
            set { if (value != _ViewZ) { _ViewZ = value; OnPropertyChanged(); } }
        }

        private int _MouseX = 0;
        public int MouseX
        {
            get { return _MouseX; }
            set { if (value != _MouseX) { _MouseX = value; OnPropertyChanged(); } }
        }

        private int _MouseY = 0;
        public int MouseY
        {
            get { return _MouseY; }
            set { if (value != _MouseY) { _MouseY = value; OnPropertyChanged(); } }
        }

        private int _MouseZ = 0;
        public int MouseZ
        {
            get { return _MouseZ; }
            set { if (value != _MouseZ) { _MouseZ = value; OnPropertyChanged(); } }
        }

        public void Save(string path)
        {
            XmlTextWriter Writer = new XmlTextWriter(File.Create(path), Encoding.Unicode);
            Writer.Formatting = Formatting.Indented;
            Writer.IndentChar = '\t';
            Writer.Indentation = 1;
            Writer.WriteStartDocument();
            Writer.WriteStartElement("Settings");

            XMLHelper.WriteParamNode(Writer, "InputLowpass", InputLowpass);
            XMLHelper.WriteParamNode(Writer, "InputAverageSlices", InputAverageSlices);
            XMLHelper.WriteParamNode(Writer, "DisplayIntensityMin", DisplayIntensityMin);
            XMLHelper.WriteParamNode(Writer, "DisplayIntensityMax", DisplayIntensityMax);
            XMLHelper.WriteParamNode(Writer, "BoxSize", BoxSize);

            Writer.WriteEndElement();
            Writer.WriteEndDocument();
            Writer.Flush();
            Writer.Close();
        }

        public void Load(string path)
        {
            using (Stream SettingsStream = File.OpenRead(path))
            {
                XPathDocument Doc = new XPathDocument(SettingsStream);
                XPathNavigator Reader = Doc.CreateNavigator();
                Reader.MoveToRoot();

                InputLowpass = XMLHelper.LoadParamNode(Reader, "InputLowpass", InputLowpass);
                InputAverageSlices = XMLHelper.LoadParamNode(Reader, "InputAverageSlices", InputAverageSlices);
                DisplayIntensityMin = XMLHelper.LoadParamNode(Reader, "DisplayIntensityMin", DisplayIntensityMin);
                DisplayIntensityMax = XMLHelper.LoadParamNode(Reader, "DisplayIntensityMax", DisplayIntensityMax);
                BoxSize = XMLHelper.LoadParamNode(Reader, "BoxSize", BoxSize);
            }
        }
    }
}