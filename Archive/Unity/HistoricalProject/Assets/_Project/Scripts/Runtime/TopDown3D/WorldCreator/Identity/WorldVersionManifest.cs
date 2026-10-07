using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum WorldVersionDomain : byte
    {
        Topology = 1,
        Coordinate = 2,
        Landform = 3,
        Material = 4,
        Decoration = 5,
        Resource = 6,
        Site = 7
    }

    public readonly struct WorldVersionManifest : IEquatable<WorldVersionManifest>
    {
        public const int CurrentSchemaVersion = 1;

        public WorldVersionManifest(
            int topology,
            int coordinate,
            int landform,
            int material,
            int decoration,
            int resource,
            int site)
        {
            Topology = ValidateVersion(topology, nameof(topology));
            Coordinate = ValidateVersion(coordinate, nameof(coordinate));
            Landform = ValidateVersion(landform, nameof(landform));
            Material = ValidateVersion(material, nameof(material));
            Decoration = ValidateVersion(decoration, nameof(decoration));
            Resource = ValidateVersion(resource, nameof(resource));
            Site = ValidateVersion(site, nameof(site));
        }

        public int Topology { get; }
        public int Coordinate { get; }
        public int Landform { get; }
        public int Material { get; }
        public int Decoration { get; }
        public int Resource { get; }
        public int Site { get; }

        public int GetVersion(WorldVersionDomain domain)
        {
            return domain switch
            {
                WorldVersionDomain.Topology => Topology,
                WorldVersionDomain.Coordinate => Coordinate,
                WorldVersionDomain.Landform => Landform,
                WorldVersionDomain.Material => Material,
                WorldVersionDomain.Decoration => Decoration,
                WorldVersionDomain.Resource => Resource,
                WorldVersionDomain.Site => Site,
                _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown world version domain.")
            };
        }

        public bool Equals(WorldVersionManifest other)
        {
            return Topology == other.Topology
                && Coordinate == other.Coordinate
                && Landform == other.Landform
                && Material == other.Material
                && Decoration == other.Decoration
                && Resource == other.Resource
                && Site == other.Site;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldVersionManifest other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("world-version-manifest-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        public override string ToString()
        {
            return $"topology={Topology};coordinate={Coordinate};landform={Landform};material={Material};"
                + $"decoration={Decoration};resource={Resource};site={Site}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(Topology);
            hash.Append(Coordinate);
            hash.Append(Landform);
            hash.Append(Material);
            hash.Append(Decoration);
            hash.Append(Resource);
            hash.Append(Site);
        }

        private static int ValidateVersion(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "World versions cannot be negative.");
            }

            return value;
        }
    }
}
