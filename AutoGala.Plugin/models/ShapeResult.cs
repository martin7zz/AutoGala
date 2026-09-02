namespace AutoGala.Plugin.models
{
    public class ShapeResult
    {
        public string Name { get; set; } = "";
        public List<PointData> Vertices { get; set; } = new();
        public List<CircleData> Rebars { get; set; } = new();
    }
}
