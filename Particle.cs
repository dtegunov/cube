using Warp.Tools;

namespace Cube
{
    public class Particle
    {
        public int3 Position;
        public float3 Angle;
        public bool IsSelected;

        public Particle(int3 position, float3 angle)
        {
            Position = position;
            Angle = angle;
        }
    }
}