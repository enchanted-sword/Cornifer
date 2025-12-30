using System;

namespace Cornifer.UI.Structures
{
    public struct Angle
    {
        public float Radians;
        public float Degrees
        {
            get => Radians / MathF.PI * 180;
            set => Radians = value / 180 * MathF.PI;
        }

        public Angle(float radians)
        {
            Radians = radians;
        }

        public Angle FromRad(float radians) => new(radians);
        public Angle FromDeg(float degrees) => new() { Degrees = degrees };

		public static float PI2 = MathF.PI * 2f;
		public static float NormalizeRadians(float radians) {
			if (radians >= 0 && radians < PI2) return radians;
			else if (radians > PI2) return radians - PI2;
			else return radians + PI2;
		}
		public float NormalizedRadians() {
			return NormalizeRadians(Radians);
		}

		public static bool IsAcute(float radians) {
			return (radians < 0.5f * MathF.PI || radians > 1.5f * MathF.PI);
		}

        public override bool Equals(object? obj)
        {
            return obj is Angle angle &&
                   Radians == angle.Radians;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Radians);
        }
        public override string ToString()
        {
            return $"{Degrees:0.##}° | {Radians / MathF.PI:0.##}π";
        }

        public static Angle operator +(Angle a, Angle b) => new(a.Radians + b.Radians);
        public static Angle operator -(Angle a, Angle b) => new(a.Radians - b.Radians);

        public static Angle operator *(Angle a, float b) => new(a.Radians * b);
        public static Angle operator /(Angle a, float b) => new(a.Radians / b);

        public static bool operator ==(Angle a, Angle b) => a.Radians == b.Radians;
        public static bool operator !=(Angle a, Angle b) => a.Radians != b.Radians;
    }
}
