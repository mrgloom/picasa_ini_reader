using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

//using ImageDownloader;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;// Requires reference to WebDriver.Support.dll
using System.Net;

using IniParser;

//http://unrar.me/2010/11/export-dannyh-ob-otmechennyh-lyudyah-iz-picasa/
//http://stackoverflow.com/questions/2201393/how-to-reverse-a-rectanglef-to-a-picasa-face-hash
//https://gist.github.com/fbuchinger/1073823


//надо добавить возможность остановки и продолжения
//вывод отчётов в консоль + времени выполнения т.к. производительность может деградировать
namespace picasa_ini_reader
{
    class Program
    {
        //public static void test_hdf5()
        //{
        //    H5.Open();
        //    var h5 = H5F.open("example.h5", H5F.OpenMode.ACC_RDONLY);
        //    var dataset = H5D.open(h5, "/Timings/aaPCBTimes");
        //    var space = H5D.getSpace(dataset);
        //    var size = H5S.getSimpleExtentDims(space);
        //}

        //надо сделать на базе нового .ini reader 

        //public static void GetFace(string filename,string path, bool resize)
        //{
        //    try
        //    {
        //        string ini_path = path + "\\.picasa.ini";
        //        IniParser parser = new IniParser(ini_path);//если нету ини файла в папке то падает тут

        //        String img_rects = parser.GetSetting(Path.GetFileName(filename), "faces");
        //        if(img_rects!=null)
        //        try// тут еще похоже не все типы файлов читает
        //        {
        //            string[] str_rects = GetRectStrings(img_rects);

        //            for (int i = 0; i < str_rects.Length; ++i)
        //            {
        //                Bitmap img = (Bitmap)Image.FromFile(filename, true);

        //                RectangleF rectF = GetRectangle(str_rects[i]);

        //                int im_w = img.Width;
        //                int im_h = img.Height;

        //                rectF.X = rectF.X * im_w;
        //                rectF.Y = rectF.Y * im_h;
        //                rectF.Width = rectF.Width * im_w;
        //                rectF.Height = rectF.Height * im_h;

        //                Bitmap bmpCrop = img.Clone(rectF, img.PixelFormat);

        //                string text_path = Directory.GetParent(path).FullName + "\\db.txt";
        //                string crop_path = path + "\\face\\" +
        //                    Path.GetFileNameWithoutExtension(filename) + "_" + i.ToString() + "_crop.png";

        //                //непонятно производиться ли копирование при присвоении?
        //                if (resize)
        //                {
        //                    Bitmap resized = new Bitmap(bmpCrop, new Size(24, 32));//вынести в параметры
        //                    resized.Save(crop_path,
        //                        System.Drawing.Imaging.ImageFormat.Png);

        //                    Bitmap gr = ConvertGray(resized);

        //                    AppendToTxtFile(gr, text_path);
        //                }
        //                else
        //                {
        //                    bmpCrop.Save(crop_path,
        //                        System.Drawing.Imaging.ImageFormat.Png);

        //                    Bitmap gr = ConvertGray(bmpCrop);

        //                    AppendToTxtFile(gr, text_path);
        //                }
        //            }
        //        }
        //        catch
        //        {
        //            Console.WriteLine("error: " + Path.GetFileName(Path.GetDirectoryName(path + "\\"))
        //                    + " " + Path.GetFileName(filename));
        //        }

        //    }
        //    catch
        //    {
        //        //Console.WriteLine("error: " + Path.GetFileName(Path.GetDirectoryName(path + "\\"))+
        //        //    " no .ini file?");
        //    }
        //}

        public static RectangleF GetRectangle(string hashstr)
        {
            UInt64 hash = UInt64.Parse(hashstr, System.Globalization.NumberStyles.HexNumber);
            byte[] bytes = BitConverter.GetBytes(hash);

            UInt16 l16 = BitConverter.ToUInt16(bytes, 6);
            UInt16 t16 = BitConverter.ToUInt16(bytes, 4);
            UInt16 r16 = BitConverter.ToUInt16(bytes, 2);
            UInt16 b16 = BitConverter.ToUInt16(bytes, 0);

            float left = l16 / 65535.0F;
            float top = t16 / 65535.0F;
            float right = r16 / 65535.0F;
            float bottom = b16 / 65535.0F;

            return new RectangleF(left, top, right - left, bottom - top);
        }

        public static string ByteArrayToDecimalString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder();
            string format = "{0}";
            foreach (byte b in ba)
            {
                hex.AppendFormat(format, b);
                format = " {0}";
            }
            return hex.ToString();
        }
        public static Bitmap ConvertGray(Bitmap Image)
        {
            Bitmap grey = new Bitmap(Image.Width, Image.Height);
            for (int y = 0; y < grey.Height; y++)
            {
                for (int x = 0; x < grey.Width; x++)
                {
                    Color c = Image.GetPixel(x, y);
                    int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    grey.SetPixel(x, y, Color.FromArgb(luma, luma, luma));
                }
            }
            return grey;
        }
        public static void AppendToTxtFile(Bitmap img, string path)
        {
            // Lock the bitmap's bits.
            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData bmpData =
            img.LockBits(rect, ImageLockMode.ReadWrite,
                         img.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int numBytes = Math.Abs(bmpData.Stride) * img.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

            // Unlock the bits.
            img.UnlockBits(bmpData);

            string str = ByteArrayToDecimalString(rgbValues);

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(path,true))
            {
                file.WriteLine(str);
            }
        }

        public static string[] GetRectStrings(string str)
        {
            //example string
            //String text = "faces=rect64(3f845bcb59418507),8e62398ebda8c1a5;rect64(9eb15e89b6b584c1),d10a8325c557b085";

            //case sensitive?
            Regex re = new Regex(@"(?<=rect64\()(\w|\d)+");
            //Regex re = new Regex(@"(?<=RECT64\()(\w|\d)+");
            string[] matches = re.Matches(str).Cast<Match>().Select(m => m.Value).ToArray();

            return matches;
        }

        static public void test_123()
        {
            //to download files
            using (WebClient Client = new WebClient())
            {
                Client.DownloadFile("http://www.abc.com/file/song/a.mpeg", "a.mpeg");
            }

            //надо использовать x_path или как то так
            //или хотя бы получить обычные ссылки

            IWebDriver driver = new FirefoxDriver();

            driver.Navigate().GoToUrl("http://www.google.com/");

            // Find the text input element by its name
            IWebElement query = driver.FindElement(By.Name("q"));

            // Enter something to search for
            query.SendKeys("abc");

            // Now submit the form. WebDriver will find the form for us from the element
            query.Submit();

            // Google's search is rendered dynamically with JavaScript.
            // Wait for the page to load, timeout after 10 seconds
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until((d) => { return d.Title.ToLower().StartsWith("abc"); });

            // Should see: "Cheese - Google Search"
            System.Console.WriteLine("Page title is: " + driver.Title);

            //Close the browser
            driver.Quit();
        }

        static void Main(string[] args)
        {
            //по поводу тредов, скорее всего стоит сделать пул некоторого размера
            //в который постоянно загружать, потом из него читать вырезать и ресайзить
            //и отдельный тред чтобы писать
            //т.е. читать только в 1 тред и класть задачи в очередь
            //сохранять тоже только в 1 тред, но вопрос когда? как только готовы?

            //лучще сделать in/out параметры из командной строки для входной и выходной папки
            //еще непонятно какая была проставлена точность для поиска
            //поддерживает ли несколько ректов в парсинге и детекте
            //будут ли проблемы с большим кол-вом папок + с базой пикасы
            //поможет ли многопоточность?


            //проблема в том, что надо как то настроить гит на общую папку?
            //ImageDownloaderClass.
            test_123();

            string path= @"F:\db";
            //string path = @"..\..\..\data\";
            //string path = args[0];

            int counter = 0;
            foreach (string dir in Directory.GetDirectories(path))
            {
                Console.WriteLine("processing: " + dir);

                //create folder for faces
                string dir_path = dir + "\\face";
                System.IO.Directory.CreateDirectory(dir_path);

                try// упасть может не только на ини файле? что если прерывание на цикле?
                {
                    string ini_path = dir + "\\.picasa.ini";
                    if (File.Exists(ini_path))
                    {
                        FileIniDataParser parser = new FileIniDataParser();
                        IniData data = parser.LoadFile(ini_path);
                        foreach (SectionData section in data.Sections)
                        {
                            if (section.SectionName.Contains(".jpg"))
                            {
                                //Console.WriteLine("[" + section.SectionName + "]");
                                //Console.WriteLine(data[section.SectionName]["faces"]);

                                string rects = data[section.SectionName]["faces"];

                                string[] str_rects = GetRectStrings(rects);

                                for (int i = 0; i < str_rects.Length; ++i)
                                {
                                    Bitmap img = (Bitmap)Image.FromFile(dir + "\\" + section.SectionName, true);

                                    RectangleF rectF = GetRectangle(str_rects[i]);

                                    int im_w = img.Width;
                                    int im_h = img.Height;

                                    rectF.X = rectF.X * im_w;
                                    rectF.Y = rectF.Y * im_h;
                                    rectF.Width = rectF.Width * im_w;
                                    rectF.Height = rectF.Height * im_h;

                                    Bitmap bmpCrop = img.Clone(rectF, img.PixelFormat);

                                    string text_path = Directory.GetParent(path).FullName + "\\db.txt";
                                    string crop_path = dir + "\\face\\" +
                                        Path.GetFileNameWithoutExtension(dir + "\\" + section.SectionName) + "_" + i.ToString() + "_crop.png";

                                    bool resize = true;
                                    if (resize)
                                    {
                                        Bitmap resized = new Bitmap(bmpCrop, new Size(24, 32));//вынести в параметры
                                        resized.Save(crop_path,
                                            System.Drawing.Imaging.ImageFormat.Png);

                                        Bitmap gr = ConvertGray(resized);

                                        AppendToTxtFile(gr, text_path);
                                    }
                                    else
                                    {
                                        bmpCrop.Save(crop_path,
                                            System.Drawing.Imaging.ImageFormat.Png);

                                        Bitmap gr = ConvertGray(bmpCrop);

                                        AppendToTxtFile(gr, text_path);
                                    }

                                    counter++;
                                }
                            }
                        }
                    }

                }
                catch
                {
                    Console.WriteLine("problem in: " + dir);
                }

                Console.WriteLine("rects: " + counter.ToString());
            }

            Console.WriteLine("all done");
            Console.ReadLine();
        }
    }
}


//OLD
////string path = @"F:\db\";
//string path= @"..\..\..\data\";
////string path = args[0];
////тут получается обработка непопорядку - плохо если делать обработку с продолжением
//foreach (string dir in Directory.GetDirectories(path))
//{
//    Console.WriteLine("processing: " + dir);

//    //create folder for faces
//    string dir_path = dir + "\\face";
//    System.IO.Directory.CreateDirectory(dir_path);

//    //правильней пройти по секциям ини файла, а не по изображениям
//    //по идее должны отталкиваться от ини файла, а не от изображений в папке?
//    //тут для каждого изображения смотрим его вхождение в ини файл
//    //он может не входить и ини файл может не существовать
//    string[] files = Directory.GetFiles(dir, "*.jpg");
//    //foreach (string filename in files)
//        //GetFace(filename, dir, true);
//}
