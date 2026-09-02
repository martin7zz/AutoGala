using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AutoGala.Ipc;
using AutoGala.Plugin.models;
using System.Diagnostics;
using System.Text.Json;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace AutoGala.Plugin
{
    public class AutoGalaPlugin : IExtensionApplication
    {
        private PipeServer? _pipeServer;

        public void Initialize()
        {
            int pid = Process.GetCurrentProcess().Id;
            _pipeServer = new PipeServer($"AutoGala_{pid}")
            {
                RequestHandler = HandleRequestAsync
            };
            _pipeServer.Start();
        }

        public async void Terminate()
        {
            if (_pipeServer != null)
            {
                try
                {
                    await _pipeServer.StopAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PipeServer shutdown failed: {ex}");
                }
                finally
                {
                    _pipeServer = null;
                }
            }
        }

        private async Task<string> HandleRequestAsync(string json)
        {
            PluginResponse pluginResponse;

            try
            {
                var request = JsonSerializer.Deserialize<PluginRequest>(json) ?? throw new InvalidOperationException("Bad request");

                // IMPORTANT: AutoCAD API calls must run on AutoCAD's main thread.
                // The pipe read happens on a background thread, so marshal back:

                object? result = await RunOnAutoCADThreadAsync(request);

                pluginResponse = new PluginResponse
                {
                    Success = true,
                    ResultJson = JsonSerializer.Serialize(result)
                };
            }
            catch (Exception ex)
            {
                pluginResponse = new PluginResponse { Success = false, Error = ex.Message };
            }

            return JsonSerializer.Serialize(pluginResponse);
        }

        // Hops from this background pipe thread onto AutoCAD's document
        // execution context, runs the real work there, and hands the
        // result back via a TaskCompletionSource.
        private Task<object?> RunOnAutoCADThreadAsync(PluginRequest request)
        {
            var tcs = new TaskCompletionSource<object?>();

            Application.DocumentManager.ExecuteInApplicationContext(_ =>
            {
                try
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;

                    Status(doc);

                    object? result = Dispatch(request, doc);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        // document needs to get locks every time
        private object? Dispatch(PluginRequest request, Document doc)
        {
            using (doc.LockDocument())
            {
                Database acDatabase = doc.Database;

                switch (request.Action)
                {
                    case "GetShape":
                        try
                        {
                            var requestPayload = JsonSerializer.Deserialize<JsonElement>(request.PayloadJson);

                            string name = requestPayload.GetProperty("name").GetString();

                            return BuildShape(doc, acDatabase, name);
                        }
                        catch (Exception ex)
                        {
                            doc.Editor.WriteMessage($"\n** {ex.Message}\n");
                            throw;
                        }
                    default:
                        throw new System.Exception($"Unknown action: {request.Action}");
                }
            }
        }

        private const double Fuzz = 1.0e-3;

        private object? BuildShape(Document doc, Database acDatabase, string currName)
        {
            var lineSegs = new List<(Point2d a, Point2d b)>();
            var circles = new List<(Point2d center, double area)>();

            using (Transaction acTrans = acDatabase.TransactionManager.StartTransaction())
            {
                PromptSelectionResult acSSPrompt = doc.Editor.GetSelection(
                    new PromptSelectionOptions(),
                    new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Operator, "<or"),
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "CIRCLE"),
                        new TypedValue((int)DxfCode.Operator, "or>")
                    })
                );
                doc.Editor.WriteMessage($"\nDEBUG: selection status = {acSSPrompt.Status}\n");


                if (acSSPrompt.Status != PromptStatus.OK)
                {
                    throw new InvalidOperationException("Nothing selected.");
                }

                foreach (SelectedObject acSSObj in acSSPrompt.Value)
                {
                    if (acSSObj == null) continue;
                    Entity? ent = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;

                    if (ent is Line line)
                    {
                        var a = new Point2d(line.StartPoint.X, line.StartPoint.Y);
                        var b = new Point2d(line.EndPoint.X, line.EndPoint.Y);
                        // no zero-length lines
                        if (a.GetDistanceTo(b) > Fuzz)
                        {
                            lineSegs.Add((a, b));
                        }
                    }
                    else if (ent is Circle circle)
                    {
                        if (circle.Radius <= 0.0)
                        {
                            throw new InvalidOperationException(
                                $"Circle at ({circle.Center.X:F4},{circle.Center.Y:F4}) has a zero/negative radius.");
                        }
                        circles.Add((new Point2d(circle.Center.X, circle.Center.Y),
                            Math.PI * circle.Radius * circle.Radius));
                    }
                }
                acTrans.Commit();
            }

            if (lineSegs.Count < 3)
            {
                throw new InvalidOperationException("Fewer than 3 usable LINE entities selected.");
            }

            List<Point2d> ring = BuildRing(lineSegs);

            double signedArea = ShoelaceArea(ring);
            if (Math.Abs(signedArea) < 1e-9)
            {
                throw new InvalidOperationException("The outline encloses no area.");
            }
            if (signedArea < 0) ring.Reverse();

            if (SelfIntersects(ring))
            {
                throw new InvalidOperationException("The outline crosses itself.");
            }

            foreach (var c in circles)
            {
                if (!PointInPolygon(c.center, ring))
                {
                    throw new InvalidOperationException(
                        $"Circle at {c.center.X:F4}, {c.center.Y:F4}) is outside the outline.");
                }
            }

            double xmin = ring.Min(p => p.X), ymin = ring.Min(p => p.Y);

            string defaultName = string.IsNullOrWhiteSpace(currName)
                ? "Column1"
                : currName;

            var options = new PromptStringOptions($"\nShape name: ")
            {
                AllowSpaces = true,
                DefaultValue = defaultName
            };

            string name = doc.Editor.GetString(options).StringResult;

            return new ShapeResult
            {
                Name = name,
                Vertices = ring.Select(p => new PointData { X = p.X - xmin, Y = p.Y - ymin }).ToList(),
                Rebars = circles.Select(c => new CircleData
                {
                    PositionData = new PointData { X = c.center.X - xmin, Y = c.center.Y - ymin },
                    Area = c.area
                }).ToList()
            };
        }

        private List<Point2d> BuildRing(List<(Point2d a, Point2d b)> segs)
        {
            var nodes = new List<Point2d>();
            var edges = new List<(int a, int b)>();

            int FindOrAddNode(Point2d p)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].GetDistanceTo(p) < Fuzz)
                    {
                        return i;
                    }
                }
                nodes.Add(p);
                return nodes.Count - 1;
            }

            foreach (var (a, b) in segs)
            {
                edges.Add((FindOrAddNode(a), FindOrAddNode(b)));
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                int degree = edges.Count(e => e.a == i || e.b == i);

                if (degree < 2)
                {
                    throw new InvalidOperationException(
                    $"The outline is not closed — only {degree} line end meets at ({nodes[i].X:F4},{nodes[i].Y:F4}).");
                }
                if (degree > 2)
                {
                    throw new InvalidOperationException(
                    $"The outline branches — {degree} line ends meet at ({nodes[i].X:F4},{nodes[i].Y:F4}).");
                }
            }

            var used = new HashSet<int>();
            var ring = new List<Point2d>();
            int cur = 0;

            while (true)
            {
                int edgeIdx = -1, other = -1;
                for (int i = 0; i < edges.Count; i++)
                {
                    if (used.Contains(i))
                    {
                        continue;
                    }
                    if (edges[i].a == cur)
                    {
                        edgeIdx = i;
                        other = edges[i].b;
                        break;
                    }
                    if (edges[i].b == cur)
                    {
                        edgeIdx = i;
                        other = edges[i].a;
                        break;
                    }
                }
                if (edgeIdx == -1) break;
                used.Add(edgeIdx);
                ring.Add(nodes[cur]);
                cur = other;
            }

            if (ring.Count < edges.Count)
                throw new InvalidOperationException(
                    $"The selection contains more than one closed loop ({ring.Count} of {edges.Count} lines form the first one).");
            if (ring.Count < 3)
                throw new InvalidOperationException("The outline has fewer than 3 vertices.");

            return ring;
        }

        private double ShoelaceArea(List<Point2d> pts)
        {
            double sum = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var q = pts[(i + 1) % pts.Count];
                sum += p.X * q.Y - q.X * p.Y;
            }
            return 0.5 * sum;
        }

        private bool PointInPolygon(Point2d pt, List<Point2d> pts)
        {
            bool inside = false;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                var p = pts[i];
                var q = pts[(i + 1) % n];

                if ((p.Y > pt.Y && q.Y <= pt.Y) || (p.Y <= pt.Y && q.Y > pt.Y))
                {
                    double xint = p.X + (pt.Y - p.Y) * (q.X - p.X) / (q.Y - p.Y);
                    if (pt.X < xint) inside = !inside;
                }
            }

            return inside;
        }

        private bool SelfIntersects(List<Point2d> pts)
        {
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (j - i <= 1)
                    {
                        continue;
                    }
                    // adjacent wrap-around
                    if (i == 0 && j == n - 1)
                    {
                        continue;
                    }
                    if (SegmentsCross(pts[i], pts[(i + 1) % n], pts[j], pts[(j + 1) % n]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private double Cross(Point2d o, Point2d a, Point2d b) =>
            (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

        private bool SegmentsCross(Point2d p1, Point2d p2, Point2d p3, Point2d p4)
        {
            double d1 = Cross(p1, p2, p3), d2 = Cross(p1, p2, p4);
            double d3 = Cross(p3, p4, p1), d4 = Cross(p3, p4, p2);
            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        // for debugging
        [CommandMethod("AUTOGALASTATUS")]
        public void Status(Document doc)
        {
            doc.Editor.WriteMessage($"\nAutoGala plugin loaded. PID: {Process.GetCurrentProcess().Id}\n");
        }
    }
}
