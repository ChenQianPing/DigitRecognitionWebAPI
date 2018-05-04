using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace DigitRecognitionWebAPI.Controllers
{
    public class UploadController : ApiController
    {
        public HttpResponseMessage Post()
        {
            var content = Request.Content;
            var uploadDir = HttpContext.Current.Server.MapPath("~/Upload/" + DateTime.Now.ToString("yyyy/MM/dd"));
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);
            var newFileName = "";
            var dir = "/Upload/" + DateTime.Now.ToString("yyyy/MM/dd");
            var dirFileName = "";
            var sp = new MultipartMemoryStreamProvider();
            Task.Run(async () => await Request.Content.ReadAsMultipartAsync(sp)).Wait();
            List<string> list = new List<string>();
            foreach (var item in sp.Contents)
            {
                if (item.Headers.ContentDisposition.FileName != null)
                {
                    var filename = item.Headers.ContentDisposition.FileName.Replace("\"", "");
                    string extension = Path.GetExtension(filename);
                    string guidStr = Guid.NewGuid().ToString("n");
                    newFileName = uploadDir + "\\" + guidStr + extension;
                    dirFileName = dir + "/" + guidStr + extension;
                    var ms = item.ReadAsStreamAsync().Result;
                    using (var br = new BinaryReader(ms))
                    {
                        var data = br.ReadBytes((int)ms.Length);
                        File.WriteAllBytes(newFileName, data);
                    }
                    list.Add(dirFileName);
                }
            }
            var result = new Dictionary<string, object>();
            result.Add("result", list);
            var resp = Request.CreateResponse(HttpStatusCode.OK, result);
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            return resp;
        }
    }
}
