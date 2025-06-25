using System.Diagnostics.CodeAnalysis;

namespace AnodyneArchipelago
{
    public readonly record struct Version(int Major, int Minor, int Patch)
    {
        public Version(string? version)
            : this(0, 0, 0)
        {
            if (string.IsNullOrEmpty(version))
            {
                Major = Minor = Patch = -1;
                return;
            }

            string[] parts = version.Split('.');

            if (parts.Length < 3)
            {
                Major = Minor = Patch = -1;
                return;
            }

            Major = int.Parse(parts[0]);
            Minor = int.Parse(parts[1]);
            Patch = int.Parse(parts[2]);
        }

        public bool IsNewer(int major, int minor, int patch)
        {
            return IsNewer(new(major, minor, patch));
        }

        public bool IsNewer(Version version)
        {
            if (this == version)
            {
                return true;
            }

            int majorDif = Major.CompareTo(version.Major);
            int minorDif = Minor.CompareTo(version.Minor);
            int patchDif = Patch.CompareTo(version.Patch);

            if (majorDif != 0)
            {
                return majorDif > 0;
            }

            if (minorDif != 0)
            {
                return minorDif > 0;
            }

            return patchDif >= 0;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}