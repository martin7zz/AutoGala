using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AutoGala.Ipc;
using AutoGala.Plugin.models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        public void Terminate() => _pipeServer?.Stop();

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

            Status();

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Application.DocumentManager.ExecuteInApplicationContext(_ =>
            {
                try
                {
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
                    case "GetSections":
                        List<LineData> lineCoordinates = new List<LineData>();

                        using (Transaction acTrans = acDatabase.TransactionManager.StartTransaction())
                        {
                            PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                            if (acSSPrompt.Status == PromptStatus.OK)
                            {
                                SelectionSet acSSet = acSSPrompt.Value;

                                foreach (SelectedObject acSSObj in acSSet)
                                {
                                    if (acSSObj == null)
                                        continue;

                                    Line? acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Line;

                                    if (acEnt == null)
                                        continue;
                                    
                                    lineCoordinates.Add(new LineData
                                    {
                                        Start = new PointData
                                        {
                                            X = acEnt.StartPoint.X,
                                            Y = acEnt.StartPoint.Y
                                        },
                                        End = new PointData
                                        {
                                            X = acEnt.EndPoint.X,
                                            Y = acEnt.EndPoint.Y
                                        }
                                    });
                                }
                            }
                            acTrans.Commit();
                        }

                        return lineCoordinates;
                    case "GetPoints":
                        List<PointData> pointCoordinates = new List<PointData>();

                        using (Transaction acTrans = acDatabase.TransactionManager.StartTransaction())
                        {
                            PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                            if (acSSPrompt.Status == PromptStatus.OK)
                            {
                                SelectionSet acSSet = acSSPrompt.Value;

                                foreach (SelectedObject acSSObj in acSSet)
                                {
                                    if (acSSObj == null)
                                        continue;

                                    DBPoint? acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as DBPoint;

                                    if (acEnt == null)
                                        continue;

                                    Point3d point = acEnt.Position;

                                    pointCoordinates.Add(new PointData
                                    {
                                        X = point.X,
                                        Y = point.Y
                                    });
                                }
                            }
                            acTrans.Commit();
                        }

                        return pointCoordinates;
                    case "GetCircles":
                        List<CircleData> circleCoordinates = new List<CircleData>();

                        using (Transaction acTrans = acDatabase.TransactionManager.StartTransaction())
                        {
                            PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                            if (acSSPrompt.Status == PromptStatus.OK)
                            {
                                SelectionSet acSSet = acSSPrompt.Value;

                                foreach (SelectedObject acSSObj in acSSet)
                                {
                                    if (acSSObj == null)
                                        continue;

                                    Circle? acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Circle;

                                    if (acEnt == null)
                                        continue;

                                    circleCoordinates.Add(new CircleData
                                    {
                                        PositionData = new PointData
                                        {
                                            X = acEnt.Center.X,
                                            Y = acEnt.Center.Y
                                        },
                                        Area = acEnt.Area
                                    });

                                }
                            }
                            acTrans.Commit();
                        }
                        return circleCoordinates;
                    case "GetAll":
                        List<LineData> lines = new List<LineData>();
                        List<PointData> points = new List<PointData>();
                        List<CircleData> circles = new List<CircleData>();

                        using (Transaction acTrans = acDatabase.TransactionManager.StartTransaction())
                        {
                            PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                            if (acSSPrompt.Status == PromptStatus.OK)
                            {
                                SelectionSet acSSet = acSSPrompt.Value;

                                foreach (SelectedObject acSSObj in acSSet)
                                {
                                    if (acSSObj == null)
                                        continue;

                                    Entity? acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;

                                    if (acEnt == null)
                                        continue;

                                    if (acEnt is Line lineEnt)
                                    {
                                        lines.Add(new LineData
                                        {
                                            Start = new PointData
                                            {
                                                X = lineEnt.StartPoint.X,
                                                Y = lineEnt.StartPoint.Y
                                            },
                                            End = new PointData
                                            {
                                                X = lineEnt.EndPoint.X,
                                                Y = lineEnt.EndPoint.Y
                                            }
                                        });
                                    }
                                    else if (acEnt is DBPoint pointEnt)
                                    {
                                        Point3d point = pointEnt.Position;

                                        points.Add(new PointData
                                        {
                                            X = point.X,
                                            Y = point.Y
                                        });
                                    }
                                    else if (acEnt is Circle circleEnt)
                                    {
                                        circles.Add(new CircleData
                                        {
                                            PositionData = new PointData
                                            {
                                                X = circleEnt.Center.X,
                                                Y = circleEnt.Center.Y
                                            },
                                            Area = circleEnt.Area
                                        });
                                    }
                                }
                            }
                            acTrans.Commit();
                        }

                        var result = new
                        {
                            Lines = lines,
                            Points = points,
                            Circles = circles
                        };

                        return result;
                    default:
                        throw new System.Exception($"Unknown action: {request.Action}");
                }
            }
        }

        // for debugging
        [CommandMethod("AUTOGALASTATUS")]
        public void Status()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.WriteMessage($"\nAutoGala plugin loaded. PID: {Process.GetCurrentProcess().Id}\n");
        }
    }
}
