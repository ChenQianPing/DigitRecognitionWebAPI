using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DigitRecognitionWebAPI.Models;

namespace DigitRecognitionWebAPI.Controllers
{
    public class PicController : ApiController
    {

        [DllImport("HWRDLL.dll", EntryPoint = "GetPictureWord", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetPictureWord(string imageFilePath, string outputFilePath);


        [Route("api/PicNumber")]
        [HttpPost]
        public string Post([FromBody]PicNumber entity)
        {
            var picPath = HttpContext.Current.Server.MapPath($@"\Upload\Pic\{DateTime.Now.ToString("yyyy/MM/dd")}");
            if (!Directory.Exists(picPath))
                Directory.CreateDirectory(picPath);

            string inPicPath = $@"{picPath}\{Guid.NewGuid()}.jpg";
            WebClient myWebClient = new WebClient();
            myWebClient.DownloadFile(new Uri(entity.PicUrl), inPicPath);
            myWebClient.Dispose();
            var picSavePath = $@"{picPath}\{Guid.NewGuid()}.bmp";
            using (Bitmap bitmap = new Bitmap(inPicPath))
            {
                // 指定8位格式，即256色
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                Bitmap bitmap2 = new Bitmap(bitmap.Width, bitmap.Height, data.Stride, PixelFormat.Format8bppIndexed, data.Scan0);
                bitmap2.Save(picSavePath, ImageFormat.Bmp);
                bitmap.UnlockBits(data);
            }

            var outSavePath = $@"{picPath}\{Guid.NewGuid()}";
            if (!Directory.Exists(outSavePath))
                Directory.CreateDirectory(outSavePath);

            foreach (var file in Directory.GetFiles(outSavePath, "*.bmp"))
            {
                File.Delete(file);
            }

            GetPictureWord(picSavePath, outSavePath);
            var val = "";
            var dataPath = HttpContext.Current.Server.MapPath("~/bin/Data/template.dat");
            foreach (var img in Directory.GetFiles(outSavePath, "*.bmp"))
            {
                var picNumber = GetNumber(img, dataPath);
                if (picNumber < 0)
                    throw new Exception("识别错误！");
                val += picNumber.ToString();
            }

            Directory.Delete(outSavePath, true);
            File.Delete(inPicPath);
            File.Delete(picSavePath);
            return val;
        }

        [Route("api/UploadPicNumber")]
        [HttpPost]
        public string Upload()
        {
            var picPath = HttpContext.Current.Server.MapPath($@"\Upload\Pic\{DateTime.Now.ToString("yyyy/MM/dd")}");

            if (!Directory.Exists(picPath))
                Directory.CreateDirectory(picPath);
            var sp = new MultipartMemoryStreamProvider();
            Task.Run(async () => await Request.Content.ReadAsMultipartAsync(sp)).Wait();
            foreach (var item in sp.Contents)
            {
                if (item.Headers.ContentDisposition.FileName != null)
                {
                    var filename = item.Headers.ContentDisposition.FileName.Replace("\"", "");
                    string extension = Path.GetExtension(filename);
                    string guidStr = Guid.NewGuid().ToString("n");
                    var inPicPath = picPath + "\\" + guidStr + extension;
                    var ms = item.ReadAsStreamAsync().Result;
                    using (var br = new BinaryReader(ms))
                    {
                        var data = br.ReadBytes((int)ms.Length);
                        File.WriteAllBytes(inPicPath, data);
                    }

                    var picSavePath = $@"{picPath}\{Guid.NewGuid()}.bmp";
                    using (Bitmap bitmap = new Bitmap(inPicPath))
                    {
                        Bitmap bitmap2 = PTransparentAdjust(bitmap);
                        // 指定8位格式，即256色
                        BitmapData data = bitmap2.LockBits(new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                        Bitmap bitmap3 = new Bitmap(bitmap2.Width, bitmap2.Height, data.Stride, PixelFormat.Format8bppIndexed, data.Scan0);
                        bitmap3.Save(picSavePath, ImageFormat.Bmp);
                        bitmap2.UnlockBits(data);
                    }

                    var outSavePath = $@"{picPath}\{Guid.NewGuid()}";
                    if (!Directory.Exists(outSavePath))
                        Directory.CreateDirectory(outSavePath);
                    foreach (var file in Directory.GetFiles(outSavePath, "*.bmp"))
                    {
                        File.Delete(file);
                    }

                    GetPictureWord(picSavePath, outSavePath);
                    var val = "";
                    var dataPath = HttpContext.Current.Server.MapPath("~/bin/Data/template.dat");
                    foreach (var img in Directory.GetFiles(outSavePath, "*.bmp"))
                    {
                        var picNumber = GetNumber(img, dataPath);
                        if (picNumber < 0)
                            throw new Exception("识别错误！");
                        val += picNumber.ToString();
                    }

                    Directory.Delete(outSavePath, true);
                    File.Delete(inPicPath);
                    File.Delete(picSavePath);
                    return val;
                }
            }
            return "";
        }

        private int GetNumber(string imgPath, string dataPath)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            string mExename = HttpContext.Current.Server.MapPath("~/bin/DigitRecognition.exe");

            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c" + mExename + " " + imgPath + " " + dataPath;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;
            process.Start();
            var strResult = process.StandardOutput.ReadLine();
            if (strResult == null) throw new ArgumentNullException(nameof(strResult));
            process.WaitForExit(3000);
            return int.Parse(strResult);
        }

        [Route("api/test")]
        [HttpGet]
        public string Get()
        {
            return "123";
        }

        /// <summary>
        /// 图片换底色
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public Bitmap PTransparentAdjust(Bitmap src)
        {
            try
            {
                int w = src.Width;
                int h = src.Height;
                Bitmap dstBitmap = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Imaging.BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Imaging.BitmapData dstData = dstBitmap.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                unsafe
                {
                    byte* pIn = (byte*)srcData.Scan0.ToPointer();
                    byte* pOut = (byte*)dstData.Scan0.ToPointer();
                    byte* p;
                    // int stride = srcData.Stride;
                    int r, g, b, a;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            p = pIn;
                            b = pIn[0];
                            g = pIn[1];
                            r = pIn[2];
                            a = pIn[3];
                            if (r > 150)
                            {

                                pOut[1] = (byte)255;
                                pOut[2] = (byte)255;
                                pOut[3] = (byte)255;
                                pOut[0] = (byte)255;

                            }
                            else
                            {
                                pOut[1] = (byte)g;
                                pOut[2] = (byte)r;
                                pOut[3] = (byte)a;
                                pOut[0] = (byte)b;
                            }

                            pIn += 4;
                            pOut += 4;
                        }
                        pIn += srcData.Stride - w * 4;
                        pOut += srcData.Stride - w * 4;
                    }
                    src.UnlockBits(srcData);
                    dstBitmap.UnlockBits(dstData);
                    return dstBitmap;
                }
            }
            catch
            {
                return null;
            }

        }
    }
}
