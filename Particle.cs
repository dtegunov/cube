using Warp;
using Warp.Tools;

namespace Cube
{
    public class Particle : DataBase
    {
        private int3 _Position = new int3(0, 0, 0);
        public int3 Position
        {
            get { return _Position; }
            set { if (value != _Position) { _Position = value; OnPropertyChanged(); } }
        }

        private float3 _Angle = new float3(0, 0, 0);
        public float3 Angle
        {
            get { return _Angle; }
            set { if (value != _Angle) { _Angle = value; OnPropertyChanged(); } }
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { if (value != _IsSelected) { _IsSelected = value; OnPropertyChanged(); } }
        }

        public Particle(int3 position, float3 angle)
        {
            Position = position;
            Angle = angle;
        }
    }
}