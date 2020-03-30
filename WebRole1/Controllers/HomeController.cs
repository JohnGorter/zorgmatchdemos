using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebRole1.Controllers
{
    public class Registration : TableEntity
    {
        public String FullName { get; set; }
        public String Image { get; set; }
        public String thumb { get; set; }
    }

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage;AccountKey=5d9Ev8xRs28UcCn6Sej3d4K4x3Xw/TYwAudQ1W0MfcYGjcosJFuFlK9fnySvo2N7iEXy6HDxIg+LMP1alzGBOg==;EndpointSuffix=core.windows.net");
            var ctc = csa.CreateCloudTableClient();
            var ctr = ctc.GetTableReference("persons");

            var query = new TableQuery<Registration>();
            var personlist = ctr.ExecuteQuery(query).ToList();

            ViewBag.persons = personlist;

            return View();
        }

        [HttpPost]
        public ActionResult Index(String firstname, String lastname, HttpPostedFileBase file)
        {
            Console.WriteLine(file.FileName);

            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage;AccountKey=5d9Ev8xRs28UcCn6Sej3d4K4x3Xw/TYwAudQ1W0MfcYGjcosJFuFlK9fnySvo2N7iEXy6HDxIg+LMP1alzGBOg==;EndpointSuffix=core.windows.net");
            var cqc = csa.CreateCloudQueueClient();
            var cbc = csa.CreateCloudBlobClient(); 

            var cqr = cqc.GetQueueReference("regist");
            var cbcontainer = cbc.GetContainerReference("testcontainer");

            cqr.CreateIfNotExists();
            cbcontainer.CreateIfNotExists(); 

            // add photo to blob  (block en page blobs);
            var photo = cbcontainer.GetBlockBlobReference($"{firstname + "_" + lastname}.png");
            photo.UploadFromStream(file.InputStream);

            cqr.AddMessage(new Microsoft.WindowsAzure.Storage.Queue.CloudQueueMessage($"{firstname + "_" + lastname}"));

            return RedirectToAction("index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}