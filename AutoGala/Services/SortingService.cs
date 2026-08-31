using Autodesk.AutoCAD.Geometry;
using AutoGala.Contracts;
using AutoGala.Plugin.models;
using DocumentFormat.OpenXml.Wordprocessing;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using Plugin.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace AutoGala.Services
{
    public class SortingService : ISortingService
    {
        private const double Tolerance = 1e-8;

        private readonly ISectionService _sectionService;

        public SortingService(
            ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        public List<SectionItem> SortLines(List<LineData> lines)
        {
            var sections = new List<SectionItem>();

            if (lines == null || lines.Count == 0)
            {
                return sections;
            }

            var adj = BuildAdjacency(lines);
            var visited = new bool[lines.Count];

            for (int i = 0; i < lines.Count; i++)
            {
                if (visited[i])
                {
                    continue;
                }

                var componentIndices = new List<int>();
                DfsComponent(adj, visited, i, componentIndices);

                var componentLines = componentIndices.Select(idx => lines[idx]).ToList();
                var orderedPoints = BuildOrderedPoints(componentLines);

                // not a closable/usable section — skip or flag as needed
                if (orderedPoints.Count < 3)
                {
                    Debug.WriteLine("The section is open.");
                    continue;
                }

                if (SignedArea(orderedPoints) < 0)
                {
                    orderedPoints.Reverse();
                }

                int id = 1;
                foreach (var point in orderedPoints)
                {
                    var section = _sectionService.CreateSection(point.X, point.Y);

                    section.Id = id++; 
                    sections.Add(section);
                }
            }

            return sections;
        }

        // Groups lines into connected components. Order of the returned
        // indices is NOT the geometric order — just membership.
        private void DfsComponent(List<List<int>> adj, bool[] visited, int s, List<int> res)
        {
            visited[s] = true;
            res.Add(s);

            foreach (var i in adj[s])
            {
                if (!visited[i])
                {
                    DfsComponent(adj, visited, i, res);
                }
            }
        }

        private List<List<int>> BuildAdjacency(List<LineData> lines)
        {
            var adj = new List<List<int>>();

            for (int i = 0; i < lines.Count; i++)
            {
                adj.Add(new List<int>());
            }

            for (int i = 0; i < adj.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (AreEqual(lines[i].Start, lines[j].Start) ||
                        AreEqual(lines[i].Start, lines[j].End) ||
                        AreEqual(lines[i].End, lines[j].Start) ||
                        AreEqual(lines[i].End, lines[j].End))
                    {
                        adj[i].Add(j);
                        adj[j].Add(i);
                    }
                }
            }

            return adj;
        }

        // Walks a component's lines by matching shared endpoints, producing
        // an ordered vertex list. Assumes each line touches at most 2 others
        // (a simple open chain or closed loop) — no branching.
        private List<PointData> BuildOrderedPoints(List<LineData> component)
        {
            var remaining = new List<LineData>(component);
            var points = new List<PointData>();

            var current = remaining[0];
            remaining.RemoveAt(0);

            points.Add(current.Start);
            var currentPoint = current.End;
            points.Add(currentPoint);

            while (remaining.Count > 0)
            {
                var nextIndex = remaining.FindIndex(l =>
                    AreEqual(l.Start, currentPoint) || AreEqual(l.End, currentPoint));
                
                // gap in the chain — shouldn't happen for a clean section
                if (nextIndex < 0)
                {
                    break;
                }

                var next = remaining[nextIndex];
                remaining.RemoveAt(nextIndex);

                currentPoint = AreEqual(next.Start, currentPoint) ? next.End : next.Start;
                points.Add(currentPoint);
            }

            // closed loop — drop the duplicated closing point
            if (points.Count > 1 && AreEqual(points[0], points[^1]))
            {
                points.RemoveAt(points.Count - 1);
            }

            return points;
        }

        private double SignedArea(List<PointData> points)
        {
            double sum = 0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % n];
                sum += (p1.X * p2.Y) - (p2.X * p1.Y);
            }

            return sum / 2.0;
        }

        private bool AreEqual(PointData? a, PointData? b)
        {
            return Math.Abs(a.X - b.X) < Tolerance &&
                   Math.Abs(a.Y - b.Y) < Tolerance;
        }
    }
}
