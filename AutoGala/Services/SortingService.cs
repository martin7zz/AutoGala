using Autodesk.AutoCAD.Geometry;
using AutoGala.Contracts;
using AutoGala.Plugin.models;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using Plugin.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AutoGala.Services
{
    public class SortingService : ISortingService
    {
        private readonly ISectionService _sectionService;

        public SortingService(
            ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        public List<SectionItem> SortLines(List<LineData> lines)
        {
            List<SectionItem> sectionItems = new List<SectionItem>();

            if (lines == null || lines.Count < 3)
                throw new ArgumentException("At least 3 lines are required.");

            var ordered = new List<(PointData Start, PointData End)>();
            var used = new bool[lines.Count];

            // Start with the first line
            var currentStart = lines[0].Start;
            var currentEnd = lines[0].End;

            used[0] = true;
            ordered.Add((currentStart, currentEnd));

            // Follow the connected loop
            for (int i = 1; i < lines.Count; i++)
            {
                bool found = false;

                for (int j = 0; j < lines.Count; j++)
                {
                    if (used[j])
                        continue;

                    var line = lines[j];

                    // Same direction
                    if (AreEqual(line.Start, currentEnd))
                    {
                        currentStart = line.Start;
                        currentEnd = line.End;

                        ordered.Add((currentStart, currentEnd));
                        used[j] = true;

                        found = true;
                        break;
                    }

                    // Opposite direction -> reverse it
                    if (AreEqual(line.End, currentEnd))
                    {
                        currentStart = line.End;
                        currentEnd = line.Start;

                        ordered.Add((currentStart, currentEnd));
                        used[j] = true;

                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new InvalidOperationException(
                        "Could not find the next connected line.");
            }

            // Calculate signed area
            double area = 0;

            foreach (var line in ordered)
            {
                area += line.Start.X * line.End.Y;
                area -= line.End.X * line.Start.Y;
            }

            // If clockwise, reverse the traversal
            if (area < 0)
            {
                ordered.Reverse();

                for (int i = 0; i < ordered.Count; i++)
                {
                    var line = ordered[i];

                    ordered[i] = (
                        line.End,
                        line.Start
                    );
                }
            }

            foreach (var item in ordered)
            {
                sectionItems.Add(_sectionService.CreateSection(item.Start.X, item.Start.Y));
            }

            return sectionItems;
        }

        private bool AreEqual(PointData a, PointData b)
        {
            const double tolerance = 1e-8;

            return Math.Abs(a.X - b.X) < tolerance &&
                   Math.Abs(a.Y - b.Y) < tolerance;
        }
    }
}
