using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AutoGala.Ipc;
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

        private object? Dispatch(PluginRequest request, Document doc)
        {
            switch (request.Action)
            {
                case "GetActiveDocumentName":
                    return doc.Name;

                case "AddLine":
                    using (var tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        var payload = JsonSerializer.Deserialize<AddLinePayload>(request.PayloadJson)!;
                        var btr = (BlockTableRecord)tr.GetObject(
                            doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                        var line = new Line(payload.Start, payload.End);
                        btr.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                        tr.Commit();

                        return line.ObjectId.Handle.ToString();
                    }
                default:
                    throw new System.Exception($"Unknown action: {request.Action}");
            }
        }

        public class AddLinePayload
        {
            public Autodesk.AutoCAD.Geometry.Point3d Start { get; set; }
            public Autodesk.AutoCAD.Geometry.Point3d End { get; set; }
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
