using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AutoGala.Plugin.models;

public class LineData
{
    public PointData? Start { get; set; }
    public PointData? End { get; set; }
}