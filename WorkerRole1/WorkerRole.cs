using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class Registration : TableEntity {
        public String FullName { get; set; }
        public String Image { get; set; }
        public String thumb { get; set; }
    }

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage;AccountKey=5d9Ev8xRs28UcCn6Sej3d4K4x3Xw/TYwAudQ1W0MfcYGjcosJFuFlK9fnySvo2N7iEXy6HDxIg+LMP1alzGBOg==;EndpointSuffix=core.windows.net");
            var cqc = csa.CreateCloudQueueClient();
            var cqr = cqc.GetQueueReference("regist");
            var cbc = csa.CreateCloudBlobClient();
            var cbr = cbc.GetContainerReference("testcontainer");
            var ctc = csa.CreateCloudTableClient();
            var ctr = ctc.GetTableReference("persons");

            cqr.CreateIfNotExists();
            cbr.CreateIfNotExists();
            ctr.CreateIfNotExists();

            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
               

                var message = cqr.GetMessage();
                if (message != null) {
                    var registration = message.AsString;
                    cqr.DeleteMessage(message);

                    var refPhoto = cbr.GetBlockBlobReference(registration + ".png");
                    var refPhotoThumb = cbr.GetBlockBlobReference(registration + "_thumb.png");


                    Image image = Image.FromStream(refPhoto.OpenRead());
                    Image thumb = image.GetThumbnailImage(60, 60, null, IntPtr.Zero);

                    MemoryStream stream = new MemoryStream();
                    thumb.Save(stream, ImageFormat.Png);
                    stream.Position = 0;

                    refPhotoThumb.UploadFromStream(stream);


                    // we are done, save this to table storage
                    Registration r = new Registration()
                    {
                        PartitionKey = registration[0].ToString(),
                        RowKey = registration,
                        FullName = registration.Replace("_", " "),
                        Image = registration + ".png",
                        thumb = registration + "_thumb.png",
                    };
                    TableOperation to = TableOperation.Insert(r);
                    ctr.Execute(to);

                }

                await Task.Delay(1000);
            }
        }
    }
}
