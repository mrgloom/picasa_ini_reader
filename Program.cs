using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

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

        public static void GetFaces(string filename,string path)
        {
            //create folder for faces
            string dir = path + "\\face";
            System.IO.Directory.CreateDirectory(dir);

            string ini_path= path+"\\.picasa.ini";
            IniParser parser = new IniParser(ini_path);

            String img_rects = parser.GetSetting(Path.GetFileName(filename), "faces");

            try
            {
                string[] str_rects = GetRectStrings(img_rects);

                for (int i = 0; i<str_rects.Length; ++i)
                {
                    Bitmap img = (Bitmap)Image.FromFile(filename, true);

                    RectangleF rectF = GetRectangle(str_rects[i]);

                    int im_w = img.Width;
                    int im_h = img.Height;

                    rectF.X = rectF.X * im_w;
                    rectF.Y = rectF.Y * im_h;
                    rectF.Width = rectF.Width * im_w;
                    rectF.Height = rectF.Height * im_h;

                    Bitmap bmpCrop = img.Clone(rectF, img.PixelFormat);
                    Bitmap resized = new Bitmap(bmpCrop, new Size(24, 32));
                    //need to remove extension
                    string crop_path = path + "\\face\\" +
                        Path.GetFileNameWithoutExtension(filename)+"_"+i.ToString()+ "_crop.png";
                    resized.Save(crop_path,
                        System.Drawing.Imaging.ImageFormat.Png);

                    //append vector to txt file in root dir
                    string text_path = Directory.GetParent(path).FullName + "\\db.txt";
                    AppendToTxtFile(resized, text_path);
                }
            }
            catch
            {
                Console.WriteLine("error: " + Path.GetFileName(Path.GetDirectoryName(path + "\\"))
                        + " " + Path.GetFileName(filename));
            }
        }

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
        //need to convert to grey?
        public static void AppendToTxtFile(Bitmap img, string path)
        { 
            // Specify a pixel format.
            PixelFormat pxf = PixelFormat.Format24bppRgb;

            // Lock the bitmap's bits.
            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData bmpData =
            img.LockBits(rect, ImageLockMode.ReadWrite,
                         pxf);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            // int numBytes = bmp.Width * bmp.Height * 3; 
            int numBytes = bmpData.Stride * img.Height;
            byte[] rgbValues = new byte[numBytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, numBytes);

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
            //Regex re = new Regex(@"(?<=rect64\()(\w|\d)+");
            Regex re = new Regex(@"(?<=RECT64\()(\w|\d)+");
            string[] matches = re.Matches(str).Cast<Match>().Select(m => m.Value).ToArray();

            return matches;
        }

        static void Main(string[] args)
        {
            string path= @"..\..\..\data\";
            //string path = args[0];
            foreach (string dir in Directory.GetDirectories(path))
            {
                string[] files = Directory.GetFiles(dir, "*.jpg");

                foreach (string filename in files)
                    GetFaces(filename, dir);
            }
        }
    }
}
